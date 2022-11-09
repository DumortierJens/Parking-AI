using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Random = UnityEngine.Random;

public class Parking : MonoBehaviour
{
    private CarSpawner carSpawner;
    private bool selectRandomTarget;
    
    private ParkingSpot[] parkingSpots;
    private ParkingSpot target;

    public void Initialize(bool selectRandomTarget)
    {
        this.selectRandomTarget = selectRandomTarget;

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
        if (selectRandomTarget)
        {
            // Clear isTarget properties
            Array.ForEach(parkingSpots, p => p.IsTarget = false);

            // Select random target
            target = parkingSpots[Random.Range(0, parkingSpots.Length - 1)];
            target.IsTarget = true;
        }
        else
        {
            // Select target
            target = parkingSpots.Where(p => p.IsTarget).FirstOrDefault();

            // If target is null, select random
            if (target == null)
            {
                selectRandomTarget = true;
                SelectTarget();
            }
        }
    }
}
