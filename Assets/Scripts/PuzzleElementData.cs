using System.Collections.Generic;
using UnityEngine;

/*
    Classes for Puzzle Element data (puzzle rules)
    Split into seperate data so node data(line layouts) can be reused
*/

public enum PuzzleElementType
{
    HEXAGON = 0,
    WHITE_SQUARE = 1,
    BLACK_SQUARE = 2,
    STAR = 3,
}

[System.Serializable]
public class PuzzleElement
{
    public PuzzleElementType type = PuzzleElementType.HEXAGON;
    public Vector2 pos = Vector2.zero;
}

[System.Serializable]
public class PuzzleElementData
{
    public List<PuzzleElement> ElementList = new List<PuzzleElement>();
}