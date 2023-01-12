using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Random = UnityEngine.Random;

public class Parking : MonoBehaviour
{
    private CarSpawner carSpawner;
    private bool onlySelectedTargets;
    private bool leftTargets;
    private bool rightTargets;

    private ParkingSpot[] parkingSpots;
    private ParkingSpot[] validParkingSpots;
    private ParkingSpot target;

    public void Initialize(bool onlySelectedTargets, bool leftTargets, bool rightTargets)
    {
        this.onlySelectedTargets = onlySelectedTargets;
        this.leftTargets = leftTargets;
        this.rightTargets = rightTargets;

        parkingSpots = GetComponentsInChildren<ParkingSpot>();
        validParkingSpots = InitializeValidParkingSpots(parkingSpots);

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

    private ParkingSpot[] InitializeValidParkingSpots(ParkingSpot[] parkingSpots)
    {
        validParkingSpots = parkingSpots.Where(p => (p.IsTarget || !onlySelectedTargets) && !p.IsIgnored).ToArray();

        if (leftTargets || rightTargets)
        {
            validParkingSpots = validParkingSpots.Where(p => (p.transform.position.x > 0 && leftTargets) || (p.transform.position.x < 0 && rightTargets)).ToArray();
        }

        return validParkingSpots;
    }

    private void SelectTarget()
    {
        // Clear isTarget properties
        Array.ForEach(parkingSpots, p => p.IsTarget = false);

        // Select random target
        target = validParkingSpots[Random.Range(0, validParkingSpots.Length)];

        // If target is null, select random
        if (target == null)
        {
            onlySelectedTargets = false;
            SelectTarget();
        }
        else
        {
            target.IsTarget = true;
        }
    }
}
