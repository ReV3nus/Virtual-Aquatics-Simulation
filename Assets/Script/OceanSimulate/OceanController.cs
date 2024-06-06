using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
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

public class Complex
{
    public float r;
    public float i;
    Complex(float r, float i)
    { 
        this.r = r;
        this.i = i;
    }
    public static Complex operator +(Complex a, Complex b)
    {
        Complex c = new Complex(a.r + b.r, a.i + b.i);
        return c;
    }
    public static Complex operator -(Complex a, Complex b)
    {
        Complex c = new Complex(a.r - b.r, a.i - b.i);
        return c;
    }
    public static Complex operator *(Complex a, Complex b)
    {
        return new Complex(a.r * b.r - a.i * b.i, a.r * b.i + a.i * b.r);
    }

}

public class OceanController : MonoBehaviour
{
    public ComputeShader oceanSimulate;

    private const int THREAD_X = 8;
    private const int THREAD_Y = 8;
    private int _groupX;
    private int _groupY;



    [Header("Ocean Plane Attributes")]
    public Vector2 PlaneSize;
    public float PatchSize = 10f;
    public int PatchVertexCount = 64;
    private int totalVertexCount = 4096;
    private float VertexDistance = 0.2f;



    [Header("Compute Shader")]
    private int kernalCalcCoefficientW;
    private Vector2[] CoefficientW;
    private ComputeBuffer CoefficientWBuffer;

    private int kernelCalcSpectrum;
    private Vector3[] SpectrumH1, SpectrumH2;//(real, image, omegak)
    private ComputeBuffer SpectrumH1Buffer, SpectrumH2Buffer;
    private int kernelCalcSpecWithTime;
    private Vector2[] FFTCalcBuffer;
    private ComputeBuffer FFTCalcComputeBuffer;
    private Vector2[] FFTGradientX, FFTGradientZ;
    private ComputeBuffer FFTGradientXBuffer, FFTGradientZBuffer;

    private int kernelFFTZ, kernelFFTX;
    private int kernelUpdate;
    private ComputeBuffer positionBuffer, origPositionBuffer;
    private ComputeBuffer normalBuffer;
    private ComputeBuffer gradientBuffer;


    [Header("Ocean Mesh")]
    private Mesh oceanMesh;
    private Vector3[] positions;
    private Vector3[] normals;
    private Vector3[] gradients;



    [Space]
    [Header("Ocean Simulation Params")]
    public float Amplitude = 1f;
    public float TimeMultiplication = 1f;
    public float ChoppyWavesLambda = 1f;
    private float SpectrumParamA = 1f;
    public Vector2 WindVelocity = new Vector2(1f, 1f);

    //frequency, amplitude, speed, dir



    /*********************************************************
    /  Initializing Functions
    *********************************************************/

    public void InitGlobalComputeShader()
    {
        oceanSimulate.SetInt("_PatchVertexCount", PatchVertexCount);
        oceanSimulate.SetFloat("_PatchSize", PatchSize);
        oceanSimulate.SetFloats("_WindVelocity", WindVelocity.x, WindVelocity.y);
        oceanSimulate.SetFloat("_Amplitude", Amplitude);
        oceanSimulate.SetFloat("_SpectrumParamA", SpectrumParamA);
        oceanSimulate.SetFloat("_ChoppyWavesLambda", ChoppyWavesLambda);

        FFTCalcBuffer = new Vector2[totalVertexCount];
        FFTGradientX = new Vector2[totalVertexCount];
        FFTGradientZ = new Vector2[totalVertexCount];

        positionBuffer = new ComputeBuffer(totalVertexCount, 3 * sizeof(float));
        origPositionBuffer = new ComputeBuffer(totalVertexCount, 3 * sizeof(float));
        normalBuffer = new ComputeBuffer(totalVertexCount, 3 * sizeof(float));
        gradientBuffer = new ComputeBuffer(totalVertexCount, 3 * sizeof(float));
        FFTCalcComputeBuffer = new ComputeBuffer(totalVertexCount, 2 * sizeof(float));
        FFTGradientXBuffer = new ComputeBuffer(totalVertexCount, 2 * sizeof(float));
        FFTGradientZBuffer = new ComputeBuffer(totalVertexCount, 2 * sizeof(float));

        positionBuffer.SetData(positions);
        origPositionBuffer.SetData(positions);
        normalBuffer.SetData(normals);
        gradientBuffer.SetData(gradients);
        FFTCalcComputeBuffer.SetData(FFTCalcBuffer);
        FFTGradientXBuffer.SetData(FFTGradientX);
        FFTGradientZBuffer.SetData(FFTGradientZ);

        kernelCalcSpecWithTime = oceanSimulate.FindKernel("CalcPhillipsSpectrumWithTime");
        kernelFFTZ = oceanSimulate.FindKernel("CalcFFTonAxisZ");
        kernelFFTX = oceanSimulate.FindKernel("CalcFFTonAxisX");
        kernelUpdate = oceanSimulate.FindKernel("updateHeight");

        oceanSimulate.SetBuffer(kernelCalcSpecWithTime, "_FFTCalcBuffer", FFTCalcComputeBuffer);
        oceanSimulate.SetBuffer(kernelCalcSpecWithTime, "_FFTGradientX", FFTGradientXBuffer);
        oceanSimulate.SetBuffer(kernelCalcSpecWithTime, "_FFTGradientZ", FFTGradientZBuffer);

        oceanSimulate.SetBuffer(kernelFFTZ, "_FFTCalcBuffer", FFTCalcComputeBuffer);
        oceanSimulate.SetBuffer(kernelFFTZ, "_FFTGradientX", FFTGradientXBuffer);
        oceanSimulate.SetBuffer(kernelFFTZ, "_FFTGradientZ", FFTGradientZBuffer);

        oceanSimulate.SetBuffer(kernelFFTX, "_FFTCalcBuffer", FFTCalcComputeBuffer);
        oceanSimulate.SetBuffer(kernelFFTX, "_FFTGradientX", FFTGradientXBuffer);
        oceanSimulate.SetBuffer(kernelFFTX, "_FFTGradientZ", FFTGradientZBuffer);

        oceanSimulate.SetBuffer(kernelUpdate, "_FFTCalcBuffer", FFTCalcComputeBuffer);
        oceanSimulate.SetBuffer(kernelUpdate, "_FFTGradientX", FFTGradientXBuffer);
        oceanSimulate.SetBuffer(kernelUpdate, "_FFTGradientZ", FFTGradientZBuffer);


        oceanSimulate.SetBuffer(kernelUpdate, "positions", positionBuffer);
        oceanSimulate.SetBuffer(kernelUpdate, "origPositions", origPositionBuffer);
        oceanSimulate.SetBuffer(kernelUpdate, "normals", normalBuffer);
        oceanSimulate.SetBuffer(kernelUpdate, "gradients", gradientBuffer);
    }
    void InitAndCalcPhillipsSpectrum()
    {
        kernelCalcSpectrum = oceanSimulate.FindKernel("CalcPhillipsSpectrum");

        SpectrumH1 = new Vector3[totalVertexCount];
        SpectrumH2 = new Vector3[totalVertexCount];
        SpectrumH1Buffer = new ComputeBuffer(totalVertexCount, 3 * sizeof(float));
        SpectrumH2Buffer = new ComputeBuffer(totalVertexCount, 3 * sizeof(float));
        SpectrumH1Buffer.SetData(SpectrumH1);
        SpectrumH2Buffer.SetData(SpectrumH2);

        oceanSimulate.SetBuffer(kernelCalcSpectrum, "_SpectrumH1", SpectrumH1Buffer);
        oceanSimulate.SetBuffer(kernelCalcSpectrum, "_SpectrumH2", SpectrumH2Buffer);
        oceanSimulate.SetBuffer(kernelCalcSpecWithTime, "_SpectrumH1", SpectrumH1Buffer);
        oceanSimulate.SetBuffer(kernelCalcSpecWithTime, "_SpectrumH2", SpectrumH2Buffer);

        oceanSimulate.Dispatch(kernelCalcSpectrum, _groupX, _groupY, 1);
        SpectrumH1Buffer.GetData(SpectrumH1);
        SpectrumH2Buffer.GetData(SpectrumH2);
    }
    void InitAndCalcCoefficientW()
    {
        kernalCalcCoefficientW = oceanSimulate.FindKernel("CalcFFTCoefficientW");

        CoefficientW = new Vector2[PatchVertexCount];
        CoefficientWBuffer = new ComputeBuffer(PatchVertexCount, 2 * sizeof(float));
        CoefficientWBuffer.SetData(CoefficientW);
        oceanSimulate.SetBuffer(kernalCalcCoefficientW, "_FFTCoefficientW", CoefficientWBuffer);
        oceanSimulate.SetBuffer(kernelFFTX, "_FFTCoefficientW", CoefficientWBuffer);
        oceanSimulate.SetBuffer(kernelFFTZ, "_FFTCoefficientW", CoefficientWBuffer);

        oceanSimulate.Dispatch(kernalCalcCoefficientW, _groupX, 1, 1);
        CoefficientWBuffer.GetData(CoefficientW);
    }



    /*********************************************************
    /  Calculative Updating Functions
    *********************************************************/
    void CalcPhillipsSpectrumWithTime()
    {
        oceanSimulate.SetFloat("_Time", Time.time * TimeMultiplication);
        oceanSimulate.Dispatch(kernelCalcSpecWithTime, _groupX, _groupY, 1);

    }
    void CalcHeightWithFFT()
    {
        oceanSimulate.Dispatch(kernelFFTZ, _groupX, 1, 1);
        oceanSimulate.Dispatch(kernelFFTX, _groupX, 1, 1);

        oceanSimulate.Dispatch(kernelUpdate, _groupX, _groupY, 1);
        positionBuffer.GetData(positions);
        normalBuffer.GetData(normals);
        gradientBuffer.GetData(gradients);
    }





    /*********************************************************
    /  Monobehavior Functions
    *********************************************************/
    void Start()
    {
        CreatePlaneMesh();

        InitGlobalComputeShader();
        InitAndCalcPhillipsSpectrum();
        InitAndCalcCoefficientW();
    }



    void Update()
    {
        CalcPhillipsSpectrumWithTime();
        CalcHeightWithFFT();

        UpdatePlaneMesh();
    }
    private void OnDestroy()
    {
        positionBuffer?.Release();
        positionBuffer = null;
        normalBuffer?.Release();
        normalBuffer = null;
        CoefficientWBuffer?.Release();
        CoefficientWBuffer = null;
        SpectrumH1Buffer?.Release();
        SpectrumH1Buffer = null;
        SpectrumH2Buffer?.Release();
        SpectrumH2Buffer = null;
        FFTCalcComputeBuffer?.Release();
        FFTCalcComputeBuffer = null;

        Array.Clear(positions, 0, positions.Length);
        positions = null;
        Array.Clear(normals, 0, normals.Length);
        normals = null;
        Array.Clear(CoefficientW, 0, CoefficientW.Length);
        CoefficientW = null;
        Array.Clear(FFTCalcBuffer, 0, FFTCalcBuffer.Length);
        FFTCalcBuffer = null;
        Array.Clear(SpectrumH1, 0, SpectrumH1.Length);
        SpectrumH1 = null;
        Array.Clear(SpectrumH2, 0, SpectrumH2.Length);
        SpectrumH2 = null;
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
        for (int z = 0, i = 0; z < PatchVertexCount; z++)
        {
            for (int x = 0; x < PatchVertexCount; x++)
            {
                positions[i] = new Vector3(x * VertexDistance, 0, z * VertexDistance);
                normals[i] = new Vector3(0f, 1f, 0f);
                i++;
            }
        }
        oceanMesh.vertices = positions;
        oceanMesh.normals = normals;

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
        meshFilter.sharedMesh = oceanMesh;
    }

    public void UpdatePlaneMesh()
    {
        oceanMesh.vertices = positions;
        oceanMesh.RecalculateNormals();
        //oceanMesh.normals = normals;
    }

    public void debugTst()
    {
        CreatePlaneMesh();

        InitGlobalComputeShader();
        InitAndCalcPhillipsSpectrum();
        InitAndCalcCoefficientW();

        CalcPhillipsSpectrumWithTime();


        CalcHeightWithFFT();

        //FFTCalcComputeBuffer.GetData(FFTCalcBuffer);
        //for (int y = 0; y < PatchVertexCount; y++)
        //{
        //    for (int x = 0; x < PatchVertexCount; x++)
        //    {
        //        Debug.Log($"pos [{x},{y}] Value: {FFTCalcBuffer[x + y * PatchVertexCount]*10000}, pos: {positions[x + y * PatchVertexCount]}");
        //    }
        //}

        UpdatePlaneMesh();


        //for (int y = 0; y < PatchVertexCount; y++)
        //{
        //    for (int x = 0; x < PatchVertexCount; x++)
        //    {
        //        Debug.Log($"pos [{x},{y}] Value: {positions[x + y * PatchVertexCount]}, grad [{x},{y}] Value: {gradients[x + y * PatchVertexCount]}, norm [{x},{y}] Value: {normals[x + y * PatchVertexCount]}, calcNorm [{x},{y}] Value: {oceanMesh.normals[x + y * PatchVertexCount]}");
        //    }
        //}
    }
}
