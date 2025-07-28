let unityInstance = null;
let unityCanvas = null;

export function startThinking() {
    unityInstance?.SendMessage('model', 'StartThinking');
}

export function startTalking() {
    unityInstance?.SendMessage('model', 'StartTalking');
}

export function startIdle() {
    unityInstance?.SendMessage('model', 'StartIdle');
}

export function startLipSync(jsonLipSyncDataString) {
    unityInstance?.SendMessage('model', 'StartLipSync', jsonLipSyncDataString);
}

export function stopLipSync() {
    unityInstance?.SendMessage('model', 'StopLipSync');
}

export function focusCanvas() {
    unityCanvas.focus();
    console.log('Unity canvas focused');
}

export async function waitForUnity() {
    return new Promise((resolve) => {
        const check = () => {
            const frame = document.getElementById('UnityFrame');
            if (frame?.contentWindow?.unityInstance) {
                unityInstance = frame.contentWindow.unityInstance;
                unityCanvas = frame.contentWindow.unityCanvas;
                resolve();
            } else {
                setTimeout(check, 100); // check again after 100ms
            }
        };
        check();
    });
}