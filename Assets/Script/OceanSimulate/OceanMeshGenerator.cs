using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(OceanMeshGenerator))]
public class OceanMeshGeneratorEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        OceanMeshGenerator generator = (OceanMeshGenerator)target;
        if (GUILayout.Button("Generate Plane"))
        {
            generator.GeneratePlane();
        }
    }
}

public class OceanMeshGenerator : MonoBehaviour
{
    //public int widthSegments = 2;
    //public int lengthSegments = 2;
    public float Width = 1.0f;
    public float Length = 1.0f;
    public float VertexDistance = 0.2f;

    public void GeneratePlane()
    {
        MeshFilter meshFilter = GetComponent<MeshFilter>();
        if (meshFilter == null)
        {
            meshFilter = gameObject.AddComponent<MeshFilter>();
        }

        MeshRenderer meshRenderer = GetComponent<MeshRenderer>();
        if (meshRenderer == null)
        {
            meshRenderer = gameObject.AddComponent<MeshRenderer>();
        }

        Mesh mesh = CreatePlaneMesh(Width, Length);
        meshFilter.sharedMesh = mesh;
    }

    Mesh CreatePlaneMesh(float Width, float Length)
    {
        Mesh mesh = new Mesh();
        int widthSegments = (int)(Width / VertexDistance);
        int lengthSegments = (int)(Length / VertexDistance);

        float PlaneSizeX = VertexDistance * widthSegments;
        float PlaneSizeY = VertexDistance * lengthSegments;

        // Calculate vertices
        Vector3[] vertices = new Vector3[(widthSegments + 1) * (lengthSegments + 1)];
        for (int z = 0, i = 0; z <= lengthSegments; z++)
        {
            for (int x = 0; x <= widthSegments; x++)
            {
                float xPos = (float)x / widthSegments;
                float zPos = (float)z / lengthSegments;
                Debug.Log(new Vector2(xPos, zPos));
                vertices[i] = new Vector3(xPos * PlaneSizeX, 0, zPos * PlaneSizeY);
                i++;
            }
        }
        mesh.vertices = vertices;

        // Calculate triangles
        int[] triangles = new int[widthSegments * lengthSegments * 6];
        for (int ti = 0, vi = 0, z = 0; z < lengthSegments; z++, vi++)
        {
            for (int x = 0; x < widthSegments; x++, ti += 6, vi++)
            {
                triangles[ti] = vi;
                triangles[ti + 3] = triangles[ti + 2] = vi + 1;
                triangles[ti + 4] = triangles[ti + 1] = vi + widthSegments + 1;
                triangles[ti + 5] = vi + widthSegments + 2;
            }
        }
        mesh.triangles = triangles;

        // Calculate normals
        Vector3[] normals = new Vector3[vertices.Length];
        for (int i = 0; i < normals.Length; i++)
        {
            normals[i] = Vector3.up;
        }
        mesh.normals = normals;

        // Calculate UVs
        Vector2[] uv = new Vector2[vertices.Length];
        for (int z = 0, i = 0; z <= lengthSegments; z++)
        {
            for (int x = 0; x <= widthSegments; x++)
            {
                uv[i] = new Vector2((float)x / widthSegments, (float)z / lengthSegments);
                i++;
            }
        }
        mesh.uv = uv;

        return mesh;
    }
}