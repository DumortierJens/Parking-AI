using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Random = UnityEngine.Random;

public class CarSpawner : MonoBehaviour
{
    [SerializeField] private GameObject[] cars;

    private Dictionary<ParkingSpot, GameObject> spawnedCars = new Dictionary<ParkingSpot, GameObject>();

    public void Initialize(ParkingSpot[] parkingSpots)
    {
        SpawnCars(parkingSpots);
        Array.ForEach(spawnedCars.Keys.ToArray(), p => SetCarState(p, false));
    }

    public void Spawn(ParkingSpot target)
    {
        Array.ForEach(spawnedCars.Keys.ToArray(), p => SetCarState(p, p != target));
        ResetCars();
    }

    private void SpawnCars(ParkingSpot[] parkingSpots)
    {
        foreach(var parkingSpot in parkingSpots)
        {
            var car = cars[Random.Range(0, cars.Length - 1)];
            var offset = new Vector3(0, parkingSpot.transform.position.y, 0);
            spawnedCars.Add(parkingSpot, Instantiate(car, parkingSpot.transform.position - offset, parkingSpot.transform.rotation));
        }
    }

    private void SetCarState(ParkingSpot parkingSpot, bool state)
    {
        spawnedCars[parkingSpot].SetActive(state);
    }

    private void ResetCars()
    {
        foreach (var spawnedCar in spawnedCars)
        {
            var parkingSpot = spawnedCar.Key;
            var pos = parkingSpot.transform.position;
            var rot = parkingSpot.transform.rotation;

            var car = spawnedCar.Value;
            car.transform.position = new Vector3(pos.x, 0.03f, pos.z);
            car.transform.rotation = rot;
            car.GetComponent<Rigidbody>().velocity = Vector3.zero;
            car.GetComponent<Rigidbody>().angularVelocity = Vector3.zero;
        }
    }
}
