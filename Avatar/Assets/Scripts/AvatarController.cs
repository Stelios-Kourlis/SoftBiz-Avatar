using System;
using System.Collections;
using UnityEngine;



public class AvatarController : MonoBehaviour
{
    private enum States
    {
        Idle = 0,
        Thinking = 1,
        Talking = 2,
    }

    [SerializeField]
    private Animator animator;

    void Awake()
    {
        if (animator == null)
        {
            Debug.LogError("Animator component is not assigned in AvatarController.");
        }
    }

    public void StartThinking()
    {
        animator.SetBool("isThinking", true);
    }

    public void StopThinking()
    {
        animator.SetBool("isThinking", false);
    }

    public void StopTalking()
    {
        throw new NotImplementedException();
    }

    public void StartTalking()
    {
        throw new NotImplementedException();
    }
}
