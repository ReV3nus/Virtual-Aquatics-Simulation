using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.UIElements;
using static UnityEditor.PlayerSettings;

[CustomEditor(typeof(OceanController))]
public class OceanControllerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        OceanController generator = (OceanController)target;
        if (GUILayout.Button("Debug"))
        {
            generator.debugTst();
        }
    }
}
[System.Serializable]
public struct FFTData
{
    Vector2 FFTCalcBuffer, FFTGradientX, FFTGradientZ, FFTDX, FFTDZ, Jacobian;
    public FFTData(Vector2 fftCalcBuffer, Vector2 fftGradientX, Vector2 fftGradientZ,
                   Vector2 fftDX, Vector2 fftDZ, Vector2 jacobian)
    {
        FFTCalcBuffer = fftCalcBuffer;
        FFTGradientX = fftGradientX;
        FFTGradientZ = fftGradientZ;
        FFTDX = fftDX;
        FFTDZ = fftDZ;
        Jacobian = jacobian;
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
    public OceanPlane ocean;
    private float PatchSize = 10f;
    private int PatchVertexCount = 64;
    private int totalVertexCount = 4096;
    private float VertexDistance = 0.2f;



    [Header("Compute Shader")]
    private int kernalCalcCoefficientW;
    private Vector2[] CoefficientW;
    private ComputeBuffer CoefficientWBuffer;

    private int kernelCalcSpectrum;
    //private RenderTexture SpectrumTexture;
    private Vector3[] SpectrumH1, SpectrumH2;//(real, image, omegak)
    private ComputeBuffer SpectrumH1Buffer, SpectrumH2Buffer;
    private int kernelCalcSpecWithTime;
    private FFTData[] FFTDatas;
    private ComputeBuffer FFTDatasBuffer;
    private int[] FFTButterflyIndices;
    private ComputeBuffer FFTButterflyIndicesBuffer;

    private int kernelFFTZ, kernelFFTX;
    private int kernelUpdate;

    private int kernelDFT;


    [Header("Ocean Mesh")]
    private RenderTexture DisplacementTexture;
    private RenderTexture FoldingTexture;
    //private RenderTexture HeightTexture;
    //private RenderTexture DxTex, DzTex;
    //private RenderTexture NormalTexture;
    //private RenderTexture JacobianTexture;



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
    public void InitFromOceanPlane()
    {
        if (ocean == null)
            ocean = GetComponent<OceanPlane>();
        _groupX = ocean._groupX;
        _groupY = ocean._groupY;

        PatchSize = ocean.PatchSize;
        PatchVertexCount = ocean.PatchVertexCount;
        totalVertexCount = ocean.totalVertexCount;
        VertexDistance = ocean.VertexDistance;
    }
    public void InitGlobalComputeShader()
    {

        kernelCalcSpecWithTime = oceanSimulate.FindKernel("CalcPhillipsSpectrumWithTime");
        kernelFFTZ = oceanSimulate.FindKernel("CalcFFTonAxisZ");
        kernelFFTX = oceanSimulate.FindKernel("CalcFFTonAxisX");
        kernelUpdate = oceanSimulate.FindKernel("updateHeight");
        kernelCalcSpectrum = oceanSimulate.FindKernel("CalcPhillipsSpectrum");
        kernelDFT = oceanSimulate.FindKernel("CalcHeightByDFT");

        oceanSimulate.SetInt("_PatchVertexCount", PatchVertexCount);
        oceanSimulate.SetFloat("_PatchSize", PatchSize);
        oceanSimulate.SetFloats("_WindVelocity", WindVelocity.x, WindVelocity.y);
        oceanSimulate.SetFloat("_Amplitude", Amplitude);
        oceanSimulate.SetFloat("_SpectrumParamA", SpectrumParamA);
        oceanSimulate.SetFloat("_ChoppyWavesLambda", ChoppyWavesLambda);

        DisplacementTexture = new RenderTexture(PatchVertexCount, PatchVertexCount, 0, RenderTextureFormat.ARGBHalf);
        FoldingTexture = new RenderTexture(PatchVertexCount, PatchVertexCount, 0, RenderTextureFormat.ARGBHalf);
        //SpectrumTexture = new RenderTexture(PatchVertexCount, PatchVertexCount, 0, RenderTextureFormat.ARGBHalf);
        //HeightTexture = new RenderTexture(PatchVertexCount, PatchVertexCount, 0, RenderTextureFormat.ARGBHalf);
        //DxTex = new RenderTexture(PatchVertexCount, PatchVertexCount, 0, RenderTextureFormat.ARGBHalf);
        //DzTex = new RenderTexture(PatchVertexCount, PatchVertexCount, 0, RenderTextureFormat.ARGBHalf);
        //NormalTexture = new RenderTexture(PatchVertexCount, PatchVertexCount, 0, RenderTextureFormat.ARGBHalf);
        //JacobianTexture = new RenderTexture(PatchVertexCount, PatchVertexCount, 0, RenderTextureFormat.ARGBHalf);

        DisplacementTexture.enableRandomWrite = true;
        FoldingTexture.enableRandomWrite = true;
        //SpectrumTexture.enableRandomWrite = true;
        //HeightTexture.enableRandomWrite = true;
        //DxTex.enableRandomWrite = true;
        //DzTex.enableRandomWrite = true;
        //NormalTexture.enableRandomWrite = true;
        //JacobianTexture.enableRandomWrite = true;

        DisplacementTexture.Create();
        FoldingTexture.Create();
        //SpectrumTexture.Create();
        //HeightTexture.Create();
        //DxTex.Create();
        //DzTex.Create();
        //NormalTexture.Create();
        //JacobianTexture.Create();

        oceanSimulate.SetTexture(kernelUpdate, "_DisplacementTexture", DisplacementTexture);
        oceanSimulate.SetTexture(kernelUpdate, "_FoldingTexture", FoldingTexture);
        //oceanSimulate.SetTexture(kernelCalcSpectrum, "_SpectrumTexture", SpectrumTexture);
        //oceanSimulate.SetTexture(kernelCalcSpecWithTime, "_SpectrumTexture", SpectrumTexture);
        //oceanSimulate.SetTexture(kernelUpdate, "_HeightTexture", HeightTexture);
        //oceanSimulate.SetTexture(kernelUpdate, "_DxTex", DxTex);
        //oceanSimulate.SetTexture(kernelUpdate, "_DzTex", DzTex);
        //oceanSimulate.SetTexture(kernelUpdate, "_JacobianTexture", JacobianTexture);

        //FFTCalcBuffer = new Vector2[totalVertexCount];
        //FFTGradientX = new Vector2[totalVertexCount];
        //FFTGradientZ = new Vector2[totalVertexCount];
        //FFTDX = new Vector2[totalVertexCount];
        //FFTDZ = new Vector2[totalVertexCount];
        //Jacobian = new Vector2[totalVertexCount];
        FFTDatas = new FFTData[totalVertexCount];


        //gradientBuffer = new ComputeBuffer(totalVertexCount, 3 * sizeof(float));
        //FFTCalcComputeBuffer = new ComputeBuffer(totalVertexCount, 2 * sizeof(float));
        //FFTGradientXBuffer = new ComputeBuffer(totalVertexCount, 2 * sizeof(float));
        //FFTGradientZBuffer = new ComputeBuffer(totalVertexCount, 2 * sizeof(float));
        //FFTDXBuffer = new ComputeBuffer(totalVertexCount, 2 * sizeof(float));
        //FFTDZBuffer = new ComputeBuffer(totalVertexCount, 2 * sizeof(float));
        //JacobianBuffer = new ComputeBuffer(totalVertexCount, 2 * sizeof(float));
        FFTDatasBuffer = new ComputeBuffer(totalVertexCount, 12 * sizeof(float));


        //gradientBuffer.SetData(gradients);
        //FFTCalcComputeBuffer.SetData(FFTCalcBuffer);
        //FFTGradientXBuffer.SetData(FFTGradientX);
        //FFTGradientZBuffer.SetData(FFTGradientZ);
        //FFTDXBuffer.SetData(FFTDX);
        //FFTDZBuffer.SetData(FFTDZ);
        //JacobianBuffer.SetData(Jacobian);
        FFTDatasBuffer.SetData(FFTDatas);

        //oceanSimulate.SetBuffer(kernelCalcSpecWithTime, "_FFTCalcBuffer", FFTCalcComputeBuffer);
        //oceanSimulate.SetBuffer(kernelCalcSpecWithTime, "_FFTGradientX", FFTGradientXBuffer);
        //oceanSimulate.SetBuffer(kernelCalcSpecWithTime, "_FFTGradientZ", FFTGradientZBuffer);
        //oceanSimulate.SetBuffer(kernelCalcSpecWithTime, "_FFTDX", FFTDXBuffer);
        //oceanSimulate.SetBuffer(kernelCalcSpecWithTime, "_FFTDZ", FFTDZBuffer);
        //oceanSimulate.SetBuffer(kernelCalcSpecWithTime, "_Jacobian", JacobianBuffer);
        oceanSimulate.SetBuffer(kernelCalcSpecWithTime, "_FFTBuffer", FFTDatasBuffer);

        //oceanSimulate.SetBuffer(kernelFFTZ, "_FFTCalcBuffer", FFTCalcComputeBuffer);
        //oceanSimulate.SetBuffer(kernelFFTZ, "_FFTGradientX", FFTGradientXBuffer);
        //oceanSimulate.SetBuffer(kernelFFTZ, "_FFTGradientZ", FFTGradientZBuffer);
        //oceanSimulate.SetBuffer(kernelFFTZ, "_FFTDX", FFTDXBuffer);
        //oceanSimulate.SetBuffer(kernelFFTZ, "_FFTDZ", FFTDZBuffer);
        //oceanSimulate.SetBuffer(kernelFFTZ, "_Jacobian", JacobianBuffer);
        oceanSimulate.SetBuffer(kernelFFTZ, "_FFTBuffer", FFTDatasBuffer);

        //oceanSimulate.SetBuffer(kernelFFTX, "_FFTCalcBuffer", FFTCalcComputeBuffer);
        //oceanSimulate.SetBuffer(kernelFFTX, "_FFTGradientX", FFTGradientXBuffer);
        //oceanSimulate.SetBuffer(kernelFFTX, "_FFTGradientZ", FFTGradientZBuffer);
        //oceanSimulate.SetBuffer(kernelFFTX, "_FFTDX", FFTDXBuffer);
        //oceanSimulate.SetBuffer(kernelFFTX, "_FFTDZ", FFTDZBuffer);
        //oceanSimulate.SetBuffer(kernelFFTX, "_Jacobian", JacobianBuffer);
        oceanSimulate.SetBuffer(kernelFFTX, "_FFTBuffer", FFTDatasBuffer);

        //oceanSimulate.SetBuffer(kernelUpdate, "_FFTCalcBuffer", FFTCalcComputeBuffer);
        //oceanSimulate.SetBuffer(kernelUpdate, "_FFTGradientX", FFTGradientXBuffer);
        //oceanSimulate.SetBuffer(kernelUpdate, "_FFTGradientZ", FFTGradientZBuffer);
        //oceanSimulate.SetBuffer(kernelUpdate, "_FFTDX", FFTDXBuffer);
        //oceanSimulate.SetBuffer(kernelUpdate, "_FFTDZ", FFTDZBuffer);
        //oceanSimulate.SetBuffer(kernelUpdate, "_Jacobian", JacobianBuffer);
        oceanSimulate.SetBuffer(kernelUpdate, "_FFTBuffer", FFTDatasBuffer);

        oceanSimulate.SetBuffer(kernelUpdate, "positions", ocean.positionBuffer);
        oceanSimulate.SetBuffer(kernelUpdate, "origPositions", ocean.origPositionBuffer);
        oceanSimulate.SetBuffer(kernelUpdate, "normals", ocean.normalBuffer);
        //oceanSimulate.SetBuffer(kernelUpdate, "gradients", gradientBuffer);

        oceanSimulate.SetBuffer(kernelDFT, "positions", ocean.positionBuffer);
        oceanSimulate.SetBuffer(kernelDFT, "_FFTBuffer", FFTDatasBuffer);
        //oceanSimulate.SetBuffer(kernelDFT, "_FFTCalcBuffer", FFTCalcComputeBuffer);
    }
    void InitAndCalcPhillipsSpectrum()
    {

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
    void InitAndPrepareForFFT()
    {
        //kernalCalcCoefficientW = oceanSimulate.FindKernel("CalcFFTCoefficientW");

        CoefficientW = new Vector2[PatchVertexCount + 1];
        for(int i = 0;i < PatchVertexCount; i++)
        {
            float v = -2.0f * Mathf.PI / PatchVertexCount * i;
            CoefficientW[i] = new Vector2(Mathf.Cos(v), Mathf.Sin(v));
        }
        CoefficientW[PatchVertexCount] = CoefficientW[0];
        CoefficientWBuffer = new ComputeBuffer(PatchVertexCount + 1, 2 * sizeof(float));
        CoefficientWBuffer.SetData(CoefficientW);

        FFTButterflyIndices = new int[PatchVertexCount];
        FFTButterflyIndicesBuffer = new ComputeBuffer(PatchVertexCount, 1 * sizeof(int));
        int logN = (int)Mathf.Round(Mathf.Log10(PatchVertexCount)/Mathf.Log10(2));
        for (int i = 0; i < PatchVertexCount; i++)
            FFTButterflyIndices[i] = (FFTButterflyIndices[i >> 1] >> 1) | ((i & 1) << (logN - 1));
        FFTButterflyIndicesBuffer.SetData(FFTButterflyIndices);

       // oceanSimulate.SetBuffer(kernalCalcCoefficientW, "_FFTCoefficientW", CoefficientWBuffer);
        oceanSimulate.SetBuffer(kernelFFTX, "_FFTCoefficientW", CoefficientWBuffer);
        oceanSimulate.SetBuffer(kernelFFTZ, "_FFTCoefficientW", CoefficientWBuffer);

        oceanSimulate.SetBuffer(kernelFFTX, "_FFTButterflyIndices", FFTButterflyIndicesBuffer);
        oceanSimulate.SetBuffer(kernelFFTZ, "_FFTButterflyIndices", FFTButterflyIndicesBuffer);

        //oceanSimulate.Dispatch(kernalCalcCoefficientW, _groupX, 1, 1);
        //CoefficientWBuffer.GetData(CoefficientW);

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

        //oceanSimulate.Dispatch(kernelDFT, _groupX, _groupY, 1);

        //positionBuffer.GetData(positions);
        //normalBuffer.GetData(normals);
        //gradientBuffer.GetData(gradients);

        //oceanMaterial.SetTexture("_HeightTexture", HeightTexture);
        //oceanMaterial.SetTexture("_DxTex", DxTex);
        //oceanMaterial.SetTexture("_DzTex", DzTex);
        ocean.oceanMaterial.SetTexture("_DisplacementTexture", DisplacementTexture);
        ocean.oceanMaterial.SetTexture("_FoldingTexture", FoldingTexture);
        //oceanMaterial.SetTexture("_JacobianTexture", JacobianTexture);
    }





    /*********************************************************
    /  Monobehavior Functions
    *********************************************************/
    void Start()
    {
        ocean.CreatePlaneMesh();
        InitFromOceanPlane();

        InitGlobalComputeShader();
        InitAndCalcPhillipsSpectrum();
        InitAndPrepareForFFT();
    }



    void Update()
    {
        CalcPhillipsSpectrumWithTime();
        CalcHeightWithFFT();

        ocean.UpdatePlaneMesh();
    }
    private void OnDestroy()
    {
        //positionBuffer?.Release();
        //positionBuffer = null;
        //normalBuffer?.Release();
        //normalBuffer = null;
        CoefficientWBuffer?.Release();
        CoefficientWBuffer = null;
        //SpectrumH1Buffer?.Release();
        //SpectrumH1Buffer = null;
        //SpectrumH2Buffer?.Release();
        //SpectrumH2Buffer = null;

        //Array.Clear(positions, 0, positions.Length);
        //positions = null;
        //Array.Clear(normals, 0, normals.Length);
        //normals = null;
        Array.Clear(CoefficientW, 0, CoefficientW.Length);
        CoefficientW = null;
        //Array.Clear(SpectrumH1, 0, SpectrumH1.Length);
        //SpectrumH1 = null;
        //Array.Clear(SpectrumH2, 0, SpectrumH2.Length);
        //SpectrumH2 = null;
    }


    //public void CreatePlaneMesh()
    //{
    //    VertexDistance = PatchSize / (PatchVertexCount - 1);

    //    _groupX = PatchVertexCount / THREAD_X;
    //    _groupY = PatchVertexCount / THREAD_Y;

    //    totalVertexCount = PatchVertexCount * PatchVertexCount;
    //    oceanMesh = new Mesh();

    //    positions = new Vector3[totalVertexCount];
    //    Vector2[] uvs = new Vector2[totalVertexCount];

    //    for (int z = 0, i = 0; z < PatchVertexCount; z++)
    //    {
    //        for (int x = 0; x < PatchVertexCount; x++)
    //        {
    //            positions[i] = new Vector3(x * VertexDistance, 0, z * VertexDistance);
    //            normals[i] = new Vector3(0f, 1f, 0f);
    //            uvs[i] = new Vector2((float)x / (PatchVertexCount - 1), (float)z / (PatchVertexCount - 1));
    //            i++;
    //        }
    //    }
    //    oceanMesh.vertices = positions;
    //    oceanMesh.normals = normals;
    //    oceanMesh.uv = uvs;

    //    int[] triangles = new int[(PatchVertexCount - 1) * (PatchVertexCount - 1) * 6];
    //    for (int ti = 0, vi = 0, z = 0; z < PatchVertexCount - 1; z++, vi++)
    //    {
    //        for (int x = 0; x < PatchVertexCount - 1; x++, ti += 6, vi++)
    //        {
    //            triangles[ti] = vi;
    //            triangles[ti + 3] = triangles[ti + 2] = vi + 1;
    //            triangles[ti + 4] = triangles[ti + 1] = vi + PatchVertexCount;
    //            triangles[ti + 5] = vi + PatchVertexCount + 1;
    //        }
    //    }
    //    oceanMesh.triangles = triangles;

    //    oceanMesh.RecalculateBounds();

    //    MeshFilter meshFilter = GetComponent<MeshFilter>();
    //    if (meshFilter == null)
    //    {
    //        meshFilter = gameObject.AddComponent<MeshFilter>();
    //    }
    //    if (GetComponent<MeshRenderer>() == null)
    //    {
    //        gameObject.AddComponent<MeshRenderer>();
    //    }
    //    if (GetComponent<MeshCollider>() == null)
    //    {
    //        gameObject.GetComponent<MeshCollider>();
    //    }
    //    meshFilter.sharedMesh = oceanMesh;

    //    oceanMaterial = GetComponent<Renderer>().sharedMaterial;
    //}

    //public void UpdatePlaneMesh()
    //{
    //    oceanMesh.vertices = positions;
    //    //oceanMesh.RecalculateNormals();
    //    oceanMesh.normals = normals;
    //}

    public void debugTst()
    {
        ocean.CreatePlaneMesh();
        InitFromOceanPlane();

        InitGlobalComputeShader();
        InitAndCalcPhillipsSpectrum();
        InitAndPrepareForFFT();

        CalcPhillipsSpectrumWithTime();

        CalcHeightWithFFT();

        //FFTCalcComputeBuffer.GetData(FFTCalcBuffer);
        //for (int y = 0; y < PatchVertexCount; y++)
        //{
        //    for (int x = 0; x < PatchVertexCount; x++)
        //    {
        //        Debug.Log($"pos [{x},{y}] FFT: {FFTCalcBuffer[x + y * PatchVertexCount] * 1000}  H1: {SpectrumH1[x + y * PatchVertexCount] * 100000} H2: {SpectrumH2[x + y * PatchVertexCount] * 100000}");
        //    }
        //}

        //SaveTextureAsPNG(HeightTexture, "E:\\School\\Unity\\Homeworks\\Project\\Virtual Aquatics Simulation\\Assets\\Images\\HeightTexture.jpg");
        //SaveTextureAsPNG(DxTex, "E:\\School\\Unity\\Homeworks\\Project\\Virtual Aquatics Simulation\\Assets\\Images\\DxTex.jpg");
        //SaveTextureAsPNG(DzTex, "E:\\School\\Unity\\Homeworks\\Project\\Virtual Aquatics Simulation\\Assets\\Images\\DzTex.jpg");
        //SaveTextureAsPNG(DisplacementTexture, "DisplacementTexture.jpg");

        ocean.UpdatePlaneMesh();

        //ReadRenderTextureData(JacobianTexture);

    }

    void SaveTextureAsPNG(RenderTexture rt, string filePath)
    {
        Texture2D texture2D = new Texture2D(rt.width, rt.height, TextureFormat.RGBAHalf, false);
        RenderTexture.active = rt;
        texture2D.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
        texture2D.Apply();
        RenderTexture.active = null;
        byte[] bytes = texture2D.EncodeToPNG();
        System.IO.File.WriteAllBytes(filePath, bytes);
        Debug.Log("Saved image to: " + filePath);
    }


    void ReadRenderTextureData(RenderTexture rt)
    {
        RenderTexture.active = rt;
        Texture2D texture2D = new Texture2D(rt.width, rt.height, TextureFormat.RGBAHalf, false);
        texture2D.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
        texture2D.Apply();

        Color[] pixels = texture2D.GetPixels();
        for (int y = 0; y < PatchVertexCount; y++)
        {
            for (int x = 0; x < PatchVertexCount; x++)
            {
                Color pixel = pixels[y * rt.width + x];
                Debug.Log($"Pixel at ({x},{y}): {pixel}");
            }
        }

        RenderTexture.active = null;
    }
}
