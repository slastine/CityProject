using System.Collections;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using System;

public class RoadSegment
{
    public Vector3 startPos;
    public Vector3 endPos;
    public int order;
    public int degree;
    public bool highway;
    public bool collided;
    public List<RoadSegment> children;
    public bool branched;
    public RoadSegment parent;
    public List<RoadSegment> connections;
    public float angle;
    
    public int Id { get; set; }
    public int Index { get; set; }
    public int Lowlink { get; set; }
    public HashSet<RoadSegment> Dependencies { get; set; }
    public RoadSegment(Vector3 startPos, Vector3 endPos, int degree, bool highway, bool branched, int order = 1, RoadSegment par = null, List<RoadSegment> roads = null, List<RoadSegment> connections = null)
    {
        this.startPos = startPos;
        this.endPos = endPos;
        this.order = order;
        this.highway = highway;
        this.degree = degree;
        angle = Mathf.Atan((this.startPos.z - this.endPos.z) / (this.startPos.x - this.endPos.x)); ;
        this.collided = false;
        if (connections == null)
        {
            this.connections = new List<RoadSegment>();
        }
        else
        {
            this.connections = connections;
        }
        if (roads == null)
        {
            children = new List<RoadSegment>();
        }
        else
        {
            children = roads;
        }
        parent = par;
        Id = -1;
        Index = -1;
        Lowlink = -1;
        Dependencies = new HashSet<RoadSegment>();

    }

    public override string ToString()
    {
        return this.startPos + ", " + this.endPos;
    }

}

public class RoadBlock : MonoBehaviour
{
    public RoadSegment road;
    public GameObject block;

    private void OnCollisionEnter(Collision collision)
    {
        road.collided = true;
    }

    public RoadBlock(RoadSegment road, GameObject go)
    {
        this.road = road;
        this.block = go;
    }
}

public class RoadEdge
{
    public RoadSegment segment1;
    public RoadSegment segment2;
    public List<RoadSegment> nodes;
    public Vector3 intersection;

    public RoadEdge(RoadSegment one, RoadSegment two)
    {
        segment1 = one;
        segment2 = two;
        nodes = new List<RoadSegment>();
        nodes.Add(segment1);
        nodes.Add(segment2);
        intersection = segment1.endPos;
    }
}

public class CityGenerator : MonoBehaviour
{
    //Sorted priority queue
    //Method pulls from queue based on order
    //Each time pulled from queue, generate new road if not at limit
    
    public int roadLimit = 500;
    public Queue<RoadSegment> roadQueue; 
    public float roadLength = 5;
    public List<RoadSegment> roadList = new List<RoadSegment>();
    public List<Vector3> positions = new List<Vector3>();
    public float highwayBranchProbability = .1f;
    public float streetBranchProbability = .1f;
    public float highwayProbability = .3f;
    public int lastBranch = 0;
    public static int maximumHighwayLength = 1000;
    public static int maximumStreetLength = 50;
    public GameObject prefab;
    public List<GameObject> roads = new List<GameObject>();
    public List<Vector3> snapPoints = new List<Vector3>();
    public GameObject background;
    List<GameObject> buildingList = new List<GameObject>();
    public GameObject[] buildingTypes;
    public Dictionary<RoadSegment, List<RoadSegment>> graph = new Dictionary<RoadSegment, List<RoadSegment>>();
    public List<RoadEdge> edges = new List<RoadEdge>();
    public List<RoadSegment[]> cycles = new List<RoadSegment[]>();
    public List<Vector3> points = new List<Vector3>();
    public List<Vector3> usedPoints = new List<Vector3>();
    public void Start()
    {
        RoadSegment road1 = new RoadSegment(this.gameObject.transform.position, this.gameObject.transform.position + (Vector3.right * roadLength), 0, true, false);
        road1.parent = road1;
        roadList.Add(road1);
        RoadSegment road2 = new RoadSegment(this.gameObject.transform.position, this.gameObject.transform.position + new Vector3(-5, 0, 0), 0, true, false);
        road2.parent = road2;
        roadList.Add(road2);
        RoadSegment road3 = new RoadSegment(this.gameObject.transform.position, this.gameObject.transform.position + new Vector3(0, 0, -5), 0, true, false);
        road3.parent = road3;
        roadList.Add(road3);
        RoadSegment road4 = new RoadSegment(this.gameObject.transform.position, this.gameObject.transform.position + new Vector3(0, 0, 5), 0, true, false);
        road4.parent = road4;
        roadList.Add(road4);
        roadQueue = new Queue<RoadSegment>();
        roadQueue.Enqueue(roadList[0]);
        roadQueue.Enqueue(roadList[1]);
        roadQueue.Enqueue(roadList[2]);
        roadQueue.Enqueue(roadList[3]);


        StartCoroutine(generationLoop());
            
        
        
        
    }

    public IEnumerator generationLoop()
    {
        
        while(roadQueue.Count > 0 && roadList.Count < roadLimit)
        {
            Generate();
            Debug.Log(roadList.Count);
            //Debug.Log("Queue: " + roadQueue.Count);
            Render();
            yield return null;
        }
        roadQueue.Clear();
        List<RoadSegment> finalChildren = new List<RoadSegment>();
        /*for(int i = 0; i < roadList.Count; i++)
        {
            if (Snap(roadList[i]) != roadList[i] && !roadList[i].highway)
            {
                RoadSegment newRoad = Snap(roadList[i]);
                //Debug.Log(Vector3.Distance(newRoad.startPos, newRoad.endPos));
                if(Vector3.Distance(newRoad.startPos, newRoad.endPos) < 6) 
                {
                    Debug.DrawLine(newRoad.startPos, newRoad.endPos, Color.green, 1000);
                    finalChildren.Add(newRoad);
                    
                }

                
                //roadQueue.Enqueue(newRoad);
                
                
            }
        }
        roadList.AddRange(finalChildren);
        for(int i = 0; i < finalChildren.Count; i++)
        {
            finalChildren[i].parent = finalChildren[i];
            finalChildren[i].children.Add(finalChildren[i]);
            roadList.Add(finalChildren[i]);
        }
        for(int i = 0; i < roadList.Count; i++)
        {
            if(!roadList[i].highway)
            {
                if (roadList[i].children.Count > 0)
                {
                    //GameObject prim = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    //prim.transform.position = roadList[i].children[roadList[i].children.Count - 1].endPos;
                    //prim.GetComponent<MeshRenderer>().material.color = Color.cyan;
                    snapPoints.Add(roadList[i].children[roadList[i].children.Count - 1].endPos);
                }
                if (roadList[i].parent == roadList[i])
                {
                    //GameObject prim = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    //prim.transform.position = roadList[i].startPos;
                    //prim.GetComponent<MeshRenderer>().material.color = Color.cyan;
                    snapPoints.Add(roadList[i].startPos);
                    if (roadList[i].children.Count == 0)
                    {
                        //GameObject prim2 = GameObject.CreatePrimitive(PrimitiveType.Cube);
                        //prim2.transform.position = roadList[i].endPos;
                        //prim2.GetComponent<MeshRenderer>().material.color = Color.cyan;
                        snapPoints.Add(roadList[i].endPos);
                    }

                }
            }
                
            
        }*/
        
        
        
        for(int i = 0; i < roadList.Count; i++)
        {
            if(!roadList[i].highway)
            {
                points.Add(roadList[i].startPos);
                points.Add(roadList[i].endPos);
            }
        }
        List<RoadSegment> dependencieslist = (from roadseg in roadList where roadList[50].startPos == roadseg.endPos || roadList[50].endPos == roadseg.startPos || roadList[50].startPos == roadseg.startPos && roadList[50] != roadseg select roadseg).ToList();
            //var dependencieslist = roadList[i].connections;
            string makeList = roadList[50].endPos + ": ";
            for (int j = 0; j < dependencieslist.Count; j++)
            {
                makeList += dependencieslist[j].endPos + ", ";
                roadList[50].Dependencies.Add(dependencieslist[j]);
            }
            if(dependencieslist.Count > 0)
            {
                //makeList += " Rightmost: " + closestToRight(dependencieslist, roadList[50]).endPos;
            }
            Debug.Log(makeList);
                
        
        var tcd = new TarjanCycleDetectStack();
        List<List<RoadSegment>> cycle_list = tcd.DetectCycle(roadList);
        for (int i = 0; i < cycle_list.Count; i++)
        {
            
                string makeString = "";
                for (int j = 0; j < cycle_list[i].Count; j++)
                {
                    //GameObject prim = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    //prim.transform.position = cycle_list[i][j].endPos;
                    //prim.GetComponent<MeshRenderer>().material.color = Color.red;
                    makeString += cycle_list[i][j].endPos + ", ";
                }
                //Debug.Log(makeString);
            
        }

            Debug.Log(edges.Count);
        //GameObject prim = GameObject.CreatePrimitive(PrimitiveType.Cube);
        //prim.transform.position = roadList[50].endPos;
        //prim.GetComponent<MeshRenderer>().material.color = Color.cyan;
        //StartCoroutine(GeneratePlot(roadList[50]));


        //Snap();
        //Debug.Log(roadList.Count);
        //Debug.Log("Queue: " + roadQueue.Count);
        //BuildingTest();
        //points = points.Distinct().ToList();
        for(int i = 0; i < points.Count; i++)
        {
            GameObject prim = GameObject.CreatePrimitive(PrimitiveType.Cube);
            prim.transform.position = points[i];
            prim.GetComponent<MeshRenderer>().material.color = Color.red;
            if (GetRoadSegment(points[i]) != null)
            {
                bool fourPoint = Create4PointPath(points[i]);
                if(!fourPoint)
                {
                    CreateLargerPlot(points[i]);
                }
            }
        }

        List<Vector3> unusedPoints = new List<Vector3>();
        for (int i = 0; i < points.Count; i++)
        {
            if(!usedPoints.Contains(points[i]))
            {
                unusedPoints.Add(points[i]);
            }
        }
        for(int i = 0; i < unusedPoints.Count; i++)
        {
            if(GetRoadSegment(unusedPoints[i]) != null)
            {
                //roadList.Remove(GetRoadSegment(unusedPoints[i]));
            }
        }
        Render();
    }

    public RoadSegment GetRoadSegment(Vector3 point)
    {
        return (from road in roadList where (Vector3.Distance(road.startPos, point) <= .2f || (Vector3.Distance(road.endPos, point) <= .2f && !road.highway)) select road).FirstOrDefault();
    }

    public RoadSegment GetRoadSegment(Vector3 point, Vector3 point2)
    {
        return (from road in roadList where (Vector3.Distance(road.startPos, point) <= .2f && Vector3.Distance(road.endPos, point2) <= .2f || (Vector3.Distance(road.endPos, point) <= .2f && Vector3.Distance(road.startPos, point2) <= .2f)) select road).FirstOrDefault();
    }

    public Vector3? DownFromPoint(Vector3 point)
    {
        Vector3 ideal = point + (Vector3.back * roadLength);
        if(GetRoadSegment(point) != null)
        {
            //ideal = Utils.RotatePoint(ideal, point, GetRoadSegment(point).angle);
        }
        //Debug.DrawLine(point, ideal, Color.green, 10000);
        var list = (from p in points where Vector3.Distance(ideal, p) < 4f && p != point select p).ToList();
        if (list.Count <= 0) return null;
        list = list.OrderBy(x => Vector3.Distance(x, point)).ToList();
        if (GetRoadSegment(list[0], point) != null)
        {
           //
        }
        //Debug.DrawLine(point, list[0], Color.black, 10000);
        //Debug.Log(Vector3.Distance(list[0], ideal));
        return list[0];
    }

    public Vector3? UpFromPoint(Vector3 point)
    {
        Vector3 ideal = point + (Vector3.forward * roadLength);
        if (GetRoadSegment(point) != null)
        {
            //ideal = Utils.RotatePoint(ideal, point, GetRoadSegment(point).angle);
        }
        //Debug.DrawLine(point, ideal, Color.green, 10000);
        var list = (from p in points where Vector3.Distance(ideal, p) < 4f && p != point select p).ToList();
        if (list.Count <= 0) return null;
        list = list.OrderBy(x => Vector3.Distance(x, point)).ToList();
        if (GetRoadSegment(list[0], point) != null)
        {
            //
        }
        //Debug.DrawLine(point, list[0], Color.black, 10000);
        //Debug.Log(Vector3.Distance(list[0], ideal));
        return list[0];
    }
    public Vector3? RightFromPoint(Vector3 point)
    {
        
        Vector3 ideal = point + (Vector3.right * roadLength);
        if (GetRoadSegment(point) != null)
        {
            //ideal = Utils.RotatePoint(ideal, point, GetRoadSegment(point).angle);
        }

        var list = (from p in points where Vector3.Distance(ideal, p) < 4f && p != point select p).ToList();
        if (list.Count <= 0) return null;
        //Debug.DrawLine(point, ideal, Color.green, 10000);
        list = list.OrderBy(x => Vector3.Distance(x, point)).ToList();
        if(GetRoadSegment(list[0], point) != null)
        {
            //
        }
        //Debug.DrawLine(point, list[0], Color.black, 10000);
        //Debug.Log(Vector3.Distance(list[0], ideal));
        return list[0];
    }

    public Vector3? LeftFromPoint(Vector3 point)
    {

        Vector3 ideal = point + (Vector3.left * roadLength);
        if (GetRoadSegment(point) != null)
        {
            //ideal = Utils.RotatePoint(ideal, point, GetRoadSegment(point).angle);
        }

        var list = (from p in points where Vector3.Distance(ideal, p) < 4f && p != point select p).ToList();
        if (list.Count <= 0) return null;
        //Debug.DrawLine(point, ideal, Color.green, 10000);
        list = list.OrderBy(x => Vector3.Distance(x, point)).ToList();
        if (GetRoadSegment(list[0], point) != null)
        {
            //
        }
        //Debug.DrawLine(point, list[0], Color.black, 10000);
        //Debug.Log(Vector3.Distance(list[0], ideal));
        return list[0];
    }


    public void CreatePlotsAlongStraightLine(Vector3 point)
    {

    }



    public bool Create4PointPath(Vector3 point)
    {
        List<RoadSegment> path = new List<RoadSegment>();
        Vector3 rightPoint;
        Vector3 downPoint;
        Vector3 leftPoint = Vector3.zero;
        Vector3 upPoint = Vector3.left;
        int count = 0;
        if (ValidRightFromPoint(point) != null) 
        {
            rightPoint = (Vector3)ValidRightFromPoint(point);
            count++;
            
                if (ValidDownFromPoint(point) != null)
                {
                    downPoint = (Vector3)ValidDownFromPoint(point);
                    count++;
                    if (ValidRightFromPoint(downPoint) != null)
                    {
                        leftPoint = (Vector3)ValidRightFromPoint(downPoint);
                        Vector3[] list = { downPoint, point, upPoint, rightPoint };
                        count++;
                        if(ValidUpFromPoint(leftPoint) != null)
                    {
                        upPoint = (Vector3)ValidUpFromPoint(leftPoint);
                        if (GetRoadSegment(point, rightPoint) != null && GetRoadSegment(point, downPoint) != null && GetRoadSegment(leftPoint, rightPoint) != null && GetRoadSegment(leftPoint, downPoint) != null)
                            {
                            Debug.Log("Cycle!");
                            //Debug.DrawLine(point, rightPoint, Color.red, 100000);
                            //Debug.DrawLine(point, downPoint, Color.red, 100000);
                            //Debug.DrawLine(rightPoint, upPoint, Color.red, 100000);
                            //Debug.DrawLine(downPoint, upPoint, Color.red, 100000);
                            GameObject prim = new GameObject();
                            Vector3 center = new Vector3((upPoint.x + point.x) / 2, 0, (upPoint.z + point.z) / 2);
                            Debug.Log("Cycle: " + point + ", " + rightPoint + ", " + leftPoint + ", " + downPoint + ", " + upPoint);
                            prim.transform.position = Vector3.down;
                            Mesh mesh = new Mesh();
                            mesh.RecalculateNormals();
                            Vector3[] vertices = { downPoint, point, leftPoint, rightPoint };
                            mesh.vertices = vertices;
                            int[] triangles = { 0, 1, 2, 2, 1, 3 };
                            mesh.triangles = triangles;
                            prim.AddComponent<MeshFilter>();
                            prim.AddComponent<MeshRenderer>();
                            prim.GetComponent<MeshRenderer>().material.color = Color.red;
                            prim.GetComponent<MeshFilter>().mesh = mesh;

                            usedPoints.Add(point);
                            usedPoints.Add(rightPoint);
                            usedPoints.Add(leftPoint);
                            usedPoints.Add(downPoint);
                            usedPoints.Add(upPoint);

                            return true;
                        }
                        //else if (Vector3.Distance(point, rightPoint) <= (roadLength) + .1f && Vector3.Distance(point, downPoint) <= (roadLength) + .1f && Vector3.Distance(rightPoint, leftPoint) <= (roadLength) + .1f && Vector3.Distance(downPoint, leftPoint) <= (roadLength) + .1f)
                        
                    }
                    }
                }
            
            
            
        }

        return false;
    }

    public Vector3? ValidDownFromPoint(Vector3 point)
    {
        Vector3 down;
        if (DownFromPoint(point) != null)
        {
            down = (Vector3)DownFromPoint(point);
            if (GetRoadSegment(point, down) != null) return down;
        }
        else return null;
        return null;
    }

    public Vector3? ValidLeftFromPoint(Vector3 point)
    {
        Vector3 down;
        if (LeftFromPoint(point) != null)
        {
            down = (Vector3)LeftFromPoint(point);
            if (GetRoadSegment(point, down) != null) return down;
        }
        else return null;
        return null;
    }

    public Vector3? ValidUpFromPoint(Vector3 point)
    {
        Vector3 down;
        if (UpFromPoint(point) != null)
        {
            down = (Vector3)UpFromPoint(point);
            if (GetRoadSegment(point, down) != null) return down;
        }
        else return null;
        return null;
    }

    public Vector3? ValidRightFromPoint(Vector3 point)
    {
        Vector3 down;
        if (RightFromPoint(point) != null)
        {
            down = (Vector3)RightFromPoint(point);
            if (GetRoadSegment(point, down) != null) return down;
        }
        else return null;
        return null;
    }

    public void CreateLargerPlot(Vector3 point)
    {
        Vector3 current = point;
        Vector3 past;
        Vector3 origin;
        List<Vector3> path = new List<Vector3>();
        //GameObject prim1 = GameObject.CreatePrimitive(PrimitiveType.Cube);
        ///prim1.transform.position = current;
        // prim1.GetComponent<MeshRenderer>().material.color = Color.red;
        if (ValidRightFromPoint(point) == null || ValidDownFromPoint(point) == null) return;
        if (ValidRightFromPoint(current) != null)
        {
            past = current;
            current = (Vector3)ValidRightFromPoint(current);
            path.Add(current);
            Debug.DrawLine(past, current, Color.red, 100000);
        }
        
        while(ValidRightFromPoint(current) != null && ValidDownFromPoint(current) == null)
        {
            past = current;
            current = (Vector3)ValidRightFromPoint(current);
            path.Add(current);
            Debug.DrawLine(past, current, Color.red, 100000);
        }
        Vector3 right = current;
        //GameObject prim2 = GameObject.CreatePrimitive(PrimitiveType.Cube);
        //prim2.transform.position = current;
        //prim2.GetComponent<MeshRenderer>().material.color = Color.green;


        if (ValidDownFromPoint(current) != null)
        {
            past = current;
            current = (Vector3)ValidDownFromPoint(current);
            path.Add(current);
            Debug.DrawLine(past, current, Color.red, 100000);
        }
        while (ValidDownFromPoint(current) != null && ValidLeftFromPoint(current) == null)
        {
            past = current;
            current = (Vector3)ValidDownFromPoint(current);
            path.Add(current);
            Debug.DrawLine(past, current, Color.red, 100000);
        }
        Vector3 up = current;
        //GameObject prim3 = GameObject.CreatePrimitive(PrimitiveType.Cube);
        //prim3.transform.position = current;
        //prim3.GetComponent<MeshRenderer>().material.color = Color.blue;

        if (ValidLeftFromPoint(current) != null)
        {
            past = current;
            current = (Vector3)ValidLeftFromPoint(current);
            path.Add(current);
            Debug.DrawLine(past, current, Color.red, 100000);
        }
        while (ValidLeftFromPoint(current) != null && ValidUpFromPoint(current) == null)
        {
            past = current;
            current = (Vector3)ValidLeftFromPoint(current);
            path.Add(current);
            Debug.DrawLine(past, current, Color.red, 100000);
        }
        Vector3 down = current;
        //GameObject prim4 = GameObject.CreatePrimitive(PrimitiveType.Cube);
        //prim4.transform.position = current;
        //prim4.GetComponent<MeshRenderer>().material.color = Color.magenta;

        if (ValidUpFromPoint(current) != null)
        {
            past = current;
            current = (Vector3)ValidUpFromPoint(current);
            path.Add(current);
            Debug.DrawLine(past, current, Color.red, 100000);
        }
        while (ValidUpFromPoint(current) != null && ValidRightFromPoint(current) == null)
        {
            past = current;
            current = (Vector3)ValidUpFromPoint(current);
            path.Add(current);
            Debug.DrawLine(past, current, Color.red, 100000);
        }

        if (current == point)
        {
            GameObject prim = new GameObject();
            Vector3 center = new Vector3((up.x + point.x) / 2, 0, (up.z + point.z) / 2);
            Debug.Log("Cycle: " + point + ", " + right + ", " + up + ", " + down + ", " + up);
            prim.transform.position = Vector3.down;
            Mesh mesh = new Mesh();
            mesh.RecalculateNormals();
            Vector3[] vertices = { down, point, up, right };
            mesh.vertices = vertices;
            int[] triangles = { 0, 1, 2, 2, 1, 3 };
            mesh.triangles = triangles;
            prim.AddComponent<MeshFilter>();
            prim.AddComponent<MeshRenderer>();
            prim.GetComponent<MeshRenderer>().material.color = Color.red;
            prim.GetComponent<MeshFilter>().mesh = mesh;
            usedPoints.AddRange(path);
        }

        else
        {
            if (ValidRightFromPoint(current) != null)
            {
                past = current;
                current = (Vector3)ValidRightFromPoint(current);
                path.Add(current);
                Debug.DrawLine(past, current, Color.blue, 100000);
            }
            while (ValidRightFromPoint(current) != null && current != point)
            {
                past = current;
                current = (Vector3)ValidRightFromPoint(current);
                path.Add(current);
                Debug.DrawLine(past, current, Color.blue, 100000);
            }
            if (current == point)
            {
                GameObject prim = new GameObject();
                Vector3 center = new Vector3((up.x + point.x) / 2, 0, (up.z + point.z) / 2);
                Debug.Log("Cycle: " + point + ", " + right + ", " + up + ", " + down + ", " + up);
                prim.transform.position = Vector3.down;
                Mesh mesh = new Mesh();
                mesh.RecalculateNormals();
                Vector3[] vertices = { down, point, up, right };
                mesh.vertices = vertices;
                int[] triangles = { 0, 1, 2, 2, 1, 3 };
                mesh.triangles = triangles;
                prim.AddComponent<MeshFilter>();
                prim.AddComponent<MeshRenderer>();
                prim.GetComponent<MeshRenderer>().material.color = Color.blue;
                prim.GetComponent<MeshFilter>().mesh = mesh;
                usedPoints.AddRange(path);
            }
        }
    }


    private void Update()
    {
        if (!Input.GetMouseButtonDown(0)) return;
        RaycastHit hit;
        if (!Physics.Raycast(Camera.main.ScreenPointToRay(Input.mousePosition), out hit)) return;
        Debug.Log("Hit");
        List<Vector3> list = points;
        list = list.OrderBy(x => Vector3.Distance(hit.point, x)).ToList();
        //Debug.Log(list[0].angle);


        //GetDirection(list[0]);
        //StartCoroutine(GeneratePlot(list[0]));
        //GameObject prim = GameObject.CreatePrimitive(PrimitiveType.Cube);
        //p/rim.transform.position = list[0];
        //prim.GetComponent<MeshRenderer>().material.color = Color.cyan;
        //GeneratePlot(list[0]);
        CreateLargerPlot(list[0]);
    }

    public Vector3 GetDirection(RoadSegment seg)
    {
        Vector3 right = seg.startPos + (Vector3.right * roadLength);
        right = Utils.RotatePoint(right, seg.startPos, seg.angle);
        Vector3 left = seg.startPos + (Vector3.left * roadLength);
        left = Utils.RotatePoint(left, seg.startPos, seg.angle);
        Vector3 north = seg.startPos + (Vector3.forward * roadLength);
        north = Utils.RotatePoint(north, seg.startPos, seg.angle);
        Vector3 south = seg.startPos + (Vector3.back * roadLength);
        south = Utils.RotatePoint(south, seg.startPos, seg.angle);
        //Debug.DrawLine(seg.startPos, right, Color.cyan, 10000);
        //Debug.DrawLine(seg.startPos, left, Color.cyan, 10000);
        //Debug.DrawLine(seg.startPos, north, Color.cyan, 10000);
        //Debug.DrawLine(seg.startPos, south, Color.cyan, 10000);
        List<Vector3> compare = new List<Vector3>();
        compare.Add(right);
        compare.Add(left);
        compare.Add(south);
        compare.Add(north);
        compare = compare.OrderBy(x => Vector3.Distance(x, seg.endPos)).ToList();
        if (compare[0] == right)
        {
            Debug.Log("Right");
            return Vector3.right;
        }
        else if (compare[0] == left)
        {
            Debug.Log("Left");
            return Vector3.left;
        }
        else if (compare[0] == south)
        {
            Debug.Log("South");
            return Vector3.back;
        }
        else if (compare[0] == north)
        {
            Debug.Log("North");
            return Vector3.forward;
        }
        return Vector3.zero;
    }

    public RoadSegment? closestToRight(List<RoadSegment> list, RoadSegment start)
    {
        Vector3 right = start.endPos + (Vector3.right * roadLength);
        float x;
        float z;
        Vector3 ideal;
        //start.angle *= Mathf.Rad2Deg;
        
            ideal = right;


            Vector3 newPos;
            if (!(start.angle >= -90 * Mathf.Deg2Rad && start.angle <= 180 * Mathf.Deg2Rad))
            {
                x = start.endPos.x - (Mathf.Cos(start.angle) * roadLength);
                z = start.endPos.z - (Mathf.Sin(start.angle) * roadLength);
                newPos = new Vector3(x, 0, z);
                ///Debug.DrawLine(start.endPos, new Vector3(x, 0, z), Color.cyan, 100000);
            }
            else
            {
                float angle = start.angle + (180 * Mathf.Deg2Rad);
                x = start.endPos.x - (Mathf.Cos(angle) * roadLength);
                z = start.endPos.z - (Mathf.Sin(angle) * roadLength);
                newPos = new Vector3(x, 0, z);
                //Debug.DrawLine(start.endPos, new Vector3(x, 0, z), Color.cyan, 100000);
        }
        
            //Debug.Log("Angle add " + angle * Mathf.Rad2Deg);
        


        list = list.OrderBy(x => Vector3.Distance(x.endPos, ideal)).ToList();
        Debug.DrawLine(start.endPos, right, Color.red, 100000);
        
        
        
        //Debug.Log(list[0].angle * Mathf.Rad2Deg);
        for(int i = 0; i < list.Count; i++)
        {
            Debug.Log(Vector3.Distance(list[i].endPos, right));
            Debug.Log(Mathf.Abs(list[0].angle * Mathf.Rad2Deg));
            if (Vector3.Distance(list[i].endPos, ideal) < 2.5) return list[i];
        }
        return null;
    }

    public RoadSegment? closestToLeft(List<RoadSegment> list, RoadSegment start)
    {
        Vector3 ideal = start.endPos + (Vector3.left * roadLength);
        Debug.DrawLine(start.endPos, ideal, Color.red, 100000);
        list = list.OrderBy(x => Vector3.Distance(x.endPos, ideal)).ToList();
        if (list.Count > 0) return list[0];
        else return null;
    }

    public RoadSegment? closestToUp(List<RoadSegment> list, RoadSegment start)
    {
        Vector3 ideal = start.endPos + (Vector3.forward * roadLength);
        list = list.OrderBy(x => Vector3.Distance(x.endPos, ideal)).ToList();
        if (list.Count > 0) return list[0];
        else return null;
    }

    public RoadSegment? closestToDown(List<RoadSegment> list, RoadSegment start)
    {
        Vector3 ideal = start.endPos + (Vector3.back * roadLength);
        list = list.OrderBy(x => Vector3.Distance(x.endPos, ideal)).ToList();
        //Debug.DrawLine(start.endPos, ideal, Color.red, 100000);
        if (Vector3.Distance(list[0].endPos, ideal) > 2.5) return null;
        if (list.Count > 0) return list[0];
        else return null;
    }

    //Create algorithm to locate plots
    //Choose roadsegment at random
    //Try to go right
    //If can go right, continue. If cannot go right, stop
    //Continue going right. If can go down, go down
    //Go down. If can go left, go left
    //Go left. If can go up, go up
    //Go up. If can go right, go right
    //If segment equals starter segment, plot found

    public void GeneratePlot(RoadSegment start)
    {
        RoadSegment current = start;
        var dependencieslist = (from roadseg in roadList where start.startPos == roadseg.endPos || start.endPos == roadseg.startPos || start.startPos == roadseg.startPos && start != roadseg select roadseg).ToList();
        bool rightFinished = false;
        bool downFinished = false;
        bool leftFinished = false;
        List<RoadSegment> path = new List<RoadSegment>();
        path.Add(start);
        Vector3 direction = GetDirection(current);
        //current = FollowInDirection(direction, current);
        if(GetInDirection(Vector3.right, start, start.endPos) != null && GetInDirection(Vector3.back, start, start.endPos) != null)
        {
            GameObject prim = GameObject.CreatePrimitive(PrimitiveType.Cube);
            prim.transform.position = start.endPos;
            prim.GetComponent<MeshRenderer>().material.color = Color.red;

            //GameObject prim2 = GameObject.CreatePrimitive(PrimitiveType.Cube);
            //prim2.transform.position = GetInDirection(Vector3.back, start, start.endPos).endPos;
            //prim2.GetComponent<MeshRenderer>().material.color = Color.cyan;
            Debug.DrawLine(GetInDirection(Vector3.right, start, start.endPos).startPos, GetInDirection(Vector3.right, start, start.endPos).endPos, Color.black, 10000);
            //Debug.DrawLine(location, left, Color.cyan, 10000);
            //Debug.DrawLine(location, north, Color.cyan, 10000);
            Debug.DrawLine(GetInDirection(Vector3.back, start, start.endPos).startPos, GetInDirection(Vector3.back, start, start.endPos).endPos, Color.cyan, 10000);
        }




    }

    

    public bool FindPerpendicular(Vector3 direction, RoadSegment start)
    {
        var list = (from roadseg in roadList where start.startPos == roadseg.endPos || start.endPos == roadseg.startPos || start.startPos == roadseg.startPos && start != roadseg select roadseg).ToList();
        
        
        if (direction == Vector3.right)
        {
            if (ExistsInDirection(Vector3.forward, start, start.endPos) || ExistsInDirection(Vector3.back, start, start.endPos) || ExistsInDirection(Vector3.forward, start, start.startPos) || ExistsInDirection(Vector3.back, start, start.startPos)) return true;
        }
        else if (direction == Vector3.left)
        {
            if (ExistsInDirection(Vector3.forward, start, start.endPos) || ExistsInDirection(Vector3.back, start, start.endPos)) return true;
        }
        else if (direction == Vector3.forward)
        {
            if (ExistsInDirection(Vector3.right, start, start.endPos) || ExistsInDirection(Vector3.left, start, start.endPos)) return true;
        }
        else if (direction == Vector3.back)
        {
            if (ExistsInDirection(Vector3.right, start, start.endPos) || ExistsInDirection(Vector3.left, start, start.endPos)) return true;
        }
        return false;
    }

    public RoadSegment GetPerpendicular(Vector3 direction, RoadSegment start)
    {
        var list = (from roadseg in roadList where start.startPos == roadseg.endPos || start.endPos == roadseg.startPos || start.startPos == roadseg.startPos && start != roadseg select roadseg).ToList();


        if (direction == Vector3.right)
        {
            if (ExistsInDirection(Vector3.forward, start, start.endPos) || ExistsInDirection(Vector3.back, start, start.endPos) || ExistsInDirection(Vector3.forward, start, start.startPos) || ExistsInDirection(Vector3.back, start, start.startPos))
            {
                if (ExistsInDirection(Vector3.back, start, start.endPos)) return GetInDirection(Vector3.back, start, start.endPos);
                if (ExistsInDirection(Vector3.forward, start, start.endPos)) return GetInDirection(Vector3.forward, start, start.endPos);
                if (ExistsInDirection(Vector3.back, start, start.startPos)) return GetInDirection(Vector3.back, start, start.startPos);
                if (ExistsInDirection(Vector3.forward, start, start.startPos)) return GetInDirection(Vector3.forward, start, start.startPos);
            }
        }
        else if (direction == Vector3.left)
        {
            if (ExistsInDirection(Vector3.forward, start, start.endPos) || ExistsInDirection(Vector3.back, start, start.endPos) || ExistsInDirection(Vector3.forward, start, start.startPos) || ExistsInDirection(Vector3.back, start, start.startPos))
            {
                if (ExistsInDirection(Vector3.back, start, start.endPos)) return GetInDirection(Vector3.back, start, start.endPos);
                if (ExistsInDirection(Vector3.forward, start, start.endPos)) return GetInDirection(Vector3.forward, start, start.endPos);
                if (ExistsInDirection(Vector3.back, start, start.startPos)) return GetInDirection(Vector3.back, start, start.startPos);
                if (ExistsInDirection(Vector3.forward, start, start.startPos)) return GetInDirection(Vector3.forward, start, start.startPos);
            }
        }
        else if (direction == Vector3.forward)
        {
            if (ExistsInDirection(Vector3.right, start, start.endPos) || ExistsInDirection(Vector3.left, start, start.endPos) || ExistsInDirection(Vector3.right, start, start.startPos) || ExistsInDirection(Vector3.left, start, start.startPos))
            {
                if (ExistsInDirection(Vector3.right, start, start.endPos)) return GetInDirection(Vector3.back, start, start.endPos);
                if (ExistsInDirection(Vector3.left, start, start.endPos)) return GetInDirection(Vector3.forward, start, start.endPos);
                if (ExistsInDirection(Vector3.right, start, start.startPos)) return GetInDirection(Vector3.back, start, start.startPos);
                if (ExistsInDirection(Vector3.left, start, start.startPos)) return GetInDirection(Vector3.forward, start, start.startPos);
            }
        }
        else if (direction == Vector3.back)
        {
            if (ExistsInDirection(Vector3.right, start, start.endPos) || ExistsInDirection(Vector3.left, start, start.endPos) || ExistsInDirection(Vector3.right, start, start.startPos) || ExistsInDirection(Vector3.left, start, start.startPos))
            {
                if (ExistsInDirection(Vector3.right, start, start.endPos)) return GetInDirection(Vector3.back, start, start.endPos);
                if (ExistsInDirection(Vector3.left, start, start.endPos)) return GetInDirection(Vector3.forward, start, start.endPos);
                if (ExistsInDirection(Vector3.right, start, start.startPos)) return GetInDirection(Vector3.back, start, start.startPos);
                if (ExistsInDirection(Vector3.left, start, start.startPos)) return GetInDirection(Vector3.forward, start, start.startPos);
            }
        }
        return null;
    }

    public RoadSegment FollowInDirection(Vector3 direction, RoadSegment start)
    {
        var list = (from roadseg in roadList where start.endPos == roadseg.startPos || start.startPos == roadseg.startPos && start != roadseg select roadseg).ToList();
        Vector3 ideal = start.endPos + (direction * roadLength);
        ideal = Utils.RotatePoint(ideal, start.endPos, start.angle);
        list = list.OrderBy(x => Vector3.Distance(x.endPos, ideal)).ToList();
        //Debug.DrawLine(start.endPos, ideal, Color.red, 100000);
        //Debug.DrawLine(ideal, list[0].endPos, Color.red, 100000);
        
        //if (Vector3.Distance(list[0].endPos, ideal) > 4) return null;

        //Debug.DrawLine(list[0].startPos, ideal, Color.green, 100000);
        for (int i = 0; i < list.Count; i++)
        {
            Debug.Log(Vector3.Distance(list[i].endPos, ideal));
            if (Vector3.Distance(list[i].endPos, ideal) < 4) return list[i];
        }
        return null;
    }

    public bool ExistsInDirection(Vector3 direction, RoadSegment start, Vector3 location)
    {
        var list = (from roadseg in roadList where start.startPos == roadseg.endPos || start.endPos == roadseg.startPos || start.startPos == roadseg.startPos && start != roadseg select roadseg).ToList();
        Vector3 ideal = location + (direction * roadLength);
        ideal = Utils.RotatePoint(ideal, location, start.angle);
        list = list.OrderBy(x => Vector3.Distance(x.endPos, ideal)).ToList();
        Vector3 right = start.startPos + (Vector3.right * roadLength);
        right = Utils.RotatePoint(right, location, start.angle);
        Vector3 left = location + (Vector3.left * roadLength);
        left = Utils.RotatePoint(left, location, start.angle);
        Vector3 north = location + (Vector3.forward * roadLength);
        north = Utils.RotatePoint(north, location, start.angle);
        Vector3 south = location + (Vector3.back * roadLength);
        south = Utils.RotatePoint(south, location, start.angle);
        

        if ((from seg in list where Vector3.Distance(location, ideal) <= 4 select seg).Any()) return true;
        
        if(direction == Vector3.right)
        {
            Debug.Log("Hit at right " + list[0].endPos + " from " + location);
        }
        else if (direction == Vector3.left && list[0].endPos != start.startPos)
        {
            Debug.Log("Hit at left " + list[0].endPos + " from " + location);
        }
        else if (direction == Vector3.back)
        {
            Debug.Log("Hit at south " + list[0].endPos + " from " + location);
        }
        else if (direction == Vector3.forward)
        {
            Debug.Log("Hit at north " + list[0].endPos + " from " + location);
        }
        for(int i = 0; i < list.Count; i++)
        {
            if (Vector3.Distance(list[i].endPos, ideal) < 4) return true;
        }
        return false;
    }

    public RoadSegment GetInDirection(Vector3 direction, RoadSegment start, Vector3 location)
    {
        Vector3 ideal = start.endPos + (direction * roadLength);
        //var list = (from roadseg in roadList where start.endPos == roadseg.startPos || start.startPos == roadseg.startPos && start != roadseg select roadseg).ToList();
        var list = (from roadseg in roadList where Vector3.Distance(roadseg.endPos, ideal) < .5f &&  start != roadseg select roadseg).ToList();
        //var list = new List<RoadSegment>();
        //ideal = Utils.RotatePoint(ideal, location, start.angle);
        
        list = roadList.OrderBy(x => Vector3.Distance(x.endPos, ideal)).ToList();
        if (list.Count <= 0) return null;
        Vector3 right = location + (Vector3.right * roadLength);
        right = Utils.RotatePoint(right, location, start.angle);
        Vector3 left = location + (Vector3.left * roadLength);
        left = Utils.RotatePoint(left, location, start.angle);
        Vector3 north = location + (Vector3.forward * roadLength);
        north = Utils.RotatePoint(north, location, start.angle);
        Vector3 south = location + (Vector3.back * roadLength);
        south = Utils.RotatePoint(south, location, start.angle);
        //Debug.DrawLine(location, right, Color.red, 10000);
        //Debug.DrawLine(location, left, Color.cyan, 10000);
        //Debug.DrawLine(location, north, Color.cyan, 10000);
        //Debug.DrawLine(location, south, Color.cyan, 10000);
        if (direction == Vector3.back)
        {
            Debug.Log("Hit at south " + list[0].endPos + " from " + start.endPos);
        }
        if (direction == Vector3.right)
        {
            Debug.Log("Hit at right " + list[0].endPos + " from " + location);
        }
        //Debug.DrawLine(start.endPos, list[0].startPos, Color.red, 100000);
        
        for (int i = 0; i < list.Count; i++)
        {
            if (Vector3.Distance(list[i].endPos, ideal) < .5f) return list[i];
        }
        return null;
    }

    public void Test(RoadSegment seg)
    {
        
        var nodes = (BreadthFirstTopDownTraversal(seg, n => (from roadseg in roadList where n.endPos == roadseg.startPos && n != roadseg select roadseg)).ToList());

        string makeString = "";
        

            for (int i = 0; i < nodes.Count; i++)
            {
                makeString += nodes[i].endPos + ", ";
                GameObject prim = GameObject.CreatePrimitive(PrimitiveType.Cube);
                prim.transform.position = nodes[i].endPos;
                prim.GetComponent<MeshRenderer>().material.color = Color.cyan;
            }
        Debug.Log(makeString);   
    }

    public static IEnumerable<T> BreadthFirstTopDownTraversal<T>(T root, Func<T, IEnumerable<T>> children)
    {
        var q = new Queue<T>();
        q.Enqueue(root);
        while (q.Count > 0)
        {
            T current = q.Dequeue();
            yield return current;
            foreach (var child in children(current))
            {
                q.Enqueue(child);
            }
                
        }
    }

    public IEnumerator MoveAlongRoad(List<RoadSegment> segList)
    {
        GameObject prim = GameObject.CreatePrimitive(PrimitiveType.Cube);
        prim.transform.position = segList[0].endPos;
        prim.GetComponent<MeshRenderer>().material.color = Color.cyan;
        string makeString = "";
        for(int i = 0; i < segList.Count; i++)
        {
            makeString += segList[i].endPos + ", ";
            GameObject prim2 = GameObject.CreatePrimitive(PrimitiveType.Cube);
            prim2.transform.position = segList[i].endPos;
            //Debug.DrawLine(segList[i].startPos, segList[i].endPos, Color.cyan, 10000);
            yield return new WaitForSeconds(.5f);
        }
        Debug.Log(makeString); 
        yield return null;
    }

    public List<RoadSegment> GeneratePath(RoadSegment initialSeg, List<RoadSegment> backwards)
    {
        string makeList = "";
        for (int i = 0; i < backwards.Count; i++)
        {
            makeList += backwards[i].endPos + ", ";
        }
        Debug.Log(makeList);
        var path = new List<RoadSegment>();
        if (backwards.Contains(initialSeg)) return null;
        else
        {
            path.Add(initialSeg);
            backwards.Add(initialSeg);
            initialSeg.connections.Remove(initialSeg);
            Debug.Log("Connections count " + initialSeg.connections.Count);
            var segConnections = (from roadseg in roadList where initialSeg.endPos == roadseg.startPos && initialSeg != roadseg select roadseg).ToList();
            if (segConnections.Intersect(backwards).Any()) return null;
            for (int i = 0; i < segConnections.Count; i++)
            {
                var forwardConnections = (from roadseg in roadList where segConnections[i].endPos == roadseg.startPos && segConnections[i] != roadseg select roadseg).ToList();
                if (!path.Intersect(forwardConnections).Any())
                {
                    var attemptToAdd = GeneratePath(segConnections[i], backwards);
                    if (attemptToAdd == null)
                    {
                        Debug.Log("Cycle at " + makeList);
                        break;
                    }
                    if (!path.Intersect(attemptToAdd).Any())
                    {
                        path.AddRange(attemptToAdd);
                    }
                }
                    
                else
                {
                    Debug.Log("Cycle");
                    return path;
                }


            }
        }



            return path;
    }

    public bool isAdjacentRoad(RoadSegment road)
    {
        if ((from roadseg in roadList where road.endPos == roadseg.startPos select roadseg).Any()) return true;
        return false;
    }

    public IEnumerator cyclesTest()
    {
        foreach (var pair in graph)
        {
            List<RoadSegment> segs = new List<RoadSegment>();
            string makeString = pair.Key.endPos + "";
            for (int i = 0; i < pair.Value.Count; i++)
            {
                makeString += pair.Value[i].endPos + ", ";
                segs.Add(pair.Value[i]);
                if (pair.Value[i] == pair.Key && segs.Count > 2)
                {
                    Debug.Log("Cycle found: " + makeString);
                    for(int j = 0; j < segs.Count; j++)
                    {
                        Debug.DrawLine(segs[j].startPos, segs[j].endPos, Color.cyan, 10000);
                    }
                }
            }
            yield return new WaitForSeconds(1);
        }
    }

 /*    void findNewCycles(RoadSegment[] path)
    {
        RoadSegment n = path[0];
        RoadSegment x;
        RoadSegment[] sub = new RoadSegment[path.Length + 1];

        string joined = "";
        for(int i = 0; i < path.Length; i++)
        {
            joined += ", " + path[i].endPos;
        }
        Debug.Log(joined);
        for (int i = 0; i < edges.Count; i++)
            for (int y = 0; y <= 1; y++)
                if (edges[i].nodes[y] == n)
                //  edge referes to our current node
                {
                    //x = graph[i, (y + 1) % 2];
                    x = edges[i].nodes[(y + 1) % 2];
                    if (!visited(x, path))
                    //  neighbor node not on path yet
                    {
                        sub[0] = x;
                        Array.Copy(path, 0, sub, 1, path.Length);
                        //  explore extended path
                        findNewCycles(sub);
                    }
                    else if ((path.Length > 2) && (x == path[path.Length - 1]))
                    //  cycle found
                    {
                        Debug.Log("*");
                        RoadSegment[] p = path;
                        RoadSegment[] inv = invert(p);
                        if (isNew(p) && isNew(inv))
                            cycles.Add(p);
                    }
                }
    

    static bool equals(RoadSegment[] a, RoadSegment[] b)
    {
        bool ret = (a[0] == b[0]) && (a.Length == b.Length);

        for (int i = 1; ret && (i < a.Length); i++)
            if (a[i] != b[i])
            {
                ret = false;
            }

        return ret;
    }

    static RoadSegment[] invert(RoadSegment[] path)
    {
        RoadSegment[] p = new RoadSegment[path.Length];

        for (int i = 0; i < path.Length; i++)
            p[i] = path[path.Length - 1 - i];

        return p;
    }

    //  rotate cycle path such that it begins with the smallest node


     bool isNew(RoadSegment[] path)
    {
        bool ret = true;

        foreach (RoadSegment[] p in cycles)
            if (equals(p, path))
            {
                ret = false;
                break;
            }

        return ret;
    }



    static bool visited(RoadSegment n, RoadSegment[] path)
    {
        bool ret = false;

        foreach (RoadSegment p in path)
            if (p == n)
            {
                ret = true;
                break;
            }

        return ret;
    }
}
 */
    public List<Vector3> VerifySnapPoints(List<Vector3> vectorList)
        {
        for(int i = 0; i < vectorList.Count; i++)
        {
            List<Vector3> closeSpots = new List<Vector3>();
            closeSpots.AddRange(from point in vectorList where Vector3.Distance(point, vectorList[i]) <= 1 && Vector3.Distance(point, snapPoints[i]) > 0 select point);
            for(int j = 0; j < closeSpots.Count; j++)
            {
                vectorList.Remove(closeSpots[j]);
            }
            RaycastHit hit;
            if (Physics.Raycast(vectorList[i] + (Vector3.up * 5), Vector3.down, out hit))
            {
                if(hit.collider.gameObject.name == "Plane")
                {
                    vectorList.Remove(vectorList[i]);
                    //GameObject prim = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    //prim.transform.position = (Physics.OverlapSphere(center, .001f)[0].gameObject.GetComponent<BoxCollider>()).center;
                    //prim.transform.position = hit.point;
                    //prim.GetComponent<MeshRenderer>().material.color = Color.red;
                }
                //Debug.Log(hit.collider.gameObject.name);
                
            }
            
        }
        return vectorList;
    }

    /*public void Snap()
    {
        for(int i = 0; i < snapPoints.Count; i++)
        {
            List<Vector3> nearPoints = new List<Vector3>();
            nearPoints.AddRange(from point in snapPoints where Vector3.Distance(point, snapPoints[i]) <= roadLength + 1 && Vector3.Distance(point, snapPoints[i]) > 0 select point);
            List<Vector3> connectionPoints = new List<Vector3>();
            for (int j = 0; j < nearPoints.Count; j++)
            {
                Vector3 center = (nearPoints[j] + snapPoints[i]) / 2;
                RaycastHit hit;
                if (Physics.Raycast(center + (Vector3.up * 5), Vector3.down, out hit))
                {
                    if (hit.collider.gameObject.name == "Plane")
                    {
                        connectionPoints.Add(nearPoints[j]);
                        //snapPoints.Remove(nearPoints[j]);
                    }

                }
                else
                {
                    //Debug.DrawLine(nearPoints[j], snapPoints[i], Color.red, 1000);
                    //GameObject prim = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    //prim.transform.position = (Physics.OverlapSphere(center, .001f)[0].gameObject.GetComponent<BoxCollider>()).center;
                    //prim.transform.position = center;
                    //prim.GetComponent<MeshRenderer>().material.color = Color.red;
                }
                
                

                
            }
            for(int j = 0; j < connectionPoints.Count; j++)
            {
                //Debug.DrawLine(connectionPoints[j], snapPoints[i], Color.green, 1000);
                GameObject go = GameObject.Instantiate(prefab, (snapPoints[i] + connectionPoints[j]) / 2, Quaternion.identity);
                LineRenderer renderer = go.AddComponent<LineRenderer>();
                renderer.positionCount = 2;
                Vector3[] positions = { connectionPoints[j], snapPoints[i] };
                renderer.SetPositions(positions);
            }
        }
    }*/

    public static bool RemoveOverMax(RoadSegment seg)
    {
        if(seg.highway && seg.degree > maximumHighwayLength)
        {
            Debug.Log("Removed highway");
            return true;
        }
        else if (!seg.highway && seg.degree > maximumStreetLength)
        {
            return true;
        }
        else
        {
            return false;
        }
    }

    public static List<RoadSegment> orderQueue(List<RoadSegment> segList)
    {
        List<RoadSegment> newSegList = new List<RoadSegment>();
        newSegList.AddRange((from road in segList where road.highway select road));
        newSegList.AddRange((from road in segList where !road.highway select road));
        

        return newSegList;
    }

    public void Generate()
    {
        if(roadQueue.Count <= 0)
        {
            return;
        }
        bool success = false;
        List<RoadSegment> roadList = roadQueue.ToList();
        
        roadList = roadList.OrderBy(x => x.order).ToList();
        string makelist = "";
        for(int i = 0; i < roadList.Count; i++)
        {
            makelist += roadList[i].order + ", ";
        }
        Debug.Log(makelist);
        //roadList.RemoveAll(RemoveOverMax);
        //roadList = orderQueue(roadList);
        Queue<RoadSegment> orderedQueue = new Queue<RoadSegment>(roadList.ToArray());
        roadQueue = orderedQueue;
        
        RoadSegment road = roadQueue.Peek();
        for (int i = 0; i < 5; i++)
        {
            
            if(GenerationStep(road))
            {

                road = roadQueue.Dequeue();
                RoadSegment newRoad = roadQueue.Peek();
                if (!newRoad.branched)
                {
                    if(newRoad.parent == null)
                    {
                        newRoad.parent = road;
                    }
                }
                
                success = true;
                break;
            }
            
            

        }
        if(!success)
        {
            
            road = roadQueue.Dequeue();
            Branch(road);
        }
        //Debug.Log(roadQueue.Count);
    }

    

    public Vector3 FindNext(RoadSegment seg)
    {

        float angle = Mathf.Atan2((seg.startPos.z - seg.endPos.z), (seg.startPos.x - seg.endPos.x));
        float originalAngle = Mathf.Atan2((seg.startPos.z - seg.endPos.z), (seg.startPos.x - seg.endPos.x));

        float deviation;
        if (seg.highway)
        {
             deviation = UnityEngine.Random.Range((float)(-22.5 * Mathf.Deg2Rad), (float)(22.5 * Mathf.Deg2Rad));
        }
        else
        {
            deviation = 0;
        }
        
        angle += deviation;
        float checkAngle = Mathf.Atan((0 - seg.endPos.z) / (0 - seg.endPos.x));
        float x;
        float z;
        if (originalAngle >= -90 && originalAngle <= 180 * Mathf.Deg2Rad)
        {
            x = seg.endPos.x - (Mathf.Cos(angle) * roadLength);
            z = seg.endPos.z - (Mathf.Sin(angle) * roadLength);
            //Debug.Log("Angle subtract " + angle * Mathf.Rad2Deg);
        }
        else
        {
            x = Mathf.Cos(angle) * roadLength + seg.endPos.x;
            z = Mathf.Sin(angle) * roadLength + seg.endPos.z;
            //Debug.Log("Angle add " + angle * Mathf.Rad2Deg);
        }
        Vector3 newPos = new Vector3(x, 0, z);
        Debug.DrawLine(seg.endPos, newPos, Color.red, 100000);
        return newPos;
    }

    

    public bool GenerationStep(RoadSegment seg)
    {
        Vector3 ideal = FindNext(seg);
        int order;
        if(seg.highway)
        {
            order = 0;
        }
        else
        {
            order = 10;
        }
        RoadSegment idealRoad = new RoadSegment(seg.endPos, ideal, seg.degree + 1, seg.highway, false, order);
        for (int i = 0; i < 5; i++)
        {
            Vector3 newPos = FindNext(seg);
            RoadSegment newRoad = new RoadSegment(seg.endPos, newPos, seg.degree + 1, seg.highway, false);
            if(GetPixelColorAtPoint(newPos) < GetPixelColorAtPoint(ideal) && localConstraints(newRoad) != null)
            {
                ideal = newPos;
                idealRoad = newRoad;
            }
        }
        
        
        

        if (localConstraints(idealRoad) != null && Physics.Raycast(ideal, Vector3.down))
        {
            if(!seg.highway)
            {
                //Snap(idealRoad);
                float angle = Mathf.Atan2((idealRoad.startPos.z - idealRoad.endPos.z), (idealRoad.startPos.x - idealRoad.endPos.x));
                angle += 90 * Mathf.Rad2Deg;
                float x = Mathf.Cos(angle) * roadLength + idealRoad.startPos.x;
                float z = Mathf.Sin(angle) * roadLength + idealRoad.startPos.z;
                Vector3 newPos = new Vector3(x, 0, z);
                RoadSegment rotatedRoad = new RoadSegment(idealRoad.startPos, newPos, seg.degree + 1, seg.highway, false);
                //Snap(rotatedRoad);
            }
            if (seg.parent != null)
            {
                idealRoad.parent = seg.parent;
                if (seg.parent.children != null)
                {
                    seg.parent.children.Add(idealRoad);
                }
            }
            if (BranchProbability(idealRoad.highway))
            {
                idealRoad.degree = 0;
                idealRoad.branched = true;
                //idealRoad.connections = seg.connections;
                idealRoad.connections.Add(seg);
                //seg.connections.Add(idealRoad);
                edges.Add(new RoadEdge(seg, idealRoad));
                //edges.Add(new RoadEdge(idealRoad, idealRoad.parent));
                roadQueue.Enqueue(idealRoad);
                roadList.Add(idealRoad);
                Branch(idealRoad);
                return true;

            }
            else
            {
                //idealRoad.connections = seg.connections;
                idealRoad.connections.Add(seg);
                //seg.connections.Add(idealRoad);
                edges.Add(new RoadEdge(seg, idealRoad));
                //edges.Add(new RoadEdge(idealRoad, idealRoad.parent));
                roadQueue.Enqueue(idealRoad);
                roadList.Add(idealRoad);
                return true;
            }
        }
        
        else
        {
            //GameObject prim = GameObject.CreatePrimitive(PrimitiveType.Cube);
            //prim.transform.position = ideal;
            //prim.GetComponent<MeshRenderer>().material.color = Color.red;
            
            //Debug.Log("Failed: " + ideal + ", Source: " + seg.endPos);
            return false;
        }
        //return true;
        
    }



    

    public void Render()
    {
        for(int i = 0; i < roads.Count; i++)
        {
            GameObject.Destroy(roads[i]);
        }
        positions.Clear();
        roads.Clear();
        List<Vector3> highwayList = new List<Vector3>();
        List<Vector3> streetList = new List<Vector3>();
        List<RoadSegment> errorRoads = new List<RoadSegment>();


        for (int i = 0; i < roadList.Count; i++)
        {
            for(int j = 0; j < roadList[i].connections.Count; j++)
            {
                if(!(roadList[i].highway && roadList[i].connections[j].highway))
                {
                    //Debug.DrawLine(roadList[i].endPos, roadList[i].connections[j].startPos, Color.red, 1000000000000);
                }
               
            }
            //graph.Add(roadList[i], roadList[i].connections);
        }


        highwayList.AddRange(from road in roadList where road.highway select road.startPos);
        highwayList.AddRange(from road in roadList where road.highway select road.endPos);

        streetList.AddRange(from road in roadList where !road.highway select road.startPos);
        streetList.AddRange(from road in roadList where !road.highway select road.endPos);

        highwayList = highwayList.Distinct().ToList();
        streetList = streetList.Distinct().ToList();
        
        positions.AddRange(highwayList);
        positions.AddRange(streetList);
        /*for (int i = 0; i < highwayList.Count; i++)
        {
            GameObject prim = GameObject.Instantiate(prefab);
            prim.transform.position = highwayList[i];
            prim.GetComponent<MeshRenderer>().material.color = Color.blue;
            roads.Add(prim);

        }
        for (int i = 0; i < streetList.Count; i++)
        {
            GameObject prim = GameObject.Instantiate(prefab);
            prim.transform.position = streetList[i];
            prim.GetComponent<MeshRenderer>().material.color = Color.gray;
            roads.Add(prim);
        }
        for (int i = 0; i < roadList.Count; i++)
        {
            float angle = Mathf.Atan2((roadList[i].startPos.z - roadList[i].endPos.z), (roadList[i].startPos.x - roadList[i].endPos.x));
            GameObject prim = GameObject.Instantiate(prefab);
            prim.transform.position = (roadList[i].startPos + roadList[i].endPos) / 2;
            //prim.transform.Rotate(Vector3.up, (angle * Mathf.Rad2Deg));
            //Debug.Log((roadList[i].startPos + roadList[i].endPos) / 2 + " , " + angle);
            if(roadList[i].highway)
            {
                prim.GetComponent<MeshRenderer>().material.color = Color.blue;
            }
            else
            {
                prim.GetComponent<MeshRenderer>().material.color = Color.gray;
            }
            RoadBlock newRoadBlock = prim.AddComponent<RoadBlock>();
            newRoadBlock.road = roadList[i];
            newRoadBlock.block = prim;
            roads.Add(newRoadBlock);
            for(int j = 0; j < roadList[i].children.Count; j++)
            {
                Debug.Log(prim.transform.position + " is parent of child number " + j + " at " + (roadList[i].children[j].startPos + roadList[i].children[j].endPos) / 2);
            }
        }*/
        for (int i = 0; i < roadList.Count; i++)
        {
            if(roadList[i].parent == roadList[i])
            {
                GameObject go = GameObject.Instantiate(prefab, (roadList[i].startPos + roadList[i].endPos) / 2, Quaternion.identity);
                roads.Add(go);
                LineRenderer renderer = go.AddComponent<LineRenderer>();
                if(roadList[i].highway)
                {
                    renderer.startWidth = 5;
                    renderer.endWidth = 5;
                }
                renderer.positionCount = roadList[i].children.Count + 2;
                List<Vector3> posList = new List<Vector3>();
                List<RoadSegment> childList = new List<RoadSegment>();
                posList.Add(roadList[i].startPos);
                posList.Add(roadList[i].endPos);
                if(roadList[i].children.Count > 0)
                {
                    
                }
                
                posList.AddRange(from road in roadList[i].children select road.endPos);
                childList.AddRange(from road in roadList[i].children select road);
                //childList.Add(roadList[i]);
                
                renderer.SetPositions(posList.ToArray());
                Vector3[] positionsArray = new Vector3[renderer.positionCount];
                renderer.GetPositions(positionsArray);
                
                //Mesh mesh = new Mesh();
                //renderer.BakeMesh(mesh);
                //go.transform.localPosition = Vector3.zero;
                //MeshFilter meshFilter = go.AddComponent<MeshFilter>();
                //meshFilter.sharedMesh = mesh;
                //go.AddComponent<MeshRenderer>();
                //MeshCollider collider = go.AddComponent<MeshCollider>();
                //collider.sharedMesh = mesh;
            }

        }
        //Expand();
    }

    public void CreateRoads(List<Vector3> posLis)
    {
        for(int i = 0; i < posLis.Count; i++)
        {
            //GameObject prim = GameObject.CreatePrimitive(PrimitiveType.Cube);
            //prim.transform.position = newPos;
            //prim.GetComponent<MeshRenderer>().material.color = Color.green;
        }
    }

    public bool IsNearbyHighway(RoadSegment seg)
    {
        List<RoadSegment> highways = new List<RoadSegment>();
        highways.AddRange(from road in roadList where road.highway && Vector3.Distance(seg.endPos, road.endPos) < 300 select road);
        if (highways.Count > 0)
        {
            return true;
        }
        else return false;
    }

    public void Snap(RoadSegment segment)
    {
        List<RoadSegment> canidates = new List<RoadSegment>();
        canidates.AddRange(from road in roadList where (Vector3.Distance(segment.endPos, road.startPos) < 3 || Vector3.Distance(segment.endPos, road.endPos) < 3) select road);

        for(int i = 0; i < canidates.Count; i++)
        {
            
                if (Vector3.Distance(segment.endPos, canidates[i].startPos) < 1 || Vector3.Distance(segment.endPos, canidates[i].endPos) < 1)
                {
                    RoadSegment newRoad = new RoadSegment(segment.endPos, canidates[i].startPos, segment.degree + 1, segment.highway, false);

                    if (!(from road in roadList where Vector3.Distance(newRoad.startPos, road.startPos) < .5 && Vector3.Distance(newRoad.endPos, road.endPos) < .5 && Vector3.Distance(newRoad.startPos, road.startPos) < .5 select road).Any())
                    {
                        if(GetRoadSegment(newRoad.startPos, newRoad.endPos) == null)
                    {
                        Debug.DrawLine(newRoad.endPos, newRoad.startPos, Color.green, 1000);
                        newRoad.parent = newRoad;
                        newRoad.children.Add(newRoad);
                        roadQueue.Enqueue(newRoad);
                        roadList.Add(newRoad);
                    }
                    }

                        
                }


            
            
        }

    }

    public RoadSegment? localConstraints(RoadSegment segment)
    {
        List<RoadSegment> intersections = new List<RoadSegment>();
        List<RoadSegment> tooClose = new List<RoadSegment>();
        intersections.AddRange(from road in roadList where (Vector3.Distance(segment.endPos, road.startPos) < 5 || Vector3.Distance(segment.endPos, road.endPos) < 5) select road);
        //intersections.AddRange(from road in roadList where road.endPos == segment.endPos && road.startPos == segment.startPos select road);

        float angle = Mathf.Atan2((segment.startPos.z - segment.endPos.z), (segment.startPos.x - segment.endPos.x));
        float angle1 = angle + (90 * Mathf.Deg2Rad);
        float x1 = segment.endPos.x - (Mathf.Cos(angle1) * roadLength);
        float z1 = segment.endPos.z - (Mathf.Sin(angle1) * roadLength);
        Vector3 newPos = new Vector3(x1, 0, z1);
        float angle2 = angle + (90 * Mathf.Deg2Rad);
        float x2 = Mathf.Cos(angle2) * roadLength + segment.endPos.x;
        float z2 = Mathf.Sin(angle2) * roadLength + segment.endPos.z;
        Vector3 newPos2 = new Vector3(x2, 0, z2);

        RaycastHit hit;

        if (GetRoadSegment(segment.startPos, segment.endPos) != null) return null;

        if(segment.highway)
        {
            if(intersections.Count > 0)
            {
                if((from road in roadList where road.highway &&  (Vector3.Distance(segment.endPos, road.startPos) < 15 || Vector3.Distance(segment.endPos, road.endPos) < 15) select road).Any())
                {
                    RoadSegment first = (from road in roadList where road.highway && (Vector3.Distance(segment.endPos, road.startPos) < 15 || Vector3.Distance(segment.endPos, road.endPos) < 15) select road).First();
                    ///if (Vector3.Distance(segment.endPos, first.startPos) < 3 || Vector3.Distance(segment.endPos, first.endPos) < 3)
                    //{
                        //Debug.DrawLine(segment.startPos, first.endPos, Color.blue, 100000);
                        //segment.endPos = first.endPos;
                        //return segment;
                    //}


                }
            }
        }

        tooClose.AddRange(from road in roadList where Vector3.Distance(road.endPos, newPos) < 1 || Vector3.Distance(road.endPos, newPos2) < 1 select road);
        if((from road in roadList where road.highway && (Vector3.Distance(segment.endPos, road.startPos) < 4.8 || Vector3.Distance(segment.endPos, road.endPos) < 4.8) select road).Any())
        {
            return null;
        }
        if (intersections.Count > 0)
        {
            
            for(int i = 0; i < intersections.Count; i++)
            {
                
                    
                    
                    if(Vector3.Distance(segment.endPos, intersections[i].startPos) < 1 || Vector3.Distance(segment.endPos, intersections[i].endPos) < 1)
                    {
                        //Snap(segment);
                        //Debug.DrawLine(segment.endPos, intersections[i].startPos, Color.green, 1000);
                        segment.endPos = intersections[i].endPos;
                        
                        return segment;
                    }
                    else if (Vector3.Distance(segment.startPos, intersections[i].startPos) < 3 || Vector3.Distance(segment.startPos, intersections[i].endPos) < 3)
                    {
                        segment.startPos = intersections[i].endPos;

                        return null;
                    }
            }
                

                //Debug.DrawLine(segment.endPos, intersections[i].startPos, Color.red, 1000);
            

            //Debug.Log("Intersection at " + segment.startPos + " ending at " + segment.endPos);
            //Snap(segment);
            return null;
        }
        if (tooClose.Count > 0 && tooClose.Count <= 2)
        {
            if (Physics.Raycast(segment.endPos, Vector3.down, out hit))
            {
                
            }
            return segment;
        }
        else if(tooClose.Count > 2)
        {
            
            return segment;
        }
        if (!segment.highway && IsNearbyHighway(segment))
        {

            return segment;
        }
        else if (segment.highway)
        {
            return segment;
        }
        else 
        {
            //GameObject prim = GameObject.CreatePrimitive(PrimitiveType.Cube);
            //prim.transform.position = segment.endPos;
            //prim.GetComponent<MeshRenderer>().material.color = Color.red;
            //Debug.Log("Too far from highway");
            return null;
        } 
    }

    public bool Branch(RoadSegment seg)
    {
        float angle = Mathf.Atan2((seg.startPos.z - seg.endPos.z), (seg.startPos.x - seg.endPos.x));
        int num = UnityEngine.Random.Range(0, 2);
        if(num == 0)
        {
            angle += 90 * Mathf.Deg2Rad;
        }
        else
        {
            angle -= 90 * Mathf.Deg2Rad;
        }
        float x = Mathf.Cos(angle) * roadLength + seg.endPos.x;
        float z = Mathf.Sin(angle) * roadLength + seg.endPos.z;
        Vector3 newPos = new Vector3(x, 0, z);
        RoadSegment newRoad = new RoadSegment(seg.endPos, newPos, 0, HighwayProbability(seg), false, 10);
        if (localConstraints(newRoad) != null)
        {
            lastBranch = 5;
            newRoad.parent = newRoad;
            roadQueue.Enqueue(newRoad);
            roadList.Add(newRoad);
            //Debug.Log("Branch");
            //GameObject prim = GameObject.CreatePrimitive(PrimitiveType.Cube);
            //prim.transform.position = newRoad.startPos;
            //prim.GetComponent<MeshRenderer>().material.color = Color.red;
            //Debug.Log("Intersection");
            return true;
        }
        else
        {
            //GameObject prim = GameObject.CreatePrimitive(PrimitiveType.Cube);
            //prim.transform.position = newPos;
            //prim.GetComponent<MeshRenderer>().material.color = Color.black;
            return false;
        }
    }

    public bool BranchProbability(bool highway)
    {
        float num = UnityEngine.Random.Range(0f, 1f);
        //Debug.Log(num);
        if (num < highwayBranchProbability && highway)
        {
            return true;
        }
        else if(num < streetBranchProbability && !highway)
        {
            return true;
        }
        else return false;
    }

    public bool HighwayProbability(RoadSegment road)
    {
        if(road.highway != true)
        {
            return false;
        }
        float num = UnityEngine.Random.Range(0f, 1f);
        //Debug.Log(num);
        if (num < highwayProbability)
        {
            return true;
        }
        else return false;
    }

    public float GetPixelColorAtPoint(Vector3 point)
    {
        RaycastHit hit;
        if (!Physics.Raycast(point, Vector3.down, out hit))
            
            return 100;
        //Debug.Log(hit);
        Renderer rend = hit.transform.GetComponent<Renderer>();
        MeshCollider meshCollider = hit.collider as MeshCollider;

        if (rend == null || rend.sharedMaterial == null || rend.sharedMaterial.mainTexture == null || meshCollider == null)
            return 100;

        Texture2D tex = rend.material.mainTexture as Texture2D;
        Vector2 pixelUV = hit.textureCoord;
        pixelUV.x *= tex.width;
        pixelUV.y *= tex.height;

        Color color =  tex.GetPixel((int)pixelUV.x, (int)pixelUV.y);
        return color.r;
    }

    public void BuildingTest()
    {
        
        for(int i = 0; i < roadList.Count; i++)
        {
            if(!roadList[i].highway )
            {
                
                    for(int j = 0; j < 10; j++)
                    {
                        float angle = Mathf.Atan2((roadList[i].startPos.z - roadList[i].endPos.z), (roadList[i].startPos.x - roadList[i].endPos.x));
                        Vector3 center = (roadList[i].startPos + roadList[i].endPos) / 2;
                        float x = Mathf.Cos(angle) * j + 1 + center.x;
                        float z = Mathf.Sin(angle) * j + 1 + center.z;
                        Vector3 newPos = new Vector3(x, 0, z);
                        GameObject building1 = SpawnBuilding(newPos, angle);
                        //building1.transform.localScale = new Vector3(1, 1, 1);
                    //GameObject building2 = SpawnBuilding(roadList[i].startPos, angle);
                    building1= MoveBuilding(building1);

                        //if((from road in roadList where Vector3.Distance(building1.transform.position, road.endPos) < 2 || Vector3.Distance(building1.transform.position, road.startPos) < 2 && !road.highway select road).Any()) 
                        if (((from go in buildingList where Vector3.Distance(go.transform.position, building1.transform.position) < 1.5f select go).Any()))
                        {
                        //Destroy(building1);
                        //GameObject conflict = (from go in buildingList where Vector3.Distance(go.transform.position, building1.transform.position) < 1 select go).First();

                            GameObject.Destroy(building1);
                        ;                        }
                        else if(Vector3.Distance(center, building1.transform.position) < 3)
                        {
                            buildingList.Add(building1);
                        }
                        
                        else
                        {
                        building1.gameObject.GetComponentInChildren<MeshRenderer>().material.color = Color.yellow;
                        GameObject.Destroy(building1);
                    }
                        
                        
                    }   
                    
                

                
            }
            
        }

    }

    public GameObject SpawnBuilding(Vector3 position, float angle)
    {
        int random = UnityEngine.Random.Range(0, 4);
        GameObject prim = Instantiate(buildingTypes[random]);
        Building building = prim.AddComponent<Building>();
        prim.transform.position = position;
        prim.transform.Rotate(new Vector3(0, angle * Mathf.Rad2Deg * -1, 0));
        return prim;
    }

    public GameObject MoveBuilding(GameObject building)
    {
        while (Physics.CheckBox(building.transform.position, building.GetComponentInChildren<Collider>().bounds.size /2, building.transform.rotation))
        {
            Vector3 movement = RandomMovement() ;
            while ((from go in buildingList where Vector3.Distance(go.transform.position, building.transform.position + movement) < 2 select go).Any())
            {
                movement += RandomMovement()  ;
            }
            building.transform.position += movement;
        }
        return building;
    }

    public Vector3 RandomMovement()
    {
        int random = UnityEngine.Random.Range(0, 6);
        switch(random)
        {
            case 0: return Vector3.forward;
            case 1: return Vector3.back;
            case 2: return Vector3.right;
            case 3: return Vector3.left;
            case 4: return Vector3.back;
            case 5: return Vector3.forward;
            default: return Vector3.forward;
        }
    }

}

public class Building : MonoBehaviour
{
    public bool colliding;
    public GameObject geometry;

    void OnCollisionEnter()
    {
        Debug.Log("*");
        colliding = true;
    }

    void OnCollisionExit()
    {
        Debug.Log("*");
        colliding = false;
    }

    void OnCollisionStay()
    {
        Debug.Log("*");
        colliding = true;
    }
}