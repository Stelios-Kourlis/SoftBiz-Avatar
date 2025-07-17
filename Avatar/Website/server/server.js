// server.js  (Node ≥18, ES modules)
import path from 'path';
import { fileURLToPath } from 'url';
import express from 'express';
import fetch from 'node-fetch';
import dotenv from 'dotenv';
import cors from 'cors';
import multer from 'multer';
import fs from 'fs';
import FormData from 'form-data';

const __filename = fileURLToPath(import.meta.url);
const __dirname = path.dirname(__filename);
dotenv.config({ path: path.join(__dirname, '.env') });

const {
  OPENAI_API_KEY,
  FRONTEND_ORIGIN = 'http://127.0.0.1:5500',
  PORT = 3000
} = process.env;

if (!OPENAI_API_KEY) {
  console.error('OPENAI_API_KEY missing in .env');
  process.exit(1);
}

const app = express();
const upload = multer({ dest: 'uploads/' });

app.use(cors({ origin: FRONTEND_ORIGIN }));
app.use(express.json());

/* ——— CHAT ——— */
app.post('/api/openai/chat', async (req, res) => {
  try {
    const r = await fetch('https://api.openai.com/v1/chat/completions', {
      method: 'POST',
      headers: {
        'Authorization': `Bearer ${OPENAI_API_KEY}`,
        'Content-Type': 'application/json'
      },
      body: JSON.stringify(req.body)
    });
    res.status(r.status).type('application/json').send(await r.text());
  } catch (err) {
    console.error('Chat proxy failure:', err);
    res.status(500).json({ error: 'Chat proxy failure' });
  }
});

/* ——— TTS ——— */
app.post('/api/openai/tts', async (req, res) => {
  try {
    const { input, voice = 'ash' } = req.body;
    const r = await fetch('https://api.openai.com/v1/audio/speech', {
      method: 'POST',
      headers: {
        'Authorization': `Bearer ${OPENAI_API_KEY}`,
        'Content-Type': 'application/json'
      },
      body: JSON.stringify({
        model: 'tts-1-hd',
        input,
        voice,
        format: 'mp3'
      })
    });

    res.status(r.status);
    r.body.pipe(res);
  } catch (err) {
    console.error('TTS proxy failure:', err);
    res.status(500).send('TTS proxy failure');
  }
});

/* ——— STT (Whisper) ——— */
app.post('/api/openai/stt', upload.single('audio'), async (req, res) => {
  try {
    const originalPath = req.file.path;
    const webmPath = `${originalPath}.webm`;
    fs.renameSync(originalPath, webmPath); // ✅ Rename to add .webm extension

    const formData = new FormData();
    formData.append('file', fs.createReadStream(webmPath));
    formData.append('model', 'whisper-1');
    formData.append('language', 'en');

    const r = await fetch('https://api.openai.com/v1/audio/transcriptions', {
      method: 'POST',
      headers: {
        'Authorization': `Bearer ${OPENAI_API_KEY}`,
        ...formData.getHeaders()
      },
      body: formData
    });

    const result = await r.json();
    fs.unlinkSync(webmPath); // ✅ Clean up renamed file
    res.json(result);
  } catch (err) {
    console.error('STT proxy failure:', err);
    res.status(500).send('STT proxy failure');
  }
});

app.listen(PORT, () =>
  console.log(`Proxy listening on http://localhost:${PORT}`)
);
