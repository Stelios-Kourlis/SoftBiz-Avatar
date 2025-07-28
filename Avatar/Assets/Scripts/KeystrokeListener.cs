using System;
using System.Collections;
using System.Threading;
using TMPro;
using UnityEngine;

public class KeystrokeListener : MonoBehaviour
{
    private readonly float MAX_TIME_BETWEEN_KEYSTROKES = 0.75f;
    private float timer = 0f;
    private int currentIndex = 0;
    private readonly KeyCode[] sequence = { KeyCode.UpArrow, KeyCode.UpArrow, KeyCode.DownArrow, KeyCode.DownArrow, KeyCode.LeftArrow, KeyCode.RightArrow, KeyCode.LeftArrow, KeyCode.RightArrow, KeyCode.B, KeyCode.A };

    // Update is called once per frame
    void Update()
    {
        if (timer > 0f) timer -= Time.deltaTime;

        if (Input.GetKeyDown(sequence[currentIndex]) && (timer > 0f || currentIndex == 0))
        {
            timer = MAX_TIME_BETWEEN_KEYSTROKES;
            currentIndex++;

            if (currentIndex == sequence.Length)
            {
                currentIndex = 0;
                Animator animator = gameObject.GetComponent<Animator>();
                animator.SetInteger("State", (int)AvatarAnimationController.States.Dancing);
            }
        }
        else if (Input.anyKeyDown)
        {
            timer = 0f;
            currentIndex = 0;
        }
    }
}
