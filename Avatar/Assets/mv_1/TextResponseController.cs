using System.Collections;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using System.Runtime.InteropServices;
using Unity.VisualScripting;

public class TextResponseController : MonoBehaviour
{

#if UNITY_WEBGL && !UNITY_EDITOR
    [DllImport("__Internal")]
    private static extern void ReceiveMessageFromUnity(string message); //defined in JS
#endif

    [SerializeField] private float RESPONSE_DURATION_PER_WORD = 0.3f;
    [SerializeField] private float BOX_ANIMATION_DURATION = 0.5f;
    [SerializeField] private float ARROW_ANIMATION_DURATION = 0.5f;
    [SerializeField] private Ease ANIMATION_EASE_TYPE = Ease.OutCubic;
    [SerializeField, Range(0.1f, 2f)] private float TEXT_ANIMATION_SPEED_MULTIPLIER = 0.5f;
    [SerializeField] private int TRIANGLE_JUMP_HEIGHT_PIXELS = 30;
    private float TEXT_TO_SPEECH_AUDIO_DURATION = -1f;

    [SerializeField] private GameObject textResponseObject, clickCaptureObject, thinkingTextObject;
    [SerializeField] private TalkingSimulator talkingSimulator;
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
    }

    public void RespondEntry(string response)
    {
        if (!responseConcluded) return;
        responseConcluded = false;
        StartCoroutine(Respond(response));
    }

    public void Think()
    {
        if (thinkingText != null)
        {
            thinkingText.GetComponent<TextAnimation>().StopAllCoroutines();
            Destroy(thinkingText);
            thinkingText = null;
        }

        thinkingText = Instantiate(thinkingTextObject, transform);
        StartCoroutine(thinkingText.GetComponent<TextAnimation>().AnimateTextLoop());
    }


    private IEnumerator Respond(string response)
    {
        if (string.IsNullOrEmpty(response))
        {
            yield break;
        }


        if (thinkingText != null)
        {
            thinkingText.GetComponent<TextAnimation>().StopAllCoroutines();
            Destroy(thinkingText);
            thinkingText = null;
        }

        yield return AnimateTextBox(); //Animate the text box
        if (clickForwader == null)
        {
            clickForwader = Instantiate(clickCaptureObject, transform); //Capture click events
        }
        // responseConcluded = false;
        clickForwader.GetComponent<ClickForwader>().OnClick += OnClick;
        TMP_Text textComponent = responseObject.GetComponentInChildren<TMP_Text>();
        RectTransform tmpRectTransform = textComponent.GetComponent<RectTransform>();
        string[] responseSentences = response.Split(new char[] { '.', '!', '?' }, System.StringSplitOptions.RemoveEmptyEntries);
        string responsePiece = string.Empty;
        bool fitsWidth = true, fitsHeight = true;
        int index = 0;
        yield return null; //Wait for the text component to be initialized

        while (index < responseSentences.Length) //Keep adding sentences until they don't fit or the text runs out
        {
            string nextSentence = responseSentences[index].Trim() + ". ";

            string testPiece = responsePiece + nextSentence;

            Vector2 sizeRestraints = textComponent.GetPreferredValues(testPiece, tmpRectTransform.rect.width, tmpRectTransform.rect.height);

            fitsWidth = sizeRestraints.x <= tmpRectTransform.rect.width;
            fitsHeight = sizeRestraints.y <= tmpRectTransform.rect.height;

            if (fitsWidth && fitsHeight)
            {
                responsePiece = testPiece;
                index++;
            }
            else
            {
                responseCoroutine = StartCoroutine(RespondPiece(responsePiece));
                yield return new WaitUntil(() => responsePieceConcluded);
                responsePieceConcluded = false; //Reset the flag for the next piece
                responsePiece = string.Empty; //Reset the response piece
                responseObject.SetActive(false);
                yield return new WaitForSecondsRealtime(0.1f);
                responseObject.SetActive(true); //To signify text change

            }
        }

        if (!string.IsNullOrEmpty(responsePiece))
        {
            responseCoroutine = StartCoroutine(RespondPiece(responsePiece));
            yield return new WaitUntil(() => responsePieceConcluded);
            responsePieceConcluded = false;
        }

        Destroy(responseObject);
        Destroy(clickForwader);
        responseObject = null;
        responseConcluded = true;
    }

    private IEnumerator AnimateTextBox()
    {
        if (responseObject == null)
        {
            responseObject = Instantiate(textResponseObject, transform);
        }

        responseObject.GetComponentInChildren<TMP_Text>().text = string.Empty; //Clear the text initially
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

    private IEnumerator RespondPiece(string responsePiece)
    {
        TMP_Text textComponent = responseObject.GetComponentInChildren<TMP_Text>();

        if (textComponent == null)
        {
            Debug.LogError("TMP_Text component not found in the response object.");
            Destroy(responseObject);
            yield break;
        }

#if UNITY_WEBGL && !UNITY_EDITOR
        ReceiveMessageFromUnity(responsePiece);
#endif

        textComponent.text = responsePiece;
        int wordCount = responsePiece.Split(new char[] { ' ', '\n', '\t' }, System.StringSplitOptions.RemoveEmptyEntries).Length; //Get word count
        talkingSimulator.StartTalking();
        yield return null;
        float totalTime = TEXT_TO_SPEECH_AUDIO_DURATION > 0 ? TEXT_TO_SPEECH_AUDIO_DURATION : wordCount * RESPONSE_DURATION_PER_WORD;
        float textAnimationTime = totalTime * TEXT_ANIMATION_SPEED_MULTIPLIER;
        // This makes the animation duration be totalTime
        textComponent.GetComponent<TextAnimation>().delayBetweenJumps = (textAnimationTime - textComponent.GetComponent<TextAnimation>().jumpDuration) / (textComponent.GetComponent<TextAnimation>().TmpCharCount - 1);
        StartCoroutine(textComponent.GetComponent<TextAnimation>().AnimateTextOnce());
        yield return new WaitForSeconds(textAnimationTime);
        StartCoroutine(WaitForUserClick());
        yield return new WaitForSeconds(totalTime - textAnimationTime); //Wait for the rest of the time
        talkingSimulator.StopTalking();
    }

    private IEnumerator WaitForUserClick()
    {
        RectTransform triangleRectTransform = responseObject.transform.Find("Triangle").GetComponent<RectTransform>();
        triangleRectTransform.gameObject.SetActive(true);
        Vector2 trianglePos = triangleRectTransform.anchoredPosition;

        while (true)
        {
            // triangleRectTransform.anchoredPosition = new(-50, 50);
            if (responsePieceConcluded || !triangleRectTransform.gameObject.activeInHierarchy || triangleRectTransform == null) //If the response piece is concluded, stop the coroutine
            {
                triangleRectTransform.gameObject.SetActive(false);
                yield break;
            }

            Tween tween = triangleRectTransform.DOAnchorPos(trianglePos - new Vector2(0, TRIANGLE_JUMP_HEIGHT_PIXELS), 0.5f).SetEase(Ease.InQuad);
            yield return tween.WaitForCompletion();
            tween = triangleRectTransform.DOAnchorPos(trianglePos, 0.5f).SetEase(Ease.OutQuad);
            yield return tween.WaitForCompletion();
        }
    }

    public void OnClick(PointerEventData eventData)
    {
        talkingSimulator.StopTalking(); //If user prematurely stops the response also stop the talking
        if (responseCoroutine != null)
        {
            // Debug.Log("Ending response cor.");
            StopCoroutine(responseCoroutine); //Refrence the exact coroutine
            responseCoroutine = null;
            responsePieceConcluded = true;
        }
    }
}
