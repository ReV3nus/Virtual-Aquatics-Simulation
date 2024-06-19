using System;
using UnityEngine;


public class BoatBuoyancy : MonoBehaviour
{
    public Transform water;
    
    public int floatingPointCount;
    public Rigidbody rigidbody;
    public float submergeDepth;
    public float floatFactor;
    public float waterDrag;
    public float waterAngularDrag;

    private void FixedUpdate()
    {
        ApplyBuoyancy();
    }

    private void ApplyBuoyancy()
    {
        rigidbody.AddForceAtPosition(UnityEngine.Physics.gravity / floatingPointCount, transform.position,
            ForceMode.Acceleration);
        float waterHeight = GetWaterHeight(transform.position);
        if (transform.position.y < waterHeight)
        {
            float factor = Mathf.Clamp01((waterHeight - transform.position.y) / submergeDepth) * floatFactor;
            rigidbody.AddForceAtPosition(new Vector3(0.0f, -UnityEngine.Physics.gravity.y * factor, 0.0f),
                transform.position, ForceMode.Acceleration);
            rigidbody.AddForce(waterDrag * factor * -rigidbody.velocity, ForceMode.Acceleration);
            rigidbody.AddTorque(waterAngularDrag * factor * -rigidbody.angularVelocity, ForceMode.Acceleration);
        }
    }

    private float GetWaterHeight(Vector3 position)
    {
        return water.position.y;
    }
}