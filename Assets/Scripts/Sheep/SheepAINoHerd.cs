﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SheepAINoHerd : MonoBehaviour {

    public enum State { WANDER, FLOCK, FLEE, GRAZE};

    public State state;
    public List<GameObject> neighbors;
    public float speed;
    public float alignmentWeight, cohesionWeight, seperationWeight, fearWeight;
    public float seperationVariance;
    public float tooCloseDistance = 1.25f;
    public float rotationSpeed = 1f;
    public bool scared;
    public List<GameObject> scaryThings;
    public LayerMask sightLayers;
    public float fov;
    public float sightDistance;
    public float wanderRate;
    public float wanderStrength;
    public float grazeBase, grazeVariance;

    private Rigidbody rb;
    private float originalCohesion, originalSeperation, originalSpeed;
    private float nextWanderTime;
    private Vector3 internalVelocity;
    private float nextGrazeTime;

	// Use this for initialization
	void Start () {
        neighbors = new List<GameObject>();
        rb = GetComponent<Rigidbody>();
        originalCohesion = cohesionWeight;
        originalSeperation = seperationWeight;
        originalSpeed = speed;
        sightDistance = GetComponent<SphereCollider>().radius;
        nextWanderTime = Time.time;
        state = State.WANDER;
        nextGrazeTime = Time.time + grazeBase + Random.Range(-grazeVariance, grazeVariance);
    }
	
	// Update is called once per frame
	void Update () {
        
        if (state == State.FLOCK)
        {
            //Flock
            internalVelocity = Vector3.Lerp(internalVelocity,(ComputeAlignment() * alignmentWeight + ComputeCohesion() * cohesionWeight + ComputeSeperation() * seperationWeight + ComputeFear() * fearWeight).normalized * speed,0.35f);
            internalVelocity.y = 0f;
            rb.velocity = internalVelocity;
            transform.rotation = Quaternion.LookRotation(internalVelocity);
            if (neighbors.Count == 0)
            {
                state = State.WANDER;
            }
        }
        else if (state == State.WANDER)
        {
            //Wander
            internalVelocity = transform.forward * 0.5f * speed;
            internalVelocity.y = 0f;
            rb.velocity = internalVelocity;
            if (Time.time > nextWanderTime)
            {
                Vector3 center = transform.forward * Mathf.Sqrt(2);
                Vector3 pos = center + Quaternion.Euler(0, Random.Range(-wanderStrength, wanderStrength), 0) * transform.forward;
                transform.LookAt(pos+transform.position);
                nextWanderTime = Time.time + 1 / wanderRate;
            }
            if (Time.time > nextGrazeTime)
            {
                state = State.GRAZE;
                nextWanderTime = Time.time + grazeBase / wanderRate; //We will see if this stays
            }
        }
        else if (state == State.GRAZE)
        {
            //Graze
            rb.velocity = Vector3.zero;

            if (Time.time > nextWanderTime)
            {
                state = State.WANDER;
                nextGrazeTime = Time.time + grazeBase + Random.Range(-grazeVariance, grazeVariance);
            }
        }
        //Represent Motion and Turn
        Debug.DrawLine(transform.position, internalVelocity+transform.position, Color.yellow, 0.2f, true);
	}

    public void OnTriggerEnter(Collider other)
    {
        SheepAINoHerd shep = other.GetComponent<SheepAINoHerd>();
        if (shep != null)
        {
            if (!neighbors.Contains(other.gameObject))
            {
                neighbors.Add(other.gameObject);
                if (Random.Range(0f,100f) <= 15 * neighbors.Count)
                {
                    state = State.FLOCK; //Wander -> Flock
                }
            }
        }

        if (other.tag == "Scary")
        {
            if (!scaryThings.Contains(other.gameObject))
            {
                scared = true;
                cohesionWeight *= 2f;
                seperationVariance *= 0.8f;
                speed *= 1.5f;
                scaryThings.Add(other.gameObject);
            }
        }
    }

    public void OnTriggerExit(Collider other)
    {
        SheepAINoHerd shep = other.GetComponent<SheepAINoHerd>();
        if (shep != null)
        {
            if (neighbors.Contains(other.gameObject))
            {
                neighbors.Remove(other.gameObject);
                if (Random.Range(0f, 100f) >= 20 * neighbors.Count)
                {
                    state = State.WANDER; //Flock -> Wander
                    nextGrazeTime = Time.time + grazeBase + Random.Range(-grazeVariance, grazeVariance);
                }
            }
        }

        if (other.tag == "Scary")
        {
            if (scaryThings.Contains(other.gameObject))
            {
                scaryThings.Remove(other.gameObject);
            }
            if (scaryThings.Count <= 0)
            {
                scared = false;
                cohesionWeight = originalCohesion;
                seperationVariance = originalSeperation;
                speed = originalSpeed;
            }
        }
    }


    public bool CanSee(GameObject target)
    {
        return true;
        Vector3 pos = target.transform.position;
        Vector3 toTarget = pos - transform.position;
        float angle = Vector3.Angle(toTarget, transform.forward);
        if (angle <= fov && angle >= -fov)
        {
            RaycastHit hit;
            if (Physics.Raycast(transform.position,pos - transform.position, out hit, sightDistance,sightLayers.value))
            {
                if (hit.collider.gameObject == target)
                {
                    Debug.DrawLine(transform.position, pos);
                    return true;
                }
            }
        }
        return false;
    }

    //Switch to a flock controller unit for better performance at a loss of agency?
    public Vector3 ComputeAlignment()
    {
        Vector3 dir = Vector3.zero;
        int count = 0;
        foreach(GameObject shep in neighbors)
        {
            if (CanSee(shep))
            {
                SheepAINoHerd ai = shep.GetComponent<SheepAINoHerd>();
                dir += ai.GetVelocity();
                count++;
            }
        }
        dir = dir / neighbors.Count;
        dir.y = 0f;

        //Debug.Log(name + ": A ("+count+") : " + dir);

        return dir;
    }

    public Vector3 ComputeCohesion()
    {
        Vector3 dir = Vector3.zero;
        int count = 0;
        foreach (GameObject shep in neighbors)
        {
            if (CanSee(shep))
            {
                dir += shep.transform.position;
                count++;
            }
        }
        dir = dir / neighbors.Count;
        dir.y = 0f;

        //Debug.Log(name + ": C (" + count + ") : " + dir);

        return (dir - transform.position).normalized;
    }

    public Vector3 ComputeSeperation()
    {
        Vector3 dir = Vector3.zero;
        int count = 0;
        foreach (GameObject shep in neighbors)
        {
            if (CanSee(shep))
            {
                Vector3 dif = shep.transform.position - transform.position;
                if (dif.magnitude <= tooCloseDistance + Random.Range(-seperationVariance, seperationVariance))
                {
                    dir += -dif;
                }
                count++;
            }
        }
        dir = dir / neighbors.Count;
        dir.y = 0f;

       // Debug.Log(name + ": S (" + count + ") : " + dir);

        return dir;
    }

    public Vector3 ComputeFear()
    {
        Vector3 dir = Vector3.zero;
        if (scared)
        {
            foreach (GameObject thing in scaryThings)
            {
                if (CanSee(thing))
                {
                    Vector3 dif = thing.GetComponent<Collider>().ClosestPoint(transform.position) - transform.position;
                    dir += dif;
                }
            }
        }
        dir = dir / scaryThings.Count;
        dir.y = 0f;

        //Debug.DrawLine(transform.position, dir + transform.position, Color.red);

        return -dir.normalized;
    }

    public Vector3 GetVelocity()
    {
        return rb.velocity;
    }
}
