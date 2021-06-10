using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Unity.Mathematics;
using UnityEngine.UI;
using System.Text;
using System.Linq;
using TMPro;
using System;

public enum Symbols {
    HLine,
    VLine,
    TopHat,
    LeftHat,
    RightHat,
    BottomHat,
    Circle,
    Heart,
    Spiral,
    Delta,
    Lightning,
    None
}

public class ShapeDrawSystem4 : MonoBehaviour {
    [Header("References")]
    public LineRenderer lineRenderer;
    public LineRenderer lineRenderer2;
    public LineRenderer outlineRenderer;
    public ParticleSystem starsParticles;
    public TextMeshProUGUI resultText;
    new public Camera camera;
    public SymbolParameter[] symbolParameters;

    [Header("Draw Parameters")]
    public float minMovingDistance = 0.1f;
    public float waveScale = 0.1f;
    public float waveSpeed = 1f;
    public float waveAmplitude = 1f;
    public float waveFadeSpeed = 1f;
    public float colorFadeSpeed = 4f;
    public float fadeOutSpeed = 4f;
    public float preFadeOutTime = 2f;

    [Header("Shape Parameters")]
    public float minLineAngleDelta = 20f;
    public float maxLineAngleDelta = 30f;
    public float minLineDistance = 10f;
    public float maxLineDistance = 25f;
    public float minLineRatio = 0.3f;
    public float minCircleStartEndDistance;
    public float maxCircleStartEndDistance;
    public float minHearthStartEndDistance;
    public float maxHearthStartEndDistance;
    public float minCircleRevolution = 0.9f;
    public float maxCircleRevolution = 1f;
    public float minCircleRadius = 1f;
    public float maxCircleRadius = 8f;

    [HideInInspector] public bool isDrawing { private set; get; } = false;
    private List<Vector2> points;
    private Vector2 lastPoint;
    private Dictionary<Symbols, SymbolParameter> symbolParametersDict;

    private List<float> waveFadeValues;
    private SymbolParameter detectedShape;
    private float fadeOutValue;

    public delegate void ExecuteSymbolHandler (Symbols symbol);
    public event ExecuteSymbolHandler executeSymbol;


    #region Setup
    public static ShapeDrawSystem4 inst;
    void Start () {
        inst = this;

        fromColor = Color.white;
        lineRenderer.startColor = Color.white;
        lineRenderer.endColor = Color.white;
        points = new List<Vector2>();
        waveFadeValues = new List<float>();
        symbolParametersDict = new Dictionary<Symbols, SymbolParameter>();
        for(int i = 0; i < symbolParameters.Length; i++) {
            symbolParametersDict.Add(symbolParameters[i].symbol, symbolParameters[i]);
        }
        InitAnalysis();
    }

    void Update () {
        Vector2 point = camera.ScreenToWorldPoint(Input.mousePosition);
        if(Input.GetMouseButtonDown(0)) {
            points.Clear();
            waveFadeValues.Clear();
            lineRenderer.positionCount = 1;
            lineRenderer2.positionCount = 1;
            lineRenderer.SetPosition(0, point);
            lineRenderer2.SetPosition(0, point);
            waveFadeValues.Add(0f);
            points.Add(point);
            lastPoint = point;
        } else if(Input.GetMouseButton(0)) {
            if(Vector2.Distance(point, lastPoint) > minMovingDistance) {
                lineRenderer.positionCount++;
                lineRenderer2.positionCount++;
                lineRenderer.SetPosition(lineRenderer.positionCount - 1, point);
                lineRenderer2.SetPosition(lineRenderer2.positionCount - 1, point);
                points.Add(point);
                waveFadeValues.Add(0f);
                lastPoint = point;
                OnAddPoint();
            }
        }
        isDrawing = Input.GetMouseButton(0) || Input.GetMouseButtonDown(0);

        if(Input.GetMouseButtonUp(0)) {
            fadeOutValue = 0f;
            OnEndLine();
        }

        UpdateVisuals();
    }
    #endregion



    #region Visuals

    void UpdateVisuals () {
        float alpha = 1f;
        if(points.Count >= 2 && !isDrawing) {
            fadeOutValue = Mathf.Max(0f, fadeOutValue + Time.deltaTime * fadeOutSpeed);
            alpha = 1f - Mathf.Clamp01(fadeOutValue - preFadeOutTime);
        }

        colorLerpValue = Mathf.Clamp01(colorLerpValue + colorFadeSpeed * Time.deltaTime);
        Color color = Color.Lerp(fromColor, toColor, colorLerpValue);
        Color color2 = Color.black;
        color.a = alpha;
        color2.a = alpha;
        lineRenderer.startColor = color;
        lineRenderer2.startColor = color2;
        lineRenderer.endColor = color;
        lineRenderer2.endColor = color2;

        for(int i = 0; i < waveFadeValues.Count; i++) {
            waveFadeValues[i] = Mathf.Clamp01(waveFadeValues[i] + waveFadeSpeed * Time.deltaTime);
        }
        for(int i = 0; i < points.Count; i++) {
            float waveX = Mathf.PerlinNoise(points[i].x * waveScale + Time.time * waveSpeed, points[i].y * waveScale + Time.time * waveSpeed) * 2f - 1f;
            float waveY = Mathf.PerlinNoise(points[i].x * waveScale + Time.time * waveSpeed + 1000f, points[i].y * waveScale + Time.time * waveSpeed) * 2f - 1f;
            lineRenderer.SetPosition(i, Vector2.Lerp(points[i], points[i] + new Vector2(waveX, waveY) * waveAmplitude, waveFadeValues[i]));
            lineRenderer2.SetPosition(i, lineRenderer.GetPosition(i));
        }
    }

    Vector2 GetWorldFromValuePosition (Vector2 valuePosition) {
        return new Vector2(
            Mathf.Lerp(minBound.x, maxBound.x, valuePosition.x),
            Mathf.Lerp(minBound.y, maxBound.y, valuePosition.y));
    }
    #endregion



    #region Analysis
    private float startAngle;
    private float endAngle;
    private Vector2 startPosition;
    private Vector2 endPosition;
    private float yXRatio;
    private float xYRatio;
    private List<float> angles;
    private float maxDistanceReached;
    private Vector2 minBound;
    private Vector2 maxBound;
    private Vector2 maxXBoundReachPos;
    private Vector2 minXBoundReachPos;
    private Vector2 maxYBoundReachPos;
    private Vector2 minYBoundReachPos;
    private float maxTopHatValue;
    private float maxBottomHatValue;
    private float maxLeftHatValue;
    private float maxRightHatValue;
    private float totalRevolution;
    private Vector2 boundCenter;
    private Vector2 minXLimitReachPos;
    private Vector2 maxXLimitReachPos;
    private int corners;

    private Symbols lastColorKey;
    private float colorLerpValue = 0f;
    private Color fromColor;
    private Color toColor;

    void InitAnalysis () {
        angles = new List<float>();
    }

    void OnAddPoint () {
        if(points.Count <= 2) {
            maxDistanceReached = 0f;
            minBound = points[0];
            maxBound = points[0];
            maxXBoundReachPos = points[0];
            minXBoundReachPos = points[0];
            maxYBoundReachPos = points[0];
            minYBoundReachPos = points[0];
            maxTopHatValue = 0f;
            maxBottomHatValue = 0f;
            maxLeftHatValue = 0f;
            maxRightHatValue = 0f;
            minXLimitReachPos = points[0];
            maxXLimitReachPos = points[0];
            corners = 0;
            return;
        }

        // Managing line angles
        Vector2 lastPointDelta = points[points.Count - 1] - points[points.Count - 2];
        angles.Add(Mathf.Repeat(Mathf.Atan2(lastPointDelta.y, lastPointDelta.x) / (Mathf.PI * 2f), 1f));
        totalRevolution = 0f;
        for(int i = 0; i < angles.Count - 1; i++) {
            totalRevolution += LoopValueDelta(angles[i], angles[i + 1]);
        }

        // Retreiving basic data
        startAngle = angles[0];
        endAngle = angles[angles.Count - 1];
        startPosition = points[0];
        endPosition = points[points.Count - 1];
        
        // Finding bounds and ratios
        for(int i = 0; i < points.Count; i++) {
            if(points[i].x > maxBound.x) {
                maxRightHatValue = 0f;
                maxXBoundReachPos = points[i];
            }
            if(points[i].x < minBound.x) {
                maxLeftHatValue = 0f;
                minXBoundReachPos = points[i];
            }
            if(points[i].y > maxBound.y) {
                maxTopHatValue = 0f;
                maxYBoundReachPos = points[i];
            }
            if(points[i].y < minBound.y) {
                maxBottomHatValue = 0f;
                minYBoundReachPos = points[i];
            }
            maxBound = new Vector2(Mathf.Max(maxBound.x, points[i].x), Mathf.Max(maxBound.y, points[i].y));
            minBound = new Vector2(Mathf.Min(minBound.x, points[i].x), Mathf.Min(minBound.y, points[i].y));
        }
        yXRatio = (maxBound.y - minBound.y) / Mathf.Max(0.0001f, (maxBound.x - minBound.x));
        xYRatio = (maxBound.x - minBound.x) / Mathf.Max(0.0001f, (maxBound.y - minBound.y));
        boundCenter = Vector2.Lerp(minBound, maxBound, 0.5f);

        // Checking corners
        corners = 0;
        for(int i = 2; i < points.Count; i++) {
            if(points[i - 2].x < points[i - 1].x && points[i - 1].x >= points[i].x) {
                corners++;
                maxXLimitReachPos = points[i - 1];
            }
            if(points[i - 2].x > points[i - 1].x && points[i - 1].x <= points[i].x) {
                corners++;
                minXLimitReachPos = points[i - 1];
            }
        }

        // Total max distance reached
        maxDistanceReached = Mathf.Max(Vector2.Distance(startPosition, endPosition), maxDistanceReached);

        OnValidateDrawing();
    }

    void OnEndLine () {
        for(int i = 0; i < points.Count; i++) {
            starsParticles.transform.position = points[i];
            starsParticles.Emit(1);
        }
        if(lastColorKey != Symbols.None)
            executeSymbol(lastColorKey);
        angles.Clear();
    }

    void OnValidateDrawing () {
        List<DrawingResult> results = new List<DrawingResult>();

        results.Add(Shape_HorizontalLine());
        results.Add(Shape_VerticalLine());
        results.Add(Shape_TopHat());
        results.Add(Shape_BottomHat());
        results.Add(Shape_LeftHat());
        results.Add(Shape_RightHat());
        results.Add(Shape_Delta());
        results.Add(Shape_Lightning());
        results.Add(Shape_Circle());
        results.Add(Shape_Hearth());
        results.Add(Shape_Spiral());

        results.Sort();
        results.Reverse();

        resultText.transform.position = Input.mousePosition;
        if(results[0].score > 0.2f) {
            resultText.SetText(symbolParametersDict[results[0].key].name);
            if(lastColorKey != results[0].key) {
                lastColorKey = results[0].key;
                colorLerpValue = 0f;
                fromColor = lineRenderer.startColor;
                toColor = symbolParametersDict[results[0].key].color;
            }
            detectedShape = symbolParametersDict[results[0].key];
        } else {
            if(lastColorKey != Symbols.None) {
                lastColorKey = Symbols.None;
                colorLerpValue = 0f;
                fromColor = lineRenderer.startColor;
                toColor = Color.white;
            }
            resultText.SetText(string.Empty);
            detectedShape = null;
        }
    }
    #endregion



    #region Custom Symbol Analyser
    const int totalDrawings = 1;

    // Priority: 10
    DrawingResult Shape_HorizontalLine () {
        float score = 0f;
        
        float endAngleDelta = Mathf.Abs(LoopValueDelta(endAngle, 0f)) * 360f;
        float endAngleDelta2 = Mathf.Abs(LoopValueDelta(endAngle, 0.5f)) * 360f;

        float distanceValue = Mathf.InverseLerp(minLineDistance, maxLineDistance, maxDistanceReached);
        float endAngleValue = 1f - Mathf.InverseLerp(minLineAngleDelta, maxLineAngleDelta, endAngleDelta);
        endAngleValue = Mathf.Max(endAngleValue, 1f - Mathf.InverseLerp(minLineAngleDelta, maxLineAngleDelta, endAngleDelta2));
        float ratio = (yXRatio < minLineRatio) ? 1f : 0f;

        score = endAngleValue * distanceValue * ratio;

        return new DrawingResult(Symbols.HLine, score, 10);
    }

    // Priority: 10
    DrawingResult Shape_VerticalLine () {
        float score = 0f;

        float startAngleDelta = Mathf.Abs(LoopValueDelta(startAngle, 0.25f)) * 360f;
        float endAngleDelta = Mathf.Abs(LoopValueDelta(endAngle, 0.25f)) * 360f;

        float startAngleDelta2 = Mathf.Abs(LoopValueDelta(startAngle, 0.75f)) * 360f;
        float endAngleDelta2 = Mathf.Abs(LoopValueDelta(endAngle, 0.75f)) * 360f;

        float distanceValue = Mathf.InverseLerp(minLineDistance, maxLineDistance, maxDistanceReached);
        float startAngleValue = 1f - Mathf.InverseLerp(minLineAngleDelta, maxLineAngleDelta, startAngleDelta);
        startAngleValue = Mathf.Max(startAngleValue, 1f - Mathf.InverseLerp(minLineAngleDelta, maxLineAngleDelta, startAngleDelta2));
        float endAngleValue = 1f - Mathf.InverseLerp(minLineAngleDelta, maxLineAngleDelta, startAngleDelta);
        endAngleValue = Mathf.Max(endAngleValue, 1f - Mathf.InverseLerp(minLineAngleDelta, maxLineAngleDelta, endAngleDelta2));
        float ratio = (xYRatio < minLineRatio) ? 1f : 0f;

        score = startAngleValue * endAngleValue * distanceValue * ratio;

        return new DrawingResult(Symbols.VLine, score, 10);
    }

    // Priority: 8
    DrawingResult Shape_TopHat () { // ^
        float score = 0f;

        if(endPosition.x > startPosition.x) { // Left-Right
            if(maxYBoundReachPos.y > startPosition.y && endPosition.x > maxYBoundReachPos.x && maxYBoundReachPos.x > startPosition.x) { // Is past top point?
                float topBottomValue = Mathf.InverseLerp(maxBound.y, minBound.y, endPosition.y);
                maxTopHatValue = Mathf.Max(maxTopHatValue, Mathf.Clamp01(topBottomValue * 1.5f - 0.5f));
                score = maxTopHatValue;
            }
        } else { // Right-Left
            if(maxYBoundReachPos.y > startPosition.y && endPosition.x < maxYBoundReachPos.x && maxYBoundReachPos.x < startPosition.x) { // Is past top point?
                float topBottomValue = Mathf.InverseLerp(maxBound.y, minBound.y, endPosition.y);
                maxTopHatValue = Mathf.Max(maxTopHatValue, Mathf.Clamp01(topBottomValue * 1.5f - 0.5f));
                score = maxTopHatValue;
            }
        }

        return new DrawingResult(Symbols.TopHat, score, 8);
    }

    // Priority: 7
    DrawingResult Shape_LeftHat () { // <
        float score = 0f;
        
        if(endPosition.y < startPosition.y) { // Top-Bottom
            if(minXBoundReachPos.x < startPosition.x && endPosition.y < minXBoundReachPos.y && minXBoundReachPos.y < startPosition.y) { // Is past top point?
                float leftRightValue = Mathf.InverseLerp(minBound.x, maxBound.x, endPosition.x);
                maxLeftHatValue = Mathf.Max(maxLeftHatValue, Mathf.Clamp01(leftRightValue * 1.5f - 0.5f));
                score = maxLeftHatValue;
            }
        } else { // Bottom-Top
            if(minXBoundReachPos.x < startPosition.x && endPosition.y > minXBoundReachPos.y && minXBoundReachPos.y > startPosition.y) { // Is past top point?
                float leftRightValue = Mathf.InverseLerp(minBound.x, maxBound.x, endPosition.x);
                maxLeftHatValue = Mathf.Max(maxLeftHatValue, Mathf.Clamp01(leftRightValue * 1.5f - 0.5f));
                score = maxLeftHatValue;
            }
        }

        return new DrawingResult(Symbols.LeftHat, score, 7);
    }

    // Priority: 7
    DrawingResult Shape_RightHat () { // >
        float score = 0f;


        if(endPosition.y < startPosition.y) { // Top-Bottom
            if(maxXBoundReachPos.x > startPosition.x && endPosition.y < maxXBoundReachPos.y && maxXBoundReachPos.y < startPosition.y) { // Is past top point?
                float rightLeftValue = Mathf.InverseLerp(maxBound.x, minBound.x, endPosition.x);
                maxRightHatValue = Mathf.Max(maxRightHatValue, Mathf.Clamp01(rightLeftValue * 1.5f - 0.5f));
                score = maxRightHatValue;
            }
        } else { // Bottom-Top
            if(maxXBoundReachPos.x > startPosition.x && endPosition.y > maxXBoundReachPos.y && maxXBoundReachPos.y > startPosition.y) { // Is past top point?
                float rightLeftValue = Mathf.InverseLerp(maxBound.x, minBound.x, endPosition.x);
                maxRightHatValue = Mathf.Max(maxRightHatValue, Mathf.Clamp01(rightLeftValue * 1.5f - 0.5f));
                score = maxRightHatValue;
            }
        }

        return new DrawingResult(Symbols.RightHat, score, 7);
    }

    // Priority: 5
    DrawingResult Shape_BottomHat () { // v
        float score = 0f;
        
        if(endPosition.x > startPosition.x) { // Left-Right
            if(minYBoundReachPos.y < startPosition.y && endPosition.x > minYBoundReachPos.x && minYBoundReachPos.x > startPosition.x) { // Is past top point?
                float bottomTopValue = Mathf.InverseLerp(minBound.y, maxBound.y, endPosition.y);
                maxBottomHatValue = Mathf.Max(maxBottomHatValue, Mathf.Clamp01(bottomTopValue * 1.5f - 0.5f));
                score = maxBottomHatValue;
            }
        } else { // Right-Left
            if(minYBoundReachPos.y < startPosition.y && endPosition.x < minYBoundReachPos.x && minYBoundReachPos.x < startPosition.x) { // Is past top point?
                float bottomTopValue = Mathf.InverseLerp(minBound.y, maxBound.y, endPosition.y);
                maxBottomHatValue = Mathf.Max(maxBottomHatValue, Mathf.Clamp01(bottomTopValue * 1.5f - 0.5f));
                score = maxBottomHatValue;
            }
        }

        return new DrawingResult(Symbols.BottomHat, score, 5);
    }

    // Priority: 11
    DrawingResult Shape_Circle () {
        float score = 0f;

        float revolutionValue = Mathf.InverseLerp(minCircleRevolution, maxCircleRevolution, Mathf.Abs(totalRevolution));
        float distanceValue = 1f - Mathf.InverseLerp(minCircleStartEndDistance, maxCircleStartEndDistance, Vector2.Distance(startPosition, endPosition));
        float averageRadius = ((maxBound.x - minBound.x) + (maxBound.y - minBound.y)) * 0.5f;
        float radiusValue = Mathf.InverseLerp(minCircleRadius, maxCircleRadius, averageRadius);

        score = revolutionValue * distanceValue * radiusValue;

        return new DrawingResult(Symbols.Circle, score, 11);
    }

    // Priority: 13
    DrawingResult Shape_Hearth () {
        float score = 0f;
        
        float distanceValue = 1f - Mathf.InverseLerp(minHearthStartEndDistance, maxHearthStartEndDistance, Vector2.Distance(startPosition, endPosition));
        float averageRadius = ((maxBound.x - minBound.x) + (maxBound.y - minBound.y)) * 0.5f;
        float radiusValue = Mathf.InverseLerp(minCircleRadius, maxCircleRadius, averageRadius);

        float isHearth = 0f;
        if(minXBoundReachPos.y > boundCenter.y && maxXBoundReachPos.y > boundCenter.y) {
            float averageSplitLine = Mathf.Lerp(minXBoundReachPos.y, maxXBoundReachPos.y, 0.5f);
            float topSplitHeight = maxBound.y - averageSplitLine;
            float bottomSplitHeight = (maxBound.y - minBound.y) - topSplitHeight;
            if((100 * (topSplitHeight / bottomSplitHeight)) < 50f) {
                isHearth = 1f;
            }
        }

        score = isHearth * distanceValue * radiusValue;

        return new DrawingResult(Symbols.Heart, score, 13);
    }

    // Priority: 12
    DrawingResult Shape_Spiral () {
        float score = 0f;

        float averageRadius = ((maxBound.x - minBound.x) + (maxBound.y - minBound.y)) * 0.5f;
        float radiusValue = Mathf.InverseLerp(minCircleRadius, maxCircleRadius, averageRadius);
        score = radiusValue * Mathf.Clamp01((Mathf.Abs(totalRevolution) - 1f) * 2f);

        return new DrawingResult(Symbols.Spiral, score, 12);
    }

    // Priority: 10
    DrawingResult Shape_Delta () {
        float score = 0f;

        float distanceValue = 1f - Mathf.InverseLerp(minHearthStartEndDistance, maxHearthStartEndDistance, Vector2.Distance(startPosition, endPosition));

        float isDelta = 0f;
        if(minXBoundReachPos.y < boundCenter.y && maxXBoundReachPos.y < boundCenter.y) {
            float averageSplitLine = Mathf.Lerp(minXBoundReachPos.y, maxXBoundReachPos.y, 0.5f);
            float bottomSplitHeight = averageSplitLine - minBound.y;
            float topSplitHeight = (maxBound.y - minBound.y) - bottomSplitHeight;
            if((100 * (bottomSplitHeight / topSplitHeight)) < 20f) {
                isDelta = 1f;
            }
        }
        score = isDelta * distanceValue;

        return new DrawingResult(Symbols.Delta, score, 10);
    }

    // Priority: 9
    DrawingResult Shape_Lightning () {
        float score = 0f;
        
        if(corners <= 3) {
            // Top to bottom
            if((startPosition.y > minXLimitReachPos.y && startPosition.y > maxXLimitReachPos.y) && (endPosition.y < minXLimitReachPos.y && endPosition.y < maxXLimitReachPos.y)) { // Makes sure all points are in order
                if(minXLimitReachPos.x != startPosition.x && maxXLimitReachPos.x != startPosition.x) { // Makes sure there's some zigzag going on
                    score = 1f;
                }
            }
            // Bottom to top
            if((startPosition.y < minXLimitReachPos.y && startPosition.y < maxXLimitReachPos.y) && (endPosition.y > minXLimitReachPos.y && endPosition.y > maxXLimitReachPos.y)) { // Makes sure all points are in order
                if(minXLimitReachPos.x != startPosition.x && maxXLimitReachPos.x != startPosition.x) { // Makes sure there's some zigzag going on
                    score = 1f;
                }
            }
            outlineRenderer.positionCount = 4;
            outlineRenderer.SetPosition(0, startPosition);
            outlineRenderer.SetPosition(1, minXLimitReachPos);
            outlineRenderer.SetPosition(2, maxXLimitReachPos);
            outlineRenderer.SetPosition(3, endPosition);
        }

        return new DrawingResult(Symbols.Lightning, score, 9);
    }
    #endregion



    #region Utils
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

public struct DrawingResult : IComparable {
    public Symbols key;
    public float score;
    public int priority;

    public DrawingResult (Symbols key, float score, int priority) {
        this.key = key;
        this.score = score;
        this.priority = priority;
    }

    public int CompareTo (object obj) {
        DrawingResult compareTo = ((DrawingResult)obj);

        if(compareTo.score == score) {
            return priority.CompareTo(compareTo.priority);
        }
        return score.CompareTo(compareTo.score);
    }
}

[Serializable]
public class SymbolParameter {
    public string name;
    public Symbols symbol;
    public Color color = Color.white;
}