using System;
using TMPro;
using UnityEngine;

namespace Interaction
{
    public class SampleInteractions : MonoBehaviour
    {
        // public TextMeshProUGUI axis2DText;
        //
        // private void Start()
        // {
        //     Debug.Log(OVRPlugin.GetRenderModelPaths());
        // }
        //
        // private void Update()
        // {
        //     var axis2D = OVRInput.Get(OVRInput.Axis2D.PrimaryThumbstick);
        //     axis2DText.SetText("x: " + axis2D.x + " y: " + axis2D.y);
        // }
        
        public Camera sceneCamera;
        private Vector3 targetPosition;
        private Quaternion targetRotation;
        private float step;
        
        void Start()
        {
            transform.position = sceneCamera.transform.position + sceneCamera.transform.forward * 3.0f;
        }
        
        void centerCube()
        {
            targetPosition = sceneCamera.transform.position + sceneCamera.transform.forward * 3.0f;
            targetRotation = Quaternion.LookRotation(transform.position - sceneCamera.transform.position);

            transform.position = Vector3.Lerp(transform.position, targetPosition, step);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, step);
        }
        
        void Update()
        {
            step = 5.0f * Time.deltaTime;
            if (OVRInput.Get(OVRInput.RawButton.RIndexTrigger)) centerCube();
            if (OVRInput.Get(OVRInput.RawButton.RThumbstickLeft)) transform.Rotate(0, 5.0f * step, 0);
            if (OVRInput.Get(OVRInput.RawButton.RThumbstickRight)) transform.Rotate(0, -5.0f * step, 0);
            if (OVRInput.GetUp(OVRInput.Button.One))
            {
                OVRInput.SetControllerVibration(1, 1, OVRInput.Controller.RTouch);
            }
            if (OVRInput.Get(OVRInput.Axis1D.PrimaryHandTrigger) > 0.0f)
            {
                transform.position = OVRInput.GetLocalControllerPosition(OVRInput.Controller.LTouch);
                transform.rotation = OVRInput.GetLocalControllerRotation(OVRInput.Controller.LTouch);
            }
        }
        
        
    }
}