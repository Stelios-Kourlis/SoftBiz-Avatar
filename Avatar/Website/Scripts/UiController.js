import * as UnityAnimationController from './UnityAnimationController.js';

export class BubbleTextController {

    static #appendQueue = [];
    static #isAnimating = false;
    static #blockAppends = false;
    static userPressedSkip = false;

    static #createBubbleText() {
        const container = document.getElementById('bubbleContainer');
        container.innerHTML = '';

        const bubble = document.createElement('div');
        bubble.className = 'speech-bubble';

        const paragraph = document.createElement('p');
        paragraph.id = 'bubble-text';
        bubble.appendChild(paragraph);
        container.appendChild(bubble);

        this.#blockAppends = false;
        this.#appendQueue = [];
        this.userPressedSkip = false;
        this.#isAnimating = false;
    }

    static appendToBubbleText(text) {
        if (!document.getElementById('bubble-text')) {
            this.#createBubbleText();
        }

        if (this.#blockAppends) return;

        this.#appendQueue.push(text);
        this.#processQueue();
    }

    static destroyBubbleText() {
        const container = document.getElementById('bubbleContainer');
        container.innerHTML = '';
    }

    static async #animateBubbleText(text, startIndex) {
        return new Promise((resolve) => {
            const paragraph = document.getElementById('bubble-text');
            let i = startIndex;

            const endAnim = (forceStop = false) => {
                clearInterval(timer);
                paragraph.innerHTML = marked.parse(text);
                UnityAnimationController.startIdle();
                ButtonController.showFinishButton();
                audioPlayer.pause();
                if (forceStop) {
                    while (this.#appendQueue.length > 0) {
                        const text = this.#appendQueue.shift();
                        paragraph.innerHTML = marked.parse(paragraph.innerHTML + text);
                    }
                    this.#blockAppends = true;
                    this.#appendQueue = [];
                    this.userPressedSkip = true;
                    console.error("Animation stopped by user");
                }
                resolve();
            };

            // document.getElementById('sendBtn').style.display = 'none';
            document.getElementById('stopBtn').onclick = () => endAnim(true);
            window.addEventListener('keydown', e => {
                if (e.key === 'Enter')
                    endAnim(true)
            }, { once: true });
            ButtonController.showSkipButton()
            UnityAnimationController.startTalking();

            const timer = setInterval(() => {
                paragraph.innerHTML = marked.parse(text.slice(0, i++));
                if (i > text.length) endAnim();
            }, 15);
        });
    }

    static async #processQueue() {
        if (this.#isAnimating || this.#appendQueue.length === 0) {
            UnityAnimationController.startIdle();
            return;
        }

        this.#isAnimating = true;

        const text = this.#appendQueue.shift();

        let paragraph = document.getElementById('bubble-text');
        if (!paragraph) {
            this.#createBubbleText();
            paragraph = document.getElementById('bubble-text');
        }

        const currentText = paragraph.innerText;
        const fullText = currentText + text;

        await this.#animateBubbleText(fullText, currentText.length, 25);

        this.#isAnimating = false;
        this.#processQueue(); // check for next
    }
}

export class ButtonController {

    static getCurrentButton() {
        const finishButton = document.getElementById('finishBtn');
        const finishButtonIsShown = !!(finishButton.offsetWidth || finishButton.offsetHeight || finishButton.getClientRects().length);
        if (finishButtonIsShown) return finishButton;

        const sendButton = document.getElementById('sendBtn');
        const sendButtonIsShown = !!(sendButton.offsetWidth || sendButton.offsetHeight || sendButton.getClientRects().length);
        if (sendButtonIsShown) return sendButton;

        const stopButton = document.getElementById('stopBtn');
        const stopButtonIsShown = !!(stopButton.offsetWidth || stopButton.offsetHeight || stopButton.getClientRects().length);
        if (stopButtonIsShown) return stopButton;
    }

    static restoreSendBtn() {
        document.getElementById('stopBtn').style.display = 'none';
        document.getElementById('finishBtn').style.display = 'none';
        document.getElementById('userInput').style.display = 'inline-block';
        document.getElementById('micBtn').style.display = 'inline-block';
        document.getElementById('stopBtn').style.display = 'none';
        const btn = document.getElementById('sendBtn');
        btn.disabled = false;
        btn.textContent = 'Send';
        btn.style.display = 'inline-block';
        BubbleTextController.destroyBubbleText();
        UnityAnimationController.startIdle();
    }

    static showFinishButton() {
        document.getElementById('sendBtn').style.display = 'none';
        document.getElementById('finishBtn').style.display = 'inline-block';
        document.getElementById('userInput').style.display = 'none';
        document.getElementById('micBtn').style.display = 'none';
        document.getElementById('stopBtn').style.display = 'none';
        document.getElementById('audioPlayer').pause();
    }

    static showSkipButton() {
        document.getElementById('sendBtn').style.display = 'none';
        document.getElementById('finishBtn').style.display = 'none';
        document.getElementById('userInput').style.display = 'none';
        document.getElementById('micBtn').style.display = 'none';
        document.getElementById('stopBtn').style.display = 'inline-block';
    }

    static disableSendButton() {
        document.getElementById('sendBtn').disabled = true;
        document.getElementById('sendBtn').textContent = 'Thinking…';
        document.getElementById('userInput').style.display = 'none';
        document.getElementById('micBtn').style.display = 'none';
        document.getElementById('stopBtn').style.display = 'none';
        document.getElementById('finishBtn').style.display = 'none';
    }
}