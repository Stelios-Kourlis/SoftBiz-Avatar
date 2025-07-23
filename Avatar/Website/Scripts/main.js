
import { ButtonController, BubbleTextController } from './UiController.js';
import { StreamedResponseHandler } from './ResponseHandlers/StreamedResponseHandler.js';
import { NonStreamedResponseHandler } from './ResponseHandlers/NonStreamedResponseHandler.js';
import * as UnityAnimationController from './UnityAnimationController.js';

/* main.js — chat + TTS + Unity */
const audioPlayer = document.getElementById('audioPlayer');
const micBtn = document.getElementById('micBtn');
let conversationHistory = [];
let TextAreaShown = false;
let isRecording = false;
let recorder = null;
let audioChunks = [];
let micStream = null;
let ignoreTTS = !document.getElementById('ttsCheckbox').checked;
let streamResponse = document.getElementById('streamCheckbox').checked;
const unityFrame = document.getElementById("UnityFrame");
/* ——— INIT ——— */
window.addEventListener('DOMContentLoaded', async () => {
  await UnityAnimationController.waitForUnity();
  UnityAnimationController.startIdle();
  document.getElementById('sendBtn').addEventListener('click', streamResponse ? sendMessageStreamed : sendMessageNonStreamed);
  document.getElementById('finishBtn').addEventListener('click', ButtonController.restoreSendBtn);
  document.getElementById('micBtn').addEventListener('click', handleMicClick);

  conversationHistory.push({ role: 'user', content: "Always respond with more than 50 characters but less than 200. Never include markdown syntax in your responses. That is an order!" });

  document.getElementById('streamCheckbox').addEventListener('change', () => {
    streamResponse = document.getElementById('streamCheckbox').checked;
    console.log("Stream checkbox changed, streamResponse is now:", streamResponse);
  });

  document.getElementById('ttsCheckbox').addEventListener('change', () => {
    ignoreTTS = !document.getElementById('ttsCheckbox').checked;
    console.log("TTS checkbox changed, ignoreTTS is now:", ignoreTTS);
  });

  document.getElementById('toggleDebugBtn').addEventListener('click', () => {
    document.querySelector('.debugContent').classList.toggle('collapsed');
  });

  try {
    const stream = await navigator.mediaDevices.getUserMedia({ audio: true });
    const audioTrack = stream.getAudioTracks()[0];
    document.getElementById('selectedMicDebug').textContent = audioTrack?.label || 'None';
    stream.getTracks().forEach(t => t.stop());
  } catch {
    document.getElementById('selectedMicDebug').textContent = 'None';
  }
});

window.addEventListener('keydown', e => {
  if (e.key === 'Enter') {
    const currentButton = ButtonController.getCurrentButton();
    if (currentButton.id === 'finishBtn') ButtonController.restoreSendBtn();
    else if (currentButton.id === 'sendBtn') { streamResponse ? sendMessageStreamed() : sendMessageNonStreamed(); }
  }
});

document.getElementById('clickOverlay').addEventListener('click', () => {
  const controls = document.querySelector('.userControls');
  const wrapper = document.querySelector('.unityWrapper');
  const modelAndInput = document.querySelector('.modelAndInput');
  TextAreaShown = !TextAreaShown;

  controls.style.margin = TextAreaShown ? '15px' : '-200px';
  wrapper.style.width = TextAreaShown ? "700px" : "256px";
  modelAndInput.style.backgroundColor = TextAreaShown ? "#0000007e" : "transparent";
  modelAndInput.style.gap = TextAreaShown ? '0px' : (ButtonController.getCurrentButton().id == 'sendBtn' ? '225px' : '330px');

  if (BubbleTextController.isShowing()) BubbleTextController.cacheText();
  else BubbleTextController.restoreCachedText();
});

function getResponseHandler() {
  return streamResponse ? StreamedResponseHandler : NonStreamedResponseHandler;
}

function getUserInput() {
  const userInputEl = document.getElementById('userInput');
  const userInput = userInputEl.value.trim();
  if (!userInput) return null;
  userInputEl.value = '';
  return userInput;
}

async function sendMessageStreamed() {
  const userInput = getUserInput();
  if (!userInput) return;

  ButtonController.disableSendButton();
  UnityAnimationController.startThinking();

  conversationHistory.push({ role: 'user', content: userInput });

  let isFirtstChunk = true;
  let fullResponse = '';
  for await (const chunk of getResponseHandler().getResponse(conversationHistory)) {
    if (isFirtstChunk) {
      isFirtstChunk = false;
      ButtonController.showSkipButton();
    }
    if (BubbleTextController.userPressedSkip) break;
    BubbleTextController.appendToBubbleText(chunk);
    fullResponse += chunk;
  }
  UnityAnimationController.startIdle();

  if (ignoreTTS || BubbleTextController.userPressedSkip) return;

  const lipsyncRes = await fetch('http://localhost:3000/api/openai/lipsync', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ text: fullResponse })
  });

  const { audioUrl, visemes } = await lipsyncRes.json();
  console.log('Playing audio from URL:', `http://localhost:3000${audioUrl}`);
  audioPlayer.src = `http://localhost:3000${audioUrl}`;
  audioPlayer.volume = 1.0;
  audioPlayer.muted = false;
  audioPlayer.oncanplay = () => console.log('[audio] canplay event fired');;
  audioPlayer.onplay = () => {
    console.log('Audio playback started');
    UnityAnimationController.startLipSync(JSON.stringify(visemes));
  };

  try {
    await audioPlayer.play();
  } catch (err) {
    console.warn('Audio play interrupted:', err);
  }

  ButtonController.showFinishButton();
}

async function sendMessageNonStreamed() {
  const userInput = getUserInput();
  if (!userInput) return;

  ButtonController.disableSendButton();
  UnityAnimationController.startThinking();
  conversationHistory.push({ role: 'user', content: userInput });

  const response = await getResponseHandler().getResponse(conversationHistory);
  if (!response) {
    ButtonController.restoreSendBtn();
    UnityAnimationController.startIdle();
    return;
  }

  conversationHistory.push({ role: 'assistant', content: response });
  BubbleTextController.appendToBubbleText(response);

  if (ignoreTTS || BubbleTextController.userPressedSkip) return;

  const lipsyncRes = await fetch('http://localhost:3000/api/openai/lipsync', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ text: response })
  });

  const { audioUrl, visemes } = await lipsyncRes.json();

  audioPlayer.src = `http://localhost:3000${audioUrl}`;
  console.log('Playing audio from URL:', `http://localhost:3000${audioUrl}`);
  audioPlayer.volume = 1.0;
  audioPlayer.muted = false;
  audioPlayer.oncanplay = () => console.log('[audio] canplay event fired');
  audioPlayer.onplay = () => console.log('[audio] play event fired');
  audioPlayer.onended = () => console.log('[audio] ended event fired');
  audioPlayer.onerror = (e) => console.error('[audio] error event', e);
  audioPlayer.onplay = () => {
    console.log('Audio playback started');
    UnityAnimationController.startLipSync(JSON.stringify(visemes));
  };

  try {
    await audioPlayer.play();
  } catch (err) {
    console.warn('Audio play interrupted:', err);
  }

  ButtonController.showFinishButton();
}
async function handleMicClick(event) {
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

          //PLAY BACK YOUR RECORDED AUDIO
          // await new Promise((resolve) => {
          //   const audioUrl = URL.createObjectURL(audioBlob);
          //   const audio = new Audio(audioUrl);
          //   audio.onended = () => {
          //     URL.revokeObjectURL(audioUrl);
          //     resolve();
          //   };
          //   
          //   audio.play().then(() => {
          //     console.log('Playing audio...');
          //   }).catch(err => {
          //     console.error('Audio play error:', err);
          //   });
          // });

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
            streamResponse ? sendMessageStreamed() : sendMessageNonStreamed()
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
}