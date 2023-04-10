using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using Line = System.Collections.Generic.List<PuzzleNode>;
using NodePair = System.Tuple<PuzzleNode, PuzzleNode>;

/*
    Class for holding current puzzle information
    Used for testing inputted lines from LinePuzzle.cs
*/

[System.Serializable]
public class PuzzleData
{
    public PuzzleNodeData NodeData = null;
    public PuzzleElementData ElementData = null;

    private Vector2 m_boundsLow = Vector2.zero;
    private Vector2 m_boundsHigh = Vector2.zero;
    private Line m_bounds = new Line();

    #region Public Helpers

    public List<NodePair> GetNodeConnections()
    {
        List<NodePair> ret = new List<NodePair>();
        foreach (NodePath p in NodeData.Paths)
        {
            PuzzleNode n1 = NodeData.Nodes[p.a];
            PuzzleNode n2 = NodeData.Nodes[p.b];
            ret.Add(new NodePair(n1, n2));
        }
        return ret;
    }

    public Vector2 GetBoundsLow()
    {
        return m_boundsLow;
    }

    public Vector2 GetBoundsHigh()
    {
        return m_boundsHigh;
    }

    public Line GetBounds()
    {
        return m_bounds;
    }
    #endregion //Public Helpers End

    #region Public Functions

    //Call before doing anything else. Required to check for valid data and generate puzzle bounds
    public bool Init()
    {
        //Pre validate Data
        if (NodeData == null || !NodeData.IsValid())
        {
            return false;
        }


        //Temp copy of node data
        
        PuzzleNodeData tempData = ScriptableObject.CreateInstance<PuzzleNodeData>();
        NodeData.CopyBoundedNodes(out tempData.Nodes, out tempData.Paths);

        //Determine puzzle max bounds
        m_boundsLow = new Vector2(Mathf.Infinity, Mathf.Infinity);
        m_boundsHigh = new Vector2(Mathf.NegativeInfinity, Mathf.NegativeInfinity);
        foreach (PuzzleNode n in tempData.Nodes)
        {
            if (n.pos.x < m_boundsLow.x) m_boundsLow.x = n.pos.x;
            if (n.pos.y < m_boundsLow.y) m_boundsLow.y = n.pos.y;
            if (n.pos.x > m_boundsHigh.x) m_boundsHigh.x = n.pos.x;
            if (n.pos.y > m_boundsHigh.y) m_boundsHigh.y = n.pos.y;
        }

        //Determine what corner nodes need generated
        Vector2[] boundCorners = new Vector2[4] { 
            m_boundsLow,
            new Vector2(m_boundsLow.x, m_boundsHigh.y),
            m_boundsHigh,
            new Vector2(m_boundsHigh.x, m_boundsLow.y)
        }; //BL TL TR BR

        //Used to start off bounds, this should be set to a corner
        PuzzleNode startCorner = null;

        //Find or create new corners to generate 'square' bounds
        for (int i = 0; i<4; ++i)
        {
            bool foundCorner = false;
            foreach (PuzzleNode n in tempData.Nodes)
            {
                if (n.pos == boundCorners[i])
                {
                    startCorner = n;
                    foundCorner = true;
                    break;
                }
            }
            if (!foundCorner)
            {
                //Missing a corner at this point so make a new temp one for the sake of generating our puzzle bounds
                System.Predicate<PuzzleNode> GrabEdge1 = n => n.pos.x == boundCorners[i].x && n.pos.y != boundCorners[i].y;
                System.Predicate<PuzzleNode> GrabEdge2 = n => n.pos.x != boundCorners[i].x && n.pos.y == boundCorners[i].y;
                System.Func<PuzzleNode, float> OrderDist = n => Vector2.Distance(boundCorners[i], n.pos);
                PuzzleNode nb = tempData.Nodes.FindAll(GrabEdge1).OrderBy(OrderDist).First();
                PuzzleNode nc = tempData.Nodes.FindAll(GrabEdge2).OrderBy(OrderDist).First();

                tempData.Nodes.Add(new PuzzleNode(boundCorners[i]));
                int a = tempData.Nodes.Count - 1;
                int b = tempData.Nodes.IndexOf(nb);
                int c = tempData.Nodes.IndexOf(nc);
                tempData.Paths.Add(new NodePath(a, b));
                tempData.Paths.Add(new NodePath(a, c));
                startCorner = tempData.Nodes.Last();
            }
        }

        if (startCorner == null)
        {
            Debug.LogError("Failed to generate puzzle max bounds!!!");
            return false;
        }

        //Generate Puzzle bounds
        PuzzleNode currentNode = startCorner;
        Vector2 boundCenter = (m_boundsLow + m_boundsHigh) / 2.0f;
        m_bounds.Clear();
        m_bounds.Add(currentNode);
        do
        {
            float maxDist = 0;
            foreach (PuzzleNode n in tempData.GetConnectedNodes(currentNode))
            {
                if(n == startCorner && m_bounds.Count > 3)
                {
                    currentNode = n;
                    break; //Close loop
                }
                if (m_bounds.Contains(n))
                {
                    continue; //This is a double back
                }
                float d = Vector2.Distance(boundCenter, n.pos);
                if (d > maxDist)
                {
                    maxDist = d;
                    currentNode = n;
                }
            }
            if (currentNode == m_bounds.Last())
            {
                break; //Error
            }
            m_bounds.Add(currentNode);
        } while (currentNode != startCorner || m_bounds.Count < tempData.Nodes.Count);

        //Ensure no bound error
        if (currentNode != startCorner || m_bounds.Count < 4)
        {
            Debug.LogError("Failed to generate puzzle bounds!!!");
            m_bounds.Clear();
            return false;
        }

        //Validate the elements are bounded within the puzzle
        if (ElementData != null)
        {
            foreach (PuzzleElement element in ElementData.ElementList)
            {
                if (!PuzzleUtils.InBorder(element.pos, m_bounds))
                {
                    Debug.LogWarning(string.Format("Element at {0} is outside of puzzle bounds", element.pos));
                    return false;
                }
            }
        }
        return true;
    }

    //Test a lines validity against puzzle elements
    public bool Test(Line playerLine, out List<Line> borders) //Returns borders for silly debug reasons. Feel free to remove
    {
        borders = null;
        if (m_bounds.Count == 0) return false; //Invalid data or uninitialized
        if (playerLine.Count < 2) return false; //we need at least one line
        if (ElementData == null) return true; //no test needed

        if (PuzzleUtils.GetRequiresBorders(ElementData.ElementList))
        {
            //Generate borders for this test
            borders = MakeBorders(playerLine);
        }

        foreach (PuzzleElement e in ElementData.ElementList)
        {
            ElementTest test = PuzzleUtils.GetTest(e);
            if (test == null) return false; //Error

            test.Setup(this, borders);
            if (!test.Test(e, playerLine))
            {
                return false; //Test failed
            }
        }
        return true;
    }

    #endregion //Public Functions End

    #region Private Functions

    private List<Line> MakeBorders(Line testLine)
    {
        List<Line> borders = new List<Line>();

        //Find border edges
        int len = 1;
        for (int i = 1; i < testLine.Count; ++i)
        {
            bool startBound = PuzzleUtils.OnBorder(testLine[i - 1], m_bounds);
            bool endBound = PuzzleUtils.OnBorder(testLine[i], m_bounds);

            //left edge
            if (startBound && !endBound)
            {
                len = 1;
            }
            //hit edge
            if (!startBound && endBound)
            {
                borders.Add(new Line(testLine.GetRange(i - len, len + 1)));
            }
            len++;
        }

        //Inflate edges to full borders
        for (int i = 0; i < borders.Count; ++i)
        {
            int startIndex = borders[i].Count;
            int t = 0;
            do
            {
                if (ConnectToCorner(i, borders, out PuzzleNode newestPoint))
                {
                    borders[i].Add(newestPoint);

                    if (!CheckBorderOverlap(i, borders))
                    {
                        continue;
                    }
                }

                //Error, try again
                ++t;
                borders[i].RemoveRange(startIndex, borders[i].Count - startIndex);
            } while (borders[i].Last() != borders[i].First() && t <= 10000);

            if (borders[i].Last() != borders[i].First())
            {
                //Error in pathing!
                Debug.LogError(string.Format("Border {0} FAILED in {1} tries!", i, t + 1));
            }
            else
            {
                Debug.Log(string.Format("Border {0} created in {1} tries", i, t + 1));
            }
        }
        return borders;
    }

    private bool ConnectToCorner(int Id, List<Line> borders, out PuzzleNode nextNode)
    {
        PuzzleNode cNode = borders[Id].Last();

        //Shuffle the connected nodes for random pathing priority
        List<PuzzleNode> connectedNodes = ShuffleList(NodeData.GetConnectedNodes(cNode));

        bool DoesConnect = connectedNodes.Contains(borders[Id].First());
        //Favor closing this loop
        if (borders[Id].Count > 2 && DoesConnect)
        {
            nextNode = borders[Id].First();
            return true;
        }

        //Check along edges
        foreach (PuzzleNode c in connectedNodes)
        {
            if (borders[Id].Contains(c)) continue; //We have been here

            //Walk along a border
            foreach (Line border in borders)
            {
                if (border == borders[Id]) continue; //Skip self

                GetAdjacentNode(cNode, border, out PuzzleNode nA, out PuzzleNode nB);
                if ((c == nA || c == nB))
                {
                    nextNode = c; //Walk along other border
                    return true;
                }
            }

            //No borders to walk, only walk along bounds
            if (PuzzleUtils.OnBorder(c, m_bounds) && PuzzleUtils.OnBorder(cNode, m_bounds))
            {
                nextNode = c;
                return true;
            }
        }

        //Failure
        nextNode = null;
        return false;
    }
    #endregion //Private Functions End

    #region Private Utils
    private bool CheckBorderOverlap(int ID, List<Line> borders)
    {
        for (int j = ID - 1; j >= 0; --j)
        {
            if (borders.Count == borders[j].Count && borders[ID].All(borders[j].Contains))
            {
                return true; //Double border overlap
            }
        }
        return false;
    }

    private void GetAdjacentNode(PuzzleNode n, Line b, out PuzzleNode out1, out PuzzleNode out2)
    {
        int i = b.IndexOf(n);
        if(i<=0)
        {
            out1 = null;
            out2 = null;
            return;
        }
        out1 = i - 1 < 0 ? b.Last() : b[i - 1];
        out2 = i + 1 >= b.Count ? b.First() : b[i + 1];
    }

    private List<T> ShuffleList<T>(List<T> list)
    {
        List<T> ret = new List<T>(list);
        int n = ret.Count;
        while (n > 1)
        {
            int k = Random.Range(0,n);
            n--;
            T value = ret[k];
            ret[k] = ret[n];
            ret[n] = value;
        }
        return ret;
    }
    #endregion
}