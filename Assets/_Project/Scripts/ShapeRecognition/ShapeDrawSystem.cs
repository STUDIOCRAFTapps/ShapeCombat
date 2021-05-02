using System.Collections.Generic;
using UnityEngine;
using System.Text;
using TMPro;

public class ShapeDrawSystem : MonoBehaviour {

    [Header("References")]
    public LineRenderer lineRenderer;
    new public Camera camera;
    public TextMeshProUGUI debugText;
    public ShapeTemplate[] shapeTemplates;

    [Header("Parameters")]
    [Tooltip("The minimun dist. bettween the lastest point on the line before creating a new segment.")]
    public float minMovingDistance;
    [Tooltip("The minimum delta of line angle that can be considered as a corner.")]
    public float minCornerAngleDelta;
    [Tooltip("The max delta of line angle that can be considered as a line.")]
    public float maxLineAngleDelta;
    [Tooltip("If the corner is formed by many close points, the analyser will check back x segments earlier to see if there was really a corner.")]
    public int maxCornerCheckCount;
    [Tooltip("If the corner is formed by many close points, the analyser will check if the last few corners points are actually close togheter.")]
    public float minCornerMergeDistance;

    private List<Vector2> points;



    void Start () {
        points = new List<Vector2>();
        InitAnalyser();
    }

    void Update () {
        Vector2 point = camera.ScreenToWorldPoint(Input.mousePosition);
        if(Input.GetMouseButtonDown(0)) {
            points.Clear();
            lineRenderer.positionCount = 1;
            lineRenderer.SetPosition(0, point);
            points.Add(point);
            lastPoint = point;
        } else if(Input.GetMouseButton(0)) {
            if(Vector2.Distance(point, lastPoint) > minMovingDistance) {
                lineRenderer.positionCount++;
                lineRenderer.SetPosition(lineRenderer.positionCount - 1, point);
                points.Add(point);
                lastPoint = point;
            }
        }

        if(Input.GetMouseButtonUp(0)) {
            AnalyseDrawing();
        }
    }


    #region Analysis
    private List<float> angles;
    private List<float> angleDeltas;
    private Vector2 lastPoint;
    private float averageContinuousDeltaSum = 0f;
    private int averageContinuousDeltaCount = 0;
    private Vector2 lastSegmentStart;
    private ShapeDrawing shape;


    void InitAnalyser () {
        angles = new List<float>();
        angleDeltas = new List<float>();

        for(int i = 0; i < shapeTemplates.Length; i++) {
            shapeTemplates[i].PrepareTemplate();
        }
    }


    void AnalyseDrawing () {
        
        // Quick analysis preparation
        shape = new ShapeDrawing();
        angles.Clear();
        angleDeltas.Clear();


        // Prepare a list of segment angle and segment angle deltas
        float previousAngle = 0f;
        for(int i = 0; i < points.Count - 1; i++) {
            float angle = Mathf.Atan2(points[i].y - points[i + 1].y, points[i].x - points[i + 1].x) / (Mathf.PI * 2f);
            angles.Add(angle);
            if(i != 0) {
                angleDeltas.Add(LoopValueDelta(previousAngle, angle));
            }
            previousAngle = angle;
        }
        if(angleDeltas.Count < 1)
            return;


        // Figure out bounds and other comparaison values
        shape.startPos = points[0];
        shape.endPos = points[points.Count - 1];
        for(int i = 0; i < points.Count; i++) {
            shape.maxBound = new Vector2(Mathf.Max(shape.maxBound.x, points[i].x), Mathf.Max(shape.maxBound.y, points[i].y));
            shape.minBound = new Vector2(Mathf.Min(shape.minBound.x, points[i].x), Mathf.Min(shape.minBound.y, points[i].y));
        }


        float totalAngleDelta = 0f;
        lastSegmentStart = points[0];
        Vector2 lastCornerPos = Vector2.one * 256f;
        for(int i = 0; i < angleDeltas.Count; i++) {
            bool wasCornerFound = false;
            if(Mathf.Abs(angleDeltas[i] * 360f) > minCornerAngleDelta) {
                Vector2 cornerPoint = points[i + 1];
                if(Vector2.Distance(cornerPoint, lastCornerPos) > minCornerMergeDistance) {
                    lastCornerPos = cornerPoint;

                    // Corner Found
                    wasCornerFound = true;
                    float cornerValue = angleDeltas[i];
                    shape.points.Add(new PointElement(lastCornerPos, cornerValue * 360f));
                    //Debug.Log($"Corner at {lastCornerPos} of {cornerValue * 360f}");

                    // Sum up line
                    EndLine(shape, lastCornerPos);
                }
            } else {
                float finalAngle = angles[i + 1];
                for(int l = 1; l < maxCornerCheckCount; l++) {
                    if(i - l >= 0) {
                        float checkAngle = angles[i - l];
                        float angleDelta = LoopValueDelta(checkAngle, finalAngle);
                        if((angleDelta * 360f) > minCornerAngleDelta) {
                            Vector2 cornerPoint = points[(i - Mathf.RoundToInt(l * 0.5f)) + 1];
                            if(Vector2.Distance(cornerPoint, lastCornerPos) > minCornerMergeDistance && Vector2.Distance(cornerPoint, points[i + 1]) < minCornerMergeDistance) {
                                lastCornerPos = cornerPoint;
                                
                                // Corner Found
                                wasCornerFound = true;
                                float cornerValue = angleDelta;
                                shape.points.Add(new PointElement(lastCornerPos, cornerValue * 360f));
                                //Debug.Log($"Corner at {lastCornerPos} of {cornerValue * 360f}");

                                // Sum up line
                                EndLine(shape, lastCornerPos);
                            }
                            break;
                        }
                    } else {
                        break;
                    }
                }
            }
            
            if(!wasCornerFound && Mathf.Abs(angleDeltas[i] * 360f) < maxLineAngleDelta) {
                averageContinuousDeltaCount++;
                averageContinuousDeltaSum += angleDeltas[i];
            }
            totalAngleDelta += angleDeltas[i];
        }
        EndLine(shape, points[points.Count - 1]);
        shape.totalAngleDelta = totalAngleDelta;

        // Comparaison
        shape.Normalize();
        int topScoreIndex = 0;
        float topScore = Mathf.NegativeInfinity;

        StringBuilder sb = new StringBuilder();
        sb.Append("Scores:\n");

        for(int i = 0; i < shapeTemplates.Length; i++) {
            float score = shapeTemplates[i].CalculateScore(shape);
            sb.Append(shapeTemplates[i].name);
            sb.Append(" : ");
            sb.Append(score);
            sb.Append("\n");
            if(score > topScore) {
                topScore = score;
                topScoreIndex = i;
            }
        }
        debugText.SetText(sb);

        Debug.Log("This shape ressembles a... " + shapeTemplates[topScoreIndex].name);
    }


    void EndLine (ShapeDrawing shape, Vector2 endPoint) {
        float curviness = 0f;
        if(averageContinuousDeltaCount > 0) {
            float averageContinuousDelta = averageContinuousDeltaSum / averageContinuousDeltaCount;
            averageContinuousDeltaCount = 0;
            averageContinuousDeltaSum = 0f;
            curviness = averageContinuousDelta * 360f;
        }
        shape.lines.Add(new LineElement(
            lastSegmentStart,
            endPoint,
            curviness
        ));
        lastSegmentStart = endPoint;
    }


    public static float LoopValueDelta (float valueA, float valueB) {
        float rA = Mathf.Repeat(valueA, 1f);
        float rB = Mathf.Repeat(valueB, 1f);

        if(Mathf.Abs(rB - 1 - rA) < Mathf.Abs(rB - rA)) {
            return rA - (rB - 1);
        } else if(Mathf.Abs(rA - 1 - rB) < Mathf.Abs(rB - rA)) {
            return (rA - 1) - rB;
        } else {
            return rA - rB;
        }
    }
    #endregion
}

[System.Serializable]
public class ShapeDrawing {
    public List<LineElement> lines;
    public List<PointElement> points;

    public ShapeDrawing () {
        lines = new List<LineElement>();
        points = new List<PointElement>();
    }

    public Vector2 minBound = new Vector2(Mathf.Infinity, Mathf.Infinity);
    public Vector2 maxBound = new Vector2(Mathf.NegativeInfinity, Mathf.NegativeInfinity);
    private Vector2 scalingVector;

    public Vector2 startPos;
    public Vector2 endPos;
    public float totalAngleDelta;

    public Vector2 StartEndDelta {
        get {
            return endPos - startPos;
        }
    }

    public void Normalize () {
        Vector2 boundDelta = maxBound - minBound;
        scalingVector = Vector2.one;
        if(boundDelta.x > boundDelta.y) {
            scalingVector.x = 1f / boundDelta.x;
            scalingVector.y = 1f / boundDelta.x;
        } else {
            scalingVector.x = 1f / boundDelta.y;
            scalingVector.y = 1f / boundDelta.y;
        }

        for(int i = 0; i < lines.Count; i++) {
            lines[i].startPos = TransformPoint(lines[i].startPos);
            lines[i].endPos = TransformPoint(lines[i].endPos);
        }
        for(int i = 0; i < points.Count; i++) {
            points[i].pos = TransformPoint(points[i].pos);
        }
        startPos = TransformPoint(startPos);
        endPos = TransformPoint(endPos);

        maxBound -= minBound;
        maxBound *= scalingVector;
        minBound = Vector2.zero;
    }

    private Vector2 TransformPoint (Vector2 p) {
        return (p - minBound) * scalingVector;
    }
}

[System.Serializable]
public class ShapeTemplate {
    public string name;
    public Vector2 extendBounds;

    public List<LineElement> lines;
    private List<PointElement> points;

    private Vector2 maxBound;
    private Vector2 startPos;
    private Vector2 endPos;
    private float totalAngleDelta;

    public void PrepareTemplate () {
        points = new List<PointElement>();
        Vector2 maxBound = new Vector2(Mathf.NegativeInfinity, Mathf.NegativeInfinity);

        for(int i = 0; i < lines.Count; i++) {
            maxBound = new Vector2(Mathf.Max(maxBound.x, lines[i].startPos.x), Mathf.Max(maxBound.y, lines[i].startPos.y));
            maxBound = new Vector2(Mathf.Max(maxBound.x, lines[i].endPos.x), Mathf.Max(maxBound.y, lines[i].endPos.y));
        }

        // Min bound should always be 0, meaning the bound delta is always the maxBound
        this.maxBound = maxBound + extendBounds;

        // Start/End positions
        startPos = lines[0].startPos;
        endPos = lines[lines.Count - 1].endPos;

        // Calculating total angle delta
        totalAngleDelta = 0f;
        if(lines.Count > 1) {
            float previousAngle = Mathf.Atan2(lines[0].startPos.y - lines[0].endPos.y, lines[0].startPos.x - lines[0].endPos.x) / (Mathf.PI * 2f);
            for(int i = 1; i < lines.Count; i++) {
                float angle = Mathf.Atan2(lines[i].startPos.y - lines[i].endPos.y, lines[i].startPos.x - lines[i].endPos.x) / (Mathf.PI * 2f);
                float angleDelta = ShapeDrawSystem.LoopValueDelta(previousAngle, angle);
                points.Add(new PointElement(lines[i].endPos, angleDelta * 360f));
                totalAngleDelta += angleDelta;
                previousAngle = angle;
            }
        }
    }

    public float CalculateScore (ShapeDrawing s) {
        float total = 0f;

        total += Mathf.Clamp01(1f - Mathf.Abs(totalAngleDelta - s.totalAngleDelta)) * 3f;   // Total Angle Delta
        total += Mathf.Clamp01(1f - Mathf.Abs(maxBound.x - s.maxBound.x));                  // X Bound
        total += Mathf.Clamp01(1f - Mathf.Abs(maxBound.y - s.maxBound.y));                  // Y Bound
        total += Mathf.Clamp01(1f - Vector2.Distance(startPos, s.startPos));                // Start Distance
        total += Mathf.Clamp01(1f - Vector2.Distance(endPos, s.endPos));                    // End Distance

        if(lines.Count == s.lines.Count)
            total += 2.5f;                                                                  // Same feature count?

        if(points.Count > 0 && s.points.Count > 0) {
            for(int i = 0; i < points.Count && i < s.points.Count; i++) {
                total += Mathf.Clamp01(1f - Vector2.Distance(points[i].pos, s.points[i].pos)) * 3f;             // Points pos accuracy
                total += Mathf.Clamp01(1f - (Mathf.Abs(points[i].degree - s.points[i].degree) / 360f)) * 1.5f;  // Points deg accuracy
            }
        }

        if(lines.Count > 0 && s.lines.Count > 0) {
            for(int i = 0; i < lines.Count && i < s.lines.Count; i++) {
                total += Mathf.Clamp01(1f - Vector2.Distance(lines[i].startPos, s.lines[i].startPos)) * 2f;         // Line start accuracy
                total += Mathf.Clamp01(1f - Vector2.Distance(lines[i].endPos, s.lines[i].endPos)) * 2f;         // Line end accuracy
                total += Mathf.Clamp01(1f - (Mathf.Abs(lines[i].curviness - s.lines[i].curviness) / 5f)) * 1f;    // Line curv accuracy
            }
        }

        return total;
    }
}

[System.Serializable]
public class LineElement {
    public float curviness;
    public Vector2 startPos;
    public Vector2 endPos;

    public LineElement (Vector2 startPos, Vector2 endPos, float curviness) {
        this.startPos = startPos;
        this.endPos = endPos;
        this.curviness = curviness;
    }
}

[System.Serializable]
public class PointElement {
    public Vector2 pos;
    public float degree;

    public PointElement (Vector2 pos, float degree) {
        this.pos = pos;
        this.degree = degree;
    }
}