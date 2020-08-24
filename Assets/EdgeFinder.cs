using System;
using System.Collections.Generic;
using System.Linq;
using Pathfinding;
using UnityEditor;
using UnityEngine;


[Serializable]
public class EdgePoint
{
    public Vector3 position;
    public float angle;
    public int id;
}

[Serializable]
public class EdgeGroup
{
    public List<EdgePoint> corners = new List<EdgePoint>();

    private Vector3 cachedCenter;
    private bool centerCached;

    public int firstCorner;
    public int lastCorner;

    public Vector3 center
    {
        get {
            if (!centerCached)
            {
                centerCached = true;
                foreach (EdgePoint corner in corners)
                {
                    cachedCenter += corner.position;
                }
                cachedCenter /= corners.Count;
            }

            return cachedCenter;
        }
    }

    public void SetNewCorners(List<Vector3> newCorners)
    { 
        newCorners = newCorners.Distinct().ToList();
        corners.Clear();
        for (var index = 0; index < newCorners.Count; index++)
        {
            Vector3 corner = newCorners[index];
            corners.Add(new EdgePoint()
            {
                angle = 0,
                position = corner,
                id = index
            });
        }
    }

    public void MoveCornersTowardsCenter(float amount)
    {
        for (int index = 0; index < corners.Count; index++)
        {
            EdgePoint currentCorner = corners[index];
            Vector3 dir = (center - currentCorner.position).normalized;
            currentCorner.position += dir * amount;
        }
    }

    public void SortCorners(Vector2 playerPos)
    {
        float distance = center.x - playerPos.x;
        bool enableMove = distance > 0;

        foreach (EdgePoint currentCorner in corners)
        {
            Vector2 cornerPos = new Vector2(currentCorner.position.x, currentCorner.position.z);

            float correctedX = cornerPos.x - playerPos.x;
            float correctedY = cornerPos.y - playerPos.y;

            if (enableMove)
            {
                correctedX += distance * 2;
            }
            
            float calculatedAngle = Mathf.Atan2(correctedX, correctedY);
            currentCorner.angle = calculatedAngle * 180 / Mathf.PI ;
        }

        corners.Sort((a, b) => a.angle < b.angle ? -1 : 1);
    }

    public void DrawDebugGizmos(Color color)
    {
        for (int i = 0; i < corners.Count - 1; i++)
        {
            Handles.Label(corners[i].position + Vector3.up * i, $"corner {i}");
            
            Debug.DrawLine(corners[i].position, corners[i + 1].position, color, .1f);
        }

        Handles.Label(corners[corners.Count - 1].position, $"corner {corners.Count - 1}");
        Debug.DrawLine(corners[0].position, corners[corners.Count - 1].position, color, .1f);
    }

    public void SortCornersInOrder(Vector2 playerPos)
    {
        float correctedCenterX = center.x - playerPos.x;
        float correctedCenterY = center.z - playerPos.y;

        float calculatedCenterAngle = Mathf.Atan2(correctedCenterX, correctedCenterY);
        calculatedCenterAngle -= Mathf.PI;
        calculatedCenterAngle = calculatedCenterAngle < 0 ? Mathf.PI * 2 + calculatedCenterAngle : calculatedCenterAngle;
        float cosCenterAngle = Mathf.Cos(calculatedCenterAngle);
        float sinCenterAngle = Mathf.Sin(calculatedCenterAngle);

        foreach (EdgePoint currentCorner in corners)
        {
            Vector2 cornerPos = new Vector2(currentCorner.position.x, currentCorner.position.z);
            
            float correctedX = cornerPos.x - playerPos.x;
            float correctedY = cornerPos.y - playerPos.y;

            float rotatedX = correctedX * cosCenterAngle - correctedY * sinCenterAngle;
            float rotatedY = correctedY * cosCenterAngle + correctedX * sinCenterAngle;

            float calculatedAngle = Mathf.Atan2(rotatedX, rotatedY);
            calculatedAngle = calculatedAngle < 0 ? Mathf.PI * 2 + calculatedAngle : calculatedAngle;
            calculatedAngle = Mathf.Repeat(calculatedAngle, Mathf.PI * 2);
            currentCorner.angle = calculatedAngle * 180 / Mathf.PI ;
        }

        List<EdgePoint> tempSorted = new List<EdgePoint>(corners);
        tempSorted.Sort((a, b) => a.angle < b.angle ? -1 : 1);
        firstCorner = tempSorted[0].id;
        lastCorner = tempSorted[tempSorted.Count - 1].id;
        List<EdgePoint> removedCorners = new List<EdgePoint>();
        
        //TODO optimize sorting of corners
        //Find first and last corner maybe ignore the rest?
        
        for (int i = 0; i < firstCorner; i++)
        {
            removedCorners.Add(corners[0]);
            corners.RemoveAt(0);
        }

        for (int i = 0; i < removedCorners.Count; i++)
        {
            corners.Add(removedCorners[i]);
        }

        for (int i = 0; i < corners.Count; i++)
        {
            corners[i].id = i;
        }
    }
}

[ExecuteInEditMode]
public class EdgeFinder : GraphModifier
{
    public List<EdgeGroup> edgeGroups = new List<EdgeGroup>();

    public float moveToCenterAmount = 1f;

    public bool enableEdgeGroupsDebug;

    public override void OnPostScan()
    {
        if (AstarPath.active.graphs.Length == 0)
            return;

        List<Vector3> segments = GraphUtilities.GetContours(AstarPath.active.graphs[0]);
        edgeGroups = new List<EdgeGroup>();

        if (AstarPath.active.graphs[0].GetType() == typeof(GridGraph))
        {
            int currentIndex = 0;
            bool atEnd = false;
            while (currentIndex < segments.Count)
            {
                Vector3 currentCorner = segments[currentIndex];

                for (int i = currentIndex + 3; i < segments.Count; i += 2)
                {
                    if (currentCorner == segments[i])
                    {
                        EdgeGroup newEdgeGroup = new EdgeGroup();
                        newEdgeGroup.SetNewCorners(segments.GetRange(currentIndex, i - currentIndex));

                        edgeGroups.Add(newEdgeGroup);
                        currentIndex = i + 1;
                        break;
                    }

                    if (i >= segments.Count)
                        atEnd = true;
                }

                if (atEnd)
                {
                    EdgeGroup newEdgeGroup = new EdgeGroup();
                    newEdgeGroup.SetNewCorners(segments.GetRange(currentIndex, segments.Count - 1));
                    edgeGroups.Add(newEdgeGroup);
                    break;
                }
            }
        }
        else if (AstarPath.active.graphs[0].GetType() == typeof(RecastGraph))
        {
            int firstGroupIndex = 0;
            for (int i = 0; i < segments.Count - 1; i++)
            {
                Vector3 currentCorner = segments[i];

                if (currentCorner == segments[i + 2])
                {
                    EdgeGroup newEdgeGroup = new EdgeGroup();
                    newEdgeGroup.SetNewCorners(segments.GetRange(firstGroupIndex, i - firstGroupIndex));

                    edgeGroups.Add(newEdgeGroup);
                    firstGroupIndex = i + 2;
                }
            }

            EdgeGroup finalEdgeGroup = new EdgeGroup();
            finalEdgeGroup.SetNewCorners(segments.GetRange(firstGroupIndex, segments.Count - firstGroupIndex));

            edgeGroups.Add(finalEdgeGroup);
        }

        //MoveTowards Center
        foreach (EdgeGroup edgeGroup in edgeGroups)
        {
            edgeGroup.MoveCornersTowardsCenter(moveToCenterAmount);
        }      
    }

    private void OnDrawGizmos()
    {
        Color[] colorArray = { Color.red, Color.blue, Color.green, Color.magenta };

        if (enableEdgeGroupsDebug && edgeGroups != null && edgeGroups.Count > 0)
        {
            for (int i = 0; i < edgeGroups.Count; i++)
            {
                edgeGroups[i].DrawDebugGizmos(colorArray[i]);
            }
        }
    }
}
