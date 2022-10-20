using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ParkingEnvironment : MonoBehaviour
{
    private Parking parking;

    void Start()
    {
        parking = GetComponentInChildren<Parking>();
        parking.Initialize();
    }
}
