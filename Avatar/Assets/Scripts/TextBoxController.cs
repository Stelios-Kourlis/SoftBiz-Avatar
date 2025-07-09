using System;
using System.Collections;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
// using System.Runtime.InteropServices;
// using Unity.VisualScripting;

public class TextBoxController : MonoBehaviour
{
    [SerializeField, Range(0.1f, 2f)] private float TEXT_ANIMATION_SPEED_MULTIPLIER = 0.5f;
    [SerializeField] private float RESPONSE_DURATION_PER_WORD = 0.3f;
    private float TEXT_TO_SPEECH_AUDIO_DURATION = -1f;

    [SerializeField] private GameObject textResponseObject, clickCaptureObject, thinkingTextObject;
    [SerializeField] private TalkingSimulator talkingSimulator;
    // [SerializeField] private TextAnimation responseTextAnimation;
    private TextBoxAnimator Animator => gameObject.GetComponent<TextBoxAnimator>();
    private GameObject responseObject, thinkingText;
    private Coroutine responseCoroutine;

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


    private IEnumerator CreateResponseObject(bool forceCreate = false)
    {
        if (responseObject != null)
        {
            if (!forceCreate) yield break;
            Destroy(responseObject);
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
        yield return Animator.AnimateTextBoxAppearance(responseObject);
    }
    public void ClearResponse()
    {
        Debug.Log("[ClearResponse] Called");

        if (responseObject == null)
            return;

        TMP_Text textComponent = responseObject.GetComponentInChildren<TMP_Text>();
        if (textComponent != null)
        {
            textComponent.text = "";
        }
        Animator.StopWaitForUserInput(responseObject);
    }
    public void AddToResponse(string nextSentense)
    {
        Debug.Log($"[AddToResponse] Adding: {nextSentense}");
        talkingSimulator.StopTalking();
        if (responseCoroutine != null) StopCoroutine(responseCoroutine);
        responseCoroutine = StartCoroutine(AddToResponseCor(MarkdownToTMPConverter.ConvertToTMPCompatibleText(nextSentense)));
    }

    public void ConcludeResponse()
    {
        StartCoroutine(Animator.AnimateTextBoxDisappearance(responseObject));
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

        if (responseObject != null) ConcludeResponse();

        thinkingText = Instantiate(thinkingTextObject, transform);
        StartCoroutine(thinkingText.GetComponent<TextAnimator>().AnimateTextLoop(1));
    }

    private IEnumerator AddToResponseCor(string nextSentence)
    {
        if (responseObject == null)
        {
            yield return CreateResponseObject();
        }

        TMP_Text textComponent = responseObject.GetComponentInChildren<TMP_Text>();
        TextAnimator responseTextAnimation = textComponent.GetComponent<TextAnimator>();
        Animator.StopWaitForUserInput(responseObject);

        if (textComponent == null)
        {
            Debug.LogError("TMP_Text component not found in the response object.");
            Destroy(responseObject);
            yield break;
        }

        int oldCharCount = responseTextAnimation.TmpCharCount;
        if (!TextFitsInTextBox(textComponent.text + nextSentence))
        {
            textComponent.text = string.Empty;
        }

        textComponent.text += nextSentence;
        int wordCount = nextSentence.Split(new char[] { ' ', '\n', '\t' }, System.StringSplitOptions.RemoveEmptyEntries).Length; //Get word count
        talkingSimulator.StartTalking();
        // yield return null;
        float totalTime = TEXT_TO_SPEECH_AUDIO_DURATION > 0 ? TEXT_TO_SPEECH_AUDIO_DURATION : wordCount * RESPONSE_DURATION_PER_WORD;
        if (totalTime <= 0) totalTime = 0.1f;
        float textAnimationTime = totalTime * TEXT_ANIMATION_SPEED_MULTIPLIER;
        // This makes the animation duration be textAnimationTime
        // responseTextAnimation.delayBetweenJumps = (textAnimationTime - responseTextAnimation.jumpDuration) / (responseTextAnimation.TmpCharCount - 1);
        // if (responseTextAnimation.delayBetweenJumps < 0) responseTextAnimation.delayBetweenJumps = 0.05f;
        Debug.Log("Animating text for " + textAnimationTime);
        StartCoroutine(responseTextAnimation.AnimateTextBounce(textAnimationTime, oldCharCount));
        yield return new WaitForSeconds(textAnimationTime);
        Animator.StartWaitForUserInput(responseObject);
        yield return new WaitForSeconds(totalTime - textAnimationTime); //Wait for the rest of the time
        talkingSimulator.StopTalking();
    }



    // #if !DEVELOPMENT_BUILD && !UNITY_EDITOR
    public void ForceMDTest()
    {
        string mdText = "This is a **bold** text with *italic* and ***both*** formatting.\n\n" +
            "Here is a unordered list:\n" +
            "- Item 1.\n" +
            "- Item 2.\n" +
            "- Item 3.\n\n" +
            "# And a heading 1.\n" +
            "## And a heading 2.\n" +
            "###### And a heading 6.\n" +
            "> This is a block quote.\n" +
            ">> This is a nested block quote.\n";

        AddToResponse(mdText);
    }

    public void ForceMDTest2()
    {
        string mdText = "# Starting Header.\n\n" +
            "Here is a numbered list:\n" +
            "1. Item 1.\n" +
            "2. Item 2.\n" +
            "Line:\n" +
            "---\n" +
            "Normal text.\n" +
            "` unclosed single line code\n" +
            "` * single line code` preserved\n" +
            "``` multiline\n" +
            "code block```\n";

        AddToResponse(mdText);
    }
    // #endif
}
