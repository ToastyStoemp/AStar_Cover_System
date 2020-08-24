using UnityEngine;
using Pathfinding;

public class TestBlockerOnSphere : MonoBehaviour
{
    private SingleNodeBlocker blocker;
    
    private void Start()
    {
        blocker = GetComponent<SingleNodeBlocker>();
    }

    public void Update () {
        
        if(blocker != null)
            blocker.BlockAtCurrentPosition();
    }
}