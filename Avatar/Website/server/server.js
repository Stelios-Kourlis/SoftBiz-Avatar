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
import { exec } from 'child_process';
import { promisify } from 'util';
import ffmpegPath from 'ffmpeg-static';

const execAsync = promisify(exec);
const __filename = fileURLToPath(import.meta.url);
const __dirname = path.dirname(__filename);
const rhubarbPath = process.env.RHUBARB_PATH;
/* ——— load .env ——— */
dotenv.config({ path: path.join(__dirname, '.env') });

const {
  OPENAI_API_KEY,
  FRONTEND_ORIGIN = 'http://127.0.0.1:5500',
  PORT = 3000
} = process.env;

// console.log(rhubarbPath);

if (!OPENAI_API_KEY) {
  console.error('OPENAI_API_KEY missing in .env');
  process.exit(1);
}

const app = express();
const upload = multer({ dest: 'uploads/' });

app.use(cors({ origin: FRONTEND_ORIGIN }));
app.use(express.json());
app.use('/uploads', express.static(path.join(__dirname, 'uploads')));

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

    if (JSON.stringify(req.body).includes('"stream":true')) {
      res.status(r.status);
      res.setHeader('Content-Type', 'text/event-stream');

      // Stream OpenAI response to client
      if (r.body) {
        for await (const chunk of r.body) {
          res.write(chunk);
        }
      }

      res.end();
    } else {
      res.status(r.status).type('application/json').send(await r.text());
    }
  } catch (err) {
    console.error('Chat proxy failure:', err);
    res.status(500).json({ error: 'Chat proxy failure' });
  }
});

///This post is obsolete, use /api/openai/lipsync instead
// app.post('/api/openai/tts', async (req, res) => {
//   try {
//     const { input, voice = 'ash', stream = false } = req.body;

//     const r = await fetch('https://api.openai.com/v1/audio/speech', {
//       method: 'POST',
//       headers: {
//         'Authorization': `Bearer ${OPENAI_API_KEY}`,
//         'Content-Type': 'application/json'
//       },
//       body: JSON.stringify({
//         model: 'tts-1',
//         input: input,
//         voice: voice,
//         response_format: 'mp3',
//         stream_format: "audio",
//         stream: stream
//       })
//     });

//     console.log('Stream:', typeof r.body, r.body?.[Symbol.asyncIterator]);

//     if (stream) {
//       res.setHeader('Content-Type', 'audio/mpeg');
//       res.setHeader('Transfer-Encoding', 'chunked');
//       res.setHeader('Cache-Control', 'no-cache');

//       for await (const chunk of r.body) {
//         res.write(chunk); // write raw audio chunks as they come
//       }
//       res.end();
//     } else {
//       res.status(r.status);
//       r.body.pipe(res);          // stream MP3 straight through
//     }
//   } catch (err) {
//     console.error('TTS proxy failure:', err);
//     res.status(500).send('TTS proxy failure');
//   }
// });

app.post('/api/openai/lipsync', async (req, res) => {
  try {
    const { text, voice = 'ash' } = req.body;
    const uploadsDir = path.join(__dirname, 'uploads');

    if (!fs.existsSync(uploadsDir)) {
      fs.mkdirSync(uploadsDir, { recursive: true });
    }

    const mp3Path = path.join(uploadsDir, 'tts.mp3');
    const wavPath = path.join(uploadsDir, 'tts.wav');
    const visemePath = path.join(uploadsDir, 'tts.json');

    // 1. Request MP3 from OpenAI TTS
    const response = await fetch('https://api.openai.com/v1/audio/speech', {
      method: 'POST',
      headers: {
        'Authorization': `Bearer ${OPENAI_API_KEY}`,
        'Content-Type': 'application/json',
      },
      body: JSON.stringify({
        model: 'tts-1',
        voice,
        input: text,
        response_format: 'wav',
      }),
    });

    if (!response.ok) {
      throw new Error(`TTS request failed: ${response.status} ${response.statusText}`);
    }

    // 2. Save MP3 file
    await new Promise((resolve, reject) => {
      const fileStream = fs.createWriteStream(mp3Path);
      response.body.pipe(fileStream);
      response.body.on('error', reject);
      fileStream.on('finish', resolve);
    });

    // 3. Convert MP3 to WAV (mono 44.1kHz 16-bit)
    await execAsync(`"${ffmpegPath}" -y -i "${mp3Path}" -ar 44100 -ac 1 -sample_fmt s16 "${wavPath}"`);

    // 4. Run Rhubarb to get visemes
    // Adjust path to rhubarb.exe if necessary
    const rhubarbFile = path.join(rhubarbPath, 'rhubarb.exe');
    console.log('Rhubarb path:', rhubarbPath);
    console.log('Rhubarb file:', rhubarbFile);
    if (!fs.existsSync(rhubarbPath)) {
      throw new Error(`rhubarb.exe not found at ${rhubarbFile}`);
    }

    await execAsync(`"${rhubarbFile}" -f json -o "${visemePath}" "${wavPath}"`);

    // 5. Read visemes JSON
    const visemes = JSON.parse(fs.readFileSync(visemePath, 'utf8'));

    // 6. Return audio URL (MP3) and visemes
    res.json({
      audioUrl: '/uploads/tts.wav',
      visemes,
    });
  } catch (err) {
    console.error('Lipsync pipeline failed:', err);
    res.status(500).json({ error: 'Lip sync generation failed', details: err.message });
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
