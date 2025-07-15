/* main.js — chat + TTS + Unity */
let unityInstance = null;
let audioPlayer   = document.getElementById('audioPlayer');
let ignoreTTS     = false;              // toggle if you want silent mode
let conversationHistory = [];

/* ——— INIT ——— */
window.addEventListener('DOMContentLoaded', () => {
  waitForUnity();
  document.getElementById('sendBtn').addEventListener('click', sendMsg);
});

window.addEventListener('keydown', e => {
  if (e.key === 'Enter') sendMsg();
});

/* ——— SEND MESSAGE ——— */
async function sendMsg() {
  const userInputEl = document.getElementById('userInput');
  const userInput   = userInputEl.value.trim();
  if (!userInput) return;

  // UI state
  document.getElementById('sendBtn').disabled  = true;
  document.getElementById('sendBtn').textContent = 'Thinking…';
  addMessage(userInput, 'me');
  userInputEl.value = '';

  // add to history
  conversationHistory.push({ role: 'user', content: userInput });

  /* 1️⃣  CHAT  */
  const chatRes = await fetch('http://localhost:3000/api/openai/chat', {
    method : 'POST',
    headers: { 'Content-Type': 'application/json' },
    body   : JSON.stringify({
      model   : 'gpt-4o',
      messages: conversationHistory
    })
  });

  if (!chatRes.ok) {
    console.error('Chat error:', await chatRes.text());
    restoreSendBtn();
    return;
  }

  const chatData  = await chatRes.json();
  const replyText = chatData.choices?.[0]?.message?.content || '(empty reply)';
  conversationHistory.push({ role: 'assistant', content: replyText });

  // add bubble placeholder
  const bubbleEl = await addMessage('', 'AI', true);

  /* 2️⃣  TTS  */
  if (ignoreTTS) {
    animateBubbleText(bubbleEl, marked.parse(replyText));
    restoreSendBtn();
    return;
  }

  const ttsRes = await fetch('http://localhost:3000/api/openai/tts', {
    method : 'POST',
    headers: { 'Content-Type': 'application/json' },
    body   : JSON.stringify({ input: replyText, voice: 'ash' }) // male
  });

  if (!ttsRes.ok) {
    console.error('TTS error:', await ttsRes.text());
    animateBubbleText(bubbleEl, marked.parse(replyText));
    restoreSendBtn();
    return;
  }

  const audioBlob = await ttsRes.blob();
  const audioURL  = URL.createObjectURL(audioBlob);
  audioPlayer.src = audioURL;

  const duration = await playAndGetDuration(audioPlayer);
  const msPerChar = (duration * 500) / replyText.length;
  animateBubbleText(bubbleEl, marked.parse(replyText), msPerChar);
  restoreSendBtn();
}

function restoreSendBtn() {
  const btn = document.getElementById('sendBtn');
  btn.disabled = false;
  btn.textContent = 'Send';
}

/* ——— AUDIO UTIL ——— */
function playAndGetDuration(player) {
  return new Promise((resolve, reject) => {
    player.addEventListener('loadedmetadata', () => {
      player.play().catch(reject);
    }, { once: true });

    player.addEventListener('playing', () => resolve(player.duration), { once: true });
  });
}

/* ——— UNITY WAIT ——— */
function waitForUnity() {
  const frame = document.getElementById('UnityFrame');
  if (frame?.contentWindow?.unityInstance) {
    unityInstance = frame.contentWindow.unityInstance;
  } else {
    setTimeout(waitForUnity, 500);
  }
}

/* ——— HANDLE UNITY → JS ——— */
function HandleUnityMessage(txt) {
  console.log('[Unity]', txt);
  if (!ignoreTTS) sendMsgWithPreset(txt);   // optional: echo back
}

/* ——— BUBBLE ANIMATION ——— */
function animateBubbleText(bubbleEl, html, msPerChar = 25) {
  let i = 0;
  const p = bubbleEl.querySelector('p');

  const endAnim = () => {
    clearInterval(timer);
    p.innerHTML = html;
    unityInstance?.SendMessage('poppy_v1_prefab', 'StopTalking');
    document.getElementById('stopBtn').style.display = 'none';
  };

  document.getElementById('stopBtn').onclick = endAnim;
  document.getElementById('stopBtn').style.display = 'inline-block';
  unityInstance?.SendMessage('poppy_v1_prefab', 'StartTalking');

  const timer = setInterval(() => {
    p.innerHTML = html.slice(0, i++);
    if (i > html.length) endAnim();
  }, msPerChar);
}

/* ——— CHAT UI DOM ——— */
async function addMessage(text, sender, animated = false) {
  const html = await (await fetch('chatBubble.html')).text();
  const temp = document.createElement('div');
  temp.innerHTML = html.trim();
  const bubble = temp.firstChild;

  bubble.classList.add(sender === 'me' ? 'right' : 'left');
  document.getElementById('resBox').appendChild(bubble);

  if (animated) {
    bubble.querySelector('p').innerHTML = '';
    return bubble;
  }
  bubble.querySelector('p').innerHTML = text;
}
