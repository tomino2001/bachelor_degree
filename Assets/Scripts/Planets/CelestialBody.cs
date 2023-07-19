using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

[ExecuteInEditMode]
[RequireComponent (typeof (Rigidbody))]
public class CelestialBody : GravityObject {

    public float radius;
    public float surfaceGravity;
    public Vector3 initialVelocity;
    public string bodyName = "Unnamed";
    Transform meshHolder;

    public Vector3 velocity { get; private set; }
    public float mass { get; private set; }
    Rigidbody rb;

    // for detecting double clicks
    float clicked = 0;
    float clicktime = 0;
    float clickdelay = 0.25f;
    float lastClickTime = 0;

    void Awake () {
        rb = GetComponent<Rigidbody> ();
        rb.mass = mass;
        velocity = initialVelocity;
    }

    // 
    public void UpdateVelocity (CelestialBody[] allBodies, float timeStep) {
        foreach (var otherBody in allBodies) {
            if (otherBody != this) {
                float sqrDst = (otherBody.rb.position - rb.position).sqrMagnitude;
                Vector3 forceDir = (otherBody.rb.position - rb.position).normalized;

                Vector3 acceleration = forceDir * Universe.gravitationalConstant * otherBody.mass / sqrDst;
                velocity += acceleration * timeStep;
            }
        }
    }

    public void UpdateVelocity (Vector3 acceleration, float timeStep) {
        velocity += acceleration * timeStep;
    }

    public void UpdatePosition (float timeStep) {
        rb.MovePosition (rb.position + velocity * timeStep);

    }

    void OnValidate () {
        mass = surfaceGravity * radius * radius / Universe.gravitationalConstant;
        meshHolder = transform.GetChild (0);
        meshHolder.localScale = Vector3.one * radius;
        gameObject.name = bodyName;
    }

    public void OnMouseUpAsButton()
    {
        Debug.Log("Click handled by Change Scene" + clicked);
        if (lastClickTime == 0)
            lastClickTime = Time.time;
        else
        {
            float currentTime = Time.time;
            if (Time.time - lastClickTime < clickdelay)
                SceneManager.LoadScene("Stars on Earth");
            lastClickTime = currentTime;
        }
    }

    public Rigidbody Rigidbody {
        get {
            return rb;
        }
    }

    public Vector3 Position {
        get {
            return rb.position;
        }
    }

}