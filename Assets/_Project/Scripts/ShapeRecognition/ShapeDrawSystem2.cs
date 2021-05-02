using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Unity.Mathematics;
using UnityEngine.UI;
using System.Text;
using System.Linq;
using TMPro;
using System;

public class SortingPair : IComparable {
    public float score;
    public string key;

    public int CompareTo (object obj) {
        return score.CompareTo(((SortingPair)obj).score);
    }
}


public class ShapeDrawSystem2 : MonoBehaviour {

    [Header("References")]
    public LineRenderer lineRenderer;
    new public Camera camera;
    public RectTransform drawingRect;
    public RawImage targetSimplified;
    public TextMeshProUGUI debugText;

    [Header("Parameters")]
    public int imageSize = 32;
    public float minMovingDistance;


    private List<Vector2> points;
    private Vector2 lastPoint;
    private Vector2 minBound;
    private Vector2 maxBound;

    private Texture2D simplifiedTexture;
    private byte[] rawSimplifiedTextureData;
    private float[] averagedData;
    private Color[] simplifiedTextureData;
    private float totalAverageCount;



    
    void Start () {
        points = new List<Vector2>();

        rawSimplifiedTextureData = new byte[imageSize * imageSize];
        simplifiedTextureData = new Color[imageSize * imageSize];
        averagedData = new float[imageSize * imageSize];

        simplifiedTexture = new Texture2D(imageSize, imageSize);
        simplifiedTexture.filterMode = FilterMode.Point;
        targetSimplified.texture = simplifiedTexture;
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
            FindBestMatch();
        }

        if(Input.GetKeyDown(KeyCode.K)) {
            GenerateVisualTextureFromAverageData();
        }
        if(Input.GetKeyDown(KeyCode.R)) {
            for(int i = 0; i < (imageSize * imageSize); i++) {
                averagedData[i] = 0;
            }
            totalAverageCount = 0;
        }
    }


    void AnalyseDrawing () {
        for(int i = 0; i < (imageSize * imageSize); i++) {
            rawSimplifiedTextureData[i] = 0;
        }

        Vector2 minBound = new Vector2(Mathf.Infinity, Mathf.Infinity);
        Vector2 maxBound = new Vector2(Mathf.NegativeInfinity, Mathf.NegativeInfinity);
        for(int i = 0; i < points.Count; i++) {
            maxBound = new Vector2(Mathf.Max(maxBound.x, points[i].x), Mathf.Max(maxBound.y, points[i].y));
            minBound = new Vector2(Mathf.Min(minBound.x, points[i].x), Mathf.Min(minBound.y, points[i].y));
        }
        Vector2 boundDelta = maxBound - minBound;
        Vector2 scalingVector = Vector2.one;
        float xDisp = 0f;
        float yDisp = 0f;
        /*if(boundDelta.x > boundDelta.y) {
            scalingVector.x = 1f / boundDelta.x;
            scalingVector.y = 1f / boundDelta.x;
            yDisp += (1f - (boundDelta.y * scalingVector.y)) * 0.5f;
        } else {
            scalingVector.x = 1f / boundDelta.y;
            scalingVector.y = 1f / boundDelta.y;
            xDisp += (1f - (boundDelta.x * scalingVector.x)) * 0.5f;
        }*/
        if(boundDelta.x != 0f)
            scalingVector.x = 1f / boundDelta.x;
        if(boundDelta.y != 0f)
            scalingVector.y = 1f / boundDelta.y;

        for(int i = 0; i < points.Count; i++) {
            points[i] = (((points[i] - minBound) * scalingVector) + new Vector2(xDisp, yDisp)) * new Vector2(imageSize, imageSize);
        }
        for(int i = 0; i < points.Count - 1; i++) {
            DrawLine(points[i].x, points[i].y, points[i + 1].x, points[i + 1].y);
        }
        for(int i = 0; i < (imageSize * imageSize); i++) {
            averagedData[i] += rawSimplifiedTextureData[i] / 255f;
        }
        totalAverageCount++;

        GenerateVisualTextureFromRawData();
    }

    #region Comparing
    void FindBestMatch () {
        float topScore = float.NegativeInfinity;
        string topKey = "None";

        List<SortingPair> sortingPairs = new List<SortingPair>();
        foreach(KeyValuePair<string, List<List<byte>>> kernelCollection in ShapeDatabase.shapesKernels) {
            float topKernelScore = float.NegativeInfinity;
            for(int i = 0; i < kernelCollection.Value.Count; i++) {
                float currentScore = ComputeDrawingScore(kernelCollection.Key, i);
                //Debug.Log($"Kernel {kernelCollection.Key} v{i} = {currentScore}");
                if(currentScore > topScore) {
                    topScore = currentScore;
                    topKey = kernelCollection.Key;
                }
                topKernelScore = math.max(topKernelScore, currentScore);
            }
            sortingPairs.Add(new SortingPair() { key = kernelCollection.Key, score = topKernelScore });

            sortingPairs.Sort();
            sortingPairs.Reverse();

            StringBuilder sb = new StringBuilder();
            for(int l = 0; l < sortingPairs.Count && l < 5; l++) {
                sb.Append($"{sortingPairs[l].key} with {math.round(sortingPairs[l].score * 1000)}\n");
            }

            debugText.SetText(sb);
        }

        Debug.Log($"Best match is... " + topKey);
    }

    float ComputeDrawingScore (string key, int index) {
        List<byte> compareData = ShapeDatabase.shapesKernels[key][index];

        float totalPotentialPositives = 0f;
        for(int i = 0; i < compareData.Count; i++) {
            totalPotentialPositives += (compareData[i] / 255f);
        }

        float totalScore = 0f;
        for(int i = 0; i < compareData.Count; i++) {
            float rawValue = rawSimplifiedTextureData[i] / 255f;
            float targetRawValue = InverseDeg2(compareData[i] / 255f);

            totalScore += rawValue * targetRawValue;
            totalScore -= rawValue * (1f - targetRawValue) * 2f;
        }
        totalScore /= totalPotentialPositives;
        return totalScore;
    }

    private static float InverseDeg2 (float x) {
        return -x * x + 2 * x;
    }

    public void CopyKernelToClipboard () {
        if(totalAverageCount > 0) {
            StringBuilder sb = new StringBuilder();
            sb.Append("new List<byte>() {");

            float topAverageValue = 1f;
            for(int i = 0; i < (imageSize * imageSize); i++) {
                float value = averagedData[i];
                topAverageValue = math.max(value, topAverageValue);
            }

            for(int i = 0; i < (imageSize * imageSize); i++) {
                float value = averagedData[i] / topAverageValue;
                byte roundedValue = (byte)math.round(math.saturate(value) * 255f);
                sb.Append(roundedValue);
                if(i != (imageSize * imageSize) - 1) {
                    sb.Append(",");
                }
            }

            sb.Append("}\n");
            EditorGUIUtility.systemCopyBuffer = sb.ToString();
        }
    }
    #endregion

    #region Rendering
    void GenerateVisualTextureFromRawData () {
        for(int i = 0; i < (imageSize * imageSize); i++) {
            float value = rawSimplifiedTextureData[i] / 255f;
            simplifiedTextureData[i] = new Color(value, value, value, 1f);
        }
        simplifiedTexture.SetPixels(simplifiedTextureData);
        simplifiedTexture.Apply();
    }

    void GenerateVisualTextureFromAverageData () {
            if(totalAverageCount > 0) {
                float topAverageValue = 1f;
                for(int i = 0; i < (imageSize * imageSize); i++) {
                    float value = averagedData[i];
                    topAverageValue = math.max(value, topAverageValue);
                }

                for(int i = 0; i < (imageSize * imageSize); i++) {
                    float value = averagedData[i] / topAverageValue;
                    simplifiedTextureData[i] = new Color(value, value, value, 1f);
                }
                simplifiedTexture.SetPixels(simplifiedTextureData);
                simplifiedTexture.Apply();
            }
    }

    void DrawLine (float x0, float y0, float x1, float y1) {

        bool steep = math.abs(y1 - y0) > math.abs(x1 - x0);
        float temp;
        if(steep) {
            temp = x0;
            x0 = y0;
            y0 = temp;
            temp = x1;
            x1 = y1;
            y1 = temp;
        }
        if(x0 > x1) {
            temp = x0;
            x0 = x1;
            x1 = temp;
            temp = y0;
            y0 = y1;
            y1 = temp;
        }

        float dx = x1 - x0;
        float dy = y1 - y0;
        float gradient = dy / dx;

        // handle first endpoint
        float xend = math.round(x0);
        float yend = y0 + gradient * (xend - x0);
        float xgap = inverse_fract(x0 + 0.5f);
        float xpxl1 = xend; // this will be used in the main loop
        float ypxl1 = math.floor(yend);

        if(steep) {
            plot(ypxl1, xpxl1, inverse_fract(yend) * xgap);
            plot(ypxl1 + 1, xpxl1, math.frac(yend) * xgap);
        } else {
            plot(xpxl1, ypxl1, inverse_fract(yend) * xgap);
            plot(xpxl1, ypxl1 + 1, math.frac(yend) * xgap);
        }

        // first y-intersection for the main loop
        float intery = yend + gradient;

        // handle second endpoint
        xend = math.round(x1);
        yend = y1 + gradient * (xend - x1);
        xgap = math.frac(x1 + 0.5f);
        float xpxl2 = xend; // this will be used in the main loop
        float ypxl2 = math.floor(yend);

        if(steep) {
            plot(ypxl2, xpxl2, inverse_fract(yend) * xgap);
            plot(ypxl2 + 1, xpxl2, math.frac(yend) * xgap);
        } else {
            plot(xpxl2, ypxl2, inverse_fract(yend) * xgap);
            plot(xpxl2, ypxl2 + 1, math.frac(yend) * xgap);
        }

        // main loop
        for(float x = xpxl1 + 1; x <= xpxl2 - 1; x++) {
            if(steep) {
                plot(math.floor(intery), x, inverse_fract(intery));
                plot(math.floor(intery) + 1, x, math.frac(intery));
            } else {
                plot(x, math.floor(intery), inverse_fract(intery));
                plot(x, math.floor(intery) + 1, math.frac(intery));
            }
            intery = intery + gradient;
        }
    }

    float inverse_fract (float value) {
        return 1f - math.frac(value);
    }

    void plot (float y, float x, float value) {
        plot_v(y - 1, x - 1, value * 0.4f);
        plot_v(y + 0, x - 1, value * 0.7f);
        plot_v(y + 1, x - 1, value * 0.4f);
        plot_v(y - 1, x, value * 0.7f);
        plot_v(y + 0, x, value * 1.0f);
        plot_v(y + 1, x, value * 0.7f);
        plot_v(y - 1, x + 1, value * 0.4f);
        plot_v(y + 0, x + 1, value * 0.7f);
        plot_v(y + 1, x + 1, value * 0.4f);
    }

    void plot_v (float y, float x, float value) {
        float temp = y;
        y = x;
        x = temp;
        int index = (int)math.floor(math.clamp(x, 0, imageSize - 1)) + (int)math.floor(math.clamp(y, 0, imageSize - 1)) * imageSize;
        float totalSetValue = (rawSimplifiedTextureData[index] / 255f) + value;
        rawSimplifiedTextureData[index] = (byte)math.floor(math.saturate(totalSetValue) * 255);
    }
#endregion
}
