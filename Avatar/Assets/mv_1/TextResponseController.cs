using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

public class TextResponseController : MonoBehaviour
{

    [SerializeField] private GameObject textResponseObject, clickCaptureObject;
    [SerializeField] private TalkingSimulator talkingSimulator;
    private GameObject responseObject, clickForwader;
    private Coroutine stopTalkingCoroutine;

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

    public void Respond(string response)
    {
        responseObject = Instantiate(textResponseObject, transform);
        clickForwader = Instantiate(clickCaptureObject, transform); //Capture click events
        clickForwader.GetComponent<ClickForwader>().OnClick += OnClick;
        TMP_Text textComponent = responseObject.GetComponentInChildren<TMP_Text>();

        if (textComponent == null)
        {
            Debug.LogError("TMP_Text component not found in the response object.");
            Destroy(responseObject);
            return;
        }

        textComponent.text = response;
        int wordCount = response.Split(new char[] { ' ', '\n', '\t' }, System.StringSplitOptions.RemoveEmptyEntries).Length; //Get word count
        talkingSimulator.StartTalking();
        stopTalkingCoroutine = StartCoroutine(StopResponseTalking(wordCount * 0.3f)); //Assuming each word takes 0.3 seconds to say
    }

    private IEnumerator StopResponseTalking(float duration)
    {
        yield return new WaitForSeconds(duration);
        talkingSimulator.StopTalking();
    }

    public void OnClick(PointerEventData eventData)
    {
        if (responseObject == null)
        {
            return;
        }

        Destroy(responseObject);
        Destroy(clickForwader);
        responseObject = null;
        talkingSimulator.StopTalking(); //If user prematurely stops the response also stop the talking
        if (stopTalkingCoroutine != null)
        {
            StopCoroutine(stopTalkingCoroutine); //Refrence the exact coroutine
            stopTalkingCoroutine = null;
        }
    }
}
