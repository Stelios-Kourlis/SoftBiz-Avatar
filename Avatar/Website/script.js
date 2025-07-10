
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
        return;
      }

      document.getElementById("response-movement").style.display = "flex";
      console.log("Response text:", responseText);
      unityInstance.SendMessage("Canvas", "AddToResponse", responseText);

    } catch (error) {
      console.error("Network error:", error);
    }
  });
}


// Skip button handler
function skipLine() {
  if (audioPlayer && !audioPlayer.paused) {
    audioPlayer.pause();
  }

  unityInstance.SendMessage("Canvas", "ShowNextPart");
  hasUsedNext = true;
}

function prevLine() {
  if (audioPlayer && !audioPlayer.paused) {
    audioPlayer.pause();
  }

  unityInstance.SendMessage("Canvas", "ShowPreviousPart");
}

function updatePrevButtonVisibility(index, total) {
  const prevButton = document.getElementById("prevButton");
  if (index == total) {
    prevButton.style.display = "none";
  } else if (index > 0) {
    prevButton.style.display = "inline-block";
  } else {
    prevButton.style.display = "none";
  }
}
function updateNextButtonLabel(index, total) {
  const nextButton = document.getElementById("nextButton");
  if (index == total - 1) {
    nextButton.textContent = "Finish";
  }
  else if (index == total) {
    nextButton.style.display = "none";
  } else {
    nextButton.textContent = "Next";
  }
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

function UpdateButtonsBasedOnIndex({ index, total }) {
  console.log("JS received index:", index, "of", total);
  updatePrevButtonVisibility(index, total);
  updateNextButtonLabel(index, total);
}