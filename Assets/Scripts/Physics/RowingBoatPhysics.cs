using System;
using Interaction;
using UnityEngine;

namespace Physics
{
    public class RowingBoatPhysics : MonoBehaviour
    {
        public OarController leftOar;
        public OarController rightOar;
        
        private void Update()
        {
            
        }

        private void FixedUpdate()
        {
            if (rightOar.lastFrameEndpointPosition != rightOar.endpointPosition)
            {
                Debug.Log("last frame" + rightOar.lastFrameEndpointPosition);
                Debug.Log("this frame" + rightOar.endpointPosition);
            }
        }
    }
}