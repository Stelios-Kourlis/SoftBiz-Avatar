import { ResponseHandler } from './IBaseHandler.js';

export class NonStreamedResponseHandler extends ResponseHandler {
    static async getResponse(conversationHistory) {
        const response = await fetch('http://localhost:3000/api/openai/chat', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({
                model: 'gpt-4o',
                messages: conversationHistory,
                stream: false
            })
        });

        if (!response.ok) {
            console.error('Chat error:', await response.text());
            return null;
        }

        const chatData = await response.json();
        return chatData.choices?.[0]?.message?.content || '(empty reply)';
    }

    static async getTTSAudio(text) {
        const speech = await fetch('http://localhost:3000/api/openai/tts', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ input: text, voice: 'ash' }) // male
        });

        if (!speech.ok) {
            console.error('TTS error:', await speech.text());
            return null;
        }

        return await speech.blob();
    }
}