using System.Collections;
using UnityEngine;



public class TalkingSimulator : MonoBehaviour
{

    private enum BodyBlendSpapes
    {
        Blink = 0,
        Frown,
        Smile,
        Sad,
        RightHand,
        LeftHand,
        UrgeToLaugh,
        Laugh,
        Malicious,
        HappyEyesClosed,
        Hands1,
        Hands2
    }

    private enum EyeBlendShapes
    {
        UpDown = 0,
        LeftRight
    }

    [SerializeField] private float BLINK_DURATION = 0.2f;
    [SerializeField] private float BLINK_INTERVAL = 2f;

    [SerializeField] private float TALKING_DURATION = 0.25f;

    [SerializeField] private float EYE_MOVEMENT_DURATION = 0.25f;
    [SerializeField] private float EYE_MOVEMENT_INTERVAL = 0.25f;

    private SkinnedMeshRenderer bodySkinnedMeshRenderer, eyesSkinnedMeshRenderer;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        bodySkinnedMeshRenderer = gameObject.transform.Find("body_Applied").GetComponent<SkinnedMeshRenderer>();
        if (bodySkinnedMeshRenderer == null)
        {
            Debug.LogError("SkinnedMeshRenderer not found on body_Applied.");
            return;
        }

        eyesSkinnedMeshRenderer = gameObject.transform.Find("eyes_Applied").GetComponent<SkinnedMeshRenderer>();
        if (eyesSkinnedMeshRenderer == null)
        {
            Debug.LogError("eyesSkinnedMeshRenderer not found on eyes_Applied.");
            return;
        }

        StartCoroutine(StartBlinking());
        StartCoroutine(StartTalking());
        StartCoroutine(StartRandomEyeMovement());
    }

    IEnumerator StartBlinking()
    {
        bodySkinnedMeshRenderer.SetBlendShapeWeight((int)BodyBlendSpapes.Blink, 10f);
        while (true)
        {
            float elapsed = 0f;

            while (elapsed < BLINK_DURATION) //Lerp Close Eyes
            {
                float value = Mathf.Lerp(10, 100, Mathf.Clamp01(elapsed / BLINK_DURATION));
                bodySkinnedMeshRenderer.SetBlendShapeWeight((int)BodyBlendSpapes.Blink, value);
                elapsed += Time.deltaTime;
                yield return null;
            }

            bodySkinnedMeshRenderer.SetBlendShapeWeight((int)BodyBlendSpapes.Blink, 100);
            elapsed = 0f;

            while (elapsed < BLINK_DURATION) //Lerp Open Eyes
            {
                float value = Mathf.Lerp(100, 10, Mathf.Clamp01(elapsed / BLINK_DURATION));
                bodySkinnedMeshRenderer.SetBlendShapeWeight((int)BodyBlendSpapes.Blink, value);
                elapsed += Time.deltaTime;
                yield return null;
            }

            bodySkinnedMeshRenderer.SetBlendShapeWeight((int)BodyBlendSpapes.Blink, 10);

            yield return new WaitForSeconds(BLINK_INTERVAL);
        }

    }

    IEnumerator StartTalking()
    {
        bodySkinnedMeshRenderer.SetBlendShapeWeight((int)BodyBlendSpapes.Smile, 25f);
        bodySkinnedMeshRenderer.SetBlendShapeWeight((int)BodyBlendSpapes.Sad, 50f);
        int currentTalkValue = 0;
        int nextTalkValue;

        while (true)
        {
            do
            {
                nextTalkValue = Random.Range(0, 100);
            } while (Mathf.Abs(nextTalkValue - currentTalkValue) < 25); //Have a change of at least 25

            float elapsed = 0f;
            while (elapsed < TALKING_DURATION)
            {
                float value = Mathf.Lerp(currentTalkValue, nextTalkValue, Mathf.Clamp01(elapsed / TALKING_DURATION));
                bodySkinnedMeshRenderer.SetBlendShapeWeight((int)BodyBlendSpapes.Laugh, value);
                elapsed += Time.deltaTime;
                yield return null;
            }

            currentTalkValue = nextTalkValue;
        }
    }

    IEnumerator StartRandomEyeMovement()
    {
        (int, int) currentEyePosition = (50, 30);
        (int, int) nextEyePosition;
        while (true)
        {
            nextEyePosition = (Random.Range(0, 100), Random.Range(0, 100));

            float elapsed = 0f;
            while (elapsed < EYE_MOVEMENT_DURATION)
            {
                float value1 = Mathf.Lerp(currentEyePosition.Item1, nextEyePosition.Item1, Mathf.Clamp01(elapsed / TALKING_DURATION));
                float value2 = Mathf.Lerp(currentEyePosition.Item2, nextEyePosition.Item2, Mathf.Clamp01(elapsed / TALKING_DURATION));
                eyesSkinnedMeshRenderer.SetBlendShapeWeight((int)EyeBlendShapes.UpDown, value1);
                eyesSkinnedMeshRenderer.SetBlendShapeWeight((int)EyeBlendShapes.LeftRight, value2);
                elapsed += Time.deltaTime;
                yield return null;
            }

            currentEyePosition = nextEyePosition;

            yield return new WaitForSeconds(EYE_MOVEMENT_INTERVAL);
        }
    }
}
