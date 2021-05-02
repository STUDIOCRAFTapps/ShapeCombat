using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Unity.Mathematics;
using UnityEngine.UI;
using System.Text;
using System.Linq;
using TMPro;
using System;

[Serializable]
public class ShapeTemplate3 {
    public string name;
    public Vector2[] points;
}

public class ShapeDrawSystem3 : MonoBehaviour {
    [Header("References")]
    public LineRenderer lineRenderer;
    new public Camera camera;
    public TextMeshProUGUI debugText;
    public ShapeTemplate3[] shapes;

    [Header("Parameters")]
    public float minMovingDistance = 0.1f;


    private List<Vector2> points;
    private Vector2 lastPoint;
    private Vector2 minBound;
    private Vector2 maxBound;



    void Start () {
        points = new List<Vector2>();
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
            AnalyseDrawingඞ();
        }
    }


    void AnalyseDrawingඞ () {
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
        float ratio = boundDelta.y / boundDelta.x;
        if(float.IsNaN(ratio))
            ratio = 0f;
        if(ratio < 3f && ratio > 0.333f) {
            if(boundDelta.x != 0f)
                scalingVector.x = 1f / boundDelta.x;
            if(boundDelta.y != 0f)
                scalingVector.y = 1f / boundDelta.y;
        } else {
            if(boundDelta.x > boundDelta.y) {
                scalingVector.x = 1f / boundDelta.x;
                scalingVector.y = 1f / boundDelta.x;
                yDisp += (1f - (boundDelta.y * scalingVector.y)) * 0.5f;
            } else {
                scalingVector.x = 1f / boundDelta.y;
                scalingVector.y = 1f / boundDelta.y;
                xDisp += (1f - (boundDelta.x * scalingVector.x)) * 0.5f;
            }
        }
        

        for(int i = 0; i < points.Count; i++) {
            points[i] = (((points[i] - minBound) * scalingVector) + new Vector2(xDisp, yDisp));
        }

        float smallestScore = Mathf.Infinity;
        int bestMatch = 0;
        for(int i = 0; i < shapes.Length; i++) {
            float totalDistance = 0f;
            float totalMissedDistance = 0f;
            for(int l = 0; l < shapes[i].points.Length; l++) {
                float minDistance = Mathf.Infinity;
                for(int p = 0; p < points.Count; p++) {
                    float sqrDist = Vector2.SqrMagnitude((shapes[i].points[l] - points[p]) * 10f);
                    minDistance = Mathf.Min(minDistance, sqrDist);
                }
                totalDistance += minDistance;
            }
            for(int p = 0; p < points.Count; p++) {
                float minDistance = Mathf.Infinity;
                for(int l = 0; l < shapes[i].points.Length; l++) {
                    float sqrDist = Vector2.SqrMagnitude((shapes[i].points[l] - points[p]) * 10f);
                    minDistance = Mathf.Min(minDistance, sqrDist);
                }
                totalMissedDistance += minDistance;
            }
            totalDistance /= shapes[i].points.Length;
            totalMissedDistance /= points.Count;
            float score = totalDistance + totalMissedDistance * 3f;
            if(score < smallestScore) {
                bestMatch = i;
                smallestScore = score;
            }
        }

        Debug.Log(shapes[bestMatch].name);
    }
}
