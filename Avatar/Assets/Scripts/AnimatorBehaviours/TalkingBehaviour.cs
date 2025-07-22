using System;
using UnityEngine;

[Obsolete("This class has been deprecated. Whole bodh talking animations are no longer used. Use AvatarBlendKeysController.StartLipSync instead for precise lip synced mouth animation.")]
public class TalkingBehaviour : StateMachineBehaviour
{
    //OnStateEnter is called before OnStateEnter is called on any state inside this state machine
    override public void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        if (stateInfo.IsName("Empty"))
        {
            int rand = UnityEngine.Random.Range(1, 3);
            animator.SetInteger("TalkingAnimation", rand);
        }
    }

    public override void OnStateMachineEnter(Animator animator, int stateMachinePathHash)
    {
        animator.transform.GetComponent<AvatarBlendKeysController>().BlendStartTalking();
        base.OnStateMachineEnter(animator, stateMachinePathHash);
    }

    public override void OnStateMachineExit(Animator animator, int stateMachinePathHash)
    {
        animator.transform.GetComponent<AvatarBlendKeysController>().BlendStopTalking();
        base.OnStateMachineExit(animator, stateMachinePathHash);
    }
}
