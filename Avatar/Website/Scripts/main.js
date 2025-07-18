import { ButtonController, BubbleTextController } from './UiController.js';
import { StreamedResponseHandler } from './ResponseHandlers/StreamedResponseHandler.js';
import { NonStreamedResponseHandler } from './ResponseHandlers/NonStreamedResponseHandler.js';
import * as UnityAnimationController from './UnityAnimationController.js';


/* main.js — chat + TTS + Unity */
let audioPlayer = document.getElementById('audioPlayer');
let conversationHistory = [];
let TextAreaShown = false;
let isRecording = false;
let recorder = null;
let audioChunks = [];
let micStream = null;
let ignoreTTS = false;
let streamResponse = true;
let responseHandler = streamResponse ? StreamedResponseHandler : NonStreamedResponseHandler;

/* ——— INIT ——— */
window.addEventListener('DOMContentLoaded', async () => {
  await UnityAnimationController.waitForUnity();
  UnityAnimationController.startIdle();
  document.getElementById('sendBtn').addEventListener('click', streamResponse ? sendMessageStreamed : sendMessageNonStreamed);
  document.getElementById('finishBtn').addEventListener('click', ButtonController.restoreSendBtn);
  document.getElementById('micBtn').addEventListener('click', handleMicClick);

  document.getElementById('streamCheckbox').addEventListener('change', () => {
    streamResponse = document.getElementById('streamCheckbox').checked;
    responseHandler = streamResponse ? StreamedResponseHandler : NonStreamedResponseHandler;
    console.log("Stream checkbox changed, streamResponse is now:", streamResponse);
  });

  document.getElementById('ttsCheckbox').addEventListener('change', () => {
    ignoreTTS = !document.getElementById('ttsCheckbox').checked;
    console.log("TTS checkbox changed, ignoreTTS is now:", ignoreTTS);
  });

  try {
    // Try to get mic permission and stream
    const stream = await navigator.mediaDevices.getUserMedia({ audio: true });
    const audioTrack = stream.getAudioTracks()[0];
    document.getElementById('selectedMicDebug').textContent = audioTrack?.label || 'None';
    // Stop the tracks immediately since we just want the label
    stream.getTracks().forEach(t => t.stop());
  } catch {
    // Permission denied or no mic
    document.getElementById('selectedMicDebug').textContent = 'None';
  }
});

window.addEventListener('keydown', e => {
  if (e.key === 'Enter') {
    const finishButton = document.getElementById('finishBtn');
    const finishButtonIsShownInsteadOfSend = !!(finishButton.offsetWidth || finishButton.offsetHeight || finishButton.getClientRects().length);
    console.log("Enter pressed, ", streamResponse);
    if (finishButtonIsShownInsteadOfSend) ButtonController.restoreSendBtn();
    else streamResponse ? sendMessageStreamed() : sendMessageNonStreamed();
  }
});

document.getElementById('clickOverlay').addEventListener('click', () => {
  const controls = document.querySelector('.userControls');
  const wrapper = document.querySelector('.unityWrapper');
  TextAreaShown = !TextAreaShown;
  controls.style.display = TextAreaShown ? 'flex' : 'none';
  wrapper.style.width = TextAreaShown ? "700px" : "256px";
});

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

function getUserInput() {
  const userInputEl = document.getElementById('userInput');
  const userInput = userInputEl.value.trim();
  if (!userInput) return null;
  userInputEl.value = '';
  return userInput;
}

async function sendMessageStreamed() {
  const userInput = getUserInput()
  if (!userInput) return;

  ButtonController.disableSendButton();
  UnityAnimationController.startThinking();

  conversationHistory.push({ role: 'user', content: userInput });

  let isFirtstChunk = true;
  let fullResponse = '';
  for await (const chunk of responseHandler.getResponse(conversationHistory)) {
    if (isFirtstChunk) { //Start talking only on the first chunk
      UnityAnimationController.startTalking();
      isFirtstChunk = false;
    }
    BubbleTextController.appendToBubbleText(chunk);
    fullResponse += chunk;
  }
  UnityAnimationController.startIdle();

  if (ignoreTTS) {
    ButtonController.showFinishButton();
    return;
  }

  const blob = await responseHandler.getTTSAudio(fullResponse);
  const audio = new Audio(URL.createObjectURL(blob));
  const duration = await playAndGetDuration(audio);
  UnityAnimationController.startTalking();
  await new Promise(resolve => setTimeout(resolve, duration * 1000)); // wait duration
  UnityAnimationController.startIdle();
  ButtonController.showFinishButton();
}

async function sendMessageNonStreamed() {
  const userInput = getUserInput();
  if (!userInput) return;

  // UI state
  ButtonController.disableSendButton();
  conversationHistory.push({ role: 'user', content: userInput });
  UnityAnimationController.startThinking();

  const response = await responseHandler.getResponse(conversationHistory)

  if (!response) {
    ButtonController.restoreSendBtn();
    UnityAnimationController.startIdle();
    return;
  }

  conversationHistory.push({ role: 'assistant', content: response });

  if (ignoreTTS) {
    UnityAnimationController.startTalking();
    BubbleTextController.appendToBubbleText(response);
    ButtonController.showFinishButton();
    return;
  }

  BubbleTextController.appendToBubbleText(response);
  const tts = await responseHandler.getTTSAudio(response);

  if (!tts) {
    BubbleTextController.appendToBubbleText(response);
    ButtonController.showFinishButton();
    return;
  }

  audioPlayer.src = URL.createObjectURL(tts);

  const duration = await playAndGetDuration(audioPlayer);
  UnityAnimationController.startTalking();
  await new Promise(resolve => setTimeout(resolve, duration * 1000)); // wait duration
  UnityAnimationController.startIdle();
  ButtonController.showFinishButton();
}

function playAndGetDuration(player) {
  return new Promise((resolve, reject) => {
    player.addEventListener('loadedmetadata', () => {
      player.play().catch(reject);
    }, { once: true });

    player.addEventListener('playing', () => resolve(player.duration), { once: true });
  });
}
