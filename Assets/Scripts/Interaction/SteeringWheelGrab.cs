﻿using System;
using System.Linq;
using UnityEngine;
using Oculus.Interaction.HandGrab;

namespace Interaction
{
    public class SteeringWheelGrab : MonoBehaviour
    {
        public HandGrabInteractable handGrabInteractable;

        public float angle { get; private set; }
        public float lastFrameAngle { get; private set; }
        private bool _isOn;
        private float _timeStep;
        private float _lastGrabAngle;
        private HandGrabInteractor _lastHand;

        public GameObject debugPoint;
        
        private void Update()
        {
            _timeStep = 10.0f * Time.deltaTime;
            ProcessInput();
        }

        private void LateUpdate()
        {
            transform.localPosition = new Vector3(0.0f, 0.0f, 0.0f);
            transform.localRotation = Quaternion.Slerp(Quaternion.Euler(0.0f, lastFrameAngle * Mathf.Rad2Deg, 0.0f),
                Quaternion.Euler(0.0f, angle * Mathf.Rad2Deg, 0.0f), _timeStep);
            lastFrameAngle = transform.localRotation.eulerAngles.y * Mathf.Deg2Rad;
        }

        private void ProcessInput()
        {
            var hand = handGrabInteractable.Interactors.FirstOrDefault();
            if (hand != null)
            {
                Debug.Log("grabbing");
                var localPos = transform.InverseTransformPoint(hand.PalmPoint.position);
                debugPoint.transform.position = transform.TransformPoint(localPos);
                localPos = new Vector3(localPos.x, 0.0f, localPos.z);
                if (!_isOn || hand != _lastHand)
                {
                    _isOn = true;
                    _lastGrabAngle = Quaternion.LookRotation(localPos).eulerAngles.y * Mathf.Deg2Rad;
                }
                else
                {
                    var curAngle = Quaternion.LookRotation(localPos).eulerAngles.y * Mathf.Deg2Rad;
                    var angleDiff = curAngle - _lastGrabAngle;
                    _lastGrabAngle = curAngle;
                    angle += angleDiff;
                }
                _lastHand = hand;
                Debug.Log("current angle: " + angle);
            }
            else
            {
                _isOn = false;
            }
        }
    }
}
