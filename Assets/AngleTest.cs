using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class AngleTest : MonoBehaviour
{
    public List<Vector3> vertices = new List<Vector3>();
    List<int> triangles = new List<int>();

    public void Start()
    {
        Test();
    }


    // Update is called once per frame
    void Update()
    {
        /*if(!Input.GetMouseButtonDown(0))
        {
            return;
        }
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;
        if (Physics.Raycast(ray, out hit))
        {
            Mesh mesh = CreateMesh(hit.point);
            GameObject go = new GameObject();
            go.transform.position = hit.point;
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            go.AddComponent<MeshRenderer>();
        }*/
    }

    public void Test()
    {
        Vector3 center = Vector3.zero;
        int size = 3;
        for (int x = 0 - size; x < 0 + size + 1; x++)
        {
            for (int y = 0 - size; y < 0 + size + 1; y++)
            {
                RaycastHit hit;
                if(Physics.Raycast(new Vector3(x, 5, y), Vector3.down, out hit))   
                {
                    if(hit.collider.gameObject.name == "Plane")
                    {
                        vertices.Add(new Vector3(x * 5, 1, y * 5));
                    }
                }
                
            }
        }
        Debug.Log(vertices.Count);
    }

    void OnDrawGizmos()
    {
        for(int i = 0; i < vertices.Count; i++)
            {
                Gizmos.DrawSphere(vertices[i], 1);
            }
    }
}
