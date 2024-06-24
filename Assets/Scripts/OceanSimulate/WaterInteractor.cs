
using System.IO;
using System.Linq;
// using UnityEditor;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.UIElements;
using static OceanCollide;
// using static UnityEditor.PlayerSettings;

// [CustomEditor(typeof(WaterInteractor))]
// public class WaterInteractorEditor : Editor
// {
//     public override void OnInspectorGUI()
//     {
//         DrawDefaultInspector();
//
//         WaterInteractor generator = (WaterInteractor)target;
//         if (GUILayout.Button("Init"))
//         {
//             generator.initTst();
//         }
//         if (GUILayout.Button("Debug"))
//         {
//             generator.debugTst();
//         }
//     }
// }
public class WaterInteractor : MonoBehaviour
{
    public OceanPlane ocean;
    public OceanCollide obj;


    private const int THREAD_X = 8;
    private const int THREAD_Y = 8;
    private int _groupX;
    private int _groupY;

    [Header("Navier-Stokes Attributes")]
    public ComputeShader Solver;
    public float _deltaTime;
    public float ChoppyLambda;
    private float _Timer;
    private RenderTexture GridVxTexture, GridVzTexture;
    private RenderTexture newGridVxTexture, newGridVzTexture;
    //private RenderTexture VelocityTexture, newVelocityTexture;
    private RenderTexture PressureTexture, newPressureTexture;
    private int kernelUpdate, kernelInit, kernelAdvect, kernelPress, kernelProj, kernelCheckCon, kernelCalcDisp, kernelApplyDisp;

    private RenderTexture DisplacementTexture;
    private RenderTexture FoldingTexture;


    [Header("Ocean Plane Attributes")]
    private float PatchSize = 10f;
    private int PatchVertexCount = 64;
    private int totalVertexCount = 4096;
    private float VertexDistance = 0.2f;

    private ComputeBuffer boxColliderBuffer;
    private BoxColliderInfo[] boxColliderData;
    private RenderTexture ConstraintTexture;
    private int[] zeroDispRes;
    private ComputeBuffer dispResBuffer;



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

    public void InitConstraints()
    {
        boxColliderData = new BoxColliderInfo[1];
        boxColliderBuffer = new ComputeBuffer(1, 11 * sizeof(float));

        zeroDispRes = new int[2];
        zeroDispRes[0] = 0;
        zeroDispRes[1] = 0;
        dispResBuffer = new ComputeBuffer(2, sizeof(int));
        dispResBuffer.SetData(zeroDispRes);
    }
    public void InitGlobalComputeShader()
    {
        InitConstraints();

        kernelUpdate = Solver.FindKernel("UpdateStatus");
        kernelInit = Solver.FindKernel("InitPressureAndVelocity");
        kernelAdvect = Solver.FindKernel("CalcAdvectionAndForce");
        kernelPress = Solver.FindKernel("UpdatePoissonPressure");
        kernelProj = Solver.FindKernel("CalcProjection");
        kernelCheckCon = Solver.FindKernel("CheckConstraints");
        kernelCalcDisp = Solver.FindKernel("ClacDisplacement");
        kernelApplyDisp = Solver.FindKernel("ApplyDisplacement");

        Solver.SetInt("_PatchVertexCount", PatchVertexCount);
        Solver.SetInt("_TotalBoxColliders", obj != null ? 1 : 0);
        Solver.SetFloat("_PatchSize", PatchSize);
        Solver.SetFloat("dl", VertexDistance);
        Solver.SetFloat("_ChoppyLambda", ChoppyLambda);

        GridVxTexture = new RenderTexture(PatchVertexCount + 1, PatchVertexCount, 0, RenderTextureFormat.RHalf);
        //GridVyTexture = new RenderTexture(PatchVertexCount, PatchVertexCount, 0, RenderTextureFormat.RHalf);
        GridVzTexture = new RenderTexture(PatchVertexCount, PatchVertexCount + 1, 0, RenderTextureFormat.RHalf);
        newGridVxTexture = new RenderTexture(PatchVertexCount + 1, PatchVertexCount, 0, RenderTextureFormat.RHalf);
        //newGridVyTexture = new RenderTexture(PatchVertexCount, PatchVertexCount, 0, RenderTextureFormat.RHalf);
        newGridVzTexture = new RenderTexture(PatchVertexCount, PatchVertexCount + 1, 0, RenderTextureFormat.RHalf);
        //VelocityTexture = new RenderTexture(PatchVertexCount, PatchVertexCount, 0, RenderTextureFormat.ARGBHalf);
        PressureTexture = new RenderTexture(PatchVertexCount, PatchVertexCount, 0, RenderTextureFormat.RHalf);
        //newVelocityTexture = new RenderTexture(PatchVertexCount, PatchVertexCount, 0, RenderTextureFormat.ARGBHalf);
        newPressureTexture = new RenderTexture(PatchVertexCount, PatchVertexCount, 0, RenderTextureFormat.RHalf);
        ConstraintTexture = new RenderTexture(PatchVertexCount, PatchVertexCount, 0, RenderTextureFormat.RHalf);
        DisplacementTexture = new RenderTexture(PatchVertexCount, PatchVertexCount, 0, RenderTextureFormat.ARGBHalf);
        FoldingTexture = new RenderTexture(PatchVertexCount, PatchVertexCount, 0, RenderTextureFormat.ARGBHalf);

        GridVxTexture.enableRandomWrite = true;
        //GridVyTexture.enableRandomWrite = true;
        GridVzTexture.enableRandomWrite = true;
        newGridVxTexture.enableRandomWrite = true;
        //newGridVyTexture.enableRandomWrite = true;
        newGridVzTexture.enableRandomWrite = true;
        //VelocityTexture.enableRandomWrite = true;
        PressureTexture.enableRandomWrite = true;
        //newVelocityTexture.enableRandomWrite = true;
        newPressureTexture.enableRandomWrite = true;
        ConstraintTexture.enableRandomWrite = true;
        DisplacementTexture.enableRandomWrite = true;
        FoldingTexture.enableRandomWrite = true;

        GridVxTexture.Create();
        //GridVyTexture.Create();
        GridVzTexture.Create();
        newGridVxTexture.Create();
        //newGridVyTexture.Create();
        newGridVzTexture.Create();
        //VelocityTexture.Create();
        PressureTexture.Create();
        //newVelocityTexture.Create();
        newPressureTexture.Create();
        ConstraintTexture.Create();
        DisplacementTexture.Create();
        FoldingTexture.Create();

        //Solver.SetTexture(kernelInit, "_VelocityTexture", VelocityTexture);
        Solver.SetTexture(kernelInit, "_GridVxTexture", GridVxTexture);
        //Solver.SetTexture(kernelInit, "_GridVyTexture", GridVyTexture);
        Solver.SetTexture(kernelInit, "_GridVzTexture", GridVzTexture);
        Solver.SetTexture(kernelInit, "_PressureTexture", PressureTexture);
        Solver.SetBuffer(kernelInit, "positions", ocean.positionBuffer);

        //Solver.SetTexture(kernelAdvect, "_VelocityTexture", VelocityTexture);
        //Solver.SetTexture(kernelAdvect, "_NewVelocityTexture", newVelocityTexture);
        Solver.SetTexture(kernelAdvect, "_GridVxTexture", GridVxTexture);
        //Solver.SetTexture(kernelAdvect, "_GridVyTexture", GridVyTexture);
        Solver.SetTexture(kernelAdvect, "_GridVzTexture", GridVzTexture);
        Solver.SetTexture(kernelAdvect, "_newGridVxTexture", newGridVxTexture);
        //Solver.SetTexture(kernelAdvect, "_newGridVyTexture", newGridVyTexture);
        Solver.SetTexture(kernelAdvect, "_newGridVzTexture", newGridVzTexture);
        Solver.SetBuffer(kernelAdvect, "boxInfos", boxColliderBuffer);

        //Solver.SetTexture(kernelPress, "_NewVelocityTexture", newVelocityTexture);
        Solver.SetTexture(kernelPress, "_newGridVxTexture", newGridVxTexture);
        //Solver.SetTexture(kernelPress, "_newGridVyTexture", newGridVyTexture);
        Solver.SetTexture(kernelPress, "_newGridVzTexture", newGridVzTexture);
        Solver.SetTexture(kernelPress, "_PressureTexture", PressureTexture);
        Solver.SetTexture(kernelPress, "_NewPressureTexture", newPressureTexture);

        //Solver.SetTexture(kernelProj, "_NewVelocityTexture", newVelocityTexture);
        Solver.SetTexture(kernelProj, "_GridVxTexture", GridVxTexture);
        //Solver.SetTexture(kernelProj, "_GridVyTexture", GridVyTexture);
        Solver.SetTexture(kernelProj, "_GridVzTexture", GridVzTexture);
        Solver.SetTexture(kernelProj, "_newGridVxTexture", newGridVxTexture);
        //Solver.SetTexture(kernelProj, "_newGridVyTexture", newGridVyTexture);
        Solver.SetTexture(kernelProj, "_newGridVzTexture", newGridVzTexture);
        Solver.SetTexture(kernelProj, "_NewPressureTexture", newPressureTexture);
        Solver.SetTexture(kernelProj, "_ConstraintTexture", ConstraintTexture);
        Solver.SetBuffer(kernelProj, "_DispRes", dispResBuffer);

        //Solver.SetTexture(kernelUpdate, "_VelocityTexture", VelocityTexture);
        Solver.SetTexture(kernelUpdate, "_PressureTexture", PressureTexture);
        //Solver.SetTexture(kernelUpdate, "_NewVelocityTexture", newVelocityTexture);
        Solver.SetTexture(kernelUpdate, "_NewPressureTexture", newPressureTexture);
        Solver.SetBuffer(kernelUpdate, "positions", ocean.positionBuffer);
        Solver.SetTexture(kernelUpdate, "_DisplacementTexture", DisplacementTexture);
        Solver.SetTexture(kernelUpdate, "_FoldingTexture", FoldingTexture);
        Solver.SetTexture(kernelUpdate, "_GridVxTexture", GridVxTexture);
        Solver.SetTexture(kernelUpdate, "_GridVzTexture", GridVzTexture);

        Solver.SetTexture(kernelCheckCon, "_PressureTexture", PressureTexture);
        Solver.SetTexture(kernelCheckCon, "_ConstraintTexture", ConstraintTexture);
        Solver.SetBuffer(kernelCheckCon, "boxInfos", boxColliderBuffer);

        Solver.SetTexture(kernelCalcDisp, "_ConstraintTexture", ConstraintTexture);
        Solver.SetTexture(kernelCalcDisp, "_NewPressureTexture", newPressureTexture);
        Solver.SetBuffer(kernelCalcDisp, "boxInfos", boxColliderBuffer);
        Solver.SetBuffer(kernelCalcDisp, "_DispRes", dispResBuffer);

        Solver.SetTexture(kernelApplyDisp, "_ConstraintTexture", ConstraintTexture);
        Solver.SetTexture(kernelApplyDisp, "_NewPressureTexture", newPressureTexture);
        Solver.SetBuffer(kernelApplyDisp, "boxInfos", boxColliderBuffer);
        Solver.SetBuffer(kernelApplyDisp, "_DispRes", dispResBuffer);



        ocean.planeShader.SetTexture(ocean.kernelUpdate, "_DisplacementTexture_NS", DisplacementTexture);
        ocean.planeShader.SetTexture(ocean.kernelUpdate, "_FoldingTexture_NS", FoldingTexture);


        Solver.Dispatch(kernelInit, _groupX, _groupY, 1);
        Debug.Log("Init dispatched");

        //oceanMaterial.SetTexture("_VelocityTexture", VelocityTexture);
        //ocean.oceanMaterial.SetTexture("_PressureTexture", PressureTexture);
        //oceanMaterial.SetTexture("_newVelocityTexture", newVelocityTexture);
        //ocean.oceanMaterial.SetTexture("_newPressureTexture", newPressureTexture);
    }



    /*********************************************************
    /  Updating Functions
    *********************************************************/
    public void updateConstraints()
    {
        if (obj == null)
            return;
        obj.UpdateInfo();
        boxColliderData[0] = obj.info;

        boxColliderBuffer.SetData(boxColliderData);
        Solver.Dispatch(kernelCheckCon, _groupX, _groupY, 1);
    }
    public void UpdateFields(float dT)
    {
        updateConstraints();

        //Debug.Log("Dispatched "+ dT);
        Solver.SetFloat("_deltaTime", dT);
        Solver.Dispatch(kernelAdvect, _groupX, _groupY, 1);
        Solver.Dispatch(kernelPress, _groupX, _groupY, 1);

        if (obj != null)
        {
            dispResBuffer.SetData(zeroDispRes);
            Solver.Dispatch(kernelCalcDisp, _groupX, _groupY, 1);
            Solver.Dispatch(kernelApplyDisp, _groupX, _groupY, 1);
        }

        Solver.Dispatch(kernelProj, _groupX, _groupY, 1);
        Solver.Dispatch(kernelUpdate, _groupX, _groupY, 1);
    }


    /*********************************************************
    /  Monobehavior Functions
    *********************************************************/
    void Start()
    {
        //ocean.CreatePlaneMesh();
        InitFromOceanPlane();
        InitGlobalComputeShader();
        _Timer = 0.0f;
    }



    void Update()
    {
        _Timer += Time.deltaTime;
        if(_Timer > _deltaTime)
        {
            UpdateFields(_Timer);
            _Timer = 0;
        }

        //ocean.UpdatePlaneMesh();
    }
    public void initTst()
    {
        ocean.CreatePlaneMesh();
        InitFromOceanPlane();
        InitGlobalComputeShader();
        if(obj!=null)obj.Init();
        _Timer = 0.0f;
    }
    public void debugTst()
    {
        float dT = 0.2f;
        Solver.SetFloat("_deltaTime", dT);

        updateConstraints();

        Debug.Log("Dispatched " + dT);
        Solver.SetFloat("_deltaTime", dT);
        Solver.Dispatch(kernelAdvect, _groupX, _groupY, 1);
        Solver.Dispatch(kernelPress, _groupX, _groupY, 1);

        dispResBuffer.SetData(zeroDispRes);
        Solver.Dispatch(kernelCalcDisp, _groupX, _groupY, 1);
        Solver.Dispatch(kernelApplyDisp, _groupX, _groupY, 1);

        Solver.Dispatch(kernelProj, _groupX, _groupY, 1);
        Solver.Dispatch(kernelUpdate, _groupX, _groupY, 1);

        ocean.UpdatePlaneMesh();

    }
    void ReadRenderTextureData(string name, RenderTexture rt, bool RGBA)
    {
        RenderTexture.active = rt;
        TextureFormat format = TextureFormat.RGBAHalf;
        if (!RGBA) format = TextureFormat.RHalf;
        Texture2D texture2D = new Texture2D(rt.width, rt.height, format, false);
        texture2D.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
        texture2D.Apply();
        int width = rt.width;
        int height = rt.height;

        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        Color[] pixels = texture2D.GetPixels();
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                Color pixel = pixels[y * rt.width + x];
                //Debug.Log($"{name} at ({x},{y}): {pixel}");
                sb.AppendFormat("({0,7:F2})\t", pixel.r);
            }
            sb.AppendLine();
        }

        using (StreamWriter writer = new StreamWriter("E:\\School\\Unity\\Debug\\" + name + ".txt"))
        {
            writer.Write(sb.ToString());
        }
        RenderTexture.active = null;
    }
}
