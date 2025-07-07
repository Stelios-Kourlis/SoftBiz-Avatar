using System.Collections;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

public class TextResponseController : MonoBehaviour
{

    [SerializeField] private float RESPONSE_DURATION_PER_WORD = 0.3f;
    [SerializeField] private float BOX_ANIMATION_DURATION = 0.5f;
    [SerializeField] private float ARROW_ANIMATION_DURATION = 0.5f;
    [SerializeField] private Ease ANIMATION_EASE_TYPE = Ease.OutCubic;

    [SerializeField] private GameObject textResponseObject, clickCaptureObject, thinkingTextObject;
    [SerializeField] private TalkingSimulator talkingSimulator;
    private GameObject responseObject, clickForwader, thinkingText;
    private Coroutine responseCoroutine;
    private bool responseConcluded = false;

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

    public void RespondEntry(string response)
    {
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
        clickForwader = Instantiate(clickCaptureObject, transform); //Capture click events
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
                yield return new WaitUntil(() => responseConcluded);
                responseConcluded = false; //Reset the flag for the next piece
                responsePiece = string.Empty; //Reset the response piece
                responseObject.SetActive(false);
                yield return new WaitForSecondsRealtime(0.1f);
                responseObject.SetActive(true); //To signify text change

            }
        }

        if (!string.IsNullOrEmpty(responsePiece))
        {
            responseCoroutine = StartCoroutine(RespondPiece(responsePiece));
            yield return new WaitUntil(() => responseConcluded);
            responseConcluded = false;
        }

        Destroy(responseObject);
        Destroy(clickForwader);
        responseObject = null;
    }

    private IEnumerator AnimateTextBox()
    {
        responseObject = Instantiate(textResponseObject, transform);
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

        arrowRectTransform.DORotate(new Vector3(0, 0, 270), ARROW_ANIMATION_DURATION).SetEase(ANIMATION_EASE_TYPE);
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

        textComponent.text = responsePiece;
        int wordCount = responsePiece.Split(new char[] { ' ', '\n', '\t' }, System.StringSplitOptions.RemoveEmptyEntries).Length; //Get word count
        talkingSimulator.StartTalking();
        // This makes the animation duration be wordCount * RESPONSE_DURATION_PER_WORD
        textComponent.GetComponent<TextAnimation>().delayBetweenJumps = (wordCount * RESPONSE_DURATION_PER_WORD - textComponent.GetComponent<TextAnimation>().jumpDuration) / (textComponent.GetComponent<TextAnimation>().TmpCharCount - 1);
        StartCoroutine(textComponent.GetComponent<TextAnimation>().AnimateTextOnce());
        yield return new WaitForSeconds(wordCount * RESPONSE_DURATION_PER_WORD);
        talkingSimulator.StopTalking();
        responseConcluded = true;
    }

    public void OnClick(PointerEventData eventData)
    {
        talkingSimulator.StopTalking(); //If user prematurely stops the response also stop the talking
        if (responseCoroutine != null)
        {
            // Debug.Log("Ending response cor.");
            StopCoroutine(responseCoroutine); //Refrence the exact coroutine
            responseCoroutine = null;
            responseConcluded = true;
        }
    }
}
