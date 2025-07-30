using DG.Tweening;
using UnityEngine;

public class ThinkingBehaviour : StateMachineBehaviour
{
    private Vector3 thinkingCameraPosition = new(0.09f, 1.27f, 6.65f), thinkingCameraRotation = new(15f, 155f, 0f);
    private Vector3 searchingHighCameraPosition = new(-0.665f, 1.533f, 9.107f), searchingHighCameraRotation = new(15f, 155f, 0f);
    private Vector3 searchingLowCameraPosition = new(-0.5f, 2.3f, 8.7f), searchingLowCameraRotation = new(40f, 155f, 0f);
    private readonly float duration = 0.5f;
    Tween posTween, rotTween;

    private Vector3 cabinetShownPosition = new(0.52f, 0.024f, 6.657f), cabinetHiddenPosition = new(0.5f, 0.024f, 8.5f);
    private Vector3 cabinetLowShownPosition = new(0.5f, -0.545f, 6.8f);

    override public void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        if (stateInfo.IsName("Thinking"))
        {
            animator.transform.GetComponent<AvatarBlendKeysController>().BlendEyesLookUp();
            animator.transform.GetComponent<AvatarBlendKeysController>().BlendRightEyebrowUp();
            posTween = Camera.main.transform.DOMove(thinkingCameraPosition, duration).SetEase(Ease.InOutQuad).OnComplete(() => posTween = null);
            rotTween = Camera.main.transform.DORotate(thinkingCameraRotation, duration).SetEase(Ease.InOutQuad).OnComplete(() => rotTween = null);
            int newAnimation = Random.Range(1, 3);
            animator.SetInteger("ThinkingAnimation", newAnimation);
        }

        else if (stateInfo.IsName("Searching Files High"))
        {
            GameObject.Find("Cabinet").GetComponent<Transform>().DOMove(cabinetShownPosition, duration).SetEase(Ease.InOutQuad);
            posTween = Camera.main.transform.DOMove(searchingHighCameraPosition, duration).SetEase(Ease.InOutQuad).OnComplete(() => posTween = null);
            rotTween = Camera.main.transform.DORotate(searchingHighCameraRotation, duration).SetEase(Ease.InOutQuad).OnComplete(() => rotTween = null);
        }

        else if (stateInfo.IsName("Searching Files Low"))
        {
            animator.applyRootMotion = true;
            GameObject.Find("CabinetLow").GetComponent<Transform>().DOMove(cabinetLowShownPosition, duration).SetEase(Ease.InOutQuad);
            posTween = Camera.main.transform.DOMove(searchingLowCameraPosition, duration).SetEase(Ease.InOutQuad).OnComplete(() => posTween = null);
            rotTween = Camera.main.transform.DORotate(searchingLowCameraRotation, duration).SetEase(Ease.InOutQuad).OnComplete(() => rotTween = null);
        }

        else if (stateInfo.IsName("Looking Up")) //Unused state
        {
            animator.transform.GetComponent<AvatarBlendKeysController>().BlendEyesLookUp();
            animator.transform.GetComponent<AvatarBlendKeysController>().BlendBothEyebrowsUp();
            posTween = Camera.main.transform.DOMove(thinkingCameraPosition, duration).SetEase(Ease.InOutQuad).OnComplete(() => posTween = null);
            rotTween = Camera.main.transform.DORotate(thinkingCameraRotation, duration).SetEase(Ease.InOutQuad).OnComplete(() => rotTween = null);
        }
    }

    public override void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        if (stateInfo.IsName("Thinking"))
        {
            animator.transform.GetComponent<AvatarBlendKeysController>().BlendRightEyebrowDown();
            animator.transform.GetComponent<AvatarBlendKeysController>().BlendEyesLookDown();
        }
        else if (stateInfo.IsName("Searching Files High"))
        {
            GameObject.Find("Cabinet").GetComponent<Transform>().DOMove(cabinetHiddenPosition, duration).SetEase(Ease.InOutQuad);
        }
        else if (stateInfo.IsName("Searching Files Low"))
        {
            animator.applyRootMotion = false;
            GameObject.Find("CabinetLow").GetComponent<Transform>().DOMove(cabinetHiddenPosition, duration).SetEase(Ease.InOutQuad);
            GameObject.Find("model").GetComponent<Transform>().SetPositionAndRotation(new Vector3(0.546f, -0.71f, 5.746f), Quaternion.Euler(0, 0, 0));
        }
        else if (stateInfo.IsName("Looking Up")) //Unused state
        {
            animator.transform.GetComponent<AvatarBlendKeysController>().BlendEyesLookDown();
            animator.transform.GetComponent<AvatarBlendKeysController>().BlendBothEyebrowsDown();
        }
    }

    public override void OnStateMachineEnter(Animator animator, int stateMachinePathHash)
    {
        Debug.Log($"ThinkingBehaviour: Entering state machine with path hash {stateMachinePathHash}");
        base.OnStateMachineEnter(animator, stateMachinePathHash);
    }

    public override void OnStateMachineExit(Animator animator, int stateMachinePathHash)
    {
        if (posTween != null && posTween.IsActive()) posTween.Kill();
        if (rotTween != null && rotTween.IsActive()) rotTween.Kill();
        animator.applyRootMotion = false;
        animator.transform.GetComponent<AvatarBlendKeysController>().BlendEyesLookDown();
        animator.transform.GetComponent<AvatarBlendKeysController>().BlendBothEyebrowsDown();
        animator.transform.GetComponent<AvatarBlendKeysController>().BlendRightEyebrowDown();
        GameObject.Find("Cabinet").GetComponent<Transform>().DOMove(cabinetHiddenPosition, duration).SetEase(Ease.InOutQuad);
        GameObject.Find("model").GetComponent<Transform>().SetPositionAndRotation(new Vector3(0.546f, -0.71f, 5.746f), Quaternion.Euler(0, 0, 0));
    }


}
