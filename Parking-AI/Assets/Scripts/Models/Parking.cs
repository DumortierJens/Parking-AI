using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Parking : MonoBehaviour
{
    private System.Random random = new System.Random();

    private CarSpawner carSpawner;
    
    private ParkingSpot[] parkingSpots;
    private ParkingSpot target;

    public void Initialize()
    {
        parkingSpots = GetComponentsInChildren<ParkingSpot>();

        carSpawner = GetComponent<CarSpawner>();
        carSpawner.Initialize(parkingSpots);
    }

    public void Reset()
    {
        SelectTarget();
        carSpawner.Spawn(target);
    }

    public ParkingSpot GetTarget()
    {
        return target;
    }

    private void SelectTarget()
    {
        // Reset isTarget properties
        Array.ForEach(parkingSpots, p => p.IsTarget = false);
        
        // Set random target
        target = parkingSpots[random.Next(parkingSpots.Length)];
        target.IsTarget = true;
    }
}
