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
        Sitting = 3,
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

    public void StartIdle()
    {
        animator.SetInteger("State", (int)States.Idle);
    }

    public void StartSitting()
    {
        animator.SetInteger("State", (int)States.Sitting);
    }

    public void StartThinking()
    {
        animator.SetInteger("State", (int)States.Thinking);
    }

    public void StartTalking()
    {
        animator.SetInteger("State", (int)States.Talking);
    }
}
