import * as UnityAnimationController from './UnityAnimationController.js';

export class BubbleTextController {

    static #appendQueue = [];
    static #isAnimating = false;
    static #blockAppends = false;
    static userPressedSkip = false;
    static #cache = "";
    static #timer = null;
    static #fullResponse = "";

    static isShowing() {
        return !!document.getElementById('bubble-text');
    }

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
        this.#fullResponse = "";
    }

    static appendToBubbleText(text) {
        if (!document.getElementById('bubble-text')) {
            this.#createBubbleText();
        }

        if (this.#blockAppends) return;

        this.#appendQueue.push(text);
        this.#fullResponse += text;
        this.#processQueue();
    }

    static #endAnim = (forceStop = false) => {
        clearInterval(this.#timer);
        window.removeEventListener('keydown', this.#onEnterKey);
        const paragraph = document.getElementById('bubble-text');
        if (!paragraph) return;
        UnityAnimationController.startIdle();
        ButtonController.showFinishButton();
        //audioPlayer.pause();
        if (forceStop) {
            document.getElementById('audioPlayer').pause();
            paragraph.innerHTML = marked.parse(this.#fullResponse);
            this.#blockAppends = true;
            this.#appendQueue = [];
            this.userPressedSkip = true;
            console.error("Animation stopped by user");
        }
    };

    static #onEnterKey = (e) => {
        window.removeEventListener('keydown', this.#onEnterKey);
        if (e.key === 'Enter') this.#endAnim(true);
    };

    static destroyBubbleText() {
        const container = document.getElementById('bubbleContainer');
        container.innerHTML = '';
    }

    static async #animateBubbleText(text, startIndex) {
        return new Promise((resolve) => {
            const paragraph = document.getElementById('bubble-text');
            let i = startIndex;

            document.getElementById('stopBtn').onclick = () => this.#endAnim(true);
            window.addEventListener('keydown', this.#onEnterKey);
            ButtonController.showSkipButton()

            this.#timer = setInterval(() => {
                paragraph.innerHTML = marked.parse(text.slice(0, i++));
                if (i > text.length) {
                    console.log("Animation finished");
                    this.#endAnim();
                    resolve();
                }
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

    static cacheText() {
        let paragraph = document.getElementById('bubble-text');
        if (this.#appendQueue.length > 0) this.#endAnim(true);
        console.log("Caching text", paragraph.innerText);
        this.#cache = paragraph ? paragraph.innerText : "";
        this.destroyBubbleText();
    }

    static restoreCachedText() {
        if (this.isShowing()) return; //Aleady text showing
        if (!this.#cache || this.#cache == "") return; //No cache to restore
        console.log("Restoring cached text", this.#cache);
        this.#createBubbleText();
        let paragraph = document.getElementById('bubble-text');
        paragraph.innerHTML = marked.parse(this.#cache);
    }

    static flushCache() {
        console.log("Flushing cache", this.#cache);
        this.#cache = "";

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
        console.log("Restoring send button");
        document.getElementById('stopBtn').style.display = 'none';
        document.getElementById('finishBtn').style.display = 'none';
        document.getElementById('userInput').style.display = 'inline-block';
        document.getElementById('userInput').focus();
        document.getElementById('micBtn').style.display = 'inline-block';
        document.getElementById('stopBtn').style.display = 'none';
        const btn = document.getElementById('sendBtn');
        btn.disabled = false;
        btn.textContent = 'Send';
        btn.style.display = 'inline-block';
        BubbleTextController.destroyBubbleText();
        BubbleTextController.flushCache();
        UnityAnimationController.startIdle();
    }

    static showFinishButton() {
        console.trace("Showing finish button");
        document.getElementById('sendBtn').style.display = 'none';
        document.getElementById('finishBtn').style.display = 'inline-block';
        document.getElementById('userInput').style.display = 'none';
        document.getElementById('micBtn').style.display = 'none';
        document.getElementById('stopBtn').style.display = 'none';
        //document.getElementById('audioPlayer').pause();
    }

    static showSkipButton() {
        console.trace("Showing skip button");
        document.getElementById('sendBtn').style.display = 'none';
        document.getElementById('finishBtn').style.display = 'none';
        document.getElementById('userInput').style.display = 'none';
        document.getElementById('micBtn').style.display = 'none';
        document.getElementById('stopBtn').style.display = 'inline-block';
    }

    static disableSendButton() {
        console.trace("Disabling send button");
        document.getElementById('sendBtn').disabled = true;
        document.getElementById('sendBtn').textContent = 'Thinking…';
        document.getElementById('userInput').style.display = 'none';
        document.getElementById('micBtn').style.display = 'none';
        document.getElementById('stopBtn').style.display = 'none';
        document.getElementById('finishBtn').style.display = 'none';
    }
}