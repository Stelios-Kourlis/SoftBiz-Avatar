
let unityInstance = null;
let audioPlayer = document.getElementById("audioPlayer");
let ignoreTTS = true;
let conversationHistory = [];

window.addEventListener("DOMContentLoaded", () => {
  waitForUnity();
  document.getElementById("sendBtn").addEventListener("click", sendToGemini);
});



window.addEventListener("keydown", (event) => {
  if (event.key === "Enter") {
    sendToGemini();
  }
});

// === TTS ===
async function sendToTTS(text) {
  // waitForUnity();
  return new Promise(async (resolve, reject) => {
    try {
      const ttsRes = await fetch('http://localhost:3000/api/tts', {
        method: "POST", // critical: must be POST
        headers: {
          "Content-Type": "application/json",
        
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
        console.error("TTS error (Not OK):", errText);
        resolve(-1);
        return;
      }

      const audioBlob = await ttsRes.blob();
      const audioUrl = URL.createObjectURL(audioBlob);
      audioPlayer.src = audioUrl;

      audioPlayer.addEventListener("loadedmetadata", () => {
        audioPlayer.play().catch(reject);
      }, { once: true });

      audioPlayer.addEventListener("playing", () => resolve(audioPlayer.duration), { once: true });
    } catch (error) {
      console.error("TTS error (Exception):", error);
      reject(error);
    }
  });
}

// === Main Send Function ===
async function sendToGemini() {
  const userInput = document.getElementById("userInput").value;
  if (!userInput.trim()) return;

  document.getElementById("sendBtn").disabled = true;
  document.getElementById("sendBtn").innerHTML = "Thinking...";

  addMessage(userInput, "me");
  document.getElementById("userInput").value = "";

  conversationHistory.push({
    role: "user",
    parts: [{ text: userInput }]
  });

  const requestBody = {
    contents: conversationHistory
  };

  try {
    const response = await fetch("http://localhost:3000/api/gemini", {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify(requestBody)
    });

    if (!response.ok) {
      const errText = await response.text();
      console.error("Gemini proxy error:", errText);
      return;
    }

    const data = await response.json();
    const replyText = data?.candidates?.[0]?.content?.parts?.[0]?.text;

    if (!replyText) return;

    conversationHistory.push({
      role: "model",
      parts: [{ text: replyText }]
    });

    document.getElementById("sendBtn").disabled = false;
    document.getElementById("sendBtn").innerHTML = "Send";

    const bubbleEl = await addMessage("", "AI", true);

    if (ignoreTTS) {
      animateBubbleText(bubbleEl, window.marked.parse(replyText));
    } else {
      const duration = await sendToTTS(replyText);
      const msPerChar = duration == -1 ? 25 : (duration * 1000) / replyText.length;
      animateBubbleText(bubbleEl, window.marked.parse(replyText), msPerChar);
    }
  } catch (err) {
    console.error("Gemini fetch error:", err);
  }
}

// === Animate Text Bubble ===
function animateBubbleText(bubbleEl, fullText, msPerChar = 25) {
  function finishAnimation() {
    clearInterval(interval);
    bubbleEl.querySelector("p").innerHTML = fullText;
    unityInstance.SendMessage("poppy_v1_prefab", "StopTalking");
    document.getElementById("sendBtn").style.display = "inline-block";
    document.getElementById("stopBtn").style.display = "none";
    document.getElementById("stopBtn").removeEventListener("click", finishAnimation);
  };

  let i = 0;
  document.getElementById("stopBtn").addEventListener("click", finishAnimation)
  unityInstance.SendMessage("poppy_v1_prefab", "StartTalking")
  document.getElementById("sendBtn").style.display = "none";
  document.getElementById("stopBtn").style.display = "inline-block";
  const interval = setInterval(() => {
    bubbleEl.querySelector("p").innerHTML = fullText.slice(0, i++);
    if (i > fullText.length) {
      clearInterval(interval);
      finishAnimation()
    }
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
    bubbleEl.querySelector("p").innerHTML = "";
    return bubbleEl;
  } else {
    bubbleEl.querySelector("p").innerHTML = text;
  }
}

// === Unity Integration ===
function waitForUnity(callback) {
  const frame = document.getElementById("UnityFrame");
  if (frame?.contentWindow?.unityInstance) {
    unityInstance = frame.contentWindow.unityInstance;
    // callback();
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
