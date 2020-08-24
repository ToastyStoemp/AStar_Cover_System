using UnityEngine;

public class PolyDrawer : MonoBehaviour
{
    private Mesh polyMesh;
    private MeshFilter polyMeshFilter;
    
    
    public Vector3[] newVertices;
    public Vector2[] newUV;
    public int[] newTriangles;

    void Start()
    {
        polyMeshFilter = gameObject.AddComponent<MeshFilter>();
        gameObject.AddComponent<MeshRenderer>();
        
        polyMesh = new Mesh();
        polyMesh.vertices = newVertices;
        polyMesh.uv = newUV;
        polyMesh.triangles = newTriangles;
        
        polyMeshFilter.mesh = polyMesh;
    }
}
