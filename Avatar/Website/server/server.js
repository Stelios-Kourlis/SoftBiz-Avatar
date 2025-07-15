// server.js  (Node ≥18, ES modules)
import path from 'path';
import { fileURLToPath } from 'url';
import express from 'express';
import fetch from 'node-fetch';
import dotenv from 'dotenv';
import cors from 'cors';

const __filename = fileURLToPath(import.meta.url);
const __dirname  = path.dirname(__filename);

/* ——— load .env ——— */
dotenv.config({ path: path.join(__dirname, '.env') });

const {
  OPENAI_API_KEY,
  FRONTEND_ORIGIN = 'http://127.0.0.1:5500',   // change if needed
  PORT = 3000
} = process.env;

if (!OPENAI_API_KEY) {
  console.error('❌  OPENAI_API_KEY missing in .env');
  process.exit(1);
}

const app = express();
app.use(cors({ origin: FRONTEND_ORIGIN }));
app.use(express.json());

/* ——— CHAT (text) ——— */
app.post('/api/openai/chat', async (req, res) => {
  try {
    const r = await fetch('https://api.openai.com/v1/chat/completions', {
      method : 'POST',
      headers: {
        'Authorization': `Bearer ${OPENAI_API_KEY}`,
        'Content-Type' : 'application/json'
      },
      body: JSON.stringify(req.body)
    });
    res.status(r.status).type('application/json').send(await r.text());
  } catch (err) {
    console.error('Chat proxy failure:', err);
    res.status(500).json({ error: 'Chat proxy failure' });
  }
});

/* ——— TTS (audio) ——— */
app.post('/api/openai/tts', async (req, res) => {
  try {
    const { input, voice = 'spruce' } = req.body;

    const r = await fetch('https://api.openai.com/v1/audio/speech', {
      method : 'POST',
      headers: {
        'Authorization': `Bearer ${OPENAI_API_KEY}`,
        'Content-Type' : 'application/json'
      },
      body: JSON.stringify({
        model : 'tts-1-hd',
        input,
        voice,
        format: 'mp3'
      })
    });

    res.status(r.status);
    r.body.pipe(res);          // stream MP3 straight through
  } catch (err) {
    console.error('TTS proxy failure:', err);
    res.status(500).send('TTS proxy failure');
  }
});

app.listen(PORT, () =>
  console.log(`✅  Proxy listening on http://localhost:${PORT}`)
);
