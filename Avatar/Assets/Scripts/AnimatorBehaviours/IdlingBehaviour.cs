using DG.Tweening;
using UnityEngine;

public class IdlingBehaviour : StateMachineBehaviour
{
    private readonly string[] idleTriggers = { "IdleArmStreching", "IdleNeckStreching" };
    [SerializeField] private float timer;
    [SerializeField] private float minIdleTimeSec = 10f, maxIdleTimeSec = 30f;
    /// <summary> Check if we are in the default idle state (Not an idle variety animation) </summary>
    [SerializeField] private bool currentStateIsIdle = false;
    [SerializeField] private Vector3 idleCameraPosition = new(0.55f, 1.25f, 7f), idleCameraRotation = new(15f, 180f, 0f);
    private readonly float duration = 0.5f;
    Tween posTween, rotTween;

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
    }

    // OnStateUpdate is called before OnStateUpdate is called on any state inside this state machine
    override public void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        //Do random idle animations

        if (timer > 0)
        {
            if (animator.TryGetComponent(out AvatarBlendKeysController controller) && controller.IsLipSyncing)
            {
                timer += Time.deltaTime; //Increase timer if lip sync is happening instead of resseting it
                return;
            }
            timer -= Time.deltaTime;
        }
        if (timer < 0f)
        {
            timer = 0;
            int randomIndex = Random.Range(0, idleTriggers.Length);
            Debug.Log($"IdlingBehaviour: Triggering {idleTriggers[randomIndex]}");
            animator.SetTrigger(idleTriggers[randomIndex]);
        }
    }

    public override void OnStateMachineEnter(Animator animator, int stateMachinePathHash)
    {
        Debug.Log($"IdlingBehaviour: Entering state machine with path hash {stateMachinePathHash}");
        posTween = Camera.main.transform.DOMove(idleCameraPosition, duration).SetEase(Ease.InOutQuad).OnComplete(() => posTween = null);
        rotTween = Camera.main.transform.DORotate(idleCameraRotation, duration).SetEase(Ease.InOutQuad).OnComplete(() => rotTween = null);
        base.OnStateMachineEnter(animator, stateMachinePathHash);
    }

    public override void OnStateMachineExit(Animator animator, int stateMachinePathHash)
    {
        if (posTween != null && posTween.IsActive()) posTween.Kill();
        if (rotTween != null && rotTween.IsActive()) rotTween.Kill();
    }
}
