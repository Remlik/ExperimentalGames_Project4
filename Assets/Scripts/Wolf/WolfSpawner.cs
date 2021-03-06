﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WolfSpawner : MonoBehaviour {

    public static WolfSpawner instance;

    public bool canSpawn;
    public float radius;
    public float spawnRate;
    public List<GameObject> wolves;
    public GameObject wolfPrefab;
    public GameObject wolfInSheepsClothingPrefab;
    public float chanceForSheepsClothing;
    public float increaseTime = 120f;

    private float nextSpawn;
    private float nextIncrease;
    private int spawnAmount = 1;

	// Use this for initialization
	void Start () {
        instance = this;
        radius = GetComponent<SphereCollider>().radius + 5f;
        wolves = new List<GameObject>();
        nextIncrease = Time.time + increaseTime;
	}

    // Update is called once per frame
    void Update() {
        if (!GameController.instance.debug)
        {
            if (Input.GetKeyDown("w"))
            {
                SpawnWolf();
            }
            if (Input.GetKeyDown("s"))
            {
                canSpawn = !canSpawn;
                nextSpawn = Time.time + 1 / spawnRate;
            }
        }
        if (canSpawn)
        {
            if(Time.time > nextSpawn)
            {
                for (int i = 0; i < spawnAmount; i++)
                {

                    SpawnWolf();
                }
            }
            if(Time.time > nextIncrease)
            {
                spawnAmount++;
                nextIncrease = Time.time + increaseTime;
            }
        }
	}

    public void SpawnWolf()
    {
        float angle = Random.Range(-360, 360);
        Vector3 startLocation = Quaternion.Euler(0, angle, 0) * transform.forward * radius;
        startLocation = startLocation + startLocation.normalized * 3f;
        Vector3 targetLocation = Quaternion.Euler(0, Random.Range(angle-40,angle+40), 0) * transform.forward * radius;
        GameObject wolf = null;
        if (Random.Range(0, 100f) < chanceForSheepsClothing)
        {
            wolf = Instantiate(wolfInSheepsClothingPrefab, startLocation, Quaternion.identity, transform);
        }
        else
        {
            wolf = Instantiate(wolfPrefab, startLocation, Quaternion.identity, transform);
            targetLocation.y = wolf.transform.position.y;
            wolf.GetComponent<WolfAI>().huntPosition = targetLocation;
        }
        wolves.Add(wolf);
        nextSpawn = Time.time + 1 / spawnRate;
    }

    public void RemoveWolf(GameObject wolf)
    {
        wolves.Remove(wolf);
    }

    public List<GameObject> GetNearbyWolves(Vector3 position, float radius)
    {
        List<GameObject> nearby = new List<GameObject>();
        foreach(GameObject wolf in wolves)
        {
            if (Vector3.Distance(position, wolf.transform.position) < radius)
            {
                nearby.Add(wolf);
            }
        }
        return nearby;
    }
}
