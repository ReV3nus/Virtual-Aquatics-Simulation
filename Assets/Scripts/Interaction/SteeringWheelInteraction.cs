using System;
using UnityEngine;

namespace Interaction
{
    public class SteeringWheelInteraction : MonoBehaviour
    {
        // angles: rad

        public OVRHand leftHand;
        public OVRHand rightHand;
        public OVRSkeleton leftHandSkeleton;
        public OVRSkeleton rightHandSkeleton;
        
        public float angle { get; private set; }
        public float lastFrameAngle { get; private set; }
        private bool _isOn;
        private float _timeStep;
        private bool _isLeftHandOn;
        private bool _isRightHandOn;
        private float _leftLastGrabAngle;
        private float _rightLastGrabAngle;

        public float debugAngle = 0.0f;
        public GameObject debugPoint;

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
            // transform.localRotation = Quaternion.Euler(0.0f, angle, 0.0f);
            transform.localRotation = Quaternion.Slerp(Quaternion.Euler(0.0f, lastFrameAngle * Mathf.Rad2Deg, 0.0f),
            Quaternion.Euler(0.0f, angle * Mathf.Rad2Deg, 0.0f), _timeStep);
            lastFrameAngle = transform.localRotation.eulerAngles.y * Mathf.Deg2Rad;
        }

        private void ProcessInput()
        {
            if (leftHand.IsTracked)
            {
                if (leftHand.GetFingerIsPinching(OVRHand.HandFinger.Index))
                {
                    if (!_isLeftHandOn)
                    {
                        _isLeftHandOn = true;
                        var localPos = new Vector3();
                        foreach (var bone in leftHandSkeleton.Bones)
                        {
                            if (bone.Id == OVRSkeleton.BoneId.Hand_Middle1)
                            {
                                localPos = transform.InverseTransformPoint(bone.Transform.position);
                                break;
                            }
                        }
                        // var localPos = transform.InverseTransformPoint(leftHand.transform.position);
                        localPos = new Vector3(localPos.x, 0.0f, localPos.z);
                        _leftLastGrabAngle = Quaternion.LookRotation(localPos).eulerAngles.y * Mathf.Deg2Rad;
                        Debug.Log("grab angle: " + _leftLastGrabAngle);
                    }
                    else
                    {
                        var curLocalPosition = new Vector3();
                        foreach (var bone in leftHandSkeleton.Bones)
                        {
                            if (bone.Id == OVRSkeleton.BoneId.Hand_Middle1)
                            {
                                curLocalPosition = transform.InverseTransformPoint(bone.Transform.position);
                                break;
                            }
                        }
                        // var curLocalPosition = transform.InverseTransformPoint(leftHand.transform.position);
                        debugPoint.transform.position = transform.TransformPoint(curLocalPosition);
                        curLocalPosition = new Vector3(curLocalPosition.x, 0.0f, curLocalPosition.z);
                        var curAngle = Quaternion.LookRotation(curLocalPosition).eulerAngles.y * Mathf.Deg2Rad;
                        var angleDiff = curAngle - _leftLastGrabAngle;
                        _leftLastGrabAngle = curAngle;
                        angle += angleDiff;
                        Debug.Log("current angle: " + angle);
                    }
                    
                }
                else
                {
                    _isLeftHandOn = false;
                }
            }
            //
            // if (rightHand.IsTracked)
            // {
            //     
            // }
            // SetAngle(debugAngle);
        }

        public void SetAngle(float a)
        {
            angle = a;
        }
    }
}