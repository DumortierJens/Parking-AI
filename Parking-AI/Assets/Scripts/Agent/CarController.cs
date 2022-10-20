using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CarController : MonoBehaviour
{
    private Car car;

    private float horizontalInput;
    private float verticalInput;
    private float breakingInput;

    private void Start()
    {
        car = GetComponent<Car>();
    }

    private void FixedUpdate()
    {
        GetInput();
        car.Move(horizontalInput, verticalInput, breakingInput);
    }

    private void GetInput()
    {
        horizontalInput = Input.GetAxis("Horizontal");
        verticalInput = Input.GetAxis("Vertical");
        breakingInput = Input.GetKey(KeyCode.Space) ? 1 : 0;
    }
}
