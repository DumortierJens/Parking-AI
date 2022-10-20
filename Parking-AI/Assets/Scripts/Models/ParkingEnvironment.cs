using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ParkingEnvironment : MonoBehaviour
{
    private Parking parking;

    public void Initialize()
    {
        parking = GetComponentInChildren<Parking>();
        parking.Initialize();
    }

    public ParkingSpot GetTarget()
    {
        return parking.GetTarget();
    }
}
