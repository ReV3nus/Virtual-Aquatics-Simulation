using System;
using Interaction;
using UnityEngine;


public class RowingBoatPhysics : MonoBehaviour
{
    public Transform water;
    
    public OarController leftOar;
    public OarController rightOar;

    [Range(0, 1)] public float waterDrag;
    public float speedFactor;

    private Rigidbody _rigidbody;
    private Vector3 _lastPositionLeft;
    private Vector3 _lastPositionRight;

    private void Start()
    {
        _rigidbody = GetComponent<Rigidbody>();
        _lastPositionLeft = transform.InverseTransformPoint(leftOar.endpointPosition);
        _lastPositionRight = transform.InverseTransformPoint(rightOar.endpointPosition);
    }

    private void FixedUpdate()
    {
        // if (rightOar.lastFrameEndpointPosition != rightOar.endpointPosition)
        // {
        //     Debug.Log("last frame" + rightOar.lastFrameEndpointPosition);
        //     Debug.Log("this frame" + rightOar.endpointPosition);
        // }
        ApplyForces();
    }

    private void ApplyForces()
    {
        var leftOarPos = transform.InverseTransformPoint(leftOar.endpointPosition);
        var rightOarPos = transform.InverseTransformPoint(rightOar.endpointPosition);

        var leftOarVelocity = Vector3.zero;
        var rightOarVelocity = Vector3.zero;
        if (leftOar.endpointPosition.y <= GetWaterHeight(leftOar.endpointPosition))
            leftOarVelocity = leftOarPos - _lastPositionLeft;
        if (rightOar.endpointPosition.y <= GetWaterHeight(rightOar.endpointPosition))
            rightOarVelocity = rightOarPos - _lastPositionRight;

        var avgOarVelocity = (leftOarVelocity + rightOarVelocity) / 2;

        var force = avgOarVelocity.z * speedFactor * Vector3.back;
        var dragForce = -_rigidbody.velocity * waterDrag;


        _rigidbody.AddForce(force, ForceMode.Acceleration);
        _rigidbody.AddForce(dragForce, ForceMode.Acceleration);

        _lastPositionLeft = leftOarPos;
        _lastPositionRight = rightOarPos;
    }

    private float GetWaterHeight(Vector3 position)
    {
        return water.position.y;
    }
}