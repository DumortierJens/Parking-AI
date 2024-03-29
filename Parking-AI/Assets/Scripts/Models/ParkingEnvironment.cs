using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ParkingEnvironment : MonoBehaviour
{
    [SerializeField] private bool onlySelectedTargets;
    [SerializeField] private bool leftTargets;
    [SerializeField] private bool rightTargets;

    private Parking parking;
    
    private Tree[] trees;
    private StreetLight[] streetLights;

    public void Initialize()
    {
        // Init Parking
        parking = GetComponentInChildren<Parking>();
        parking.Initialize(onlySelectedTargets, leftTargets, rightTargets);

        // Init trees
        trees = GetComponentsInChildren<Tree>();
        foreach (Tree tree in trees)
            tree.Initialize();

        // Init street lights
        streetLights = GetComponentsInChildren<StreetLight>();
        foreach (StreetLight streetLight in streetLights)
            streetLight.Initialize();
    }

    public void Reset()
    {
        parking.Reset();

        foreach (Tree tree in trees)
            tree.Reset();

        foreach (StreetLight streetLight in streetLights)
            streetLight.Reset();
    }

    public ParkingSpot GetTarget()
    {
        return parking.GetTarget();
    }
}
