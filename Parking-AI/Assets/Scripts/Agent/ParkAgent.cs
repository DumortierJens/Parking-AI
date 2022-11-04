using System;
using System.Collections;
using System.Collections.Generic;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using UnityEngine;

public class ParkAgent : Agent
{
    private System.Random random = new System.Random();

    private ParkingEnvironment env;
    private Car car;
    
    private ParkingSpot target;

    private float steeringInput;
    private float accelerationInput;
    private float breakingInput;

    private Vector3 startPosition;
    private Vector3 previousPosition;
    private int stepsStandingStill;
    private bool isInParkingSpot;

    private float baseActionReward = 1;
    private float baseEndEpisodeReward = 500;

    public override void Initialize()
    {
        env = GetComponentInParent<ParkingEnvironment>();
        env.Initialize();
        
        car = GetComponent<Car>();
    }

    public override void OnEpisodeBegin()
    {
        env.Reset();
        ResetCar(random.Next(-175, 175) * 0.01f, random.Next(16, 25), 180);

        target = env.GetTarget();

        startPosition = transform.position;
        previousPosition = startPosition;
        stepsStandingStill = 0;
        isInParkingSpot = false;
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

        ActionSegment<int> discreteActions = actionsOut.DiscreteActions;
        discreteActions[0] = Input.GetKey(KeyCode.Space) ? 1 : 0;
    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        // Get Input
        steeringInput = actions.ContinuousActions[0];
        accelerationInput = actions.ContinuousActions[1];
        breakingInput = actions.DiscreteActions[0] / 2f;
        Debug.Log($"Steering angle: {steeringInput}, Acceleration: {accelerationInput}, Breaking: {breakingInput}");

        // Move the car
        car.Move(steeringInput, accelerationInput, breakingInput);

        // Get reward for action
        AddReward(CalculateActionReward());

        Debug.Log($"Current reward: {GetCumulativeReward()}");
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.TryGetComponent(out Car car))
        {
            EndEpisode(true, "collision with car");

        }

        else if (collision.gameObject.TryGetComponent(out Tree tree))
        {
            EndEpisode(true, "collision with tree");
        }

        else if (collision.gameObject.TryGetComponent(out StreetLight streetLight))
        {
            EndEpisode(true, "collision with street light");
        }

        else if (collision.gameObject.TryGetComponent(out Sidewalk sidewalk))
        {
            AddReward(-5 * baseEndEpisodeReward);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.TryGetComponent(out ParkingSpot parkingSpot) && parkingSpot.IsTarget)
        {
            isInParkingSpot = true;
            AddReward(2 * baseEndEpisodeReward);
        }

        else if (other.gameObject.TryGetComponent(out WorldBorder worldBorder))
        {
            AddReward(-10 * baseEndEpisodeReward);
            EndEpisode(true, "outside world border");
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.gameObject.TryGetComponent(out ParkingSpot parkingSpot) && parkingSpot.IsTarget)
        {
            isInParkingSpot = false;
            AddReward(-2 * baseEndEpisodeReward);
        }
    }

    private void OnTriggerStay(Collider other)
    {
        if (other.gameObject.TryGetComponent(out ParkingSpot parkingSpot) && parkingSpot.IsTarget)
        {
            var distanceToTarget = (target.transform.position - transform.position).magnitude;
            if (distanceToTarget <= 1) // TODO: check if car stands still
            {
                EndEpisode(false, "agent parked");
            }
        }
    }

    private void EndEpisode(bool hasCollision, string reason = "")
    {
        var endEpisodeReward = CalculateEndEpisodeReward(hasCollision);
        AddReward(endEpisodeReward);

        Debug.Log($"End of episode {CompletedEpisodes} (Reason: '{reason}'), Total episode reward: {GetCumulativeReward()}, End episode reward: {endEpisodeReward}, Steps: {StepCount}");
        EndEpisode();
    }

    private float CalculateActionReward()
    {
        // Get observations
        var progress = GetProgress();
        var distanceTraveled = GetDistanceTraveled();
        var rotationScore = GetRotationScore();

        // Agent standing still
        var isStandingStill = distanceTraveled < 0.006;
        var isParked = isStandingStill && isInParkingSpot;
        stepsStandingStill = isStandingStill && !isParked ? stepsStandingStill + 1 : 0;

        // End episode if agent is not moving
        if (stepsStandingStill >= 100)
        {
            AddReward(-10 * baseEndEpisodeReward);
            EndEpisode(false, "standing still");
        }

        // Calculate rewards
        var progressReward = (progress - 0.5f) * baseActionReward;
        var rotationReward = (rotationScore - 0.5f) * baseActionReward;
        var standingStillReward = (stepsStandingStill >= 10 ? -0.5f : 0) * baseActionReward;
        var accelerationReward = Math.Abs(accelerationInput) * baseActionReward;
        var breakingReward = -breakingInput * 2 * baseActionReward;
        var totalReward = progressReward + rotationReward + standingStillReward + accelerationReward + breakingReward;

        Debug.Log($"Total action reward: {totalReward}, Progress reward: {progressReward}, Rotation reward: {rotationReward}, Standing still reward: {standingStillReward}, Acceleration reward: {accelerationReward}, Breaking reward: {breakingReward}");

        return totalReward;
    }

    private float CalculateEndEpisodeReward(bool hasCollision)
    {
        // Get observations
        var progress = GetProgress();
        var rotationScore = GetRotationScore();

        // Calculate rewards
        var positionReward = progress * baseEndEpisodeReward;
        var rotationReward = rotationScore * baseEndEpisodeReward;
        var parkReward = (isInParkingSpot ? 1 : -2) * baseEndEpisodeReward;
        var collisionReward = (hasCollision ? -1 : 0) * baseEndEpisodeReward;
        var totalReward = positionReward + rotationReward + parkReward;

        Debug.Log($"Total end episode reward: {totalReward}, Position reward: {positionReward}, Rotation reward: {rotationReward}, Park reward: {parkReward}, Collision reward: {collisionReward}");

        return totalReward;
    }

    private float GetProgress()
    {
        var currentPosition = transform.position;
        var targetPosition = target.transform.position;

        var startDistance = (targetPosition - startPosition).magnitude;
        var distanceToTarget = (targetPosition - currentPosition).magnitude;
        var progress = (startDistance - distanceToTarget) / startDistance;

        return progress;
    }

    private float GetDistanceTraveled()
    {
        var currentPosition = transform.position;

        var distanceTraveled = (currentPosition - previousPosition).magnitude;
        previousPosition = currentPosition;

        return distanceTraveled;
    }

    private float GetRotationScore()
    {
        var currentRotation = transform.eulerAngles.y;
        var targetRotation = target.transform.eulerAngles.y;

        var rotationDifference = targetRotation - currentRotation;
        var rotationScore = (float)Math.Abs(Math.Cos(rotationDifference * Math.PI / 180));

        return rotationScore;
    }

    private void ResetCar(float x, float z, float rot)
    {
        transform.localPosition = new Vector3(x, 0, z);
        transform.localRotation = Quaternion.Euler(0, rot, 0);
        GetComponent<Rigidbody>().velocity = Vector3.zero;
        GetComponent<Rigidbody>().angularVelocity = Vector3.zero;
    }
}
