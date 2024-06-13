
using UnityEditor;
using UnityEngine;
using static UnityEditor.PlayerSettings;

[CustomEditor(typeof(TestScript))]
public class TestScriptEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        TestScript generator = (TestScript)target;
        if (GUILayout.Button("Test"))
        {
            generator.Test();
        }
    }
}
public class TestScript : MonoBehaviour
{
    float RandomFloat(uint seed)
    {
        seed = (seed << 13) ^ seed;
        return (1f - ((seed * (seed * seed * 15731u + 789221u) + 1376312589u) & 0x7fffffff) / 2147483648f);
        //1073741824
    }
    uint Hash(uint seed)
    {
        return seed * 73856093u ^ (seed + 1919810u) * 19349663u;
    }

    Vector4 GaussianRandomVariableNum4(uint seed)
    {
        float x1, x2, w;
        do
        {
            x1 = 2f * RandomFloat(seed) - 1f;
            seed = Hash(seed);
            x2 = 2f * RandomFloat(seed) - 1f;
            seed = Hash(seed);
            w = x1 * x1 + x2 * x2;
        } while (w >= 1f);
        w = Mathf.Sqrt((-2f * Mathf.Log(w)) / w);


        float y1, y2, v;
        do
        {
            y1 = 2f * RandomFloat(seed) - 1f;
            seed = Hash(seed);
            y2 = 2f * RandomFloat(seed) - 1f;
            seed = Hash(seed);
            v = y1 * y1 + y2 * y2;
        } while (v >= 1f);
        v = Mathf.Sqrt((-2f * Mathf.Log(v)) / v);

        return new Vector4(x1 * w, x2 * w, y1 * v, y2 * v);
    }
    void Start()
    {
    }

    public void Test()
    {
        //uint seed = (uint)(Random.value * 100000);
        //Vector4 ret = GaussianRandomVariableNum4(seed);
        //Debug.Log(ret);
        fft();
    }
    int fft()
    {
        float[] w = { 1f, 2f, 3f, 4f, 0f, 0f, 0f, 0f };
        float[] s = { 1f, 2f, 3f, 4f, 0f, 0f, 0f, 0f };

        int len = 4;
        int maxn = 4;
        for (int i = 0, j = 0; i < len; ++i)
        {
            if (i < j)
            {
                float tmp = s[i];
                s[i] = s[j];
                s[j] = tmp;
            }
            int k = len >> 1;
            while ((j ^= k) < k)
            {
                k >>= 1;
            }
        }

        for (int i = 1, d = maxn >> 1; i < len; i <<= 1, d >>= 1)
        {
            for (int j = 0; j < len; j += i << 1)
            {
                for (int k = 0; k < i; ++k)
                {
                    float x = s[j + k], y = w[maxn - d * k] * s[j + k + i];
                    s[j + k] = x + y;
                    s[j + k + i] = x - y;
                }
            }
        }

        for (int i = 0; i < len; i++) 
            Debug.Log($"s [{i}] = {s[i]}");
        return 0;
    }
    //void fast_fast_tle(complex* A, int type)
    //{
    //    for (int i = 0; i < limit; i++)
    //        if (i < r[i]) swap(A[i], A[r[i]]);//求出要迭代的序列 
    //    for (int mid = 1; mid < limit; mid <<= 1)//待合并区间的中点
    //    {
    //        complex Wn(cos(Pi/ mid) , type* sin(Pi/ mid) ); //单位根 
    //    for (int R = mid << 1, j = 0; j < limit; j += R)//R是区间的右端点，j表示前已经到哪个位置了 
    //    {
    //        complex w(1,0);//幂 
    //    for (int k = 0; k < mid; k++, w = w * Wn)//枚举左半部分 
    //    {
    //        complex x = A[j + k], y = w * A[j + mid + k];//蝴蝶效应 
    //        A[j + k] = x + y;
    //        A[j + mid + k] = x - y;
    //    }
    //}
}
