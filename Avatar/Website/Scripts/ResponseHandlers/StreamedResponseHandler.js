import { ResponseHandler } from './IBaseHandler.js';

export class StreamedResponseHandler extends ResponseHandler {

    static async* getResponse(conversationHistory) {
        const response = await fetch('http://localhost:3000/api/openai/chat', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({
                model: 'gpt-4o',
                messages: conversationHistory,
                stream: true
            })
        });

        const reader = response.body.getReader();
        const decoder = new TextDecoder("utf-8");
        let buffer = "";
        let doneFlag = false;

        while (true) {
            const { value, done } = await reader.read();
            if (done || doneFlag) break;

            const chunk = decoder.decode(value, { stream: true });
            buffer += chunk;

            const lines = buffer.split("\n");
            buffer = lines.pop(); // keep the last incomplete line

            for (const line of lines) {
                if (!line.trim().startsWith("data:")) continue;

                const jsonStr = line.replace("data: ", "").trim();
                if (jsonStr === "[DONE]") return;

                const data = JSON.parse(jsonStr);
                const delta = data.choices[0].delta;

                if (delta?.content) {
                    yield delta.content;
                }

                if (data.choices[0].finish_reason === "stop") {
                    doneFlag = true;
                    return;
                }
            }
        }
    }
}