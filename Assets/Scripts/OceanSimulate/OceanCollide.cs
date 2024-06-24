using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Unity.VisualScripting;
using UnityEngine;

public class OceanCollide : MonoBehaviour
{
    public OceanPlane ocean;
    private BoxCollider box;
    private Vector2 meshStart;
    private int meshCount;
    private float meshDistance;

    private Vector3 size, halfSize, center;
    private Vector3[] corners;

    public struct BoxColliderInfo
    {
        public Vector2 p0,p1,p2,p3;
        public Vector2 v;
        public float h;
    };
    public BoxColliderInfo info;

    public void Init()
    {
        box = GetComponent<BoxCollider>();
        meshStart = new Vector2(ocean.gameObject.transform.position.x, ocean.gameObject.transform.position.z);
        meshCount = ocean.PatchVertexCount;
        meshDistance = ocean.VertexDistance;
        size = box.bounds.size;
        halfSize = size * 0.5f;
        corners = new Vector3[4];
    }

    private void Start()
    {
        Init();
    }

    private void Update()
    {
        
    }

    public void UpdateInfo()
    {
        center = box.center;
        
        float angle = -box.transform.eulerAngles.y * Mathf.Deg2Rad;
        Vector3[] localCorners = new Vector3[4];
        localCorners[0] = new Vector3(-halfSize.x, -halfSize.y, -halfSize.z);
        localCorners[1] = new Vector3(halfSize.x, -halfSize.y, -halfSize.z);
        localCorners[2] = new Vector3(halfSize.x, -halfSize.y, halfSize.z);
        localCorners[3] = new Vector3(-halfSize.x, -halfSize.y, halfSize.z);

        for (int i = 0; i < 4; i++)
        {
            float x = localCorners[i].x * Mathf.Cos(angle) - localCorners[i].z * Mathf.Sin(angle);
            float z = localCorners[i].x * Mathf.Sin(angle) + localCorners[i].z * Mathf.Cos(angle);
            corners[i] = new Vector3(x, localCorners[i].y, z) + box.transform.position + center;
        }

        info.p0.x = (corners[0].x - meshStart.x) / meshDistance; 
        info.p0.y = (corners[0].z - meshStart.y) / meshDistance;
        info.p1.x = (corners[1].x - meshStart.x) / meshDistance; 
        info.p1.y = (corners[1].z - meshStart.y) / meshDistance;
        info.p2.x = (corners[2].x - meshStart.x) / meshDistance; 
        info.p2.y = (corners[2].z - meshStart.y) / meshDistance;
        info.p3.x = (corners[3].x - meshStart.x) / meshDistance; 
        info.p3.y = (corners[3].z - meshStart.y) / meshDistance;
        info.h = corners[1].y;

        info.v = new Vector2(0,0);
    }
}
