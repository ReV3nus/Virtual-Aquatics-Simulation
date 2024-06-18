using System;
using System.Linq;
using Oculus.Interaction.HandGrab;
using UnityEngine;

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

        public bool useDebug;
        public float debugAngle;

        private void Start()
        {
            _isOn = false;
        }

        private void Update()
        {
            _timeStep = 10.0f * Time.deltaTime;
            ProcessInput();
        }

        private void LateUpdate()
        {
            transform.localPosition = new Vector3(0.0f, 0.0f, 0.0f);
            transform.localRotation = Quaternion.Slerp(Quaternion.Euler(lastFrameAngle * Mathf.Rad2Deg, 0.0f, 0.0f),
                Quaternion.Euler(angle * Mathf.Rad2Deg, 0.0f, 0.0f), _timeStep);
            lastFrameAngle = transform.localRotation.eulerAngles.x * Mathf.Deg2Rad;
        }

        private void ProcessInput()
        {
            var hand = handGrabInteractable.Interactors.FirstOrDefault();
            if (hand != null)
            {
                Debug.Log("grabbing");
                transform.localRotation = Quaternion.identity;
                var localPos = transform.InverseTransformPoint(hand.PalmPoint.position);
                localPos = new Vector3(0.0f, localPos.y, localPos.z);
                if (!_isOn || hand != _lastHand)
                {
                    _isOn = true;
                    _lastGrabAngle = Quaternion.LookRotation(localPos).eulerAngles.x * Mathf.Deg2Rad;
                    // if (_lastGrabAngle > Mathf.PI)
                    //     _lastGrabAngle -= Mathf.PI * 2;
                }
                else
                {
                    var curAngle = Quaternion.LookRotation(localPos).eulerAngles.x * Mathf.Deg2Rad;
                    Debug.Log(curAngle);
                    if (curAngle > Mathf.PI * 1.5)
                    {
                        // if (Mathf.Abs(curAngle - _lastGrabAngle) > Mathf.PI)
                        // {
                        //     if (curAngle > 0.0f)
                        //         curAngle -= Mathf.PI * 2;
                        //     else
                        //         curAngle += Mathf.PI * 2;
                        // }
                        var angleDiff = curAngle - _lastGrabAngle;
                        _lastGrabAngle = curAngle;
                        angle += angleDiff;
                        Debug.Log("angle diff: " + angleDiff);
                        Debug.Log("angle: " + angle);
                    }
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

            if (useDebug)
            {
                angle = debugAngle;
            }
        }
    }
}