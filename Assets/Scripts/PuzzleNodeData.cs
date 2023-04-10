using System.Collections.Generic;
using UnityEngine;

/*
    Classes for Puzzle Node data (line layouts)
    Hand authoring this data can be a bit of a pain, so i reccomend making a tool to author this data
*/

public enum NodeType
{
    NORMAL,
    START,
    End
}

[System.Serializable]
public class PuzzleNode
{
    public PuzzleNode(Vector2 p)
    {
        pos = p;
        type = NodeType.NORMAL;
    }
    public Vector2 pos = Vector2.zero;
    public NodeType type = NodeType.NORMAL;
}

[System.Serializable]
public class NodePath
{
    public NodePath(int from, int to)
    {
        a = from;
        b = to;
    }
    public int a = 0;
    public int b = 0;
}

[CreateAssetMenu(fileName = "NewNodeData", menuName = "WittnessStudy/PuzzleNodeData")]
public class PuzzleNodeData : ScriptableObject
{
    [System.Flags]
    private enum NodeDataError
    {
        NONE,
        BADPATH,
        DUPEPATH,
        NOEND,
        NOSTART,
        OVERLAP,
    }

    public List<PuzzleNode> Nodes = new List<PuzzleNode>();
    public List<NodePath> Paths = new List<NodePath>();

    //Returns a copy of all normal nodes (and also inline start nodes)
    public void CopyBoundedNodes(out List<PuzzleNode> nodesCopy, out List<NodePath> pathsCopy)
    {
        nodesCopy = new List<PuzzleNode>();
        pathsCopy = new List<NodePath>();

        //Should we consider the start node within the bounds of the puzzle?
        bool InlineStart = GetConnectedNodes(GetStartNode()).Count > 1;

        foreach (PuzzleNode n in Nodes)
        {
            if (n.type == NodeType.End || (!InlineStart && n.type == NodeType.START))
            {
                continue; //Dont factor in end nodes or non inline start nodes as we allow them to be out of bounds
            }
            nodesCopy.Add(n);
        }
        foreach (NodePath p in Paths)
        {
            if (!nodesCopy.Contains(Nodes[p.a]) || !nodesCopy.Contains(Nodes[p.b])) continue; //irrelevent path
            pathsCopy.Add(p);
        }
    }
    
    public List<PuzzleNode> GetConnectedNodes(PuzzleNode n)
    {
        List<PuzzleNode> ret = new List<PuzzleNode>();
        int i = Nodes.IndexOf(n);
        foreach (NodePath path in Paths)
        {
            if (path.a == i)
            {
                ret.Add(Nodes[path.b]);
            }
            else if (path.b == i)
            {
                ret.Add(Nodes[path.a]);
            }
        }
        return ret;
    }

    public PuzzleNode GetStartNode()
    {
        foreach (PuzzleNode n in Nodes)
        {
            if (n.type == NodeType.START) return n;
        }
        Debug.LogError("No puzzle start node found!");
        return null;
    }

    public bool IsValid()
    {
        if(Nodes == null || Paths == null)
        {
            Debug.LogError("Null data in data: " + name);
            return false;
        }

        NodeDataError errors = NodeDataError.NONE;
        for(int i = 0; i < Paths.Count; ++i)
        {
            if (Paths[i].a < 0 || Paths[i].a >= Nodes.Count) errors |= NodeDataError.BADPATH;
            if (Paths[i].b < 0 || Paths[i].b >= Nodes.Count) errors |= NodeDataError.BADPATH;
            for(int j = 0; j < Paths.Count; ++j)
            {
                if (i == j) continue;
                if (Paths[i].a == Paths[j].a && Paths[i].b == Paths[j].b)
                {
                    errors |= NodeDataError.DUPEPATH;
                }
                if (Paths[i].b == Paths[j].a && Paths[i].a == Paths[j].b)
                {
                    errors |= NodeDataError.DUPEPATH;
                }
            }
        }

        bool hasStart = false;
        bool hasEnd = false;
        foreach (PuzzleNode n in Nodes)
        {
            foreach (PuzzleNode n1 in Nodes)
            {
                if(n!=n1 && n.pos == n1.pos)
                {
                    errors |= NodeDataError.OVERLAP;
                }
            }

            if (n.type == NodeType.START)
            {
                hasStart = true;
            }
            if (n.type == NodeType.End)
            {
                hasEnd = true;
            }
        }
        if(!hasStart) errors |= NodeDataError.NOSTART;
        if(!hasEnd) errors |= NodeDataError.NOEND;

        if (errors.HasFlag(NodeDataError.BADPATH)) Debug.LogError("Bad path indicies in data: " + name);
        if (errors.HasFlag(NodeDataError.DUPEPATH)) Debug.LogError("Duplicate path in data: " + name);
        if (errors.HasFlag(NodeDataError.NOEND)) Debug.LogError("No end in data:  " + name);
        if (errors.HasFlag(NodeDataError.NOSTART)) Debug.LogError("No start in data: " + name);
        if (errors.HasFlag(NodeDataError.OVERLAP)) Debug.LogError("Node overlap in data: " + name);

        return errors == NodeDataError.NONE;
    }
}