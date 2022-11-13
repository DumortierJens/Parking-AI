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

    private Vector3 prevPosition;
    private float prevRotation;
    private bool isInParkingSpot;
    private int stepsStandingStill;

    private float baseActionReward = 0.1f;
    private float baseParkReward;
    private float baseCollisionReward;

    public override void Initialize()
    {
        baseParkReward = MaxStep * baseActionReward;
        baseCollisionReward = baseParkReward / -5f;

        Debug.Log($"Rewards: Base action reward: {baseActionReward}, Base park reward: {baseParkReward}, Base collision reward: {baseCollisionReward}");

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
        prevPosition = transform.position;
        prevRotation = CalculateRotationToTarget(target.transform.eulerAngles.y - transform.eulerAngles.y);
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
        var position = car.GetPosition();
        var velocity = car.GetVelocity();
        var angularVelocity = car.GetAngularVelocity();

        Debug.Log($"Agent observations: Position: {position}, Velocity: {velocity}, Angular velocity: {angularVelocity}");

        sensor.AddObservation(position); // 2 floats
        sensor.AddObservation(velocity); // 3 floats
        sensor.AddObservation(angularVelocity); // 3 floats
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
        steerInput = actions.ContinuousActions[0];
        motorInput = actions.ContinuousActions[1];
        breakInput = Mathf.Clamp(actions.ContinuousActions[2], 0, 1);
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
        // Out of world
        if (other.gameObject.TryGetComponent(out WorldBorder worldBorder))
        {
            AddReward(-10 * baseParkReward);
            EndEpisode("Outside world border");
        }
    }

    private void OnTriggerStay(Collider other)
    {
        // Agent is in parking spot - Fix OnTriggerEnter (Not always working if car moves to fast)
        if (other.gameObject.TryGetComponent(out ParkingSpot parkingSpot) && parkingSpot.IsTarget && !isInParkingSpot)
        {
            isInParkingSpot = true;
            AddReward(baseParkReward);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        // Agent exits parking spot
        if (other.gameObject.TryGetComponent(out ParkingSpot parkingSpot) && parkingSpot.IsTarget && isInParkingSpot)
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

        var rotationToTarget = CalculateRotationToTarget(targetRotation - currentRotation);
        var prevRotationToTarget = CalculateRotationToTarget(targetRotation - prevRotation);
        var rotationDifference = prevRotationToTarget - rotationToTarget;
        prevRotation = currentRotation;

        // Calculate action reward
        var actionReward = CalculateActionReward(distanceDifference, rotationDifference);

        // Check if car is not moving
        var isStandingStill = Mathf.Abs(distanceDifference) < 0.01;
        stepsStandingStill = isStandingStill ? stepsStandingStill + 1 : 0;
        if (stepsStandingStill > 200)
        {
            var standingStillReward = -baseParkReward + ((MaxStep - StepCount) * -baseActionReward);
            AddReward((isInParkingSpot ? 0.5f : 1) * standingStillReward);
            EndEpisode("Agent standing still");
        }

        Debug.Log($"Action: Distance difference: {distanceDifference}, Distance to target: {distanceToTarget}, Rotation difference: {rotationDifference}, Rotation to target: {rotationToTarget}, Is in parking spot: {isInParkingSpot}, Is standing still: {isStandingStill}, Steps standing still: {stepsStandingStill}");

        // Check if agent has parked
        var maxDistanceToTarget = 1.5f;
        var maxRotationToTarget = 35;
        if (isInParkingSpot && Mathf.Abs(distanceDifference) <= 0.00001 && distanceToTarget <= maxDistanceToTarget && rotationToTarget <= maxRotationToTarget)
        {
            var endEpisodeReward = CalculateParkReward(distanceToTarget, maxDistanceToTarget, rotationToTarget, maxRotationToTarget);
            AddReward(endEpisodeReward);
            EndEpisode("Agent parked");
        }

        return actionReward;
    }

    private float CalculateRotationToTarget(float rotationDifference)
    {
        rotationDifference %= 180;
        var rotationToTarget = Mathf.Abs(rotationDifference <= 90 ? rotationDifference : 180 - rotationDifference);
        return rotationToTarget;
    }

    private float CalculateActionReward(float distanceDiff, float rotationDiff)
    {
        var stepReward = -1 * baseActionReward;
        var distanceReward = distanceDiff * baseActionReward * 100;
        var rotationReward = rotationDiff * baseActionReward * 100;

        var totalReward = stepReward + distanceReward + rotationReward;
        Debug.Log($"Total action reward (Step: {StepCount}): {totalReward}, Distance reward: {distanceReward}, Rotation reward: {rotationReward}");

        return totalReward;
    }

    private float CalculateParkReward(float distanceToTarget, float maxDistanceToTarget, float rotationDiff, float maxRotationDiff)
    {
        var distanceReward = (1 - distanceToTarget / maxDistanceToTarget) * baseParkReward;
        var rotationReward = (1 - rotationDiff / maxRotationDiff) * baseParkReward;

        var totalReward = baseParkReward + distanceReward + rotationReward;
        Debug.Log($"Total end episode reward (Step: {StepCount}): {totalReward}, Distance reward: {distanceReward}, Rotation reward: {rotationReward}");

        return totalReward;
    }

    private void EndEpisode(string reason)
    {
        Debug.Log($"End of episode {CompletedEpisodes}, total reward: {GetCumulativeReward()}, total staps: {StepCount}, reason: {reason}");
        EndEpisode();
    }
}
