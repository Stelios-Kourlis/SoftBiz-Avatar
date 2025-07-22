using DG.Tweening;
using UnityEngine;

public class BaseBehaviour : StateMachineBehaviour
{
    Vector3 initialCameraPosition = new(0.55f, 1.1f, 6.55f), thinkingCameraPosition = new(0.2f, 1.2f, 6.4f);
    Vector3 initialCameraRotation = new(15f, 180f, 0f), thinkingCameraRotation = new(15f, 155f, 0f);
    Tween posTween, rotTween;
    [SerializeField] private float duration = 0.5f;

    // OnStateEnter is called before OnStateEnter is called on any state inside this state machine
    override public void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        Transform cameraTransform = Camera.main.transform;

        if (posTween != null && posTween.IsActive()) posTween.Kill();
        if (rotTween != null && rotTween.IsActive()) rotTween.Kill();

        if (stateInfo.IsName("Thinking"))
        {
            animator.transform.GetComponent<AvatarBlendKeysController>().BlendEyesLookUp();
            posTween = cameraTransform.DOMove(thinkingCameraPosition, duration).SetEase(Ease.InOutQuad);
            rotTween = cameraTransform.DORotate(thinkingCameraPosition, duration).SetEase(Ease.InOutQuad);
        }
        else
        {
            animator.transform.GetComponent<AvatarBlendKeysController>().BlendEyesLookDown();
            posTween = cameraTransform.DOMove(initialCameraPosition, duration).SetEase(Ease.InOutQuad);
            rotTween = cameraTransform.DORotate(initialCameraRotation, duration).SetEase(Ease.InOutQuad);
        }
    }
}
