using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Parking : MonoBehaviour
{
    private CarSpawner carSpawner;
    
    private ParkingSpot[] parkingSpots;
    private ParkingSpot[] freeParkingSpots;
    private ParkingSpot target;

    public void Initialize()
    {
        parkingSpots = GetComponentsInChildren<ParkingSpot>();
        freeParkingSpots = parkingSpots.Where(p => !p.IsTarget).ToArray();
        target = parkingSpots.Where(p => p.IsTarget).FirstOrDefault();

        carSpawner = GetComponent<CarSpawner>();
        carSpawner.SpawnCars(freeParkingSpots);
    }

    public ParkingSpot GetTarget()
    {
        return target;
    }
}
