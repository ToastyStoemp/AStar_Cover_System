using System.Collections.Generic;
using UnityEngine;

public class PolyScanner : MonoBehaviour
{
    public List<Transform> corners = new List<Transform>();
    private List<CornerPoint> newCorners;

    public float RayCastDistance = 20;

    public GameObject ObjectToDrawPolyOn;
    private MeshFilter PolyDrawMeshFilter;
    
    public List<Vector3> Vertices;
    public List<Vector2> Uvs;
    public List<int> VerticesIndexes;

    private void Start()
    {
        Vertices = new List<Vector3>();
        Uvs = new List<Vector2>();
        VerticesIndexes = new List<int>();
        
        PolyDrawMeshFilter = ObjectToDrawPolyOn.AddComponent<MeshFilter>();
        ObjectToDrawPolyOn.AddComponent<MeshRenderer>();

        Mesh polyMesh = new Mesh
        {
            vertices = Vertices.ToArray(), 
            uv = Uvs.ToArray(),
            triangles = VerticesIndexes.ToArray()
        };

        PolyDrawMeshFilter.mesh = polyMesh;
    }

    // Update is called once per frame
    private void Update()
    {
        ConvertCornerTransformsToCornerPoints();
        SortCorners();
        SweepCorners();
        ConvertVerticesIntoMesh();
    }

    private void ConvertCornerTransformsToCornerPoints()
    {
        newCorners = new List<CornerPoint>(corners.Count);

        foreach (Transform currentCorner in corners)
        {
            Vector3 currentCornerPosition = currentCorner.position;
            newCorners.Add(new CornerPoint
            {
                originalObject = currentCorner,
                Pos = new Vector2(currentCornerPosition.x, currentCornerPosition.z)
            });
        }
    }
    
    private void SortCorners()
    {
        Vector3 centerPosition = transform.position;
        Vector2 center = new Vector2(centerPosition.x, centerPosition.z);
        
        foreach (CornerPoint corner in newCorners)
        {
            float calculatedAngle = Mathf.Atan2(corner.Pos.x - center.x, corner.Pos.y - center.y);
            corner.angle = calculatedAngle * 180 / Mathf.PI;
        }
        
        newCorners.Sort((a, b) => a.angle < b.angle ? -1 : 1);

        for (int index = 0; index < newCorners.Count; index++)
        {
            CornerPoint cornerPoint = newCorners[index];
            cornerPoint.originalObject.name = index.ToString();
        }
    }


    private void SweepCorners()
    {
        Vertices = new List<Vector3>();
        Uvs = new List<Vector2>();
        VerticesIndexes = new List<int>();
        
        bool wasPreviousHit = false;
        Vector3 previousDirection = Vector3.zero;
        Vector3 previousPoint = Vector3.zero;
        int currentVertexIndex = 2;

        for (int index = 0; index < newCorners.Count; index++)
        {
            CornerPoint currentCorner = newCorners[index];
            
            Vector3 direction = DirectionTo(currentCorner.Pos, true);
            Vector3 pointForTriangle;
            bool shouldProjectPointOnWall = false;
            Vector3 pointForProjectionOnWall = Vector3.zero;
            Vector3 startPos = transform.position;
            
            if (Physics.Raycast(startPos + Vector3.up, direction, out RaycastHit hit, RayCastDistance))
            {
                pointForTriangle = hit.point;

                if (!wasPreviousHit)
                {
                    //Project on wall
                    pointForProjectionOnWall = direction * RayCastDistance + startPos;
                    shouldProjectPointOnWall = true;
                }

                wasPreviousHit = true;
            }
            else
            {
                pointForTriangle = direction * RayCastDistance + startPos;

                if (wasPreviousHit)
                {
                    //project previous on wall
                    pointForProjectionOnWall = previousDirection * RayCastDistance + startPos;
                    shouldProjectPointOnWall = true;
                }

                wasPreviousHit = false;
            }

            previousDirection = direction;

            if (index == 0)
            {
                previousPoint = pointForTriangle;

                Vertices.Add(Flat(transform.position));
                Vertices.Add(pointForTriangle);

                Uvs.Add(Vector2.zero);
                Uvs.Add(Vector2.zero);

                continue;
            }

            if (shouldProjectPointOnWall)
            {
                //Make triangles!

                Vertices.Add(pointForProjectionOnWall);
                Uvs.Add(Vector2.zero);

                VerticesIndexes.Add(0);
                VerticesIndexes.Add(currentVertexIndex - 1);
                VerticesIndexes.Add(currentVertexIndex++);
            }


            //Make normal Triangle
            Vertices.Add(pointForTriangle);
            Uvs.Add(Vector2.zero);

            VerticesIndexes.Add(0);
            VerticesIndexes.Add(currentVertexIndex - 1);
            VerticesIndexes.Add(currentVertexIndex++);


            previousPoint = pointForTriangle;

            //if previous was NOT hit but current was hit
            //project on wall

            //if previous was hit but current was not hit
            //project previous on wall

            //if previous same as current 
            //Do normal
        }
        
        VerticesIndexes.Add(0);
        VerticesIndexes.Add(currentVertexIndex - 1);
        VerticesIndexes.Add(1);
    }

    private void ConvertVerticesIntoMesh()
    {
        Mesh polyMesh = PolyDrawMeshFilter.mesh;
        polyMesh.Clear();

        polyMesh.vertices = Vertices.ToArray();
        polyMesh.uv = Uvs.ToArray();
        polyMesh.triangles = VerticesIndexes.ToArray();
    }

    private Vector3 DirectionTo(Vector2 targetPos, bool flat = false)
    {
        Vector3 target = new Vector3(targetPos.x, 0, targetPos.y);
        
        if (flat)
        {
            return Flat(target - transform.position);
        }

        return target - transform.position;
    }
    
    private Vector3 DirectionTo(Vector3 targetPos, bool flat = false)
    {
        if (flat)
        {
            return Flat(targetPos - transform.position);
        }

        return targetPos - transform.position;
    }

    private Vector3 Flat(Vector3 vectorToFlat)
    {
        return new Vector3(vectorToFlat.x, 0, vectorToFlat.z);
    }
    
}