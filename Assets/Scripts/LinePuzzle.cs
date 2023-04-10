using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/*
    Component for controlling the line puzzles
    Turning on the debug flag will show the bounds of the puzzle as a transparent line.
    This will also show the borders (when/if generated)
*/

[RequireComponent(typeof(LinePuzzleRenderer))]
public class LinePuzzle : MonoBehaviour
{
    [SerializeField]
    private bool m_debug = false;
    [SerializeField]
    private float m_inputSpeed = 0.03f;
    [SerializeField]
    private float m_cornerTolerance = 0.3f;
    [SerializeField]
    private PuzzleData m_puzzleData = null;

    private LinePuzzleRenderer m_renderer = null;
    private List<PuzzleNode> m_playerLine = new List<PuzzleNode>();
    private bool m_focused = false;
    private bool m_completed = false;
    private Vector2 m_cursorPos = Vector2.zero;
    private bool m_valid = true;
    private PlayerController m_controllerRef = null;

    private void Awake()
    {
        m_valid = true;
        if (m_puzzleData == null)
        {
            m_valid = false;
            Debug.LogError("No puzzle data found on object " + transform.name);
            Destroy(gameObject);
            return;
        }

        if (!m_puzzleData.Init())
        {
            m_valid = false;
            Debug.LogError("No valid puzzle data found on object " + transform.name);
            Destroy(gameObject);
            return;
        }

        m_renderer = GetComponent<LinePuzzleRenderer>();
        if(m_renderer == null)
        {
            m_valid = false;
            Debug.LogError("No puzzle renderer found on object " + transform.name);
            Destroy(gameObject);
            return;
        }

        m_renderer.Init(m_puzzleData);

        if (m_debug)
        {
            m_renderer.DrawDebugBorder(m_puzzleData.GetBounds(), Random.ColorHSV());
        }
    }

    public void Update()
    {
        if (!m_focused) return;

        Vector2 input = new Vector2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y")) * m_inputSpeed;
        if (input.magnitude == 0.0f) return;

        PuzzleNode currentNode = m_playerLine[m_playerLine.Count - 1];
        List<PuzzleNode> connectedNodes = m_puzzleData.NodeData.GetConnectedNodes(currentNode);

        float len = Vector2.Distance(m_cursorPos + input, currentNode.pos);

        if (len <= 0.0f) return;

        Vector2 checkVal = m_cursorPos - currentNode.pos;
        if(len <= m_cornerTolerance)
        {
            checkVal = input;
        }

        System.Func<PuzzleNode, float> OrderAngle = x => Vector2.Angle(checkVal, x.pos - currentNode.pos);
        PuzzleNode targetNode = connectedNodes.OrderBy(OrderAngle).First();

        Vector2 targetDir = (targetNode.pos - currentNode.pos);


        bool reversing = false;
        if (m_playerLine.Count > 1)
        {
            Vector2 a = currentNode.pos;
            Vector2 b = m_playerLine[m_playerLine.Count - 2].pos;
            reversing = (b - a).normalized == targetDir.normalized;
        }

        if(m_puzzleData.NodeData.GetConnectedNodes(currentNode).Count <= 1)
        {
            reversing = true; //Edge case for dead ends
        }

        if(reversing)
        {
            if(len < m_cornerTolerance)
            {
                m_cursorPos = currentNode.pos;
                m_playerLine.RemoveAt(m_playerLine.Count - 1);
                m_renderer.UpdatePlayerLine(m_playerLine);
                m_renderer.MoveCursor(m_playerLine.Last().pos, m_cursorPos);
            }
            return;
        }

        m_cursorPos = currentNode.pos + Vector2.ClampMagnitude(targetDir, len);
        m_renderer.MoveCursor(currentNode.pos, m_cursorPos);
        m_renderer.ShowDynamicLine(true);

        if (m_cursorPos == targetNode.pos)
        {
            if (m_playerLine.Contains(targetNode)) return; //Disallow overlapping
            HitCorner(targetNode);
        }
    }

    public void Focus(PlayerController controller)
    {
        if (!m_valid || m_focused) return;
        m_controllerRef = controller;
        m_controllerRef.SetInputEnabled(false);
        ResetPuzzle();
        m_focused = true;
    }

    public void UnFocuse()
    {
        if (!m_focused) return;
        if (m_controllerRef!=null) m_controllerRef.SetInputEnabled(true);
        m_controllerRef = null;
        m_focused = false;
    }

    public void HitCorner(PuzzleNode node)
    {
        if (!m_valid) return;
        m_playerLine.Add(node);
        if (node.type == NodeType.End) //Hit end node
        {

            if (m_puzzleData.Test(m_playerLine, out List<List<PuzzleNode>> borders))
            {
                if(m_debug) DrawDebugBorders(borders);
                CompletePuzzle();
            }
            else
            {
                if (m_debug) DrawDebugBorders(borders);
                ResetPuzzle();
            }
        }
        m_renderer.UpdatePlayerLine(m_playerLine);
    }

    public void CompletePuzzle()
    {
        Debug.Log("Puzzle Completed");
        m_completed = true;
        m_renderer.ShowDynamicLine(false);
        UnFocuse();
    }

    public void ResetPuzzle()
    {
        m_cursorPos = m_puzzleData.NodeData.GetStartNode().pos;
        m_completed = false;
        m_playerLine.Clear();
        m_playerLine.Add(m_puzzleData.NodeData.GetStartNode());
        m_renderer.UpdatePlayerLine(m_playerLine);
        m_renderer.ShowDynamicLine(false);
        UnFocuse();
    }

    public bool IsCompleted()
    {
        return m_completed;
    }

    //Debug Functions
    public void DrawDebugBorders(List<List<PuzzleNode>> borders)
    {
        m_renderer.ClearDebugBorders();
        if (borders == null) return;
        for (int i = 0; i < borders.Count; ++i)
        {
            m_renderer.DrawDebugBorder(borders[i], Random.ColorHSV());
        }
        Debug.Log(string.Format("Drawing {0} debug borders", borders.Count));
    }
}