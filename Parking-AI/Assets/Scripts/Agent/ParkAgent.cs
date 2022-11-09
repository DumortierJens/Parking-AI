using System;
using System.Collections;
using System.Collections.Generic;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using UnityEngine;
using Random = UnityEngine.Random;

public class ParkAgent : Agent
{
    private ParkingEnvironment env;
    private Car car;
    private ParkingSpot target;

    private float steerInput;
    private float motorInput;
    private float breakInput;

    private Vector3 startPosition;
    private Vector3 prevPosition;
    private bool isInParkingSpot;
    private int stepsStandingStill;

    private float baseActionReward = 0.1f;
    private float baseParkReward = 1000;
    private float baseCollisionReward = -1000f;

    public override void Initialize()
    {
        env = GetComponentInParent<ParkingEnvironment>();
        env.Initialize();
        
        car = GetComponent<Car>();
    }

    public override void OnEpisodeBegin()
    {
        // Reset environment
        env.Reset();
        ResetCar(Random.Range(-1.75f, 1.75f), Random.Range(16f, 25f), 180);

        // Get target
        target = env.GetTarget();

        // Reset variables
        startPosition = transform.position;
        prevPosition = startPosition;
        isInParkingSpot = false;
        stepsStandingStill = 0;
    }

    private void ResetCar(float x, float z, float rot)
    {
        transform.localPosition = new Vector3(x, 0, z);
        transform.localRotation = Quaternion.Euler(0, rot, 0);
        GetComponent<Rigidbody>().velocity = Vector3.zero;
        GetComponent<Rigidbody>().angularVelocity = Vector3.zero;
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
        //continuousActions[2] = Input.GetKey(KeyCode.Space) ? 1 : -1;

        ActionSegment<int> discreteActions = actionsOut.DiscreteActions;
        discreteActions[0] = Input.GetKey(KeyCode.Space) ? 4 : 0;
    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        // Get Input
        steerInput = actions.ContinuousActions[0];
        motorInput = actions.ContinuousActions[1];
        //breakInput = Mathf.Clamp(actions.ContinuousActions[2], 0, 1);
        breakInput = actions.DiscreteActions[0] / 4f;
        Debug.Log($"Steer angle: {steerInput}, Motor: {motorInput}, Break: {breakInput}");

        // Move the car
        car.Move(steerInput, motorInput, breakInput);

        // Calculate reward for action
        AddReward(CalculateReward());
        Debug.Log($"Current reward: {GetCumulativeReward()}");
    }

    private void OnCollisionEnter(Collision collision)
    {
        // Collision with car
        if (collision.gameObject.TryGetComponent(out Car car))
        {
            AddReward(baseCollisionReward);
        }

        // Collision with tree
        else if (collision.gameObject.TryGetComponent(out Tree tree))
        {
            AddReward(baseCollisionReward);
        }

        // Collision with street light
        else if (collision.gameObject.TryGetComponent(out StreetLight streetLight))
        {
            AddReward(baseCollisionReward);
        }

        // Collision with sidewalk
        else if (collision.gameObject.TryGetComponent(out Sidewalk sidewalk))
        {
            AddReward(baseCollisionReward);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        // Agent enters parking spot
        if (other.gameObject.TryGetComponent(out ParkingSpot parkingSpot) && parkingSpot.IsTarget)
        {
            isInParkingSpot = true;
            AddReward(baseParkReward);
        }

        // Out of world
        else if (other.gameObject.TryGetComponent(out WorldBorder worldBorder))
        {
            AddReward(10 * baseCollisionReward);
            EndEpisode("Outside world border");
        }
    }

    private void OnTriggerExit(Collider other)
    {
        // Agent exits parking spot
        if (other.gameObject.TryGetComponent(out ParkingSpot parkingSpot) && parkingSpot.IsTarget)
        {
            isInParkingSpot = false;
            AddReward(-baseParkReward);
        }
    }

    private float CalculateReward()
    {
        // Position
        var currentPosition = transform.position;
        var targetPosition = target.transform.position;

        var distanceToTarget = (targetPosition - currentPosition).magnitude;
        var prevDistanceToTarget = (targetPosition - prevPosition).magnitude;
        var distanceDifference = prevDistanceToTarget - distanceToTarget;
        prevPosition = currentPosition;

        // Rotation
        var currentRotation = transform.eulerAngles.y;
        var targetRotation = target.transform.eulerAngles.y;

        var rotationToTarget = (targetRotation - currentRotation) % 180;
        var rotationDifference = rotationToTarget <= 90 ? rotationToTarget : 180 - rotationToTarget;

        Debug.Log($"Action: Distance difference: {distanceDifference}, Distance to target: {distanceToTarget}, Rotation difference: {rotationDifference}, Is in parking spot: {isInParkingSpot}");

        // Calculate action reward
        var actionReward = CalculateActionReward(distanceDifference, rotationDifference);

        // Check if car is not moving
        var isStandingStill = Mathf.Abs(distanceDifference) <= 0.006;
        stepsStandingStill = isStandingStill && !isInParkingSpot ? stepsStandingStill + 1 : 0;
        if (stepsStandingStill > 200)
        {
            AddReward(-baseParkReward);
            EndEpisode("Agent standing still");
        }

        // Check if agent has parked
        var maxDistanceToTarget = 1.5f;
        var maxRotationDifference = 30;
        if (isInParkingSpot && distanceToTarget <= maxDistanceToTarget && Mathf.Abs(distanceDifference) <= 0.00001 && Mathf.Abs(rotationDifference) <= maxRotationDifference)
        {
            var endEpisodeReward = CalculateParkReward(distanceToTarget, maxDistanceToTarget, Mathf.Abs(rotationDifference), maxRotationDifference);
            AddReward(endEpisodeReward);
            EndEpisode("Agent parked");
        }

        return actionReward;
    }

    private float CalculateActionReward(float distanceDiff, float rotationDiff)
    {
        var distanceReward = distanceDiff * baseActionReward * 2;
        var rotationReward = (1 - rotationDiff / 90f) * baseActionReward;
        var parkReward = (isInParkingSpot ? 1 : 0) * baseActionReward;

        var totalReward = distanceReward + rotationReward + parkReward;
        Debug.Log($"Total action reward (Step: {StepCount}): {totalReward}, Distance reward: {distanceReward}, Rotation reward: {rotationReward}, Park reward: {parkReward}");

        return totalReward;
    }

    private float CalculateParkReward(float distanceToTarget, float maxDistanceToTarget, float rotationDiff, float maxRotationDiff)
    {
        var distanceReward = (1 - distanceToTarget / maxDistanceToTarget) * baseParkReward;
        var rotationReward = (1 - rotationDiff / maxRotationDiff) * baseParkReward;

        var totalReward = distanceReward + rotationReward;
        Debug.Log($"Total end episode reward (Step: {StepCount}): {totalReward}, Distance reward: {distanceReward}, Rotation reward: {rotationReward}");

        return totalReward;
    }

    private void EndEpisode(string reason)
    {
        Debug.Log($"End of episode {CompletedEpisodes}, total reward: {GetCumulativeReward()}, total staps: {StepCount}, reason: {reason}");
        EndEpisode();
    }
}
