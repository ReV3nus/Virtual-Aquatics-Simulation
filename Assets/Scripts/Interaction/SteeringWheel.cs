using System;
using UnityEngine;

namespace Interaction
{
    public class SteeringWheel : MonoBehaviour
    {
        // angles: rad

        public OVRHand leftHand;
        public OVRHand rightHand;
        
        public float angle { get; private set; }
        public float lastFrameAngle { get; private set; }
        private bool _isOn;
        private float _timeStep;
        private Vector3 _leftGrabPosition;
        private Vector3 _rightGrabPosition;

        public float debugAngle = 0.0f;

        private void Start()
        {
            angle = 0.0f;
            lastFrameAngle = 0.0f;
            _isOn = false;
        }

        private void Update()
        {
            _timeStep = 10.0f * Time.deltaTime;
            ProcessInput();
            transform.rotation = Quaternion.Slerp(Quaternion.Euler(0.0f, lastFrameAngle * Mathf.Rad2Deg, 0.0f),
                Quaternion.Euler(0.0f, angle * Mathf.Rad2Deg, 0.0f), _timeStep);
            lastFrameAngle = transform.rotation.eulerAngles.y * Mathf.Deg2Rad;
        }

        private void ProcessInput()
        {
            if (leftHand.IsTracked)
            {
                
            }

            if (rightHand.IsTracked)
            {
                
            }
            SetAngle(debugAngle);
        }

        public void SetAngle(float a)
        {
            angle = a;
        }
    }
}