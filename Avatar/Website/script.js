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

/* ——— INIT ——— */
window.addEventListener('DOMContentLoaded', () => {
  waitForUnity();
  unityInstance?.SendMessage('model', 'StartIdle');
  document.getElementById('sendBtn').addEventListener('click', sendMsg);
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
      sendMsg(); 
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
  if (e.key === 'Enter') sendMsg();
});

document.getElementById('clickOverlay').addEventListener('click', () => {
  const controls = document.querySelector('.userControls');
  TextAreaShown = !TextAreaShown;
  controls.style.display = TextAreaShown ? 'flex' : 'none';
});

/* ——— SEND MESSAGE ——— */
async function sendMsg() {
  const userInputEl = document.getElementById('userInput');
  const userInput = userInputEl.value.trim();
  if (!userInput) return;
  console.log('💬 Sending message to GPT...');
  document.getElementById('sendBtn').disabled = true;
  document.getElementById('sendBtn').textContent = 'Thinking…';
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
    animateUnityBubbleText(replyText);
    restoreSendBtn();
    return;
  }

  const ttsRes = await fetch('http://localhost:3000/api/openai/tts', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ input: replyText, voice: 'ash' })
  });

  if (!ttsRes.ok) {
    console.error('TTS error:', await ttsRes.text());
    animateUnityBubbleText(replyText);
    restoreSendBtn();
    return;
  }

  const audioBlob = await ttsRes.blob();
  const audioURL = URL.createObjectURL(audioBlob);
  audioPlayer.src = audioURL;

  const duration = await playAndGetDuration(audioPlayer);
  const msPerChar = (duration * 1000) / replyText.length;
  animateUnityBubbleText(replyText, msPerChar);
  restoreSendBtn();
}

function restoreSendBtn() {
  const btn = document.getElementById('sendBtn');
  btn.disabled = false;
  btn.textContent = 'Send';
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

/* ——— ANIMATED UNITY BUBBLE ——— */
function animateUnityBubbleText(text, msPerChar = 25) {
  const container = document.getElementById('bubbleContainer');
  container.innerHTML = '';

  const bubble = document.createElement('div');
  bubble.className = 'speech-bubble';

  const paragraph = document.createElement('p');
  bubble.appendChild(paragraph);
  container.appendChild(bubble);

  const html = marked.parse(text);
  let i = 0;

  const endAnim = () => {
    clearInterval(timer);
    paragraph.innerHTML = html;
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
    paragraph.innerHTML = html.slice(0, i++);
    if (i > html.length) endAnim();
  }, msPerChar);
}
