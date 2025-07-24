// server.js  (Node ≥18, ES modules)
import path from 'path';
import { fileURLToPath } from 'url';
import express from 'express';
import dotenv from 'dotenv';
import cors from 'cors';
import multer from 'multer';
import fs from 'fs';
import { exec } from 'child_process';
import { promisify } from 'util';
import ffmpegPath from 'ffmpeg-static';
import OpenAI from 'openai';
const execAsync = promisify(exec);
const __filename = fileURLToPath(import.meta.url);
const __dirname = path.dirname(__filename);
/* ——— load .env file ——— */
dotenv.config({ path: path.join(__dirname, '.env') });
const rhubarbExePath = path.join(process.env.RHUBARB_PATH ?? "", "rhubarb.exe");
const { OPENAI_API_KEY, FRONTEND_ORIGIN = 'http://127.0.0.1:5500', PORT = 3000 } = process.env;
if (!OPENAI_API_KEY) {
    console.error('OPENAI_API_KEY missing in .env');
    process.exit(1);
}
if (!process.env.RHUBARB_PATH) {
    console.error('RHUBARB_PATH missing in .env or system environment variables');
    process.exit(1);
}
const app = express();
const upload = multer({ dest: 'uploads/' });
//This expects a const named exactly OPENAI_API_KEY
//If the name is ever changed pass it excplicity like this
//const client = new OpenAI({
//  apiKey: process.env['KEY_NAME'],
//});
const openai = new OpenAI();
app.use(cors({ origin: FRONTEND_ORIGIN }));
app.use(express.json());
app.use('/uploads', express.static(path.join(__dirname, 'uploads')));
function printCurrentTime(message = '') {
    const now = new Date();
    const timeString = now.toTimeString().split(' ')[0] + '.' + now.getMilliseconds().toString().padStart(3, '0');
    console.log(message + timeString);
}
/* ——— CHAT ——— */
// This endpoint is used for text responses only using Chat Completions
// It expected a request with a body containing a "messages" array
// An optional bool "stream" paramater may be supplied to enable streaming responses (default: false)
// It returns an entire JSON response from OpenAI or an Async Generator depending on the "stream" parameter
app.post('/api/openai/chat', async (req, res) => {
    if (req.body.stream)
        return getChatResponseStreamed(req.body.messages, res);
    return getChatResponseNonStreamed(req.body.messages, res);
});
async function* getChatResponseStreamed(conversationHistory, res) {
    try {
        const r = await openai.chat.completions.create({
            messages: conversationHistory,
            model: "gpt-4o-mini",
            modalities: ["text"],
            stream: true,
        });
        if (!r) {
            console.error("Invalid initial response from OpenAI API:", r);
            yield { error: "Invalid response from OpenAI API" };
            return;
        }
        res.status(200);
        res.setHeader('Content-Type', 'text/event-stream');
        res.setHeader('Cache-Control', 'no-cache');
        res.setHeader('Connection', 'keep-alive');
        for await (const chunk of r) {
            if (chunk.choices[0].finish_reason === 'stop') {
                res.end();
                break;
            }
            console.log(chunk.choices[0].delta.content);
            const content = chunk.choices[0].delta.content;
            if (content === null || content === undefined) {
                yield { error: 'Stream content response is undefined or null' };
                return;
            }
            yield content;
        }
    }
    catch (err) {
        console.error('Chat proxy failure:', err);
        yield { error: 'Chat proxy failure' };
    }
}
async function getChatResponseNonStreamed(conversationHistory, res) {
    try {
        const r = await openai.chat.completions.create({
            messages: conversationHistory,
            model: "gpt-4o-mini",
            modalities: ["text"],
            stream: false,
        });
        if (!r) {
            console.error("Invalid initial response from OpenAI API:", r);
            return res.status(500).json({ error: "Invalid response from OpenAI API" });
        }
        if (!r.choices || r.choices.length === 0) {
            console.error("Invalid unstreamed response from OpenAI API:", r);
            return res.status(500).json({ error: "Invalid response from OpenAI API" });
        }
        return res.status(200).type('application/json').send(r.choices?.[0]?.message?.content);
    }
    catch (err) {
        console.error('Chat proxy failure:', err);
        return res.status(500).json({ error: 'Chat proxy failure' });
    }
}
//This endpoint is used for responses with lipsync data
// It expected a request with a body containing a "messages" array
// An optional "voice" parameter may be supplied to change the voice used for TTS (default: "ash")
//It returns a json with the path to the .wav audio file ("audioUrl"), the RhubarbLipSync output ("lipSyncData"), and the transcript text ("transcript")
app.post('/api/openai/lipsync', async (req, res) => {
    try {
        const uploadsDir = path.join(__dirname, 'uploads');
        if (!fs.existsSync(uploadsDir)) {
            fs.mkdirSync(uploadsDir, { recursive: true });
        }
        const wavPathUnproccesed = path.join(uploadsDir, 'tts_unproc.wav');
        const wavPath = path.join(uploadsDir, 'tts.wav');
        const visemePath = path.join(uploadsDir, 'tts.json');
        const transcriptPath = path.join(uploadsDir, 'tts.txt');
        console.log("Messages", req.body.messages);
        printCurrentTime("[Sending request]");
        const response = await openai.chat.completions.create({
            messages: req.body.messages,
            model: "gpt-4o-mini-audio-preview",
            modalities: ["text", "audio"],
            audio: {
                format: "wav",
                voice: req.body.voice || "ash",
            },
        });
        printCurrentTime("[Response Received]");
        if (!response) {
            console.error("No available response from OpenAI API:");
            return res.status(500).json({ error: "Invalid response from OpenAI API" });
        }
        // Explicity check for all required fields in the response
        if (!response.choices
            || response.choices.length === 0
            || response.choices[0].message.audio === undefined
            || response.choices[0].message.audio?.data === undefined
            || response.choices[0].message.audio?.transcript === undefined) {
            console.error("Invalid response structure from OpenAI API:", response);
            return res.status(500).json({ error: "Invalid response structure from OpenAI API" });
        }
        //Save the transcript so it gets passed to Rhubarb as well
        fs.writeFileSync(transcriptPath, response.choices[0].message.audio.transcript);
        printCurrentTime("[Transcript Saved]");
        //Save the audio from the response
        const audioData = response.choices[0].message.audio.data;
        const buffer = Buffer.from(audioData, 'base64');
        fs.writeFileSync(wavPathUnproccesed, buffer);
        printCurrentTime("[Audio Saved]");
        // 4. Run Rhubarb to get visemes
        // The files needs to be WAV, pcm_s16le, 44100Hz for rhubarb to pick it up correctly
        await execAsync(`"${ffmpegPath}" -y -i "${wavPathUnproccesed}" -acodec pcm_s16le -ar 44100 "${wavPath}"`);
        printCurrentTime("[ffmpeg Conversion Done]");
        //parameters in order: -f: Output Format, -o: Output file, -d: Transcript file, --extendedShapes: Extended viseme shapes
        //For more info see: https://github.com/DanielSWolf/rhubarb-lip-sync?tab=readme-ov-file#options
        await execAsync(`"${rhubarbExePath}" "${wavPath}" -f json -o "${visemePath}" -d "${transcriptPath}" --extendedShapes GX `);
        printCurrentTime("[Rhubarb Completed]");
        // 5. Read visemes JSON
        const visemes = JSON.parse(fs.readFileSync(visemePath, 'utf8'));
        // 6. Return audio URL (MP3) and visemes
        res.json({
            audioUrl: '/uploads/tts.wav', // This is the processed WAV file
            lipSyncData: visemes,
            transcript: response.choices[0].message.audio.transcript
        });
    }
    catch (err) {
        console.error('Lipsync pipeline failed:', err);
        if (err instanceof Error)
            res.status(500).json({ error: 'Lip sync generation failed' + err.message });
        else
            res.status(500).json({ error: 'Lip sync generation failed due to an unknown error' });
    }
});
/* ——— STT (Whisper) ——— */
//This endpoint is used for speech-to-text transcription
// It expects a request with a body that has a path to the audio file name "audio"
// It returns the transcription result from OpenAI Whisper as text
app.post('/api/openai/stt', upload.single('audio'), async (req, res) => {
    try {
        if (!req.file) {
            return res.status(400).json({ error: 'No file uploaded' });
        }
        const originalPath = req.file.path;
        const webmPath = `${originalPath}.webm`;
        fs.renameSync(originalPath, webmPath); // ✅ Rename to add .webm extension
        const r = await openai.audio.transcriptions.create({
            model: 'whisper-1',
            language: 'en',
            file: fs.createReadStream(webmPath),
        });
        const result = r;
        console.log("STT RESULT: ", result);
        fs.unlinkSync(webmPath); // ✅ Clean up renamed file
        res.json(result);
    }
    catch (err) {
        console.error('STT proxy failure:', err);
        res.status(500).send('STT proxy failure');
    }
});
app.listen(PORT, () => console.log(`Proxy listening on http://localhost:${PORT}`));
