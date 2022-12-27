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

    private float steeringInput;
    private float accelerationInput;
    private float breakingInput;

    private Vector3 prevPosition;
    private int stepsNotMoving;

    private float baseActionReward = 0.01f;
    private float baseEndEpisodeReward;

    public override void Initialize()
    {
        // Set base rewards
        baseEndEpisodeReward = MaxStep * baseActionReward;
        Debug.Log($"Base rewards: action: {baseActionReward}, finish: {baseEndEpisodeReward}");

        // Initialize parking environment
        env = GetComponentInParent<ParkingEnvironment>();
        env.Initialize();
        
        // Initialize car component
        car = GetComponent<Car>();
    }

    public override void OnEpisodeBegin()
    {
        // Reset environment
        env.Reset();
        ResetCar(Random.Range(-1.5f, 1.5f), Random.Range(-1.55f, 4.25f), 180f); // Spawn car
        //ResetCar(Random.Range(-1.5f, 1.5f), Random.Range(-10f, 22.5f), Random.Range(170f, 190f)); // Spawn car

        // Get target
        target = env.GetTarget();

        // Reset variables
        prevPosition = transform.position;
        stepsNotMoving = 0;
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
        // Add car sensors
        //sensor.AddObservation(car.GetPosition()); // TODO: prevent overfitting by changing target
        sensor.AddObservation(car.GetVelocity());
        sensor.AddObservation(car.GetAngularVelocity());

        // Add current car inputs
        sensor.AddObservation(steeringInput);
        sensor.AddObservation(accelerationInput);
        sensor.AddObservation(breakingInput);
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        ActionSegment<float> continuousActions = actionsOut.ContinuousActions;
        continuousActions[0] = (int)Input.GetAxisRaw("Horizontal"); // Steer angle
        continuousActions[1] = (int)Input.GetAxisRaw("Vertical"); // Acceleration
        continuousActions[2] = Input.GetKey(KeyCode.Space) ? 1 : -1; // Break
    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        // Get Input
        steeringInput = actions.ContinuousActions[0];
        accelerationInput = actions.ContinuousActions[1];
        breakingInput = Mathf.Clamp(actions.ContinuousActions[2], 0, 1);
        Debug.Log($"Car inputs: Steer angle: {steeringInput}, Acceleration: {accelerationInput}, Break: {breakingInput}");

        // Move the car
        car.Move(steeringInput, accelerationInput, breakingInput);

        // Evaluate action
        EvaluateAction();

        // Log current reward
        Debug.Log($"Current reward: {GetCumulativeReward()}");
    }

    private void OnCollisionEnter(Collision collision)
    {
        // Collision with car
        if (collision.gameObject.TryGetComponent(out Car car))
        {
            Collision("Car");
        }

        // Collision with tree
        else if (collision.gameObject.TryGetComponent(out Tree tree))
        {
            Collision("Tree");
        }

        // Collision with street light
        else if (collision.gameObject.TryGetComponent(out StreetLight streetLight))
        {
            Collision("Street Light");
        }

        // Collision with sidewalk
        else if (collision.gameObject.TryGetComponent(out Sidewalk sidewalk))
        {
            Collision("Car", 0.1f);
        }
    }

    private void Collision(string objectName, float multiplier = 1f)
    {
        var reward = -1f * baseEndEpisodeReward * multiplier;
        Debug.Log($"Collision with {objectName} (reward: {reward})");
        AddReward(reward);
    }

    private void OnTriggerEnter(Collider collider)
    {
        // End episode if agent is out of the world
        if (collider.gameObject.TryGetComponent(out WorldBorder worldBorder))
        {
            var reward = -100 * baseEndEpisodeReward;
            AddReward(reward);
            EndEpisode("Out of world");
        }
    }

    private void OnTriggerStay(Collider collider)
    {
        // If agent drives on sidewalk
        if (collider.gameObject.TryGetComponent(out Sidewalk sidewalk))
        {
            AddReward(-baseActionReward * 1);
        }

        // If agent drives on grass
        if (collider.gameObject.TryGetComponent(out OffRoad offRoad))
        {
            AddReward(-baseActionReward * 2);
        }

        // If agent drives on target
        if (collider.gameObject.TryGetComponent(out ParkingSpot parkingSpot) && parkingSpot.IsTarget)
        {
            AddReward(baseActionReward * 2);
        }
    }

    private void EvaluateAction()
    {
        var currentPosition = transform.position;
        var currentRotation = transform.eulerAngles.y;

        // Calculate distance to target difference
        var distanceToTarget = target.GetDistance(currentPosition);
        var prevDistanceToTarget = target.GetDistance(prevPosition);
        var distanceToTargetDifference = prevDistanceToTarget - distanceToTarget;

        // Calculate angle to target
        var angleToTarget = target.GetAngle(currentRotation);

        // Check if agent has parked
        var isStandingStill = car.GetSpeed() < 0.1f;
        var isInParkingSpot = target.IsInParkingSpot(currentPosition);
        var hasParked = isStandingStill && isInParkingSpot;

        // Calculate action reward
        var actionreward = -baseActionReward;
        actionreward += baseActionReward * distanceToTargetDifference;
        AddReward(actionreward);

        // End episode if agent has parked
        if (hasParked)
        {
            AddReward(baseEndEpisodeReward);
            EndEpisode("Parked");
        }

        // End episode if agent is not moving
        Debug.Log("Speed: " + car.GetSpeed());
        stepsNotMoving = car.GetSpeed() < 0.8f ? stepsNotMoving+1 : 0;
        if (stepsNotMoving > 200)
        {
            AddReward(-baseEndEpisodeReward);
            EndEpisode("Not moving");
        }

        // End episode if max steps is reached
        if (StepCount >= MaxStep)
        {
            EndEpisode("Max steps reached");
        }

        // Log action stats
        Debug.Log($"Action stats: Distance to target: {distanceToTarget}, Distance to target difference: {distanceToTargetDifference}, Angle to target: {angleToTarget}, Is in parking spot: {isInParkingSpot}, Is standing still: {isStandingStill}, Steps not moving: {stepsNotMoving}, Has parked: {hasParked}");
        Debug.Log($"Action reward: {actionreward}");

        // Update variables
        prevPosition = transform.position;
    }

    private void EndEpisode(string reason)
    {
        EvaluateEpisode();
        Debug.Log($"End of episode {CompletedEpisodes} (reason: {reason}): Total reward: {GetCumulativeReward()}, Total steps: {StepCount}");
        EndEpisode();
    }

    private void EvaluateEpisode()
    {
        var currentPosition = transform.position;
        var currentRotation = transform.eulerAngles.y;

        // Calculate distance reward
        var distanceToTargetScoreX = target.GetDistanceScoreX(currentPosition);
        var distanceToTargetScoreZ = target.GetDistanceScoreZ(currentPosition);
        var distanceRewardX = baseEndEpisodeReward * -Mathf.Pow(distanceToTargetScoreX, 2);
        var distanceRewardZ = baseEndEpisodeReward * -Mathf.Pow(distanceToTargetScoreZ, 2);

        // Calculate rotation reward
        var angleToTarget = target.GetAngle(currentRotation);
        var rotationReward = baseEndEpisodeReward * -Mathf.Pow(angleToTarget / 90f * 4, 2);

        // Check if agent has parked
        var isStandingStill = car.GetSpeed() < 0.1f;
        var isInParkingSpot = target.IsInParkingSpot(currentPosition);
        var hasParked = isStandingStill && isInParkingSpot;

        // Calculate step reward
        var stepReward = baseEndEpisodeReward * Mathf.Pow((hasParked ? MaxStep - StepCount : 0) / MaxStep, 2);

        var episodeReward = distanceRewardX + distanceRewardZ + rotationReward + stepReward;
        AddReward(episodeReward);

        // Log episode stats
        Debug.Log($"Episode stats: Distance to target score X: {distanceToTargetScoreX}, Distance to target score Z: {distanceToTargetScoreZ}, Angle to target: {angleToTarget}");
        Debug.Log($"Episode reward: {episodeReward}");
    }
}
