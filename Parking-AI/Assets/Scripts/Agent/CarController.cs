using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CarController : MonoBehaviour
{
    private Car car;

    private float steerInput;
    private float motorInput;
    private float breakInput;

    private void Start()
    {
        car = GetComponent<Car>();
    }

    private void FixedUpdate()
    {
        GetInput();
        car.Move(steerInput, motorInput, breakInput);
    }

    private void GetInput()
    {
        steerInput = Input.GetAxis("Horizontal");
        motorInput = Input.GetAxis("Vertical");
        breakInput = Input.GetKey(KeyCode.Space) ? 1 : 0;
    }
}
