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

    public override void Initialize()
    {
        env = GetComponentInParent<ParkingEnvironment>();
        env.Initialize();
        target = env.GetTarget();

        car = GetComponent<Car>();
    }

    public override void OnEpisodeBegin()
    {
        ResetCar(0, 20, 180);
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
        Debug.Log($"Steering Angle: {steeringInput}, Acceleration: {accelerationInput}, Breaking: {breakingInput}");

        // Move the car
        car.Move(steeringInput, accelerationInput, breakingInput);

        // Get reward for action
        AddReward(-1);
        Debug.Log("Current reward: " + GetCumulativeReward());
    }

    private void OnTriggerEnter(Collider other)
    {
        // Check if the car is parked
        if (other.gameObject.TryGetComponent(out ParkingSpot parkingSpot))
        {
            if (parkingSpot.IsTarget)
            {
                AddReward(1000);
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

    private void ResetCar(float x, float z, float rot)
    {
        transform.localPosition = new Vector3(x, 0, z);
        transform.localRotation = Quaternion.Euler(0, rot, 0);
        GetComponent<Rigidbody>().velocity = Vector3.zero;
        GetComponent<Rigidbody>().angularVelocity = Vector3.zero;
    }
}
