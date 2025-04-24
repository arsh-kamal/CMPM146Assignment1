using UnityEngine;
using System.Collections.Generic;

public class PathFinder : MonoBehaviour
{
    public static (List<Vector3>, int) AStar(GraphNode start, GraphNode destination, Vector3 target)
    {
        var frontier = new List<AStarEntry>();
        var costSoFar = new Dictionary<int, float>();
        var visited = new HashSet<int>();

        frontier.Add(new AStarEntry(start, null, null, 0f, Vector3.Distance(start.GetCenter(), target)));
        costSoFar[start.GetID()] = 0f;

        int expanded = 0;

        while (frontier.Count > 0)
        {
            frontier.Sort((a, b) => a.F.CompareTo(b.F));
            var current = frontier[0];
            frontier.RemoveAt(0);

            if (visited.Contains(current.Node.GetID()))
                continue;

            visited.Add(current.Node.GetID());
            expanded++;

            if (current.Node.GetID() == destination.GetID())
            {
                List<Vector3> path = new List<Vector3>();
                AStarEntry trace = current;

                while (trace.Prev != null)
                {
                    path.Add(trace.PrevWall.GetWall().midpoint); // ✅ correct wall midpoint
                    trace = trace.Prev;
                }

                path.Reverse();         // from start to destination
                path.Add(target);       // add the final clicked position
                return (path, expanded);
            }

            foreach (var neighbor in current.Node.GetNeighbors())
            {
                var next = neighbor.GetNode();
                int nextID = next.GetID();
                float newCost = current.G + Vector3.Distance(current.Node.GetCenter(), next.GetCenter());

                if (!costSoFar.ContainsKey(nextID) || newCost < costSoFar[nextID])
                {
                    costSoFar[nextID] = newCost;
                    float h = Vector3.Distance(next.GetCenter(), target);
                    frontier.Add(new AStarEntry(next, current, neighbor, newCost, h));
                }
            }
        }

        Debug.LogWarning("❌ No path found.");
        return (new List<Vector3> { target }, expanded);
    }

    private class AStarEntry
    {
        public GraphNode Node;
        public AStarEntry Prev;
        public GraphNeighbor PrevWall;
        public float G;
        public float F;

        public AStarEntry(GraphNode node, AStarEntry prev, GraphNeighbor wall, float g, float h)
        {
            Node = node;
            Prev = prev;
            PrevWall = wall;
            G = g;
            F = g + h;
        }
    }

    public Graph graph;

    void Start()
    {
        EventBus.OnTarget += PathFind;
        EventBus.OnSetGraph += SetGraph;
    }

    public void SetGraph(Graph g)
    {
        graph = g;
    }

    public void PathFind(Vector3 target)
    {
        if (graph == null) return;

        GraphNode start = null;
        GraphNode destination = null;

        foreach (var n in graph.all_nodes)
        {
            if (Util.PointInPolygon(transform.position, n.GetPolygon()))
                start = n;
            if (Util.PointInPolygon(target, n.GetPolygon()))
                destination = n;
        }

        if (start == null || destination == null)
        {
            Debug.LogWarning("⚠️ Start or destination not found inside graph.");
            return;
        }

        EventBus.ShowTarget(target);
        (List<Vector3> path, int expanded) = AStar(start, destination, target);

        Debug.Log("✅ Path found with " + path.Count + " points, expanded " + expanded + " nodes.");
        EventBus.SetPath(path); // ✅ triggers drawing and car behavior
    }
}
