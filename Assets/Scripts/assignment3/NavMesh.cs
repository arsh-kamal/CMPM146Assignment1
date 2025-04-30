using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;

public class NavMesh : MonoBehaviour
{
    List<Wall> outline;

    public Graph MakeNavMesh(List<Wall> outline)
    {
        // Step 1: Split into convex polygons
        List<List<Wall>> convexPolygons = SplitIntoConvexPolygons(outline);

        // Step 2: Build graph
        Graph g = new Graph();
        g.all_nodes = new List<GraphNode>();

        for (int i = 0; i < convexPolygons.Count; i++)
        {
            GraphNode node = new GraphNode(i, convexPolygons[i]);
            g.all_nodes.Add(node);
        }

        // Step 3: Connect neighbors (shared wall logic, with correct edge indexing)
        for (int i = 0; i < g.all_nodes.Count; i++)
        {
            for (int j = i + 1; j < g.all_nodes.Count; j++)
            {
                Wall shared = FindSharedWall(g.all_nodes[i].GetPolygon(), g.all_nodes[j].GetPolygon());
                if (shared != null)
                {
                    int sharedIndexI = g.all_nodes[i].GetPolygon().FindIndex(w => w.Same(shared));
                    int sharedIndexJ = g.all_nodes[j].GetPolygon().FindIndex(w => w.Same(shared));

                    g.all_nodes[i].AddNeighbor(g.all_nodes[j], sharedIndexI);
                    g.all_nodes[j].AddNeighbor(g.all_nodes[i], sharedIndexJ);
                }
            }
        }

        return g;
    }

    // Helper to split outline into convex parts
    private List<List<Wall>> SplitIntoConvexPolygons(List<Wall> polygon)
    {
        List<List<Wall>> result = new List<List<Wall>>();
        Queue<List<Wall>> queue = new Queue<List<Wall>>();
        queue.Enqueue(polygon);

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            int reflexIndex = FindReflexVertex(current);

            if (reflexIndex == -1)
            {
                // Already convex
                result.Add(current);
                continue;
            }

            // Find best split
            int bestSplit = FindBestSplit(current, reflexIndex);

            if (bestSplit == -1)
            {
                Debug.LogWarning("Could not find valid split!");
                result.Add(current);
                continue;
            }

            // Split polygon into two
            List<Wall> poly1, poly2;
            SplitPolygon(current, reflexIndex, bestSplit, out poly1, out poly2);

            queue.Enqueue(poly1);
            queue.Enqueue(poly2);
        }

        return result;
    }

    // Find first reflex vertex
    private int FindReflexVertex(List<Wall> polygon)
    {
        int n = polygon.Count;
        for (int i = 0; i < n; i++)
        {
            Vector3 a = polygon[(i - 1 + n) % n].start;
            Vector3 b = polygon[i].start;
            Vector3 c = polygon[(i + 1) % n].start;

            if (!IsConvex(a, b, c))
                return i;
        }
        return -1;
    }

    private bool IsConvex(Vector3 a, Vector3 b, Vector3 c)
    {
        return Vector3.Cross(c - b, a - b).y >= 0f;
    }

    // Try to find a good vertex to split with
    private int FindBestSplit(List<Wall> polygon, int reflexIndex)
    {
        int n = polygon.Count;
        Vector3 reflexVertex = polygon[reflexIndex].start;
        float bestScore = float.MaxValue;
        int bestIndex = -1;

        for (int i = 0; i < n; i++)
        {
            if (i == reflexIndex || (i - 1 + n) % n == reflexIndex || (i + 1) % n == reflexIndex)
                continue; // Don't connect to adjacent vertices

            Vector3 candidate = polygon[i].start;

            if (!CanSee(reflexVertex, candidate, polygon))
                continue;

            float angle = Vector3.Angle(candidate - reflexVertex, polygon[(reflexIndex + 1) % n].start - reflexVertex);
            float score = Mathf.Abs(angle - 90f); // prefer ~90 degree splits

            if (score < bestScore)
            {
                bestScore = score;
                bestIndex = i;
            }
        }

        return bestIndex;
    }

    private bool CanSee(Vector3 from, Vector3 to, List<Wall> polygon)
    {
        foreach (var wall in polygon)
        {
            if (Crosses(from, to, wall.start, wall.end))
                return false;
        }
        return true;
    }

    private bool Crosses(Vector3 a, Vector3 b, Vector3 c, Vector3 d)
    {
        if (SamePoint(a, c) || SamePoint(a, d) || SamePoint(b, c) || SamePoint(b, d))
            return false;

        return (CCW(a, c, d) != CCW(b, c, d)) && (CCW(a, b, c) != CCW(a, b, d));
    }

    private bool CCW(Vector3 a, Vector3 b, Vector3 c)
    {
        return (c.x - a.x) * (b.z - a.z) > (b.x - a.x) * (c.z - a.z);
    }

    private bool SamePoint(Vector3 a, Vector3 b)
    {
        return (a - b).sqrMagnitude < Mathf.Epsilon;
    }

    private void SplitPolygon(List<Wall> polygon, int i, int j, out List<Wall> poly1, out List<Wall> poly2)
    {
        int n = polygon.Count;
        poly1 = new List<Wall>();
        poly2 = new List<Wall>();

        // Create the new splitting wall
        Wall splitter = new Wall(polygon[i].start, polygon[j].start);

        // Build first polygon
        int curr = i;
        while (curr != j)
        {
            poly1.Add(new Wall(polygon[curr].start, polygon[curr].end));
            curr = (curr + 1) % n;
        }
        poly1.Add(new Wall(polygon[j].start, polygon[i].start)); // close it

        // Build second polygon
        curr = j;
        while (curr != i)
        {
            poly2.Add(new Wall(polygon[curr].start, polygon[curr].end));
            curr = (curr + 1) % n;
        }
        poly2.Add(new Wall(polygon[i].start, polygon[j].start)); // close it
    }

    private Wall FindSharedWall(List<Wall> poly1, List<Wall> poly2)
    {
        foreach (var w1 in poly1)
        {
            foreach (var w2 in poly2)
            {
                if (w1.Same(w2))
                    return w1;
            }
        }
        return null;
    }

    void Start()
    {
        EventBus.OnSetMap += SetMap;
    }

    void Update()
    {
    }

    public void SetMap(List<Wall> outline)
    {
        Graph navmesh = MakeNavMesh(outline);
        if (navmesh != null)
        {
            Debug.Log("✅ Got navmesh: " + navmesh.all_nodes.Count);
            EventBus.SetGraph(navmesh);
        }
    }
}
