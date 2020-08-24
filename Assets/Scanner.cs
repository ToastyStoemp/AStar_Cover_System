using System.Collections.Generic;
using UnityEngine;

public class CornerPoint
{
    public Transform originalObject;
    public Vector2 Pos;
    public float angle;
}

public class Scanner : MonoBehaviour
{
    public List<Transform> corners = new List<Transform>();

    // Update is called once per frame
    private void Update()
    {
        List<CornerPoint> newCorners = new List<CornerPoint>(corners.Count);
        Vector3 centerPosition = transform.position;
        Vector2 center = new Vector2(centerPosition.x, centerPosition.z);

        foreach (Transform currentCorner in corners)
        {
            Vector3 currentCornerPosition = currentCorner.position;
            newCorners.Add(new CornerPoint
            {
                originalObject = currentCorner,
                Pos = new Vector2(currentCornerPosition.x, currentCornerPosition.z)
            });
        }
        
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
    
}