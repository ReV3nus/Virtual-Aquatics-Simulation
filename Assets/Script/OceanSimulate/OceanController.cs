using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.UIElements;

[CustomEditor(typeof(OceanController))]
public class OceanControllerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        OceanController generator = (OceanController)target;
        if (GUILayout.Button("Generate Plane"))
        {
            generator.CreatePlaneMesh();
        }
        if (GUILayout.Button("Debug"))
        {
            generator.debugTst();
        }
    }
}
public class OceanController : MonoBehaviour
{
    public ComputeShader oceanSimulate;

    private const int THREAD_X = 8;
    private const int THREAD_Y = 8;
    private int _groupX;
    private int _groupY;

    [Header("Plane Attributes")]
    public float sizeX = 1f;
    public float sizeZ = 1f;
    public float VertexDistance = 0.2f;

    private int vertexCountX = 5;
    private int vertexCountZ = 5;
    private int totalVertexCount;


    private int kernelHandle;
    private ComputeBuffer positionBuffer;
    private ComputeBuffer normalBuffer;
    private Vector3[] positions;
    private Vector3[] normals;

    private Mesh oceanMesh;

    [Space]
    [Header("Ocean Simulation Params")]
    public int octaves = 8;
    public float Amplitude = 1f;
    public Vector3 WindVelocity = new Vector3(1f, 0f, 0f);

    //frequency, amplitude, speed, dir

    public void Initialize()
    {
        oceanSimulate.SetInts("size", vertexCountX, vertexCountZ, totalVertexCount);
        oceanSimulate.SetFloats("_WindVelocity", WindVelocity.x, WindVelocity.y, WindVelocity.z);
        oceanSimulate.SetFloat("_Amplitude", Amplitude);
        oceanSimulate.SetInt("_Octaves", octaves);

        positionBuffer = new ComputeBuffer(totalVertexCount, 3 * sizeof(float));
        positionBuffer.SetData(positions);
        normalBuffer = new ComputeBuffer(totalVertexCount, 3 * sizeof(float));
        normalBuffer.SetData(normals);

        kernelHandle = oceanSimulate.FindKernel("CSMain");
        oceanSimulate.SetBuffer(kernelHandle, "positions", positionBuffer);
        oceanSimulate.SetBuffer(kernelHandle, "normals", normalBuffer);
    }

    void Start()
    {
        CreatePlaneMesh();
        Initialize();
    }

    void Update()
    {
        oceanSimulate.SetFloat("_Time", Time.time);
        oceanSimulate.Dispatch(kernelHandle, _groupX, _groupY, 1);
        positionBuffer.GetData(positions);

        for (int i = 0; i < 4; i++)
        {
            Debug.Log($"Vertex {i}: {positions[i]}");
        }

        UpdatePlaneMesh();
    }
    private void OnDestroy()
    {
        positionBuffer?.Release();
        positionBuffer = null;

        Array.Clear(positions, 0, positions.Length);
        positions = null;
    }


    public void CreatePlaneMesh()
    {
        vertexCountX = Mathf.RoundToInt(sizeX / VertexDistance) + 1;
        vertexCountZ = Mathf.RoundToInt(sizeZ / VertexDistance) + 1;
        _groupX = (int)Mathf.Ceil((float)vertexCountX / (float)THREAD_X);
        _groupY = (int)Mathf.Ceil((float)vertexCountZ / (float)THREAD_Y);
        vertexCountX = _groupX * THREAD_X;
        vertexCountZ = _groupY * THREAD_Y;
        sizeX = (vertexCountX - 1) * VertexDistance;
        sizeZ = (vertexCountZ - 1) * VertexDistance;
        totalVertexCount = vertexCountX * vertexCountZ;

        oceanMesh = new Mesh();

        positions = new Vector3[totalVertexCount];
        normals = new Vector3[totalVertexCount];
        for (int z = 0, i = 0; z < vertexCountZ; z++)
        {
            for (int x = 0; x < vertexCountX; x++)
            {
                positions[i] = new Vector3(x * VertexDistance, 0, z * VertexDistance);
                normals[i] = new Vector3(0f, 1f, 0f);
                i++;
            }
        }
        oceanMesh.vertices = positions;
        oceanMesh.normals = normals;

        int[] triangles = new int[(vertexCountX - 1) * (vertexCountZ - 1) * 6];
        for (int ti = 0, vi = 0, z = 0; z < vertexCountZ - 1; z++, vi++)
        {
            for (int x = 0; x < vertexCountX - 1; x++, ti += 6, vi++)
            {
                triangles[ti] = vi;
                triangles[ti + 3] = triangles[ti + 2] = vi + 1;
                triangles[ti + 4] = triangles[ti + 1] = vi + vertexCountX;
                triangles[ti + 5] = vi + vertexCountX + 1;
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
        meshFilter.sharedMesh = oceanMesh;
    }

    public void UpdatePlaneMesh()
    {
        oceanMesh.vertices = positions;
        oceanMesh.RecalculateNormals();
      //  oceanMesh.normals = normals;
    }

    public void debugTst()
    {
        CreatePlaneMesh();
        Initialize();
        oceanSimulate.Dispatch(kernelHandle, _groupX, _groupY, 1);
        positionBuffer.GetData(positions);
        UpdatePlaneMesh();
    }
}
