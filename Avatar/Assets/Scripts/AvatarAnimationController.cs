using System;
using System.Collections;
using UnityEngine;



public class AvatarAnimationController : MonoBehaviour
{
    private enum States
    {
        Idle = 0,
        Thinking = 1,
        Talking = 2,
        Sitting = 3,
        LookingAround = 4,
    }

    private Animator animator;

    void Awake()
    {
        animator = gameObject.GetComponent<Animator>();

        if (animator == null)
            Debug.LogError($"Avatar {gameObject.name} has no Animator component attached.");
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

    public void StartLookingAround()
    {
        animator.SetInteger("State", (int)States.LookingAround);
    }
}
