using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RandomTrap : MonoBehaviour
{
    [Header("Map Gen")]
    public float trapInterval = 5f; // Interval between traps
    public float mapLength = 100f; // Length of the map

    [Header("Traps")]
    public GameObject[] trapPrefabs;

    [Header("WallTrap")]
    public GameObject wallTrap;

    [Header("SpikeTrap")]
    public GameObject spikeTrap;

    [Header("FireTrap")]
    public GameObject fireTrap;

    [Header("SawPlatform")]
    public GameObject sawPlatformTrap;

    [Header("SawTrap")]
    public GameObject sawTrap;

    [Header("ArrowTrap")]
    public GameObject arrowTrap;


    void Start()
    {
        SetupTraps();
    }

    public void SetupTraps()
    {
        trapPrefabs = new GameObject[]
        {
            wallTrap,
            spikeTrap,
            fireTrap,
            sawPlatformTrap,
            sawTrap,
            arrowTrap
        };

        float currentX = 10f;

        while (currentX < mapLength)
        {
            // Randomly select a trap prefab
            int index = UnityEngine.Random.Range(0, trapPrefabs.Length);
            GameObject trapPrefab = trapPrefabs[index];

            // Instantiate the trap at current position
            Instantiate(trapPrefab, new Vector3(currentX, trapPrefabs[index].transform.position.y, trapPrefabs[index].transform.position.z), Quaternion.identity);

            // Move to the next position
            currentX += trapInterval;
        }
    }
}
