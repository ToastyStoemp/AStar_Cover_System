using System.Collections.Generic;
using Pathfinding;
using UnityEngine;

public class InversePolyScanner : MonoBehaviour
{
    public EdgeFinder edgeFinder;

    public float rayCastDistance = 20;

    public GameObject objectToDrawPolyOn;
    public bool drawMesh = true;
    public bool updateVertices = true;
    
    private MeshFilter polyDrawMeshFilter;
    public GraphUpdateScene graphUpdateScene;
    
    public List<Vector3> vertices;
    public List<Vector2> uvs;
    public List<int> verticesIndexes;
    
    private int currentVertexIndex;

    private void Start()
    {
        vertices = new List<Vector3>();
        uvs = new List<Vector2>();
        verticesIndexes = new List<int>();

        polyDrawMeshFilter = objectToDrawPolyOn.AddComponent<MeshFilter>();

        Mesh polyMesh = new Mesh
        {
            vertices = vertices.ToArray(), 
            uv = uvs.ToArray(),
            triangles = verticesIndexes.ToArray()
        };

        polyDrawMeshFilter.mesh = polyMesh;

        if (edgeFinder != null)
        {
            //create GUO objects
            for (var index = 0; index < edgeFinder.edgeGroups.Count; index++)
            {
                GameObject GUOObject = new GameObject("CoverObjectGUO");
                {
                    
                }
            }
        }
    }
    
    // Update is called once per frame
    private void Update()
    {

        if (updateVertices)
        {
            SortCorners();
            SweepCorners();
        }

        if (drawMesh)
        {
            objectToDrawPolyOn.SetActive(true);
            ConvertVerticesIntoMesh();
        }
        else
        {
            objectToDrawPolyOn.SetActive(false);
        }

        if (false)
        {
            ResetCurrentGraphCoverInfo();
            UpdateGraphWithCoverInformation();
        }
    }

    private void SortCorners()
    {
        foreach (EdgeGroup edgeGroup in edgeFinder.edgeGroups)
        {
            if(edgeGroup.corners.Count == 4)
                continue;
            var position = transform.position;
            //edgeGroup.SortCorners(new Vector2(position.x, position.z));
            edgeGroup.SortCornersInOrder(new Vector2(position.x, position.z));
        }
    }

    private void SweepCorners()
    {
        vertices = new List<Vector3>();
        uvs = new List<Vector2>();
        verticesIndexes = new List<int>();

        currentVertexIndex = 0;
        
        //TODO Global sweep rather than per object. Only use objects for closing the polygons
        
        foreach (EdgeGroup edgeGroup in edgeFinder.edgeGroups)
        {
            int direction;
            EdgePoint cornerIncrease = edgeGroup.corners[1];
            EdgePoint cornerDecrease = edgeGroup.corners[edgeGroup.corners.Count - 1];
            var position = transform.position;
            float distanceSqrIncrease = Vector3.SqrMagnitude(cornerIncrease.position - position);
            float distanceSqrDecrease = Vector3.SqrMagnitude(cornerDecrease.position - position);
            if (distanceSqrIncrease > distanceSqrDecrease)
                direction = 1;
            else
                direction = -1;

            if (direction > 0)
            {
                for (var index = 1; index < edgeGroup.corners.Count -1; index++)
                {
                    EdgePoint corner = edgeGroup.corners[index];
                    if (corner.id == edgeGroup.lastCorner)
                        break;
                    CheckCorner(corner, edgeGroup.corners[index + 1]);
                }
            }
            else if (direction < 0)
            {
                for (var index = edgeGroup.corners.Count -1; index > 1; index--)
                {
                    EdgePoint corner = edgeGroup.corners[index];
                    if (corner.id == edgeGroup.lastCorner)
                        break;
                    CheckCorner(corner, edgeGroup.corners[index - 1]);
                }
            }

            //CheckCorner(edgeGroup.corners[0], edgeGroup.corners[edgeGroup.corners.Count - 1]);
        }
    }

    private void CheckCorner(EdgePoint startCorner, EdgePoint endCorer)
    {
        Vector3 playerPos = transform.position;

        Vector3 directionToStart = DirectionTo(startCorner.position, true);
        Vector3 startCornerFarPoint = playerPos + directionToStart * rayCastDistance;
        
        Vector3 directionToEnd = DirectionTo(endCorer.position, true);
        Vector3 endCornerFarPoint = playerPos + directionToEnd * rayCastDistance;
        
        vertices.Add(startCorner.position);
        vertices.Add(startCornerFarPoint);
        vertices.Add(endCorer.position);
        vertices.Add(endCornerFarPoint);
        
        uvs.Add(Vector2.zero);
        uvs.Add(Vector2.zero);
        uvs.Add(Vector2.zero);
        uvs.Add(Vector2.zero);
        
        verticesIndexes.Add(currentVertexIndex);
        verticesIndexes.Add(currentVertexIndex + 1);
        verticesIndexes.Add(currentVertexIndex + 2);
        
        verticesIndexes.Add(currentVertexIndex + 2);
        verticesIndexes.Add(currentVertexIndex + 1);
        verticesIndexes.Add(currentVertexIndex + 3);

        currentVertexIndex += 4;
    }

    private void ConvertVerticesIntoMesh()
    {
        Mesh polyMesh = polyDrawMeshFilter.mesh;
        polyMesh.Clear();

        polyMesh.vertices = vertices.ToArray();
        polyMesh.uv = uvs.ToArray();
        polyMesh.triangles = verticesIndexes.ToArray();
    }

    private void ResetCurrentGraphCoverInfo()
    {
        foreach (EdgeGroup edgeGroup in edgeFinder.edgeGroups)
        {
            List<Vector3> guoCorners = new List<Vector3>();
            
            foreach (EdgePoint corner in edgeGroup.corners)
            {
                guoCorners.Add(corner.position);
            }

            GraphUpdateShape shape = new GraphUpdateShape(guoCorners.ToArray(), true, Matrix4x4.identity, 1);
        
            Bounds bounds = shape.GetBounds();
            GraphUpdateObject guo = new GraphUpdateObject(bounds)
            {
                shape = shape,
                modifyWalkability = false,
                setWalkability = false,
                addPenalty = 0,
                updatePhysics = false,
                updateErosion = false,
                resetPenaltyOnPhysics = false,
                modifyTag = true,
                setTag = 0
            };

            AstarPath.active.UpdateGraphs(guo);
        }
    }

    private void UpdateGraphWithCoverInformation()
    {
        foreach (EdgeGroup edgeGroup in edgeFinder.edgeGroups)
        {
            List<Vector3> guoCorners = new List<Vector3>();
            
            foreach (EdgePoint corner in edgeGroup.corners)
            {
                guoCorners.Add(corner.position);
            }

            GraphUpdateShape shape = new GraphUpdateShape(guoCorners.ToArray(), true, Matrix4x4.identity, 1);
        
            Bounds bounds = shape.GetBounds();
            GraphUpdateObject guo = new GraphUpdateObject(bounds)
            {
                shape = shape,
                modifyWalkability = false,
                setWalkability = false,
                addPenalty = 0,
                updatePhysics = false,
                updateErosion = false,
                resetPenaltyOnPhysics = false,
                modifyTag = true,
                setTag = 5
            };

            AstarPath.active.UpdateGraphs(guo);
        }
    }

    private Vector3 DirectionFrom(Vector2 targetPos, bool flat = false)
    {
        Vector3 target = new Vector3(targetPos.x, 0, targetPos.y);

        Vector3 position = transform.position;
        return flat ? Flat(position - target).normalized : (position - target).normalized;
    }

    private Vector3 DirectionTo(Vector3 targetPos, bool flat = false)
    {
        Vector3 position = transform.position;
        return flat ? Flat(targetPos - position).normalized : (targetPos - position).normalized;
    }

    private static Vector3 Flat(Vector3 vectorToFlat)
    {
        return new Vector3(vectorToFlat.x, 0, vectorToFlat.z);
    }
}