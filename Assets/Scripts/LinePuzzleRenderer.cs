using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using NodePair = System.Tuple<PuzzleNode, PuzzleNode>;

/*
    Component for rendering the line puzzles
*/

public class LinePuzzleRenderer : MonoBehaviour
{
    [SerializeField]
    private Vector2 m_borderPadding = Vector2.zero;
    [SerializeField]
    private Sprite[] m_elementSprites = null;
    [SerializeField]
    private Color m_bgColor = Color.white;
    [SerializeField]
    private Color m_bgLineColor = Color.white;
    [SerializeField]
    private Color m_fgLineColor = Color.white;
    [SerializeField]
    private GameObject m_linePrefab = null;
    [SerializeField]
    private GameObject m_circlePrefab = null;
    [SerializeField]
    private GameObject m_elementPrefab = null;
    [SerializeField]
    private Image m_bgImage = null;
    [SerializeField]
    private Transform m_elementParent = null;
    [SerializeField]
    private Transform m_lineForeground = null;
    [SerializeField]
    private Transform m_lineBackground = null;

    private Vector2 m_boundsLow = Vector2.zero;
    private Vector2 m_boundsHigh = Vector2.one;
    private RectTransform m_dynamicLine = null;
    private RectTransform m_startCircle = null;
    private List<RectTransform> m_fgLines = new List<RectTransform>();
    private List<RectTransform> m_debugLines = new List<RectTransform>();

    public void Init(PuzzleData data)
    {
        m_bgImage.color = m_bgColor;
        m_boundsLow = data.GetBoundsLow();
        m_boundsHigh = data.GetBoundsHigh();

        //Draw background lines
        foreach (NodePair line in data.GetNodeConnections())
        {
            DrawLine(line.Item1.pos, line.Item2.pos, m_lineBackground, m_bgLineColor);
        }

        //Draw start circle BG
        RectTransform circle = Instantiate(m_circlePrefab, m_lineBackground).GetComponent<RectTransform>();
        circle.anchoredPosition = RemapVec(data.NodeData.GetStartNode().pos);
        circle.GetComponent<Image>().color = m_bgLineColor;

        //Init start circle FG
        m_startCircle = Instantiate(m_circlePrefab, m_lineForeground).GetComponent<RectTransform>();
        m_startCircle.anchoredPosition = RemapVec(data.NodeData.GetStartNode().pos);
        m_startCircle.GetComponent<Image>().color = m_fgLineColor;

        //Init dynamic line
        m_dynamicLine = Instantiate(m_linePrefab, m_lineForeground).GetComponent<RectTransform>();
        m_dynamicLine.GetComponent<Image>().color = m_fgLineColor;
        ShowDynamicLine(false);

        //Draw Elements
        if (data.ElementData == null) return;
        foreach (PuzzleElement e in data.ElementData.ElementList)
        {
            RectTransform g = Instantiate(m_elementPrefab, m_elementParent).GetComponent<RectTransform>();
            g.anchoredPosition = RemapVec(e.pos);
            g.GetComponent<Image>().sprite = m_elementSprites[(int)e.type];
        }
    }

    public void ShowDynamicLine(bool v)
    {
        if (m_dynamicLine.gameObject.activeSelf != v)
        {
            m_dynamicLine.gameObject.SetActive(v);
        }
        if (m_startCircle.gameObject.activeSelf != v)
        {
            m_startCircle.gameObject.SetActive(v);
        }
    }

    public void MoveCursor(Vector2 corner, Vector2 to)
    {
        SetLine(corner, to, m_dynamicLine);
    }

    public void UpdatePlayerLine(List<PuzzleNode> nodeList)
    {
        //Clear line
        foreach (RectTransform t in m_fgLines)
        {
            Destroy(t.gameObject);
        }
        m_fgLines.Clear();

        //Draw line
        for (int i = 0; i < nodeList.Count - 1; ++i)
        {
            m_fgLines.Add(DrawLine(nodeList[i].pos, nodeList[i + 1].pos, m_lineForeground, m_fgLineColor));
        }
    }

    private void SetLine(Vector2 pointA, Vector2 pointB, RectTransform t)
    {
        pointA = RemapVec(pointA);
        pointB = RemapVec(pointB);
        //Set position
        t.anchoredPosition = new Vector2((pointA.x + pointB.x) / 2.0f, (pointA.y + pointB.y) / 2.0f); //Midpoint on line

        //Set rotation
        Vector2 dir = pointA - pointB;
        dir.Normalize();

        Vector3 eular = new Vector3(0, 0, 0);
        eular.z = Vector2.SignedAngle(Vector2.up, dir);
        t.eulerAngles = eular;

        //Set scale
        Vector3 scale = Vector3.one;
        scale.y = Vector2.Distance(pointA, pointB);
        t.localScale = scale;
    }

    private RectTransform DrawLine(Vector2 pointA, Vector2 pointB, Transform parent, Color color)
    {
        RectTransform g = Instantiate(m_linePrefab, parent).GetComponent<RectTransform>();
        SetLine(pointA, pointB, g);
        g.GetComponent<Image>().color = color;
        return g;
    }

    private Vector2 RemapVec(Vector2 value)
    {
        Vector2 worldBoundsLow = -Vector2.one * 2.0f + m_borderPadding;
        Vector2 worldBoundsHigh = Vector2.one * 2.0f - m_borderPadding;
        return worldBoundsLow + (value - m_boundsLow) * (worldBoundsHigh - worldBoundsLow) / (m_boundsHigh - m_boundsLow);
    }

    //Debug Functions
    public void ClearDebugBorders()
    {
        foreach (RectTransform t in m_debugLines)
        {
            Destroy(t.gameObject);
        }
        m_debugLines.Clear();
    }

    public void DrawDebugBorder(List<PuzzleNode> border, Color color)
    {
        color.a = 0.75f;
        for (int i = 0; i < border.Count-1; ++i)
        {
            m_debugLines.Add(DrawLine(border[i].pos, border[i + 1].pos, m_elementParent, color));
        }
    }
}