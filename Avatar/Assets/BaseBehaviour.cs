using DG.Tweening;
using UnityEngine;

public class BaseBehaviour : StateMachineBehaviour
{
    Vector3 danceCameraPostiion = new(0.55f, 2f, 9.75f), danceCameraRotation = new(21f, 180f, 0f);
    Tween posTween, rotTween;
    // OnStateEnter is called before OnStateEnter is called on any state inside this state machine
    override public void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        if (stateInfo.IsName("Default Dance"))
        {
            posTween = Camera.main.GetComponent<Transform>().DOMove(danceCameraPostiion, 0.5f).SetEase(Ease.InOutQuad).OnComplete(() => posTween = null);
            rotTween = Camera.main.GetComponent<Transform>().DORotate(danceCameraRotation, 0.5f).SetEase(Ease.InOutQuad).OnComplete(() => rotTween = null);
        }
    }

    // OnStateExit is called before OnStateExit is called on any state inside this state machine
    override public void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        if (stateInfo.IsName("Default Dance"))
        {
            animator.SetInteger("State", (int)AvatarAnimationController.States.Idle);
            Debug.Log("Animation has ended. Running additional code...");
        }
    }

    public override void OnStateMachineExit(Animator animator, int stateMachinePathHash)
    {
        if (posTween != null && posTween.IsActive()) posTween.Kill();
        if (rotTween != null && rotTween.IsActive()) rotTween.Kill();
    }
}
