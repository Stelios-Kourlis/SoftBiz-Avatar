using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

public class TextResponseController : MonoBehaviour
{

    [SerializeField] private float RESPONSE_DURATION_PER_WORD = 0.1f;

    [SerializeField] private GameObject textResponseObject, clickCaptureObject;
    [SerializeField] private TalkingSimulator talkingSimulator;
    private GameObject responseObject, clickForwader;
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


    private IEnumerator Respond(string response)
    {
        if (string.IsNullOrEmpty(response))
        {
            yield break;
        }

        responseObject = Instantiate(textResponseObject, transform);
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
            Debug.Log($"Testing piece: {testPiece}");

            Vector2 sizeRestraints = textComponent.GetPreferredValues(testPiece, tmpRectTransform.rect.width, tmpRectTransform.rect.height);
            Debug.Log($"Size restraints: {sizeRestraints.x}x{sizeRestraints.y}, RectTransform size: {tmpRectTransform.rect.width}x{tmpRectTransform.rect.height}");

            fitsWidth = sizeRestraints.x <= tmpRectTransform.rect.width;
            fitsHeight = sizeRestraints.y <= tmpRectTransform.rect.height;

            if (fitsWidth && fitsHeight)
            {
                responsePiece = testPiece;
                index++;
                Debug.Log($"Fits adding index: {index}/{responseSentences.Length}");
            }
            else
            {
                Debug.Log($"Doesn't Fit, outputing at index {index}/{responseSentences.Length}...");
                responseCoroutine = StartCoroutine(RespondPiece(responsePiece));
                yield return new WaitUntil(() => responseConcluded);
                responseConcluded = false; //Reset the flag for the next piece
                Debug.Log("Response piece sent, resetting...");
                responsePiece = string.Empty; //Reset the response piece
                responseObject.SetActive(false);
                yield return new WaitForSecondsRealtime(0.1f);
                responseObject.SetActive(true); //To signify text change

            }
        }

        if (!string.IsNullOrEmpty(responsePiece))
        {
            Debug.Log("Sending leftover piece...");
            responseCoroutine = StartCoroutine(RespondPiece(responsePiece));
            yield return new WaitUntil(() => responseConcluded);
            responseConcluded = false;
        }

        Destroy(responseObject);
        Destroy(clickForwader);
        responseObject = null;
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
        yield return new WaitForSeconds(wordCount * RESPONSE_DURATION_PER_WORD);
        talkingSimulator.StopTalking();
        responseConcluded = true;
    }

    public void OnClick(PointerEventData eventData)
    {
        talkingSimulator.StopTalking(); //If user prematurely stops the response also stop the talking
        if (responseCoroutine != null)
        {
            Debug.Log("Ending response cor.");
            StopCoroutine(responseCoroutine); //Refrence the exact coroutine
            responseCoroutine = null;
            responseConcluded = true;
        }
    }
}
