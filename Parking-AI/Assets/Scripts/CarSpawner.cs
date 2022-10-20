using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class CarSpawner : MonoBehaviour
{
    [SerializeField] private int generateCars;
    [SerializeField] private GameObject[] cars;

    private System.Random random = new System.Random();

    public void SpawnCars(ParkingSpot[] freeParkingSpots)
    {
        if (generateCars >= freeParkingSpots.Length)
        {
            generateCars = freeParkingSpots.Length;
        }

        var selectedParkingSpots = GetUniqueParkingSpots(generateCars, freeParkingSpots.Length);
        foreach (var parkingSpotIdx in selectedParkingSpots)
        {
            var car = cars[random.Next(cars.Length)];
            var parkingSpot = freeParkingSpots[parkingSpotIdx];
            
            var offset = new Vector3(0, parkingSpot.transform.position.y, 0);
            Instantiate(car, parkingSpot.transform.position - offset, parkingSpot.transform.rotation);
        }
    }

    private int[] GetUniqueParkingSpots(int count, int max)
    {
        var numbers = new List<int>();

        while (numbers.Count < count)
        {
            var num = random.Next(max);
            if (!numbers.Contains(num))
            {
                numbers.Add(num);
            }
        }

        return numbers.ToArray();
    }
}
