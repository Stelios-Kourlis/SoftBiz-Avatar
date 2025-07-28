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
    console.log(message + " " + timeString);
}
//This endpoint is used for responses with lipsync data
// It expected a request with a body containing a "messages" array
// An optional "voice" parameter may be supplied to change the voice used for TTS (default: "ash")
//It returns a json with the path to the .wav audio file ("audioUrl"), the RhubarbLipSync output ("lipSyncData"), and the transcript text ("transcript")
app.post('/api/openai/lipsync', upload.single('audio'), async (req, res) => {
    try {
        const uploadsDir = path.join(__dirname, 'uploads');
        if (!fs.existsSync(uploadsDir)) {
            fs.mkdirSync(uploadsDir, { recursive: true });
        }
        const outputWavPathUnproccesed = path.join(uploadsDir, 'tts_unproc.wav');
        const outputWavPath = path.join(uploadsDir, 'tts.wav');
        const visemePath = path.join(uploadsDir, 'tts.json');
        const transcriptPath = path.join(uploadsDir, 'tts.txt');
        let base64Audio = null;
        if (req.file) { //If audio instead of text is sent, handle it accordingly
            console.log("Audio file received:", req.file.path);
            const inputWavPath = path.join(uploadsDir, 'tts_input.wav');
            const audioBase64Path = path.join(uploadsDir, 'tts_input_base64.txt');
            await execAsync(`"${ffmpegPath}" -y -i "${req.file.path}" "${inputWavPath}"`);
            const wavBuffer = fs.readFileSync(inputWavPath);
            base64Audio = wavBuffer.toString('base64');
            fs.writeFileSync(audioBase64Path, base64Audio);
            if (typeof req.body.messages === 'string') {
                req.body.messages = JSON.parse(req.body.messages);
            }
            req.body.messages[req.body.messages.length - 1].content = [
                {
                    type: 'text',
                    text: 'The user gave this audio as input instead of text'
                }, {
                    type: 'input_audio',
                    input_audio: {
                        data: base64Audio,
                        format: 'wav'
                    }
                }
            ];
            fs.unlinkSync(req.file.path);
            fs.unlinkSync(inputWavPath);
        }
        // console.log("Messages", req.body.messages);
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
        console.log("Response:", JSON.stringify(response, null, 2));
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
            console.error("Response:", JSON.stringify(response, null, 2));
            if (response.choices[0].message.content) {
                return res.json({
                    audioUrl: null,
                    lipSyncData: null,
                    transcript: response.choices[0].message.content
                });
            }
            return res.status(500).json({ error: "Invalid response structure from OpenAI API" });
        }
        //Save the transcript so it gets passed to Rhubarb as well
        fs.writeFileSync(transcriptPath, response.choices[0].message.audio.transcript);
        printCurrentTime("[Transcript Saved]");
        //Save the audio from the response
        const audioData = response.choices[0].message.audio.data;
        const buffer = Buffer.from(audioData, 'base64');
        fs.writeFileSync(outputWavPathUnproccesed, buffer);
        printCurrentTime("[Audio Saved]");
        // 4. Run Rhubarb to get visemes
        // The files needs to be WAV, pcm_s16le, 44100Hz for rhubarb to pick it up correctly
        await execAsync(`"${ffmpegPath}" -y -i "${outputWavPathUnproccesed}" -acodec pcm_s16le -ar 44100 "${outputWavPath}"`);
        printCurrentTime("[ffmpeg Conversion Done]");
        //parameters in order: -f: Output Format, -o: Output file, -d: Transcript file, --extendedShapes: Extended viseme shapes
        //For more info see: https://github.com/DanielSWolf/rhubarb-lip-sync?tab=readme-ov-file#options
        await execAsync(`"${rhubarbExePath}" "${outputWavPath}" -f json -o "${visemePath}" -d "${transcriptPath}" --extendedShapes GX `);
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
app.listen(PORT, () => console.log(`Proxy listening on http://localhost:${PORT}`));
