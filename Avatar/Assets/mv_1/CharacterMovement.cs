using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CharacterMovement : MonoBehaviour
{
    public bool move = true;
    public float speed = 5f; // Editable speed value

    //private void Update()
    //{
    //    // Calculate the movement vector
    //    Vector3 movement = Vector3.forward * speed * Time.deltaTime;

    //    // Move the character
    //    transform.Translate(movement);
    //}

    public float rotationIntensity = 5f;  // Editable intensity of the rotation effect
    public float rotationSpeed = 10f;     // Editable speed of the rotation effect
    public float rotationNoise = 0.1f;        // Editable noise factor for rotation intensity

    private float rotationTimer = 0f;

    private void Update()
    {
        if (!move) return;

        // Calculate the movement vector
        Vector3 movement = speed * Time.deltaTime * Vector3.forward;

        // Move the character
        transform.Translate(movement);

        // Apply rotation effect
        rotationTimer += Time.deltaTime * rotationSpeed;
        float rotationAmount = Mathf.Sin(rotationTimer) * rotationIntensity + Random.Range(-rotationNoise, rotationNoise);
        transform.rotation = Quaternion.Euler(0f, rotationAmount, 0f);
    }

    public void StopOrStartMoving()
    {
        move = !move;
    }

}

