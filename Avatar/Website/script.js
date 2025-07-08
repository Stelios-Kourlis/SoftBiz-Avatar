
const geminiApiKey = "AIzaSyC2YV4tiwz6vJ6oc5pwt-w82itmvBV_ASs"; // Replace with your Gemini key
const elevenLabsApiKey = "sk_cf911377d70e13c64ba2f17dade6caa87e91f2648eeea224"; // Replace with your ElevenLabs key
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
        // unityInstance.SendMessage("Canvas", "AddToResponse", "No valid response from Gemini.");
        return;
      }

      // Break response into sentences instead of lines
      responseLines = responseText.match(/[^.!?]+[.!?]+/g) || [responseText];
      responseLines = responseLines.map(line => line.trim());

      currentLineIndex = -1;
      document.getElementById("nextButton").style.display = "inline-block";
      document.getElementById("prevButton").style.display = "inline-block";
      console.log("Response lines:", responseLines);
      skipLine();
      // console.log("Sending line:", responseLines[currentLineIndex]);
      // await sendLineToUnityAndTTS(responseLines[currentLineIndex]);
    } catch (error) {
      console.error("Network error:", error);
      // unityInstance.SendMessage("Canvas", "AddToResponse", "Network error: " + error.message);
    }
  });
}

async function sendLineToUnityAndTTS(textLine) {

  console.log("Sending line to Unity and TTS (Start):", textLine);

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
      // unityInstance.SendMessage("Canvas", "AddToResponse", "TTS Error: " + errText);
      return;
    }

    const audioBlob = await ttsRes.blob();
    const audioUrl = URL.createObjectURL(audioBlob);
    audioPlayer.src = audioUrl;

    audioPlayer.addEventListener('loadedmetadata', () => {
      console.log("Sending line to Unity and TTS (Call):", textLine);
      unityInstance.SendMessage("Canvas", "SetTTSAudioDuration", audioPlayer.duration);
      unityInstance.SendMessage("Canvas", "AddToResponse", textLine);
      audioPlayer.play().catch(e => console.error("Audio play failed:", e));
    }, { once: true });

  } catch (error) {
    console.error("TTS or Unity error:", error);
    // unityInstance.SendMessage("Canvas", "AddToResponse", "TTS Error: " + error.message);
  }
}

// Skip button handler
function skipLine() {
  if (audioPlayer && !audioPlayer.paused) {
    audioPlayer.pause();
  }

  currentLineIndex++;

  if (currentLineIndex < responseLines.length) {
    unityInstance.SendMessage("Canvas", "ClearResponse");  // Clear canvas before sending
    console.log("Sending line:", responseLines[currentLineIndex]);
    sendLineToUnityAndTTS(responseLines[currentLineIndex]);
  } else {
    unityInstance.SendMessage("Canvas", "ConcludeResponse");
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

  unityInstance.SendMessage("Canvas", "ClearResponse");  // Clear canvas before sending
  sendLineToUnityAndTTS(responseLines[currentLineIndex]);
}


// Called from Unity
function ReceiveMessageFromUnity(jsonString) {
  console.log("JS received message: ", jsonString);
  // skipLine();
}