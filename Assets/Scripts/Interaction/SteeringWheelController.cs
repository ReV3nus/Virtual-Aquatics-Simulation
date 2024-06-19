using System;
using System.Linq;
using UnityEngine;
using Oculus.Interaction.HandGrab;
using UnityEngine.Serialization;

namespace Interaction
{
    public class SteeringWheelController : MonoBehaviour
    {
        public HandGrabInteractable handGrabInteractable;

        public float angle { get; private set; } // rad
        public float lastFrameAngle { get; private set; }
        private bool _isOn;
        private float _timeStep;
        private float _lastGrabAngle;
        private HandGrabInteractor _lastHand;

        private const float AngleLowerBound = -Mathf.PI / 2;
        private const float AngleUpperBound = Mathf.PI / 2;

        public bool debug;
        public float debugAngle;
        public GameObject debugPoint;

        private void Start()
        {
            _isOn = false;
            angle = 0.0f;
        }

        private void Update()
        {
            _timeStep = 10.0f * Time.deltaTime;
            ProcessInput();
        }

        private void LateUpdate()
        {
            transform.localPosition = Vector3.zero;
            transform.localRotation = Quaternion.Slerp(Quaternion.Euler(0.0f, lastFrameAngle * Mathf.Rad2Deg, 0.0f),
                Quaternion.Euler(0.0f, angle * Mathf.Rad2Deg, 0.0f), _timeStep);
            lastFrameAngle = transform.localRotation.eulerAngles.y * Mathf.Deg2Rad;
        }

        private void ProcessInput()
        {
            var hand = handGrabInteractable.Interactors.FirstOrDefault();
            if (hand is not null)
            {
                Debug.Log("grabbing");
                transform.localRotation = Quaternion.identity;
                var localGrabPos = transform.InverseTransformPoint(hand.PalmPoint.position);
                localGrabPos = new Vector3(localGrabPos.x, 0.0f, localGrabPos.z);
                if (debug)
                {
                    debugPoint.transform.position = transform.TransformPoint(localGrabPos);
                }
                if (!_isOn || hand != _lastHand)
                {
                    _isOn = true;
                    _lastGrabAngle = Quaternion.LookRotation(localGrabPos).eulerAngles.y * Mathf.Deg2Rad;
                }
                else
                {
                    var curAngle = Quaternion.LookRotation(localGrabPos).eulerAngles.y * Mathf.Deg2Rad;
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