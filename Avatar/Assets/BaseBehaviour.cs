using DG.Tweening;
using UnityEngine;

public class BaseBehaviour : StateMachineBehaviour
{
    Vector3 initialPosition = new(0.6f, 1.1f, 8.8f), closeUpPosition = new(0.2f, 1.2f, 6.4f);
    Vector3 initialRotation = new(15f, 180f, 0f), closeUpRotation = new(15f, 155f, 0f);
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
            posTween = cameraTransform.DOMove(closeUpPosition, duration).SetEase(Ease.InOutQuad);
            rotTween = cameraTransform.DORotate(closeUpRotation, duration).SetEase(Ease.InOutQuad);
        }
        else
        {
            posTween = cameraTransform.DOMove(initialPosition, duration).SetEase(Ease.InOutQuad);
            rotTween = cameraTransform.DORotate(initialRotation, duration).SetEase(Ease.InOutQuad);
        }
    }

    // OnStateUpdate is called before OnStateUpdate is called on any state inside this state machine
    //override public void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    //{
    //    
    //}

    //OnStateExit is called before OnStateExit is called on any state inside this state machine
    // override public void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    // {

    // }

    // OnStateMove is called before OnStateMove is called on any state inside this state machine
    //override public void OnStateMove(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    //{
    //    
    //}

    // OnStateIK is called before OnStateIK is called on any state inside this state machine
    //override public void OnStateIK(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    //{
    //    
    //}

    // OnStateMachineEnter is called when entering a state machine via its Entry Node
    //override public void OnStateMachineEnter(Animator animator, int stateMachinePathHash)
    //{
    //    
    //}

    // OnStateMachineExit is called when exiting a state machine via its Exit Node
    //override public void OnStateMachineExit(Animator animator, int stateMachinePathHash)
    //{
    //    
    //}
}
