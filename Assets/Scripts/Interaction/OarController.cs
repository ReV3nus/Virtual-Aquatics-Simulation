using System;
using System.Linq;
using Oculus.Interaction.HandGrab;
using UnityEngine;

namespace Interaction
{
    public class OarController : MonoBehaviour
    {
        // public Transform anchor;
        // public HandGrabInteractable handGrabInteractableLeft;
        // public HandGrabInteractable handGrabInteractableRight;
        public HandGrabInteractable handGrabInteractable;
        public Transform endpoint;

        // public Vector3 endpointPosition { get; private set; }
        public Vector3 endpointPosition => endpoint.position;
        public Vector3 lastFrameEndpointPosition { get; private set; }

        // private Vector3 _localAnchor;
        // private bool _isOn;
        private float _timeStep;
        // private Vector3 _lastGrabPosition;
        // private HandGrabInteractor _lastHand;

        private Quaternion _lastFrameRotation;
        private Vector3 _grabLocalPosition;

        public bool debug;
        public Transform debugHand;

        private void Start()
        {
            // _localAnchor = transform.InverseTransformPoint(anchor.position);
            // _isOn = false;
            _grabLocalPosition = new Vector3(0.0f, 0.0f, -1.0f);
            _lastFrameRotation = Quaternion.identity;
        }

        private void Update()
        {
            _timeStep = 10.0f * Time.deltaTime;
            lastFrameEndpointPosition = endpoint.position;
            ProcessInput();
        }

        private void LateUpdate()
        {
            transform.localPosition = Vector3.zero;
            // transform.localRotation = Quaternion.LookRotation(-_grabLocalPosition);
            transform.localRotation = Quaternion.Slerp(_lastFrameRotation, Quaternion.LookRotation(-_grabLocalPosition),
                _timeStep);
            _lastFrameRotation = transform.localRotation;
            // lastFrameEndpointPosition = endpoint.position;
        }

        private void ProcessInput()
        {
            var hand = handGrabInteractable.Interactors.FirstOrDefault();
            if (hand is not null || debug)
            {
                transform.localRotation = Quaternion.identity;
                // var localGrabPos = transform.InverseTransformPoint(hand.PalmPoint.position);
                if (debug)
                    _grabLocalPosition = transform.InverseTransformPoint(debugHand.position);
                else
                    _grabLocalPosition = transform.InverseTransformPoint(hand.PalmPoint.position);
            }
        }
    }
}