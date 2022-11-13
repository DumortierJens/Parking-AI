using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StreetLight : MonoBehaviour
{
    private Vector3 startPosition;
    private Quaternion startRotation;

    public void Initialize()
    {
        startPosition = transform.position;
        startRotation = transform.rotation;
    }

    public void Reset()
    {
        transform.position = startPosition;
        transform.rotation = startRotation;
        GetComponent<Rigidbody>().velocity = Vector3.zero;
        GetComponent<Rigidbody>().angularVelocity = Vector3.zero;
    }
}
