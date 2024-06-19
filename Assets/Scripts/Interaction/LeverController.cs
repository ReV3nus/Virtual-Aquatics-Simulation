using System;
using System.Linq;
using Oculus.Interaction.HandGrab;
using UnityEngine;
using UnityEngine.Serialization;

namespace Interaction
{
    public class LeverController : MonoBehaviour
    {
        public HandGrabInteractable handGrabInteractable;
        // public float maxPower;

        public float angle { get; private set; } // rad
        public float lastFrameAngle { get; private set; }
        public float powerFactor => (angle - AngleLowerBound) / (AngleUpperBound - AngleLowerBound);
        private bool _isOn;
        private float _timeStep;
        private float _lastGrabAngle;
        private HandGrabInteractor _lastHand;

        private const float AngleLowerBound = 0;
        private const float AngleUpperBound = Mathf.PI / 3;

        public bool debug;
        public float debugAngle;

        private void Start()
        {
            _isOn = false;
            angle = 0.0f;
            lastFrameAngle = 0.0f;
        }

        private void Update()
        {
            _timeStep = 10.0f * Time.deltaTime;
            ProcessInput();
        }

        private void LateUpdate()
        {
            transform.localPosition = Vector3.zero;
            transform.localRotation = Quaternion.Slerp(Quaternion.Euler(lastFrameAngle * Mathf.Rad2Deg, 0.0f, 0.0f),
                Quaternion.Euler(angle * Mathf.Rad2Deg, 0.0f, 0.0f), _timeStep);
            lastFrameAngle = transform.localRotation.eulerAngles.x * Mathf.Deg2Rad;
        }

        private void ProcessInput()
        {
            var hand = handGrabInteractable.Interactors.FirstOrDefault();
            if (hand is not null)
            {
                transform.localRotation = Quaternion.identity;
                var localGrabPos = transform.InverseTransformPoint(hand.PalmPoint.position);
                localGrabPos = new Vector3(0.0f, localGrabPos.y, localGrabPos.z);
                var curAngle = Mathf.Atan(localGrabPos.z / localGrabPos.y);
                if (!_isOn || hand != _lastHand)
                {
                    _isOn = true;
                    _lastGrabAngle = curAngle;
                }
                else
                {
                    Debug.Log("curAngle: " + curAngle);
                    var angleDiff = curAngle - _lastGrabAngle;
                    _lastGrabAngle = curAngle;
                    angle += angleDiff;
                }
                _lastHand = hand;
                if (angle > AngleUpperBound)
                    angle = AngleUpperBound;
                if (angle < AngleLowerBound)
                    angle = AngleLowerBound;
                Debug.Log("current angle: " + angle);
            }
            else
            {
                _isOn = false;
            }

            if (debug)
            {
                angle = debugAngle;
            }
        }
    }
}