using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Car : MonoBehaviour
{
    [SerializeField] private float maxSteerAngle;
    [SerializeField] private float motorForce;
    [SerializeField] private float breakForce;

    [SerializeField] private WheelCollider frontLeftWheelCollider;
    [SerializeField] private WheelCollider frontRightWheelCollider;
    [SerializeField] private WheelCollider rearLeftWheelCollider;
    [SerializeField] private WheelCollider rearRightWheelCollider;

    [SerializeField] private Transform frontLeftWheelTransform;
    [SerializeField] private Transform frontRightWheelTransform;
    [SerializeField] private Transform rearLeftWheelTransform;
    [SerializeField] private Transform rearRightWheelTransform;

    private float currentSteerAngle;
    private float currentMotorForce;
    private float currentBreakForce;

    public Vector2 GetPosition()
    {
        return new Vector2(transform.position.x, transform.position.z);
    }

    public Vector3 GetVelocity()
    {
        return GetComponent<Rigidbody>().velocity;
    }

    public Vector3 GetAngularVelocity()
    {
        return GetComponent<Rigidbody>().angularVelocity;
    }

    public void Move(float steerInput, float motorInput, float breakInput)
    {
        currentSteerAngle = steerInput * maxSteerAngle;
        currentMotorForce = motorInput * motorForce;
        currentBreakForce = breakInput * breakForce;

        HandleSteering();
        HandleMotor();
        HandleBreaking();

        UpdateWheels();
    }

    private void HandleSteering()
    {
        frontLeftWheelCollider.steerAngle = currentSteerAngle;
        frontRightWheelCollider.steerAngle = currentSteerAngle;
    }

    private void HandleMotor()
    {
        rearLeftWheelCollider.motorTorque = currentMotorForce;
        rearRightWheelCollider.motorTorque = currentMotorForce;
    }

    private void HandleBreaking()
    {
        frontLeftWheelCollider.brakeTorque = currentBreakForce;
        frontRightWheelCollider.brakeTorque = currentBreakForce;
        rearLeftWheelCollider.brakeTorque = currentBreakForce;
        rearRightWheelCollider.brakeTorque = currentBreakForce;
    }

    private void UpdateWheels()
    {
        UpdateWheel(frontLeftWheelCollider, frontLeftWheelTransform);
        UpdateWheel(frontRightWheelCollider, frontRightWheelTransform);
        UpdateWheel(rearLeftWheelCollider, rearLeftWheelTransform);
        UpdateWheel(rearRightWheelCollider, rearRightWheelTransform);
    }

    private void UpdateWheel(WheelCollider wCollider, Transform wTransform)
    {
        wCollider.GetWorldPose(out Vector3 pos, out Quaternion rot);
        wTransform.position = pos;
        wTransform.rotation = rot;
    }
}
