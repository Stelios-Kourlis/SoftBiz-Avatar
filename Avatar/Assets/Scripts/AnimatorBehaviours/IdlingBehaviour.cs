using UnityEngine;

public class IdlingBehaviour : StateMachineBehaviour
{
    private readonly string[] idleTriggers = { "IdleArmStreching", "IdleNeckStreching" };
    [SerializeField] private float timer;
    [SerializeField] private float minIdleTimeSec = 10f, maxIdleTimeSec = 30f;
    private bool currentStateIsIdle = false;

    // OnStateEnter is called before OnStateEnter is called on any state inside this state machine
    override public void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        if (stateInfo.IsName("Idle") && !currentStateIsIdle)
        {
            //Only reset the timer if we are entering the Idle state from another state
            timer = Random.Range(minIdleTimeSec, maxIdleTimeSec);
            currentStateIsIdle = true;
        }
        else currentStateIsIdle = false;

        //The idle animation rotates the character around the Y axis ever so slightly but it accumulates over time
        //This is to prevent the character from rotating indefinitely
        Transform t = animator.transform;
        Vector3 euler = t.rotation.eulerAngles;
        euler.y = 0f;
        t.rotation = Quaternion.Euler(euler);
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
