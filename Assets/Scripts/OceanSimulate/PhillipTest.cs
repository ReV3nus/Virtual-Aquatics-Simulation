using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(PhillipTest))]
public class PhillipTestEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        PhillipTest generator = (PhillipTest)target;
        if (GUILayout.Button("Test"))
        {
            generator.start();
        }
    }
}
public class PhillipTest : MonoBehaviour
{
    public ComputeShader oceanSimulate;

    private const int THREAD_X = 8;
    private const int THREAD_Y = 8;
    private int _groupX = 8;
    private int _groupY = 8;

    private int kernelCalcSpectrum;
    private RenderTexture SpectrumTextureCos, SpectrumTextureSin; //float4(real, image, omega, 1)
    public float Amplitude = 1f;
    public Vector2 WindVelocity = new Vector2(1f, 0f);


    void InitAndCalcPhillipsSpectrum()
    {
        kernelCalcSpectrum = oceanSimulate.FindKernel("CalcPhillipsSpectrum");

        oceanSimulate.SetInt("_PatchVertexCount", 64);
        oceanSimulate.SetFloat("_PatchSize", 10f);
        oceanSimulate.SetFloats("_WindVelocity", 1f, 0f);
        oceanSimulate.SetFloat("_Amplitude", Amplitude);
        oceanSimulate.SetFloat("_SpectrumParamA", Amplitude);

        SpectrumTextureCos = new RenderTexture(64, 64, 0, RenderTextureFormat.ARGBHalf);
        SpectrumTextureSin = new RenderTexture(64, 64, 0, RenderTextureFormat.ARGBHalf);
        SpectrumTextureCos.enableRandomWrite = true;
        SpectrumTextureSin.enableRandomWrite = true;
        SpectrumTextureCos.Create();
        SpectrumTextureSin.Create();

        oceanSimulate.SetTexture(kernelCalcSpectrum, "_SpectrumTextureCos", SpectrumTextureCos);
        oceanSimulate.SetTexture(kernelCalcSpectrum, "_SpectrumTextureSin", SpectrumTextureSin);

        oceanSimulate.Dispatch(kernelCalcSpectrum, _groupX, _groupY, 1);
    }
    void Start()
    {
        InitAndCalcPhillipsSpectrum();
        GetComponent<Renderer>().material.mainTexture = SpectrumTextureCos;

        RenderTexture.active = SpectrumTextureCos;
        Texture2D tex = new Texture2D(64, 64, TextureFormat.RGBAFloat, false);
        tex.ReadPixels(new Rect(0, 0, 64,64), 0, 0);
        tex.Apply();
        RenderTexture.active = null;

        for (int y = 0; y < 64; y++)
        {
            for (int x = 0; x < 64; x++)
            {
                Color pixelValue = tex.GetPixel(x, y);
                Debug.Log($"Pixel [{x},{y}] Value: {pixelValue}");
            }
        }
    }
    public void start()
    {
        InitAndCalcPhillipsSpectrum();
        GetComponent<Renderer>().material.mainTexture = SpectrumTextureCos;

        RenderTexture.active = SpectrumTextureCos;
        Texture2D tex = new Texture2D(64, 64, TextureFormat.RGBAFloat, false);
        tex.ReadPixels(new Rect(0, 0, 64, 64), 0, 0);
        tex.Apply();
        RenderTexture.active = null;

        for (int y = 0; y < 64; y++)
        {
            for (int x = 0; x < 64; x++)
            {
                Color pixelValue = tex.GetPixel(x, y);
                Debug.Log($"Pixel [{x},{y}] Value: {pixelValue}");
            }
        }
    }
}
