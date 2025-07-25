using DG.Tweening;
using UnityEngine;

public class ThinkingBehaviour : StateMachineBehaviour
{
    private Vector3 thinkingCameraPosition = new(0.09f, 1.27f, 6.65f), thinkingCameraRotation = new(15f, 155f, 0f);
    private Vector3 searchingCameraPosition = new(-0.665f, 1.533f, 9.107f), searchingCameraRotation = new(15f, 155f, 0f);
    private readonly float duration = 0.5f;
    Tween posTween, rotTween;

    private Vector3 cabinetShownPosition = new(0.52f, 0.024f, 6.657f), cabinetHiddenPosition = new(0.5f, 0.024f, 7.5f);

    override public void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        if (stateInfo.IsName("Empty"))
        {
            int rand = Random.Range(1, 3);
            animator.SetInteger("ThinkingAnimation", rand);
        }

        if (stateInfo.IsName("Thinking"))
        {
            GameObject.Find("Cabinet").GetComponent<Transform>().DOMove(cabinetHiddenPosition, duration).SetEase(Ease.InOutQuad);
            animator.transform.GetComponent<AvatarBlendKeysController>().BlendEyesLookUp();
            posTween = Camera.main.transform.DOMove(thinkingCameraPosition, duration).SetEase(Ease.InOutQuad).OnComplete(() => posTween = null);
            rotTween = Camera.main.transform.DORotate(thinkingCameraRotation, duration).SetEase(Ease.InOutQuad).OnComplete(() => rotTween = null);
        }
        else if (stateInfo.IsName("Searching Files"))
        {
            GameObject.Find("Cabinet").GetComponent<Transform>().DOMove(cabinetShownPosition, duration).SetEase(Ease.InOutQuad);
            animator.transform.GetComponent<AvatarBlendKeysController>().BlendEyesLookDown();
            posTween = Camera.main.transform.DOMove(searchingCameraPosition, duration).SetEase(Ease.InOutQuad).OnComplete(() => posTween = null);
            rotTween = Camera.main.transform.DORotate(searchingCameraRotation, duration).SetEase(Ease.InOutQuad).OnComplete(() => rotTween = null);
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
    }


}
