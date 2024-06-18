using System;
using Oculus.Interaction.HandGrab;
using UnityEngine;

namespace Interaction
{
    public class OarInteraction : MonoBehaviour
    {
        public Transform anchor;
        public HandGrabInteractable handGrabInteractableLeft;
        public HandGrabInteractable handGrabInteractableRight;

        private Vector3 _localAnchor;

        public Vector3 debugHandPos;
        private void Start()
        {
            _localAnchor = transform.InverseTransformPoint(anchor.position);
        }

        private void Update()
        {
        }
    }
}