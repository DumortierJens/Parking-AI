using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ParkingSpot : MonoBehaviour
{
    public bool IsTarget;
    
    public bool IsInParkingSpot(Vector3 position)
    {
        var xMin = transform.position.x - transform.lossyScale.x;
        var xMax = transform.position.x + transform.lossyScale.x;
        var zMin = transform.position.z - transform.lossyScale.z;
        var zMax = transform.position.z + transform.lossyScale.z;

        return position.x > xMin && position.x < xMax && position.z > zMin && position.z < zMax;
    }

    public float GetDistance(Vector3 position)
    {
        return (transform.position - position).magnitude;
    }

    public float GetDistanceScoreX(Vector3 position)
    {
        return (transform.position - position).x / transform.lossyScale.x;
    }

    public float GetDistanceScoreZ(Vector3 position)
    {
        return (transform.position - position).z / transform.lossyScale.z;
    }

    public float GetAngle(float rotation)
    {
        var rotationDifference = (transform.eulerAngles.y - rotation) % 180;
        return Mathf.Abs(rotationDifference <= 90 ? rotationDifference : 180 - rotationDifference);
    }
}
