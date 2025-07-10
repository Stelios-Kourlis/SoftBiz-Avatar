using System.Collections;
using DG.Tweening;
using UnityEngine;

public class TextBoxAnimator : MonoBehaviour
{
    [SerializeField] private float BOX_ANIMATION_DURATION = 0.5f;
    [SerializeField] private float ARROW_ANIMATION_DURATION = 0.5f;
    [SerializeField] private Ease ANIMATION_EASE_TYPE = Ease.OutCubic;
    [SerializeField] private float TRIANGLE_JUMP_HEIGHT = 30f;
    [SerializeField] private float TRIANGLE_JUMP_DURATION = 2f;

    private Coroutine awaitInputCor;
    // private Vector2 originalTrianglePosition;

    public IEnumerator AnimateTextBoxAppearance(GameObject responseObject)
    {
        RectTransform boxRectTransform = responseObject.transform.Find("Box").GetComponent<RectTransform>();
        RectTransform arrowRectTransform = boxRectTransform.Find("Arrow").GetComponent<RectTransform>();

        boxRectTransform.localScale = Vector3.zero;
        boxRectTransform.rotation = Quaternion.Euler(0, 0, 90);
        arrowRectTransform.gameObject.SetActive(false);

        Tween tween = boxRectTransform.DOScale(Vector3.one, BOX_ANIMATION_DURATION).SetEase(ANIMATION_EASE_TYPE);
        boxRectTransform.DORotate(Vector3.zero, BOX_ANIMATION_DURATION).SetEase(ANIMATION_EASE_TYPE);
        yield return new WaitForSecondsRealtime(BOX_ANIMATION_DURATION / 4);

        arrowRectTransform.gameObject.SetActive(true);
        arrowRectTransform.rotation = Quaternion.Euler(0, 0, 360);
        arrowRectTransform.localScale = Vector3.zero;

        arrowRectTransform.DOLocalRotate(new Vector3(0, 0, 270), ARROW_ANIMATION_DURATION).SetEase(ANIMATION_EASE_TYPE);
        arrowRectTransform.DOScale(new Vector3(3, 3, 3), ARROW_ANIMATION_DURATION).SetEase(ANIMATION_EASE_TYPE);
        yield return tween.WaitForCompletion();
    }

    public IEnumerator AnimateTextBoxDisappearance(GameObject responseObject)
    {
        responseObject.GetComponentInChildren<TMPro.TMP_Text>().text = string.Empty;
        StopWaitForUserInput(responseObject);
        yield return null;
        RectTransform boxRectTransform = responseObject.transform.Find("Box").GetComponent<RectTransform>();
        RectTransform arrowRectTransform = boxRectTransform.Find("Arrow").GetComponent<RectTransform>();

        arrowRectTransform.DOLocalRotate(new Vector3(0, 0, 360), ARROW_ANIMATION_DURATION).SetEase(ANIMATION_EASE_TYPE);
        arrowRectTransform.DOScale(Vector3.zero, ARROW_ANIMATION_DURATION).SetEase(ANIMATION_EASE_TYPE);
        yield return new WaitForSecondsRealtime(ARROW_ANIMATION_DURATION / 4);

        Tween tween = boxRectTransform.DOScale(Vector3.zero, BOX_ANIMATION_DURATION).SetEase(ANIMATION_EASE_TYPE);
        boxRectTransform.DORotate(new Vector3(0, 0, 90), BOX_ANIMATION_DURATION).SetEase(ANIMATION_EASE_TYPE);
        yield return tween.WaitForCompletion();

        Destroy(responseObject);

    }

    public void StartWaitForUserInput(GameObject responseObject)
    {
        if (awaitInputCor != null)
        {
            StopCoroutine(awaitInputCor);
        }
        if (responseObject != null)
        {
            responseObject.transform.Find("Triangle").gameObject.SetActive(true);
            awaitInputCor = StartCoroutine(WaitForUserInput(responseObject));
        }
    }

    public void StopWaitForUserInput(GameObject responseObject)
    {
        if (awaitInputCor != null)
        {
            StopCoroutine(awaitInputCor);
            responseObject.transform.Find("Triangle").gameObject.SetActive(false);
            awaitInputCor = null;
        }
    }

    public IEnumerator WaitForUserInput(GameObject responseObject)
    {
        Debug.Log("[WaitForUserInput] Waiting for user input...");
        RectTransform triangleRectTransform = responseObject.transform.Find("Triangle").GetComponent<RectTransform>();
        Vector2 originalPosition = new(-28, 48);
        Tween tween = null;
        triangleRectTransform.anchoredPosition = originalPosition;

        try
        {
            while (true)
            {
                tween = triangleRectTransform.DOAnchorPosY(originalPosition.y + TRIANGLE_JUMP_HEIGHT, TRIANGLE_JUMP_DURATION / 2).SetEase(Ease.OutQuad);
                yield return tween.WaitForCompletion();
                tween = triangleRectTransform.DOAnchorPosY(originalPosition.y, TRIANGLE_JUMP_DURATION / 2).SetEase(Ease.InQuad);
                yield return tween.WaitForCompletion();
            }
        }
        finally
        {
            tween.Kill();
            triangleRectTransform.anchoredPosition = originalPosition;
            Debug.Log("Wait for input killed");
        }
    }
}
