using System;
using System.Collections;
using System.Collections.Generic;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using UnityEngine;

public class ParkAgent : Agent
{
    private ParkingEnvironment env;
    private Car car;
    
    private ParkingSpot target;

    private float steeringInput;
    private float accelerationInput;
    private float breakingInput;

    private Vector3 startPosition;
    private Vector3 prevPosition;
    private int stepsStandingStill;

    public override void Initialize()
    {
        env = GetComponentInParent<ParkingEnvironment>();
        env.Initialize();
        
        car = GetComponent<Car>();
        target = env.GetTarget();

        Debug.Log(target);
    }

    public override void OnEpisodeBegin()
    {
        env.Reset();
        ResetCar(0, 20, 180);

        startPosition = transform.position;
        prevPosition = startPosition;
        stepsStandingStill = 0;
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        sensor.AddObservation(car.GetSpeed());
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        ActionSegment<float> continuousActions = actionsOut.ContinuousActions;
        continuousActions[0] = (int)Input.GetAxisRaw("Horizontal");
        continuousActions[1] = (int)Input.GetAxisRaw("Vertical");
        continuousActions[2] = Input.GetKey(KeyCode.Space) ? 1 : -1;
    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        // Get Input
        steeringInput = actions.ContinuousActions[0];
        accelerationInput = actions.ContinuousActions[1];
        breakingInput = (actions.ContinuousActions[2] + 1) / 2f;
        Debug.Log($"Steering angle: {steeringInput}, Acceleration: {accelerationInput}, Breaking: {breakingInput}");

        // Move the car
        car.Move(steeringInput, accelerationInput, breakingInput);

        // Get reward for action
        var actionReward = CalculateActionReward();
        AddReward(actionReward);
        Debug.Log($"Total reward: {GetCumulativeReward()}, Action reward: {actionReward}");
    }

    private void OnCollisionEnter(Collision collision)
    {
        // Check for collision with a car
        if (collision.gameObject.TryGetComponent(out Car car))
        {
            AddReward(-100);
        }

        // Check for collision with a tree
        if (collision.gameObject.TryGetComponent(out Tree tree))
        {
            AddReward(-100);
        }

        // Check for collision with a street light
        if (collision.gameObject.TryGetComponent(out StreetLight streetLight))
        {
            AddReward(-100);
        }

        // Check for collision with a sidewalk
        if (collision.gameObject.TryGetComponent(out Sidewalk sidewalk))
        {
            AddReward(-50);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        // Check if the car is parked
        if (other.gameObject.TryGetComponent(out ParkingSpot parkingSpot))
        {
            if (parkingSpot.IsTarget)
            {
                var finishReward = CalculateFinishReward();
                AddReward(finishReward);
                Debug.Log($"Total reward: {GetCumulativeReward()}, Finish reward: {finishReward}");

                EndEpisode();
            }
        }

        // Check for worldborder
        if (other.gameObject.TryGetComponent(out WorldBorder worldBorder))
        {
            AddReward(-1000);
            EndEpisode();
        }
    }

    private float CalculateActionReward()
    {
        // Get observations
        var currentPosition = transform.position;
        var targetPosition = target.transform.position;

        // Calculate distance
        var distFromStart = (startPosition - currentPosition).magnitude;
        var distFromTarget = (targetPosition - currentPosition).magnitude;
        var distProgress = distFromStart / (distFromStart + distFromTarget);
        var distTraveled = (prevPosition - currentPosition).magnitude;
        prevPosition = currentPosition;

        // Check if car stands still
        var isStandingStill = distTraveled < 0.03;
        var isParked = isStandingStill && distFromTarget < 2;
        stepsStandingStill = isStandingStill && !isParked ? stepsStandingStill+1 : 0;
        if (stepsStandingStill > 100)
        {
            AddReward(-5000);
            EndEpisode();
        }

        // Calculate rewards
        var distProgressReward = distProgress * 10;
        var distTraveledReward = distTraveled * 10;
        var standingStillReward = stepsStandingStill / -10;
        Debug.Log($"Progress reward: {distProgressReward}, Distance traveled reward: {distTraveledReward}, Standing still reward: {standingStillReward}");

        return distProgressReward + distTraveledReward + standingStillReward;
    }

    private float CalculateFinishReward()
    {
        var currentPosition = transform.position;
        var currentRotation = transform.rotation;
        var targetPosition = target.gameObject.transform.position;
        var targetRotation = target.gameObject.transform.rotation;

        // Calculate position reward
        var diffPosition = (currentPosition - targetPosition).magnitude;
        var positionReward = diffPosition / 3; // 0 - 1

        // Calculate rotation reward
        var diffRotation = targetRotation.eulerAngles.y - currentRotation.eulerAngles.y;
        var rotationReward = Math.Abs((diffRotation < 180 ? diffRotation : diffRotation - 180) - 90) / 90; // 0 - 1

        return 2000 + (2000 * positionReward) + (4000 * rotationReward);
    }

    private void ResetCar(float x, float z, float rot)
    {
        transform.localPosition = new Vector3(x, 0, z);
        transform.localRotation = Quaternion.Euler(0, rot, 0);
        GetComponent<Rigidbody>().velocity = Vector3.zero;
        GetComponent<Rigidbody>().angularVelocity = Vector3.zero;
    }
}
