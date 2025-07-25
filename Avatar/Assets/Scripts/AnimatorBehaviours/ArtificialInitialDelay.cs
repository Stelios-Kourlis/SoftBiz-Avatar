using System.Collections;
using UnityEngine;

public class ArtificialInitialDelay : StateMachineBehaviour
{
    /// <summary>
    /// Why would there even be an artificial initial delay?
    /// Because when Unity in WebGL loading there is a lag spike that freezes the camera animation
    /// Instead add a delay so the freeze happens while the user can't see it.
    /// </summary>
    [SerializeField] private float initialDelay = 2f;

    override public void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layer)
    {
        if (initialDelay > 0f)
        {
            initialDelay -= Time.deltaTime;
            return; // Skip the rest of the update until the delay is over
        }

        Debug.Log("ArtificialInitialDelay: Delay over, triggering next state.");
        animator.SetTrigger("ArtificialDelayOver");
    }

}
