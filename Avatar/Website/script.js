 import { geminiApiKey, elevenLabsApiKey } from './keys.js';
  const voiceId = "21m00Tcm4TlvDq8ikWAM";
// Globals
let unityInstance = null;
let responseLines = [];
let currentLineIndex = 0;
let audioPlayer = document.getElementById("audioPlayer");

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
  document.getElementById("userInput").value = "";

  waitForUnity(async () => {
    unityInstance.SendMessage("Canvas", "Think");

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
  unityInstance.SendMessage("Canvas", "RespondEntry", "No valid response from Gemini.");
  return;
}

// Break response into sentences instead of lines
responseLines = responseText.match(/[^.!?]+[.!?]+/g) || [responseText];
responseLines = responseLines.map(line => line.trim());

currentLineIndex = 0;
document.getElementById("nextButton").style.display = "inline-block";
document.getElementById("prevButton").style.display = "inline-block";
await sendLineToUnityAndTTS(responseLines[currentLineIndex]);
    } catch (error) {
      console.error("Network error:", error);
      unityInstance.SendMessage("Canvas", "RespondEntry", "Network error: " + error.message);
    }
  });
}

async function sendLineToUnityAndTTS(textLine) {
  try {
    const ttsRes = await fetch(`https://api.elevenlabs.io/v1/text-to-speech/${voiceId}`, {
      method: "POST",
      headers: {
        "Content-Type": "application/json",
        "xi-api-key": elevenLabsApiKey
      },
      body: JSON.stringify({
        text: textLine,
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
      unityInstance.SendMessage("Canvas", "RespondEntry", "TTS Error: " + errText);
      return;
    }

    const audioBlob = await ttsRes.blob();
    const audioUrl = URL.createObjectURL(audioBlob);
    audioPlayer.src = audioUrl;

    unityInstance.SendMessage("Canvas", "RespondEntry", textLine);
    audioPlayer.play().catch(e => console.error("Audio play failed:", e));

  } catch (error) {
    console.error("TTS or Unity error:", error);
    unityInstance.SendMessage("Canvas", "RespondEntry", "TTS Error: " + error.message);
  }
}

// Skip button handler
function skipLine() {
  if (audioPlayer && !audioPlayer.paused) {
    audioPlayer.pause();
  }

  currentLineIndex++;

  if (currentLineIndex < responseLines.length) {
    sendLineToUnityAndTTS(responseLines[currentLineIndex]);
  } else {
    unityInstance.SendMessage("Canvas", "RespondEntry", "End of message.");
  document.getElementById("nextButton").style.display = "none";
    document.getElementById("prevButton").style.display = "none";
  }
}

function prevLine() {
  if (audioPlayer && !audioPlayer.paused) {
    audioPlayer.pause();
  }

  currentLineIndex--;

  if (currentLineIndex < 0) {
    currentLineIndex = 0;
  }

  sendLineToUnityAndTTS(responseLines[currentLineIndex]);
}


// Called from Unity
function ReceiveMessageFromUnity(jsonString) {
  console.log("JS received message: ", jsonString);
  skipLine();
}