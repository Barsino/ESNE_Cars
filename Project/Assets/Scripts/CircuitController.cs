using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class CircuitController : MonoBehaviour
{
    public GameObject[] waypoints;
    // Start is called before the first frame update
    void Start()
    {
        List<GameObject> waypointsList = new List<GameObject>();
        foreach (Transform tr in transform)
            waypointsList.Add(tr.gameObject);

        waypoints = waypointsList.ToArray();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void OnDrawGizmos()
    {
        if(waypoints.Length > 0)
        {
            Vector3 prev = waypoints[0].transform.position;
            for(int i = 1; i < waypoints.Length; i++)
            {
                Vector3 next = waypoints[i].transform.position;
                Gizmos.DrawLine(prev, next);
                prev = next;
            }
            Gizmos.DrawLine(prev, waypoints[0].transform.position);
        }
    }
}
