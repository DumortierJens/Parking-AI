using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Parking : MonoBehaviour
{
    private CarSpawner carSpawner;
    private ParkingSpot[] parkingSpots;

    public void Initialize()
    {
        parkingSpots = GetComponentsInChildren<ParkingSpot>();

        carSpawner = GetComponent<CarSpawner>();
        carSpawner.SpawnCars(parkingSpots);
    }
}
