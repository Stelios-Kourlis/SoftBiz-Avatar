using System.Collections;
using DG.Tweening;
using UnityEngine;

public class TextBoxAnimator : MonoBehaviour
{
    [SerializeField] private float BOX_ANIMATION_DURATION = 0.5f;
    [SerializeField] private float ARROW_ANIMATION_DURATION = 0.5f;
    [SerializeField] private Ease ANIMATION_EASE_TYPE = Ease.OutCubic;

    public IEnumerator AnimateTextBoxAppearance(GameObject responseObject)
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
        arrowRectTransform.DOScale(new Vector3(3, 3, 3), ARROW_ANIMATION_DURATION).SetEase(ANIMATION_EASE_TYPE);
        yield return tween.WaitForCompletion();
    }

    public IEnumerator AnimateTextBoxDisappearance(GameObject responseObject)
    {
        responseObject.GetComponentInChildren<TMPro.TMP_Text>().text = string.Empty;
        yield return null;
        RectTransform boxRectTransform = responseObject.transform.Find("Box").GetComponent<RectTransform>();
        RectTransform arrowRectTransform = responseObject.transform.Find("Arrow").GetComponent<RectTransform>();

        arrowRectTransform.DORotate(new Vector3(0, 0, 0), ARROW_ANIMATION_DURATION).SetEase(ANIMATION_EASE_TYPE);
        arrowRectTransform.DOScale(Vector3.zero, ARROW_ANIMATION_DURATION).SetEase(ANIMATION_EASE_TYPE);
        yield return new WaitForSecondsRealtime(ARROW_ANIMATION_DURATION / 2);

        Tween tween = boxRectTransform.DOScale(Vector3.zero, BOX_ANIMATION_DURATION).SetEase(ANIMATION_EASE_TYPE);
        boxRectTransform.DORotate(new Vector3(0, 0, 90), BOX_ANIMATION_DURATION).SetEase(ANIMATION_EASE_TYPE);
        yield return tween.WaitForCompletion();

        Destroy(responseObject);

    }
}
