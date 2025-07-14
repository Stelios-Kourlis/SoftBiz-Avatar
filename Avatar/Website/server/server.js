import path from 'path';
import fs from 'fs';
import { fileURLToPath } from 'url';

import express from 'express';
import fetch from 'node-fetch';
import dotenv from 'dotenv';
import cors from 'cors';

const __filename = fileURLToPath(import.meta.url);
const __dirname = path.dirname(__filename);

// 🔍 Log the current working directory
console.log("📁 Current directory:", __dirname);

// 🔍 Check if .env exists
const envPath = path.join(__dirname, '.env');
console.log("🔍 Checking for .env at:", envPath);
console.log("📄 .env exists?", fs.existsSync(envPath));

dotenv.config({ path: envPath }); // ✅ force correct path

const app = express();

// ✅ Allow requests coming from Live Server (127.0.0.1:5500)
app.use(
  cors({
    origin: 'http://127.0.0.1:5500',
  })
);

app.use(express.json());

const {
  GEMINI_API_KEY,
  ELEVEN_API_KEY,
  VOICE_ID = '21m00Tcm4TlvDq8ikWAM',
} = process.env;

/* ───────── Gemini ───────── */
app.post('/api/gemini', async (req, res) => {
  try {
    const upstream = `https://generativelanguage.googleapis.com/v1beta/models/gemini-2.5-flash:generateContent?key=${GEMINI_API_KEY}`;

    const r = await fetch(upstream, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(req.body),
    });

    res.status(r.status).type('application/json').send(await r.text());
  } catch (err) {
    console.error('Gemini proxy failure:', err);
    res.status(500).json({ error: 'Gemini proxy failure' });
  }
});

/* ───────── ElevenLabs ───────── */
app.post('/api/tts', async (req, res) => {
  try {
    const upstream = `https://api.elevenlabs.io/v1/text-to-speech/${VOICE_ID}/stream`;

    const r = await fetch(upstream, {
      method: 'POST',
      headers: {
        'xi-api-key': ELEVEN_API_KEY,
        'Content-Type': 'application/json',
      },
      body: JSON.stringify(req.body),
    });

    res.status(r.status);
    r.body.pipe(res); // stream audio through
  } catch (err) {
    console.error('TTS proxy failure:', err);
    res.status(500).send('TTS proxy failure');
  }
});

app.listen(3000, () =>
  console.log('✅ Proxy listening on http://localhost:3000')
);
