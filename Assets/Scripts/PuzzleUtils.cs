using System.Collections.Generic;
using UnityEngine;
using Line = System.Collections.Generic.List<PuzzleNode>;

/*
    Contains tests for each element type
    Element tests can be added below and registered in s_testList dictionary
*/

public static class PuzzleUtils
{

    private static readonly Dictionary<int, ElementTest> s_testList = new Dictionary<int, ElementTest>
    {
        {AsFlag(PuzzleElementType.HEXAGON), new HexElementTest()},
        {AsFlag(PuzzleElementType.WHITE_SQUARE)|AsFlag(PuzzleElementType.BLACK_SQUARE), new SquareElementTest()},
        {AsFlag(PuzzleElementType.STAR), new StarElementTest()},
    };

    public static bool GetRequiresBorders(List<PuzzleElement> list)
    {
        foreach (PuzzleElement e in list)
        {
            if (GetTest(e).RequiresBorders())
            {
                return true;
            }
        }
        return false;
    }
    
    public static ElementTest GetTest(PuzzleElement e)
    {
        foreach (var t in s_testList)
        {
            if ((t.Key | AsFlag(e.type)) == t.Key)
            {
                return t.Value;
            }
        }
        Debug.LogError("No test found for element type: " + e.type.ToString());
        return null;
    }

    public static PuzzleElement[] FilterElementList(int filter, PuzzleElementData elementData)
    {
        if (elementData == null) return new PuzzleElement[0];//Error
        if (elementData.ElementList == null) return new PuzzleElement[0];//Error
        return elementData.ElementList.FindAll(e => (filter | AsFlag(e.type)) == filter).ToArray();
    }

    public static bool InBorder(Vector2 p, Line b)
    {
        bool result = false;
        int j = b.Count - 1;
        for (int i = 0; i < b.Count; i++)
        {
            if (b[i].pos.y < p.y && b[j].pos.y >= p.y || b[j].pos.y < p.y && b[i].pos.y >= p.y)
            {
                if (b[i].pos.x + (p.y - b[i].pos.y) / (b[j].pos.y - b[i].pos.y) * (b[j].pos.x - b[i].pos.x) < p.x)
                {
                    result = !result;
                }
            }
            j = i;
        }
        return result;
    }

    public static bool OnBorder(PuzzleNode n, Line b)
    {
        return b.Contains(n);
    }

    public static Line GetSurrondingBorder(Vector2 p, List<Line> borders)
    {
        //Find the border this point lies within (borders must not overlap)
        foreach (Line b in borders)
        {
            if (InBorder(p, b))
            {
                return b;
            }
        }
        return null; //this is inside no borders
    }
    public static int AsFlag(PuzzleElementType t)
    {
        return 1 << (int)t;
    }
}

public abstract class ElementTest
{
    protected PuzzleData data = null;
    protected List<Line> borders = null;
    public void Setup(PuzzleData dataRef, List<Line> borderRef)
    {
        data = dataRef;
        borders = borderRef;
    }
    public abstract bool RequiresBorders(); //True if borders need to be created
    public abstract bool Test(PuzzleElement e, Line testLine);
}

public class HexElementTest : ElementTest
{
    /*
        Hex Elements have one simple rule: the line must pass through them
        No borders are required as we only need line info
        Remeber to ensure the hex is along one of the node connections
    */
    public override bool RequiresBorders()
    {
        return false;
    }
    public override bool Test(PuzzleElement e, Line testLine)
    {
        for (int i = 1; i < testLine.Count; ++i)
        {
            if (Vector2.Distance(testLine[i - 1].pos, e.pos) + Vector2.Distance(testLine[i].pos, e.pos) == Vector2.Distance(testLine[i - 1].pos, testLine[i].pos))
            {
                //This hex is on one of the test lines
                return true;
            }
        }
        return false;
    }
}

public class SquareElementTest : ElementTest
{
    /*
        Square Elements must be divided Color
        Currently there is only white and black, but adding more would be simple
        This test ensures each square doesnt share a border with any of the opposite color
    */
    public override bool RequiresBorders()
    {
        return true;
    }
    public override bool Test(PuzzleElement e, Line testLine)
    {
        if (data == null) return false; //Error
        if (borders == null || borders.Count == 0) return false; //Error
        Line thisPoly = PuzzleUtils.GetSurrondingBorder(e.pos, borders);

        PuzzleElementType targetFilter = e.type == PuzzleElementType.BLACK_SQUARE ? PuzzleElementType.WHITE_SQUARE : PuzzleElementType.BLACK_SQUARE;
        PuzzleElement[] otherSquares = PuzzleUtils.FilterElementList(PuzzleUtils.AsFlag(targetFilter), data.ElementData);
        foreach (PuzzleElement other in otherSquares)
        {
            if (PuzzleUtils.GetSurrondingBorder(other.pos, borders) == thisPoly)
            {
                return false;
            }
        }
        return true;
    }
}

public class StarElementTest : ElementTest
{
    /*
        Star Elements must be paired with each other.
        In the witness these sometimes interact with Squares and vice versa.
        It wouldn't be difficult to also check similar colored squares here to achieve that.
        This test ensures that exactly 1 star shares a border with this star.
    */
    public override bool RequiresBorders()
    {
        return true;
    }
    public override bool Test(PuzzleElement e, Line testLine)
    {
        if (data == null) return false; //Error
        if (borders == null || borders.Count == 0) return false; //Error
        int connectedStars = 0;
        Line thisPoly = PuzzleUtils.GetSurrondingBorder(e.pos, borders);
        PuzzleElement[] otherStars = PuzzleUtils.FilterElementList(PuzzleUtils.AsFlag(PuzzleElementType.STAR), data.ElementData);
        foreach (PuzzleElement other in otherStars)
        {
            if (other == e) continue;
            if (PuzzleUtils.GetSurrondingBorder(other.pos, borders) == thisPoly)
            {
                connectedStars++;
            }
        }
        return connectedStars == 1;
    }
}