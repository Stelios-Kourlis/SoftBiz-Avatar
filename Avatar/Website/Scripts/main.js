
import { ButtonController, BubbleTextController } from './UiController.js';
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
// let ignoreTTS = !document.getElementById('ttsCheckbox').checked;
// let streamResponse = document.getElementById('streamCheckbox').checked;
let ignoreTTS = false;
let streamResponse = false;

/* ——— INIT ——— */
window.addEventListener('DOMContentLoaded', async () => {
  await UnityAnimationController.waitForUnity();
  UnityAnimationController.startIdle();
  document.getElementById('sendBtn').addEventListener('click', sendMessage);
  document.getElementById('finishBtn').addEventListener('click', ButtonController.restoreSendBtn);
  document.getElementById('micBtn').addEventListener('click', handleMicClick);

  conversationHistory.push({ role: 'developer', content: "Always respond with more than 50 characters but less than 200. Never include markdown syntax in your responses. Use English and only english. That is an order!" });

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
    else if (currentButton.id === 'sendBtn') sendMessage();
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
  const currentButton = ButtonController.getCurrentButton();
  modelAndInput.style.gap = TextAreaShown ? '0px' : ((currentButton.id == 'sendBtn' && !currentButton.disabled) ? '225px' : '330px');

  if (!TextAreaShown && BubbleTextController.isShowing()) BubbleTextController.cacheText();
  else BubbleTextController.restoreCachedText();
});

function getUserInput() {
  const userInputEl = document.getElementById('userInput');
  const userInput = userInputEl.value.trim();
  if (!userInput) return null;
  userInputEl.value = '';
  return userInput;
}

async function sendMessage() {
  const userInput = getUserInput();
  if (!userInput) return;

  ButtonController.disableSendButton();
  UnityAnimationController.startThinking();
  conversationHistory.push({ role: 'user', content: userInput });

  let response;

  console.log('Sending conv history to server:', conversationHistory);
  try {
    response = await fetch('http://localhost:3000/api/openai/lipsync', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({
        messages: conversationHistory,
      })
    })
  } catch (error) {
    ButtonController.restoreSendBtn();
    UnityAnimationController.startIdle();
    BubbleTextController.appendToBubbleText("Something went wrong, please try again later.");
    console.error("Error sending message to server:", error);
    return;
  }

  if (!response || !response.ok) {
    ButtonController.restoreSendBtn();
    UnityAnimationController.startIdle();
    BubbleTextController.appendToBubbleText("Something went wrong, please try again later.");
    return;
  }

  const responseData = await response.json();
  console.log('Response received:', responseData);

  conversationHistory.push({ role: 'assistant', content: responseData.transcript });

  if (responseData.audioUrl === null) {
    BubbleTextController.appendToBubbleText("[No Audio Available]" + responseData.transcript);
    UnityAnimationController.startIdle();
    console.error("No audio response from the server. This happened if no audio was created, maybe ran out of tokens")
    return;
  }

  const audioUrl = responseData.audioUrl;
  const visemes = responseData.lipSyncData;

  audioPlayer.src = `http://localhost:3000${audioUrl}`;
  console.log('Playing audio from URL:', `http://localhost:3000${audioUrl}`);
  audioPlayer.volume = 1.0;
  audioPlayer.muted = false;
  audioPlayer.onplay = () => {
    console.log('Audio playback started');
    UnityAnimationController.startIdle();
    UnityAnimationController.startLipSync(JSON.stringify(visemes));
    BubbleTextController.appendToBubbleText(responseData.transcript);
  };

  try {
    await audioPlayer.play();
  } catch (err) {
    console.warn('Audio play interrupted:', err);
  }
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
          await new Promise((resolve) => {
            const audioUrl = URL.createObjectURL(audioBlob);
            const audio = new Audio(audioUrl);
            audio.onended = () => {
              URL.revokeObjectURL(audioUrl);
              resolve();
            };

            audio.play().then(() => {
              console.log('Playing audio...');
            }).catch(err => {
              console.error('Audio play error:', err);
            });
          });

          const formData = new FormData();
          formData.append('audio', audioBlob, 'recording.webm');

          console.log('🎙️ Sending audio to Whisper…');
          const res = await fetch('http://localhost:3000/api/openai/stt', {
            method: 'POST',
            body: formData
          });

          console.log('[DEBUG] STT response status:', res.status);


          const text = await res.text();

          if (!res.ok) {
            console.error('STT failed:', text);
            return;
          }

          console.log('[STT] Transcript:', text);

          if (text) {
            // either use sendMsgDirect(transcript);
            document.getElementById('userInput').value = text;
            sendMessage();
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