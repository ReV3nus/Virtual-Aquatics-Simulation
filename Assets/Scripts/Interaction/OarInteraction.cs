using System;
using UnityEngine;

namespace Interaction
{
    public class OarInteraction : MonoBehaviour
    {
        public OVRHand hand;
        public Transform anchor;

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