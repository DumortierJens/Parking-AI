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
    private bool selectOnlyOneSide;
    
    private ParkingSpot[] parkingSpots;
    private ParkingSpot target;

    public void Initialize(bool selectRandomTarget, bool selectOnlyOneSide)
    {
        this.selectRandomTarget = selectRandomTarget;
        this.selectOnlyOneSide = selectOnlyOneSide;

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
            if (selectOnlyOneSide)
            {
                var parkingSpotsOneSide = parkingSpots.Where(p => p.transform.position.x < 0).ToArray();
                target = parkingSpotsOneSide[Random.Range(0, parkingSpotsOneSide.Length)];
            }
            else
            {
                target = parkingSpots[Random.Range(0, parkingSpots.Length)];
            }

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
