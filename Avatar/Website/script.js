async function sendToGemini() {
  const geminiApiKey = "AIzaSyC2YV4tiwz6vJ6oc5pwt-w82itmvBV_ASs"; // Replace with your Gemini key
  const elevenLabsApiKey = "sk_48db9614c3e60f815f5aaa1b8f930a222519597bec8f01ad"; // Replace with your ElevenLabs key
  const voiceId = "21m00Tcm4TlvDq8ikWAM";

  const userInput = document.getElementById("userInput").value;
  document.getElementById("userInput").value = "";

  const unityInstance = document.getElementById('UnityFrame').contentWindow.unityInstance;
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

    // Call ElevenLabs TTS first (before showing text)
    const ttsRes = await fetch(`https://api.elevenlabs.io/v1/text-to-speech/${voiceId}`, {
      method: "POST",
      headers: {
        "Content-Type": "application/json",
        "xi-api-key": elevenLabsApiKey
      },
      body: JSON.stringify({
        text: responseText,
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
    const audioPlayer = document.getElementById("audioPlayer");
    audioPlayer.src = audioUrl;

    // Now send Gemini reply to Unity and play audio
    unityInstance.SendMessage("Canvas", "RespondEntry", responseText);
    // unityInstance.SendMessage("Canvas", "SetTTSAudioDuration", audioPlayer.duration);
    audioPlayer.play().catch(e => console.error("Audio play failed:", e));

  } catch (error) {
    console.error("Network error:", error);
    unityInstance.SendMessage("Canvas", "RespondEntry", "Network error: " + error.message);
  }
}

function HandleUnityMessage(jsonString) {
  console.log("JS received text: ", jsonString)
  //TODO
}