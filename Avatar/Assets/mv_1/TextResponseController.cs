using System;
using System.Collections;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
// using System.Runtime.InteropServices;
// using Unity.VisualScripting;

public class TextResponseController : MonoBehaviour
{
    [SerializeField] private float RESPONSE_DURATION_PER_WORD = 0.3f;
    [SerializeField] private float BOX_ANIMATION_DURATION = 0.5f;
    [SerializeField] private float ARROW_ANIMATION_DURATION = 0.5f;
    [SerializeField] private Ease ANIMATION_EASE_TYPE = Ease.OutCubic;
    [SerializeField, Range(0.1f, 2f)] private float TEXT_ANIMATION_SPEED_MULTIPLIER = 0.5f;
    [SerializeField] private int TRIANGLE_JUMP_HEIGHT_PIXELS = 30;
    private float TEXT_TO_SPEECH_AUDIO_DURATION = -1f;

    [SerializeField] private GameObject textResponseObject, clickCaptureObject, thinkingTextObject;
    [SerializeField] private TalkingSimulator talkingSimulator;
    // [SerializeField] private TextAnimation responseTextAnimation;
    private GameObject responseObject, clickForwader, thinkingText;
    private Coroutine responseCoroutine;
    private bool responsePieceConcluded = false, responseConcluded = true;

    public void Start()
    {
        if (textResponseObject == null)
        {
            Debug.LogError("TextResponseObject is not assigned.");
            return;
        }
        if (talkingSimulator == null)
        {
            Debug.LogError("TalkingSimulator is not assigned.");
            return;
        }
        if (gameObject.GetComponent<Canvas>() == null)
        {
            Debug.LogError("Please place this script on the Canvas");
            return;
        }
    }

    public void SetTTSAudioDuration(float duration)
    {
        TEXT_TO_SPEECH_AUDIO_DURATION = duration;
        Debug.Log($"[SetTTSAudioDuration] Duration set to: {TEXT_TO_SPEECH_AUDIO_DURATION}");
    }

    // public void RespondEntry(string response)
    // {
    //     if (!responseConcluded) return;
    //     responseConcluded = false;
    //     StartCoroutine(Respond(response));
    // }


    private IEnumerator CreateResponseObject(bool forceCreate = false)
    {
        if (responseObject != null)
        {
            if (!forceCreate) yield break;
            Destroy(responseObject);
            if (clickForwader != null) Destroy(clickForwader);
        }

        if (thinkingText != null)
        {
            thinkingText.GetComponent<TextAnimator>().StopAllCoroutines();
            Destroy(thinkingText);
            thinkingText = null;
        }

        // clickForwader = Instantiate(clickCaptureObject, transform); //Capture click events
        // clickForwader.transform.SetAsFirstSibling();
        responseObject = Instantiate(textResponseObject, transform);
        responseObject.GetComponentInChildren<TMP_Text>().text = string.Empty; //Clear the text initially
        yield return AnimateTextBox();
    }
    public void AddToResponse(string nextSentense)
    {
        Debug.Log($"[AddToResponse] Adding: {nextSentense}");
        Debug.Log($"[AddToResponse] Call stack:\n{Environment.StackTrace}");
        if (responseCoroutine != null) StopCoroutine(responseCoroutine);
        responseCoroutine = StartCoroutine(AddToResponseCor(nextSentense));
    }

    public void ConcludeResponse()
    {
        if (responseObject != null) Destroy(responseObject);
        if (clickForwader != null) Destroy(clickForwader);

        talkingSimulator.StopTalking();
    }

    private bool TextFitsInTextBox(string text)
    {
        TMP_Text textComponent = responseObject.GetComponentInChildren<TMP_Text>();
        RectTransform tmpRectTransform = textComponent.GetComponent<RectTransform>();

        Vector2 sizeRestraints = textComponent.GetPreferredValues(text, tmpRectTransform.rect.width, tmpRectTransform.rect.height);

        bool fitsWidth = sizeRestraints.x <= tmpRectTransform.rect.width;
        bool fitsHeight = sizeRestraints.y <= tmpRectTransform.rect.height;

        return fitsHeight && fitsWidth;

    }

    public void Think()
    {
        if (thinkingText != null)
        {
            thinkingText.GetComponent<TextAnimator>().StopAllCoroutines();
            Destroy(thinkingText);
            thinkingText = null;
        }

        ConcludeResponse();

        thinkingText = Instantiate(thinkingTextObject, transform);
        StartCoroutine(thinkingText.GetComponent<TextAnimator>().AnimateTextLoop());
    }

    private IEnumerator AnimateTextBox()
    {
        RectTransform boxRectTransform = responseObject.transform.Find("Box").GetComponent<RectTransform>();
        RectTransform arrowRectTransform = responseObject.transform.Find("Arrow").GetComponent<RectTransform>();

        boxRectTransform.localScale = Vector3.zero;
        boxRectTransform.rotation = Quaternion.Euler(0, 0, 90);
        arrowRectTransform.gameObject.SetActive(false);

        Tween tween = boxRectTransform.DOScale(Vector3.one, BOX_ANIMATION_DURATION).SetEase(ANIMATION_EASE_TYPE);
        boxRectTransform.DORotate(Vector3.zero, BOX_ANIMATION_DURATION).SetEase(ANIMATION_EASE_TYPE);
        yield return new WaitForSecondsRealtime(BOX_ANIMATION_DURATION / 2);

        arrowRectTransform.gameObject.SetActive(true);
        arrowRectTransform.rotation = Quaternion.Euler(0, 0, 180);
        arrowRectTransform.localScale = Vector3.zero;

        arrowRectTransform.DORotate(new Vector3(0, 0, 270), ARROW_ANIMATION_DURATION).SetEase(ANIMATION_EASE_TYPE);
        arrowRectTransform.DOScale(new Vector3(3, 3, 3), BOX_ANIMATION_DURATION).SetEase(ANIMATION_EASE_TYPE);
        yield return tween.WaitForCompletion();
    }

    private IEnumerator AddToResponseCor(string nextSentence)
    {
        if (responseObject == null)
        {
            yield return CreateResponseObject();
        }

        TMP_Text textComponent = responseObject.GetComponentInChildren<TMP_Text>();
        TextAnimator responseTextAnimation = textComponent.GetComponent<TextAnimator>();

        if (textComponent == null)
        {
            Debug.LogError("TMP_Text component not found in the response object.");
            Destroy(responseObject);
            yield break;
        }

        Debug.Log("Text before check: " + textComponent.text);

        int oldCharCount = responseTextAnimation.TmpCharCount;
        if (!TextFitsInTextBox(textComponent.text + nextSentence))
        {
            textComponent.text = string.Empty;
        }

        Debug.Log("Text after check: " + textComponent.text);

        textComponent.text += nextSentence;
        Debug.Log("Text after add: " + textComponent.text);
        int wordCount = nextSentence.Split(new char[] { ' ', '\n', '\t' }, System.StringSplitOptions.RemoveEmptyEntries).Length; //Get word count
        talkingSimulator.StartTalking();
        yield return null;
        float totalTime = TEXT_TO_SPEECH_AUDIO_DURATION > 0 ? TEXT_TO_SPEECH_AUDIO_DURATION : wordCount * RESPONSE_DURATION_PER_WORD;
        float textAnimationTime = totalTime * TEXT_ANIMATION_SPEED_MULTIPLIER;
        // This makes the animation duration be totalTime
        responseTextAnimation.delayBetweenJumps = (textAnimationTime - responseTextAnimation.jumpDuration) / (responseTextAnimation.TmpCharCount - 1);
        StartCoroutine(responseTextAnimation.AnimateTextOnce(oldCharCount));
        // StartCoroutine(WaitForUserClick());
        yield return new WaitForSeconds(totalTime); //Wait for the rest of the time
        talkingSimulator.StopTalking();
    }
    // private IEnumerator WaitForUserClick()
    // {
    //     RectTransform triangleRectTransform = responseObject.transform.Find("Triangle").GetComponent<RectTransform>();
    //     triangleRectTransform.gameObject.SetActive(true);
    //     Vector2 trianglePos = triangleRectTransform.anchoredPosition;

    //     while (true)
    //     {
    //         // triangleRectTransform.anchoredPosition = new(-50, 50);
    //         if (responsePieceConcluded || triangleRectTransform == null || !triangleRectTransform.gameObject.activeInHierarchy) //If the response piece is concluded, stop the coroutine
    //         {
    //             if (triangleRectTransform != null) triangleRectTransform.gameObject.SetActive(false);
    //             yield break;
    //         }

    //         Tween tween = triangleRectTransform.DOAnchorPos(trianglePos - new Vector2(0, TRIANGLE_JUMP_HEIGHT_PIXELS), 0.5f).SetEase(Ease.InQuad);
    //         yield return tween.WaitForCompletion();
    //         tween = triangleRectTransform.DOAnchorPos(trianglePos, 0.5f).SetEase(Ease.OutQuad);
    //         yield return tween.WaitForCompletion();
    //     }
    // }

    // public void OnClick(PointerEventData eventData)
    // {
    //     talkingSimulator.StopTalking(); //If user prematurely stops the response also stop the talking
    //     Application.ExternalCall("ReceiveMessageFromUnity", "next");
    //     if (responseCoroutine != null)
    //     {
    //         // Debug.Log("Ending response cor.");
    //         StopCoroutine(responseCoroutine); //Refrence the exact coroutine
    //         responseCoroutine = null;
    //         responsePieceConcluded = true;
    //     }
    // }
}
