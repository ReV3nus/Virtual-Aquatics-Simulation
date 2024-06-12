using System;
using Interaction;
using UnityEngine;

namespace Physics
{
    public class SteeringWheelYachtPhysics : MonoBehaviour
    {
        // public Vector3 position { get => transform.position; }
        public Vector3 position => transform.position;
        public Vector3 velocity { get; private set; }

        private Rigidbody _rigidbody;
        public SteeringWheelController steeringWheelController;

        public float power; // may control this with levers
        public float rudderPower;
        public Transform powerSource;
        [Range(0, 1)] public float rotationReduceFactor;
        private void Start()
        {
            _rigidbody = GetComponent<Rigidbody>();
        }

        private void FixedUpdate()
        {
            var (force, spinForce) = GetForceAndTorque();
            _rigidbody.AddForceAtPosition(force, powerSource.position);
            _rigidbody.AddForceAtPosition(spinForce, powerSource.position);
            // _rigidbody.AddForce(force);
            // _rigidbody.AddTorque(torque);
        }

        private (Vector3, Vector3) GetForceAndTorque()
        {
            var r = powerSource.position - _rigidbody.centerOfMass;
            r = new Vector3(r.x, 0.0f, r.z);
            var angle = -steeringWheelController.angle * rotationReduceFactor;
            var direction = new Vector3(Mathf.Sin(angle), 0.0f, Mathf.Cos(angle));
            direction = transform.TransformVector(direction);
            direction = new Vector3(direction.x, 0.0f, direction.z);
            direction = Vector3.Normalize(direction);
            var front = transform.TransformVector(Vector3.forward);
            front = new Vector3(front.x, 0.0f, front.z);
            front = Vector3.Normalize(front);
            
            var force = front * power;
            var spinForce = direction * rudderPower;
            var torque = Vector3.Cross(r, spinForce);
            return (force, spinForce);
            Debug.Log(torque);
            return (force, torque);
        }
    }
}