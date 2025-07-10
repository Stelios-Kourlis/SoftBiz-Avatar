using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
// using System.Runtime.InteropServices;
// using Unity.VisualScripting;

public class TextBoxController : MonoBehaviour
{

#if UNITY_WEBGL && !UNITY_EDITOR
        [DllImport("__Internal")]
        private static extern void ReceiveMessageFromUnity(string str);
        [DllImport("__Internal")]
        private static extern void SendCurrentIndexOutOfTotal(int index, int total);
#endif


    [SerializeField, Range(0.1f, 2f)] private float TEXT_ANIMATION_SPEED_MULTIPLIER = 0.5f;
    [SerializeField] private float RESPONSE_DURATION_PER_WORD = 0.3f;
    private float TEXT_TO_SPEECH_AUDIO_DURATION = -1f;

    [SerializeField] private GameObject textResponseObject, clickCaptureObject, thinkingTextObject;
    [SerializeField] private TalkingSimulator talkingSimulator;
    // [SerializeField] private TextAnimation responseTextAnimation;
    private TextBoxAnimator Animator => gameObject.GetComponent<TextBoxAnimator>();
    private GameObject responseObject, thinkingText;
    private Coroutine responseCoroutine;
    private bool showNextPart = false, showPreviousPart = false, TTSLoaded = false;
    List<string> responseSentences;

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

    public void ShowNextPart()
    {
        showNextPart = true;
    }

    public void ShowPreviousPart()
    {
        showPreviousPart = true;
    }

    public void SetTTSAsLoaded()
    {
        TTSLoaded = true;
    }

    private void BreakResponseIntoSentences(string responseText)
    {
        string piece = GetMaxTextBeforeOverflow(responseText, out string remainingText);
        responseSentences = new List<string>();
        while (piece != string.Empty)
        {
            Debug.Log("Showing next part: " + piece + "\n[Remaining]: " + remainingText);
            responseSentences.Add(piece);
            responseText = remainingText;
            piece = GetMaxTextBeforeOverflow(responseText, out remainingText);
        }
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
    public void AddToResponse(string allText)
    {
        Debug.Log($"[AddToResponse] Adding: {allText}");
        allText = MarkdownToTMPConverter.ConvertToTMPCompatibleText(allText);
        talkingSimulator.StopTalking();
        StartCoroutine(RespondAndWaitForInput(allText));
        // if (responseCoroutine != null) StopCoroutine(responseCoroutine);
        // responseCoroutine = StartCoroutine(AddToResponseCor(allText));
    }

    private IEnumerator RespondAndWaitForInput(string allText)
    {
        if (responseObject == null) yield return CreateResponseObject();
        BreakResponseIntoSentences(allText);
        int pieceIndex = 0;
        while (true)
        {
            string piece = responseSentences[pieceIndex];
            Debug.Log("Showing next part: " + piece);
#if UNITY_WEBGL && !UNITY_EDITOR
            ReceiveMessageFromUnity(MarkdownToTMPConverter.RemoveAllRichTextTags(piece)); //Send TTS ready script
            yield return new WaitUntil(() => TTSLoaded);
            SendCurrentIndexOutOfTotal(pieceIndex, responseSentences.Count);
            Debug.Log("Unity TTS Loaded");
            TTSLoaded = false;
#endif
            responseObject.transform.Find("Progress").GetComponent<TMP_Text>().text = $"{pieceIndex + 1}/{responseSentences.Count}";
            if (responseCoroutine != null) StopCoroutine(responseCoroutine);
            responseCoroutine = StartCoroutine(AddToResponseCor(piece));
            yield return new WaitUntil(() => showNextPart || showPreviousPart);
            TMP_Text textComponent = responseObject.GetComponentInChildren<TMP_Text>();
            TextAnimator responseTextAnimation = textComponent.GetComponent<TextAnimator>();
            // if (showPreviousPart)
            // {
            //     pieceIndex--;
            //     if (pieceIndex < 0) pieceIndex = 0;
            //     if (responseTextAnimation.isAnimationRunning)
            //     {
            //         responseTextAnimation.StopAnimatingTextBounce(); //If next mid sentence finish that sentence
            //         showNextPart = false;
            //         showPreviousPart = false;
            //     }
            //     continue;
            // }
            if (responseTextAnimation.isAnimationRunning)
            {
                responseTextAnimation.StopAnimatingTextBounce(); //If next mid sentence finish that sentence
                showNextPart = false;
                showPreviousPart = false;
                yield return new WaitUntil(() => showNextPart || showPreviousPart);
            }
            ClearResponse();
            if (showPreviousPart)
            {
                pieceIndex--;
                if (pieceIndex < 0) pieceIndex = 0;
            }
            else
            {
                pieceIndex++;
                if (pieceIndex >= responseSentences.Count)
                {
#if UNITY_WEBGL && !UNITY_EDITOR
                    SendCurrentIndexOutOfTotal(pieceIndex, responseSentences.Count);
#endif
                    StartCoroutine(Animator.AnimateTextBoxDisappearance(responseObject));
                    yield break;
                }
            }
            Debug.Log("Showing next piece");
            showNextPart = false;
            showPreviousPart = false;
            yield return null;
        }


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

    private string GetMaxTextBeforeOverflow(string allText, out string remainingText)
    {
        string currentText = string.Empty;
        string[] responseSentences = allText.Split(new char[] { '.', '!', '?' }, System.StringSplitOptions.RemoveEmptyEntries);
        int index = 0;

        while (index < responseSentences.Length)
        {
            string testPiece = currentText + responseSentences[index] + ". ";
            if (!TextFitsInTextBox(testPiece))
            {
                remainingText = string.Join(". ", responseSentences, index, responseSentences.Length - index);
                return currentText;
            }
            else currentText = testPiece;
            index++;
        }
        remainingText = string.Join(". ", responseSentences, index, responseSentences.Length - index);
        return currentText;

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
        // if (responseObject == null)
        // {
        //     yield return CreateResponseObject();
        // }

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

        textComponent.text = nextSentence;
        int wordCount = nextSentence.Split(new char[] { ' ', '\n', '\t' }, System.StringSplitOptions.RemoveEmptyEntries).Length; //Get word count
        talkingSimulator.StartTalking();
        // yield return null;
        float totalTime = TEXT_TO_SPEECH_AUDIO_DURATION > 0 ? TEXT_TO_SPEECH_AUDIO_DURATION : wordCount * RESPONSE_DURATION_PER_WORD;
        if (totalTime <= 0) totalTime = 0.1f;
        float textAnimationTime = totalTime * TEXT_ANIMATION_SPEED_MULTIPLIER;
        Debug.Log("Animating text for " + textAnimationTime + "[TEXT:] " + nextSentence);
        responseTextAnimation.AnimateTextBouncing(textAnimationTime, oldCharCount);
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
            "*   **Properties:**\n    *   **High Strength:** Very strong for its weight, especially tensile strength (resistance to being pulled apart).\n" +
            "Normal text.\n" +
            "` unclosed single line code\n" +
            "` * single line code` preserved\n" +
            "``` multiline\n" +
            "code block```\n";

        AddToResponse(mdText);
    }
    // #endif
}
