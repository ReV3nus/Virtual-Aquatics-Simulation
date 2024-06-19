using System;
using Interaction;
using UnityEngine;

public class SteeringWheelYachtPhysics : MonoBehaviour
{
    // public Vector3 position { get => transform.position; }
    public Vector3 position => transform.position;
    public Vector3 velocity => _rigidbody.velocity;

    private Rigidbody _rigidbody;
    public SteeringWheelController steeringWheelController;
    public LeverController leverController;

    public float power; // may control this with levers
    public float rudderPower;
    [Range(0, 1)] public float waterDrag;
    public Transform powerSource;
    [Range(0, 1)] public float rotationReduceFactor;

    private void Start()
    {
        _rigidbody = GetComponent<Rigidbody>();
    }

    private void FixedUpdate()
    {
        // var (force, spinForce) = GetForceAndTorque();
        // _rigidbody.AddForceAtPosition(force, powerSource.position);
        // _rigidbody.AddForceAtPosition(spinForce, powerSource.position);
        // // _rigidbody.AddForce(force);
        // // _rigidbody.AddTorque(torque);
        ApplyForces();
    }

    private void ApplyForces()
    {
        var r = powerSource.position - (_rigidbody.centerOfMass + transform.position);
        // r = new Vector3(r.x, 0.0f, r.z);
        var angle = -steeringWheelController.angle * rotationReduceFactor;
        var direction = new Vector3(Mathf.Sin(angle), 0.0f, Mathf.Cos(angle));
        direction = transform.TransformVector(direction);
        direction = new Vector3(direction.x, 0.0f, direction.z);
        direction = Vector3.Normalize(direction);
        var front = transform.TransformVector(Vector3.forward);
        front = new Vector3(front.x, 0.0f, front.z);
        front = Vector3.Normalize(front);

        var curPower = power * leverController.powerFactor;
        var curRudderPower = rudderPower * leverController.powerFactor;

        var force = front * curPower;
        var spinForce = direction * curRudderPower;
        var dragForce = -_rigidbody.velocity * waterDrag;
        var spinTorque = Vector3.Cross(r, spinForce);

        _rigidbody.AddForceAtPosition(force, powerSource.position);
        // _rigidbody.AddForceAtPosition(spinForce, powerSource.position);
        _rigidbody.AddTorque(spinTorque);
        _rigidbody.AddForce(dragForce, ForceMode.Acceleration);
    }
}