using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// Entry point for controlling avatar animations. Use this instead of directly manipulating the Animator.
/// </summary>
[RequireComponent(typeof(Animator))]
public class AvatarAnimationController : MonoBehaviour
{
    /// <summary>
    /// Enum representing the different states the avatar can be in.
    /// 
    /// Talking is Obsolete and should not be used.
    /// </summary>
    public enum States
    {
        Idle = 0,
        Thinking = 1,
        Talking = 2,
        Dancing = 3,
    }

    private Animator animator;

    void Awake()
    {
        animator = gameObject.GetComponent<Animator>();
    }

    public void StartIdle()
    {
        animator.SetInteger("State", (int)States.Idle);
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
