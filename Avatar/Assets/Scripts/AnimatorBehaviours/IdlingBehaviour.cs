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
        //Do random idle animations
        if (timer > 0) timer -= Time.deltaTime;
        if (timer < 0f)
        {
            timer = 0;
            int randomIndex = Random.Range(0, idleTriggers.Length);
            Debug.Log($"IdlingBehaviour: Triggering {idleTriggers[randomIndex]}");
            animator.SetTrigger(idleTriggers[randomIndex]);
        }
    }
}
