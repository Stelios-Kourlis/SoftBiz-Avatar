import { marked } from 'https://cdn.jsdelivr.net/npm/marked/lib/marked.esm.js';

const geminiApiKey = "AIzaSyC2YV4tiwz6vJ6oc5pwt-w82itmvBV_ASs";
const elevenLabsApiKey = "sk_cf911377d70e13c64ba2f17dade6caa87e91f2648eeea224";
const voiceId = "21m00Tcm4TlvDq8ikWAM";

let unityInstance = null;
let audioPlayer = document.getElementById("audioPlayer");
let ignoreTTS = false;
let conversationHistory = [];

window.addEventListener("DOMContentLoaded", () => {
  document.getElementById("sendBtn").addEventListener("click", sendToGemini);
});

// === TTS ===
async function sendToTTS(text) {
  return new Promise(async (resolve, reject) => {
    try {
      const ttsRes = await fetch(`https://api.elevenlabs.io/v1/text-to-speech/${voiceId}/stream`, {
        method: "POST", // critical: must be POST
        headers: {
          "Content-Type": "application/json",
          "xi-api-key": elevenLabsApiKey,
        },
        body: JSON.stringify({
          text: text,
          model_id: "eleven_monolingual_v1",
          voice_settings: {
            stability: 0.5,
            similarity_boost: 0.5,
          },
        }),
      });

      if (!ttsRes.ok) {
        const errText = await ttsRes.text();
        console.error("TTS error:", errText);
        return reject(errText);
      }

      const audioBlob = await ttsRes.blob();
      const audioUrl = URL.createObjectURL(audioBlob);
      audioPlayer.src = audioUrl;

      audioPlayer.addEventListener("loadedmetadata", () => {
        unityInstance?.SendMessage("Canvas", "SetTTSAudioDuration", audioPlayer.duration);
        unityInstance?.SendMessage("Canvas", "SetTTSAsLoaded");
        audioPlayer.play().catch(reject);
      }, { once: true });

      audioPlayer.addEventListener("playing", () => resolve(audioPlayer.duration), { once: true });
    } catch (error) {
      console.error("TTS error:", error);
      reject(error);
    }
  });
}

// === Main Send Function ===
async function sendToGemini() {
  const userInput = document.getElementById("userInput").value;
  if (!userInput.trim()) return;

  addMessage(userInput, "me");
  document.getElementById("userInput").value = "";

  conversationHistory.push({
    role: "user",
    parts: [{ text: userInput }]
  });

  const geminiUrl = `https://generativelanguage.googleapis.com/v1beta/models/gemini-2.5-flash:generateContent?key=${geminiApiKey}`;
  const requestBody = {
    contents: conversationHistory
  };

  try {
    const response = await fetch(geminiUrl, {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify(requestBody)
    });

    const data = await response.json();
    const replyText = data?.candidates?.[0]?.content?.parts?.[0]?.text;

    if (!replyText) return;

    // Add model's reply to history
    conversationHistory.push({
      role: "model",
      parts: [{ text: replyText }]
    });

   if (ignoreTTS) {
  addMessage(replyText, "AI");
} else {
  const bubbleEl = await addMessage("", "AI", true);   // placeholder

  // Kick off TTS and wait for “playing”
  const duration = await sendToTTS(replyText);

  // Compute speed so animation finishes with the audio
  const msPerChar = (duration * 1000) / replyText.length;
  animateBubbleText(bubbleEl, replyText, msPerChar);
}
  } catch (err) {
    console.error("Gemini fetch error:", err);
  }
}

// === Animate Text Bubble ===
function animateBubbleText(bubbleEl, fullText, msPerChar = 25) {
  let i = 0;
  const interval = setInterval(() => {
    bubbleEl.querySelector("p").textContent = fullText.slice(0, i++);
    if (i > fullText.length) clearInterval(interval);
  }, msPerChar);
}

// === Chat UI ===
async function addMessage(text, sender, animated = false) {
  const res = await fetch("chatBubble.html");
  const html = await res.text();

  const temp = document.createElement("div");
  temp.innerHTML = html.trim();
  const bubbleEl = temp.firstChild;

  bubbleEl.classList.add(sender === "me" ? "right" : "left");
  document.getElementById("resBox").appendChild(bubbleEl);

  if (animated) {
    bubbleEl.querySelector("p").textContent = "";
    return bubbleEl;
  } else {
    bubbleEl.querySelector("p").textContent = text;
  }
}

// === Unity Integration ===
function waitForUnity(callback) {
  const frame = document.getElementById("UnityFrame");
  if (frame?.contentWindow?.unityInstance) {
    unityInstance = frame.contentWindow.unityInstance;
    callback();
  } else {
    setTimeout(() => waitForUnity(callback), 500);
  }
}

function HandleUnityMessage(plainTextString) {
  console.log("JS received from Unity:", plainTextString);
  if (ignoreTTS) {
    unityInstance?.SendMessage("Canvas", "SetTTSAsLoaded");
    return;
  }
  sendToTTS(plainTextString);
}

function UpdateButtonsBasedOnIndex({ index, total }) {
  console.log("Unity index update:", index, "of", total);
}
