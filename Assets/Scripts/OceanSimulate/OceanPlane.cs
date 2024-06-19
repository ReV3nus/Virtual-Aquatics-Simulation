using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(OceanPlane))]
public class OceanPlaneEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        OceanPlane generator = (OceanPlane)target;
        if (GUILayout.Button("Generate Plane"))
        {
            generator.CreatePlaneMesh();
        }
    }
}
public class OceanPlane : MonoBehaviour
{
    private const int THREAD_X = 8;
    private const int THREAD_Y = 8;
    [HideInInspector]
    public int _groupX;
    [HideInInspector]
    public int _groupY;


    [Header("Ocean Plane Attributes")]
    public float PatchSize = 10f;
    public int PatchVertexCount = 64;
    [HideInInspector]
    public int totalVertexCount = 4096;
    [HideInInspector]
    public float VertexDistance = 0.2f;




    [Header("Ocean Mesh")]
    [HideInInspector]
    public Mesh oceanMesh;
    [HideInInspector]
    public Material oceanMaterial;
    [HideInInspector]
    public Vector3[] positions, normals , gradients;
    [HideInInspector]
    public ComputeBuffer positionBuffer, origPositionBuffer, normalBuffer;



    float TMPFUNC(float x, float z)
    {
        //return 0f;
        x = x / PatchVertexCount - 0.5f;
        z = z / PatchVertexCount - 0.5f;
        if (Mathf.Abs(x) > 0.1f || MathF.Abs(z) > 0.1f)
            return 0f;

        x = x * Mathf.PI / 0.2f;
        z = z * Mathf.PI / 0.2f;
        //return Mathf.Cos(x);
        //return 0f;
        return 0f;
        //return 1f * (Mathf.Cos(x) + Mathf.Cos(z));
        //Vector2 dist = new Vector2(x - PatchVertexCount / 2f, z - PatchVertexCount / 2f);
        //float tmp = Mathf.Sqrt(dist.x * dist.x + dist.y * dist.y);
        //return 2f-tmp * 1f;
    }
    public void CreatePlaneMesh()
    {
        VertexDistance = PatchSize / (PatchVertexCount - 1);

        _groupX = PatchVertexCount / THREAD_X;
        _groupY = PatchVertexCount / THREAD_Y;

        totalVertexCount = PatchVertexCount * PatchVertexCount;
        oceanMesh = new Mesh();

        positions = new Vector3[totalVertexCount];
        normals = new Vector3[totalVertexCount];
        gradients = new Vector3[totalVertexCount];
        Vector2[] uvs = new Vector2[totalVertexCount];

        for (int z = 0, i = 0; z < PatchVertexCount; z++)
        {
            for (int x = 0; x < PatchVertexCount; x++)
            {
                positions[i] = new Vector3(x * VertexDistance, 0, z * VertexDistance);
                positions[i].y = TMPFUNC(x, z);
                normals[i] = new Vector3(0f, 1f, 0f);
                uvs[i] = new Vector2((float)x / (PatchVertexCount - 1), (float)z / (PatchVertexCount - 1));
                i++;
            }
        }
        oceanMesh.vertices = positions;
        oceanMesh.normals = normals;
        oceanMesh.uv = uvs;

        int[] triangles = new int[(PatchVertexCount - 1) * (PatchVertexCount - 1) * 6];
        for (int ti = 0, vi = 0, z = 0; z < PatchVertexCount - 1; z++, vi++)
        {
            for (int x = 0; x < PatchVertexCount - 1; x++, ti += 6, vi++)
            {
                triangles[ti] = vi;
                triangles[ti + 3] = triangles[ti + 2] = vi + 1;
                triangles[ti + 4] = triangles[ti + 1] = vi + PatchVertexCount;
                triangles[ti + 5] = vi + PatchVertexCount + 1;
            }
        }
        oceanMesh.triangles = triangles;

        oceanMesh.RecalculateBounds();

        MeshFilter meshFilter = GetComponent<MeshFilter>();
        if (meshFilter == null)
        {
            meshFilter = gameObject.AddComponent<MeshFilter>();
        }
        if (GetComponent<MeshRenderer>() == null)
        {
            gameObject.AddComponent<MeshRenderer>();
        }
        if (GetComponent<MeshCollider>() == null)
        {
            gameObject.GetComponent<MeshCollider>();
        }

        oceanMesh.RecalculateNormals();
        meshFilter.sharedMesh = oceanMesh;

        
        oceanMaterial = GetComponent<Renderer>().sharedMaterial;




        positionBuffer = new ComputeBuffer(totalVertexCount, 3 * sizeof(float));
        origPositionBuffer = new ComputeBuffer(totalVertexCount, 3 * sizeof(float));
        normalBuffer = new ComputeBuffer(totalVertexCount, 3 * sizeof(float));

        positionBuffer.SetData(positions);
        origPositionBuffer.SetData(positions);
        normalBuffer.SetData(normals);
    }

    public void UpdatePlaneMesh()
    {
        positionBuffer.GetData(positions);
        oceanMesh.vertices = positions;
        oceanMesh.RecalculateNormals();
    }
}
