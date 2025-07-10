
import { marked } from 'https://cdn.jsdelivr.net/npm/marked/lib/marked.esm.js';;

const geminiApiKey = "AIzaSyC2YV4tiwz6vJ6oc5pwt-w82itmvBV_ASs"; // Replace with your Gemini key
const elevenLabsApiKey = "sk_cf911377d70e13c64ba2f17dade6caa87e91f2648eeea224"; // Replace with your ElevenLabs key
const voiceId = "21m00Tcm4TlvDq8ikWAM";
// Globals
let unityInstance = null;
// let currentLineIndex = 0;
let audioPlayer = document.getElementById("audioPlayer");
let hasUsedNext = false;
let ignoreTTS = true; // Set to true to skip TTS

function waitForUnity(callback) {
  const frame = document.getElementById("UnityFrame");
  if (
    frame &&
    frame.contentWindow &&
    frame.contentWindow.unityInstance
  ) {
    unityInstance = frame.contentWindow.unityInstance;
    callback();
  } else {
    setTimeout(() => waitForUnity(callback), 500);
  }
}

async function sendToGemini() {
  const userInput = document.getElementById("userInput").value;
  addMessage(userInput, "me")
  document.getElementById("userInput").value = "";

  waitForUnity(async () => {
    // unityInstance.SendMessage("Canvas", "Think");

    const geminiUrl = `https://generativelanguage.googleapis.com/v1beta/models/gemini-2.0-flash:generateContent?key=${geminiApiKey}`;
    const requestBody = {
      contents: [{ parts: [{ text: userInput }] }]
    };

    try {
      const geminiRes = await fetch(geminiUrl, {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify(requestBody)
      });

      const data = await geminiRes.json();
      const responseText = data?.candidates?.[0]?.content?.parts?.[0]?.text;

      if (!responseText) {
        return;
      }

      console.log("Response text:", responseText);
      // document.getElementById("resBox").textContent = responseText;
      addMessage(marked(responseText), "AI")

    } catch (error) {
      console.error("Network error:", error);
    }
  });
}


async function sendToTTS(text) {
  try {
    const ttsRes = await fetch(`https://api.elevenlabs.io/v1/text-to-speech/${voiceId}`, {
      method: "POST",
      headers: {
        "Content-Type": "application/json",
        "xi-api-key": elevenLabsApiKey
      },
      body: JSON.stringify({
        text: text,
        model_id: "eleven_monolingual_v1",
        voice_settings: {
          stability: 0.5,
          similarity_boost: 0.5
        }
      })
    });

    if (!ttsRes.ok) {
      const errText = await ttsRes.text();
      console.error("TTS error:", errText);
      // unityInstance.SendMessage("Canvas", "AddToResponse", "TTS Error: " + errText);
      return;
    }

    const audioBlob = await ttsRes.blob();
    const audioUrl = URL.createObjectURL(audioBlob);
    audioPlayer.src = audioUrl;

    audioPlayer.addEventListener('loadedmetadata', () => {
      // console.log("Sending line to Unity and TTS (Call):", textLine);
      unityInstance.SendMessage("Canvas", "SetTTSAudioDuration", audioPlayer.duration);
      unityInstance.SendMessage("Canvas", "SetTTSAsLoaded");
      console.log("JS TTS Loaded:");
      audioPlayer.play().catch(e => console.error("Audio play failed:", e));
    }, { once: true });

  } catch (error) {
    console.error("TTS or Unity error:", error);
    // unityInstance.SendMessage("Canvas", "AddToResponse", "TTS Error: " + error.message);
  }
}

// Called from Unity
function HandleUnityMessage(plainTextString) {
  console.log("JS received message: ", plainTextString);
  if (ignoreTTS) {
    console.log("Ignoring TTS, sending to Unity only.");
    unityInstance.SendMessage("Canvas", "SetTTSAsLoaded");
    return;
  }
  sendToTTS(plainTextString);
  // skipLine();
}

async function addMessage(text, sender) {
  const res = await fetch('chatBubble.html');
  let bubble = await res.text();

  // Create a temp div to modify the fetched HTML
  const temp = document.createElement('div');
  temp.innerHTML = bubble.trim();
  const bubbleEl = temp.firstChild;

  // Add message text
  bubbleEl.querySelector('p').innerHTML = text;

  // Add sender style
  bubbleEl.classList.add(sender === 'me' ? 'right' : 'left');

  document.getElementById('resBox').appendChild(bubbleEl);
}

function UpdateButtonsBasedOnIndex({ index, total }) {
  console.log("JS received index:", index, "of", total);
  // updatePrevButtonVisibility(index, total);
  // updateNextButtonLabel(index, total);
}

window.sendToGemini = sendToGemini;