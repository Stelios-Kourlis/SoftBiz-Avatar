let unityInstance = null;

export function startThinking() {
    unityInstance?.SendMessage('model', 'StartThinking');
    console.log("UnityAnimationController: StartThinking called");
}

export function startTalking() {
    unityInstance?.SendMessage('model', 'StartTalking');
}

export function startIdle() {
    unityInstance?.SendMessage('model', 'StartIdle');
}

export async function waitForUnity() {
    return new Promise((resolve) => {
        const check = () => {
            const frame = document.getElementById('UnityFrame');
            if (frame?.contentWindow?.unityInstance) {
                unityInstance = frame.contentWindow.unityInstance;
                resolve();
            } else {
                setTimeout(check, 100); // check again after 100ms
            }
        };
        check();
    });
}