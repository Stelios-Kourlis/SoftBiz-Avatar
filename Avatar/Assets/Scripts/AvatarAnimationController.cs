using System;
using System.Collections;
using UnityEngine;


[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(AvatarBlendKeysController))]
public class AvatarAnimationController : MonoBehaviour
{
    public enum States
    {
        Idle = 0,
        Thinking = 1,
        Talking = 2,
        Sitting = 3,
        LookingAround = 4,
    }

    private Animator animator;
    private AvatarBlendKeysController avatarBlendKeysController;
    public Action<States> OnStateChanged;

    void Awake()
    {
        animator = gameObject.GetComponent<Animator>();
        avatarBlendKeysController = gameObject.GetComponent<AvatarBlendKeysController>();
    }

    public void StartIdle()
    {
        animator.SetInteger("State", (int)States.Idle);
        OnStateChanged?.Invoke(States.Idle);
    }

    public void StartSitting()
    {
        animator.SetInteger("State", (int)States.Sitting);
        OnStateChanged?.Invoke(States.Sitting);
    }

    public void StartThinking()
    {
        animator.SetInteger("State", (int)States.Thinking);
        OnStateChanged?.Invoke(States.Thinking);
    }

    public void StartTalking()
    {
        animator.SetInteger("State", (int)States.Talking);
        OnStateChanged?.Invoke(States.Talking);
    }

    public void StartLookingAround()
    {
        animator.SetInteger("State", (int)States.LookingAround);
        OnStateChanged?.Invoke(States.LookingAround);
    }
}
