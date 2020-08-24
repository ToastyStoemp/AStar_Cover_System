using System.Collections.Generic;
using Pathfinding;
using UnityEngine;

public class CornerFinder : MonoBehaviour
{
    private List<GameObject> CornerObjects;

    private void Start()
    {
        CornerObjects = new List<GameObject>();
        UpdateCorners();
    }

    public void UpdateCorners()
    {
        GridGraph gg = AstarPath.active.data.gridGraph;
        for (int z = 0; z < gg.depth; z++)
        {
            for (int x = 0; x < gg.width; x++)
            {
                GridNodeBase node = gg.GetNode(x, z);

                CompareCornerNodes(node, 2, 1);
                CompareCornerNodes(node, 0, 3);
            }
        }
    }

    private bool CompareCornerNodes(GridNodeBase node, int firstDirection, int secondDirection)
    {
        GridNodeBase firstNode = node.GetNeighbourAlongDirection(firstDirection);
        GridNodeBase secondNode = node.GetNeighbourAlongDirection(secondDirection);

        if (firstNode != null && secondNode != null)
        {
            if (firstNode.Tag == secondNode.Tag && firstNode.Tag == 1)
            {
                CreateCorner((Vector3) node.position);
                return true;
            }
        }

        return false;
    }

    private void CreateCorner(Vector3 position)
    {
        GameObject corner = GameObject.CreatePrimitive(PrimitiveType.Cube);
        corner.transform.position = position;
        
        CornerObjects.Add(corner);
    }
}
