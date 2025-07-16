using UnityEngine;

public class IdlingBehaviour : StateMachineBehaviour
{
    private readonly string[] idleTriggers = { "IdleArmStreching", "IdleNeckStreching" };
    [SerializeField] private float timer;
    [SerializeField] private float minIdleTimeSec = 10f, maxIdleTimeSec = 30f;
    // OnStateEnter is called before OnStateEnter is called on any state inside this state machine
    override public void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        if (stateInfo.IsName("Idle"))
            timer = Random.Range(minIdleTimeSec, maxIdleTimeSec);
    }

    // OnStateUpdate is called before OnStateUpdate is called on any state inside this state machine
    override public void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        if (timer > 0) timer -= Time.deltaTime;
        if (timer < 0f)
        {
            timer = 0;
            int randomIndex = Random.Range(0, idleTriggers.Length);
            Debug.Log($"IdlingBehaviour: Triggering {idleTriggers[randomIndex]}");
            animator.SetTrigger(idleTriggers[randomIndex]);
        }
    }

    // OnStateExit is called before OnStateExit is called on any state inside this state machine
    //override public void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    //{
    //    
    //}

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
