/* main.js — chat + TTS + Unity */
let unityInstance = null;
let audioPlayer = document.getElementById('audioPlayer');
let ignoreTTS = false;
let conversationHistory = [];
let TextAreaShown = false;
let isRecording = false;
let recorder = null;
let audioChunks = [];
let micStream = null;
const streamResponse = true;

/* ——— INIT ——— */
window.addEventListener('DOMContentLoaded', () => {
  waitForUnity();
  unityInstance?.SendMessage('model', 'StartIdle');
  document.getElementById('sendBtn').addEventListener('click', streamResponse ? sendMsgStreamed : sendMsg);
  document.getElementById('finishBtn').addEventListener('click', restoreSendBtn);
});

const micBtn = document.getElementById('micBtn');
if (micBtn) {
  micBtn.addEventListener('click', async function handleMicClick(event) {
    event.preventDefault();
    event.stopPropagation();

    if (!isRecording) {
      try {
        micStream = await navigator.mediaDevices.getUserMedia({ audio: true });
        recorder = new MediaRecorder(micStream);
        audioChunks = [];

        recorder.ondataavailable = (evt) => {
          audioChunks.push(evt.data);
        };

        recorder.onstop = async () => {
          try {
            isRecording = false;
            micBtn.textContent = '🎤';
            micStream.getTracks().forEach(track => track.stop());

            const audioBlob = new Blob(audioChunks, { type: 'audio/webm' });
            console.log('[DEBUG] Blob size:', audioBlob.size);

            if (audioBlob.size === 0) {
              console.warn('❗️ Recording blob is empty, aborting.');
              return;
            }

            const formData = new FormData();
            formData.append('audio', audioBlob, 'recording.webm');

            console.log('🎙️ Sending audio to Whisper…');
            const res = await fetch('http://localhost:3000/api/openai/stt', {
              method: 'POST',
              body: formData
            });

            console.log('[DEBUG] STT response status:', res.status);

            const text = await res.text();
            console.log('[DEBUG] STT raw response text:', text);

            if (!res.ok) {
              console.error('STT failed:', text);
              return;
            }

            const data = JSON.parse(text);
            const transcript = data.text?.trim();
            console.log('[STT] Transcript:', transcript);

            if (transcript) {
              // either use sendMsgDirect(transcript);
              document.getElementById('userInput').value = transcript;
              streamResponse ? sendMsgStreamed() : sendMsg()
            }
          } catch (err) {
            console.error('❌ STT fetch crashed:', err);
          }
        };


        recorder.start();
        isRecording = true;
        micBtn.textContent = '⏹️';
      } catch (err) {
        console.error('Mic access error:', err);
        alert('Microphone permission denied.');
      }
    } else {
      if (recorder && recorder.state === 'recording') {
        recorder.stop(); // trigger onstop
      }
    }
  });
}

window.addEventListener('keydown', e => {
  if (e.key === 'Enter') {
    // const finishButtonIsShownInsteadOfSend = getComputedStyle(document.getElementById('finishBtn')).display === 'inline-block';
    const finishButton = document.getElementById('finishBtn');
    const finishButtonIsShownInsteadOfSend = !!(finishButton.offsetWidth || finishButton.offsetHeight || finishButton.getClientRects().length);
    console.log('Enter pressed, finishButtonIsShownInsteadOfSend:', finishButtonIsShownInsteadOfSend);
    if (finishButtonIsShownInsteadOfSend) restoreSendBtn();
    else streamResponse ? sendMsgStreamed() : sendMsg();
  }
});

document.getElementById('clickOverlay').addEventListener('click', () => {
  const controls = document.querySelector('.userControls');
  const wrapper = document.querySelector('.unityWrapper');
  TextAreaShown = !TextAreaShown;
  controls.style.display = TextAreaShown ? 'flex' : 'none';
  wrapper.style.width = TextAreaShown ? "700px" : "256px";
});

async function sendMsgStreamed() {
  const userInputEl = document.getElementById('userInput');
  const userInput = userInputEl.value.trim();
  if (!userInput) return;

  document.getElementById('sendBtn').disabled = true;
  document.getElementById('sendBtn').textContent = 'Thinking…';
  document.getElementById('userInput').style.display = 'none';
  unityInstance?.SendMessage('model', 'StartThinking');
  userInputEl.value = '';

  conversationHistory.push({ role: 'user', content: userInput });

  const chatRes = await fetch('http://localhost:3000/api/openai/chat', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({
      model: 'gpt-4o',
      messages: conversationHistory,
      stream: streamResponse
    })
  });

  unityInstance?.SendMessage('model', 'StartTalking');

  const reader = chatRes.body.getReader();
  const decoder = new TextDecoder("utf-8");
  createBubbleText();
  let fullResponse = '';
  let buffer = "";
  let doneFlag = false;

  while (true) {
    const { value, done } = await reader.read();
    if (done || doneFlag) break;

    const chunk = decoder.decode(value, { stream: true });
    buffer += chunk;

    const lines = buffer.split("\n");
    buffer = lines.pop(); // keep the last incomplete line

    for (const line of lines) {
      if (!line.trim().startsWith("data:")) continue;

      const jsonStr = line.replace("data: ", "").trim();
      if (jsonStr === "[DONE]") return;

      const data = JSON.parse(jsonStr);
      const delta = data.choices[0].delta;

      if (delta?.content) {
        appendToBubbleText(delta.content);
        fullResponse += delta.content;
      }

      if (data.choices[0].finish_reason === "stop") {
        doneFlag = true;
        break;
      }
    }
  }

  unityInstance?.SendMessage('model', 'StartIdle');
  playTTSAsync(fullResponse);
  showFinishButton();
}

async function playTTSAsync(text) {
  console.log("Starting TTS");

  const ttsRes = await fetch('http://localhost:3000/api/openai/tts', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ input: text, voice: 'ash', stream: streamResponse }) // male
  });

  console.log("Status:", ttsRes.status);
  console.log("Content-Type:", ttsRes.headers.get("content-type"));

  if (!ttsRes.ok) {
    console.error('TTS stream error: Bad status', ttsRes.status);
    return;
  }
  if (!ttsRes.body) {
    console.error('TTS stream error: No response body');
    return;
  }

  unityInstance?.SendMessage('model', 'StartTalking');
  const reader = ttsRes.body.getReader();
  const chunks = [];

  // Read chunks as they arrive
  while (true) {
    const { done, value } = await reader.read();
    if (done) break;
    chunks.push(value);
  }

  // Combine all chunks into a single Blob
  const blob = new Blob(chunks, { type: 'audio/mpeg' });
  const url = URL.createObjectURL(blob);

  // Create and play audio
  const audio = new Audio(url);
  const duration = await playAndGetDuration(audio);
  await new Promise(resolve => setTimeout(resolve, duration * 1000)); // wait duration
  unityInstance?.SendMessage('model', 'StartIdle');

}

/* ——— SEND MESSAGE ——— */
async function sendMsg() {
  const userInputEl = document.getElementById('userInput');
  const userInput = userInputEl.value.trim();
  if (!userInput) return;
  console.log('💬 Sending message to GPT...');

  // UI state
  document.getElementById('sendBtn').disabled = true;
  document.getElementById('sendBtn').textContent = 'Thinking…';
  document.getElementById('userInput').style.display = 'none';
  unityInstance?.SendMessage('model', 'StartThinking');
  userInputEl.value = '';

  conversationHistory.push({ role: 'user', content: userInput });

  const chatRes = await fetch('http://localhost:3000/api/openai/chat', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({
      model: 'gpt-4o',
      messages: conversationHistory
    })
  });

  if (!chatRes.ok) {
    console.error('Chat error:', await chatRes.text());
    restoreSendBtn();
    return;
  }

  const chatData = await chatRes.json();
  const replyText = chatData.choices?.[0]?.message?.content || '(empty reply)';
  conversationHistory.push({ role: 'assistant', content: replyText });

  if (ignoreTTS) {
    animateBubbleText(replyText);
    showFinishButton();
    return;
  }

  const ttsRes = await fetch('http://localhost:3000/api/openai/tts', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ input: replyText, voice: 'ash' }) // male
  });

  if (!ttsRes.ok) {
    console.error('TTS error:', await ttsRes.text());
    animateBubbleText(replyText);
    restoreSendBtn();
    return;
  }

  const audioBlob = await ttsRes.blob();
  const audioURL = URL.createObjectURL(audioBlob);
  audioPlayer.src = audioURL;

  const duration = await playAndGetDuration(audioPlayer);
  const msPerChar = ((duration / 2) * 1000) / replyText.length;
  animateBubbleText(replyText, msPerChar);
  showFinishButton();
}

function restoreSendBtn() {
  document.getElementById('finishBtn').style.display = 'none';
  document.getElementById('userInput').style.display = 'inline-block';
  const btn = document.getElementById('sendBtn');
  btn.disabled = false;
  btn.textContent = 'Send';
  btn.style.display = 'inline-block';
  unityInstance?.SendMessage('model', 'StartIdle');
  destroyBubbleText();
  // audioPlayer.pause();
}

function showFinishButton() {
  document.getElementById('sendBtn').style.display = 'none';
  document.getElementById('finishBtn').style.display = 'inline-block';
  document.getElementById('userInput').style.display = 'none';
}

/* ——— AUDIO UTIL ——— */
function playAndGetDuration(player) {
  return new Promise((resolve, reject) => {
    player.addEventListener('loadedmetadata', () => {
      player.play().catch(reject);
    }, { once: true });

    player.addEventListener('playing', () => resolve(player.duration), { once: true });
  });
}

/* ——— UNITY WAIT ——— */
function waitForUnity() {
  const frame = document.getElementById('UnityFrame');
  if (frame?.contentWindow?.unityInstance) {
    unityInstance = frame.contentWindow.unityInstance;
  } else {
    setTimeout(waitForUnity, 500);
  }
}

/* ——— HANDLE UNITY → JS ——— */
function HandleUnityMessage(txt) {
  console.log('[Unity]', txt);
  if (!ignoreTTS) sendMsgWithPreset(txt); // optional
}

function createBubbleText() {
  const container = document.getElementById('bubbleContainer');
  container.innerHTML = '';

  const bubble = document.createElement('div');
  bubble.className = 'speech-bubble';

  const paragraph = document.createElement('p');
  paragraph.id = 'bubble-text';
  bubble.appendChild(paragraph);
  container.appendChild(bubble);
}

function appendToBubbleText(text) {
  const paragraph = document.getElementById('bubble-text');
  let allText = paragraph.innerHTML;
  allText += text;
  paragraph.innerHTML = marked.parse(allText);
}

function destroyBubbleText() {
  const container = document.getElementById('bubbleContainer');
  container.innerHTML = '';
}

/* ——— ANIMATED UNITY BUBBLE ——— */
function animateBubbleText(text, msPerChar = 25) {
  createBubbleText();
  appendToBubbleText(marked.parse(text));
  let i = 0;
  const paragraph = document.getElementById('bubble-text');

  const endAnim = () => {
    clearInterval(timer);
    paragraph.innerHTML = marked.parse(text);
    unityInstance?.SendMessage('model', 'StartIdle');
    document.getElementById('stopBtn').style.display = 'none';
    document.getElementById('sendBtn').style.display = 'inline-block';
    audioPlayer.pause();
  };

  document.getElementById('sendBtn').style.display = 'none';
  document.getElementById('stopBtn').onclick = endAnim;
  document.getElementById('stopBtn').style.display = 'inline-block';
  unityInstance?.SendMessage('model', 'StartTalking');

  const timer = setInterval(() => {
    paragraph.innerHTML = marked.parse(text).slice(0, i++);
    if (i > marked.parse(text).length) endAnim();
  }, msPerChar);
}