using Unity.Mathematics;
using System.Collections.Generic;

public static class MeshLookupTables {

    public static readonly int[] TrisPerModelFace = {
        2, 2, 2, 2, 4,
    };



    public static readonly float3[] StairsVertsTable = {
        new float3(0f, 0f, 0f), //0
        new float3(1f, 0f, 0f), //1
        new float3(1f, 0.5f, 0f), //2
        new float3(0f, 0.5f, 0f), //3
        new float3(0f, 0.5f, 1f), //4
        new float3(1f, 0.5f, 1f), //5
        new float3(1f, 0f, 1f), //6
        new float3(0f, 0f, 1f), //7

        new float3(0f, 0.5f, 0f), //0
        new float3(0.5f, 0.5f, 0f), //1
        new float3(0.5f, 1f, 0f), //2
        new float3(0f, 1f, 0f), //3
        new float3(0f, 1f, 1f), //4
        new float3(0.5f, 1f, 1f), //5
        new float3(0.5f, 0.5f, 1f), //6
        new float3(0f, 0.5f, 1f), //7
    };
    public static readonly int[] StairsConnectsToFullFaceTable = {
        1, 1, 0, 1, 0, 1
    };
    public static readonly int[] StairsIsFullFaceTable = {
        0, 0, 0, 1, 0, 1
    };
    public static readonly int[] StairsFaceTable = {
        5, 4, 7, 5, 7, 6,   13, 12, 15, 13, 15, 14,
        0, 2, 1, 0, 3, 2,   8, 10, 9, 8, 11, 10,
        2, 3, 4, 2, 4, 5,   10, 11, 12, 10, 12, 13,
        0, 6, 7, 0, 1, 6,   -1, -1, -1, -1, -1, -1,
        1, 2, 5, 1, 5, 6,   9, 10, 13, 9, 13, 14,
        0, 7, 4, 0, 4, 3,   8, 15, 12, 8, 12, 11,
    };




    public static readonly float3[] EigthVertsTable = {
        new float3(0f, 0f, 0f), //0
        new float3(0.5f, 0f, 0f), //1
        new float3(0.5f, 0.5f, 0f), //2
        new float3(0f, 0.5f, 0f), //3
        new float3(0f, 0.5f, 0.5f), //4
        new float3(0.5f, 0.5f, 0.5f), //5
        new float3(0.5f, 0f, 0.5f), //6
        new float3(0f, 0f, 0.5f), //7
    };
    public static readonly int[] EigthConnectsToFullFaceTable = {
        0, 1, 0, 1, 0, 1
    };
    public static readonly float3[] QuarterVertsTable = {
        new float3(0f, 0f, 0f), //0
        new float3(0.5f, 0f, 0f), //1
        new float3(0.5f, 0.5f, 0f), //2
        new float3(0f, 0.5f, 0f), //3
        new float3(0f, 0.5f, 1f), //4
        new float3(0.5f, 0.5f, 1f), //5
        new float3(0.5f, 0f, 1f), //6
        new float3(0f, 0f, 1f), //7
    };
    public static readonly int[] QuarterConnectsToFullFaceTable = {
        1, 1, 0, 1, 0, 1
    };
    public static readonly float3[] SlabVertsTable = {
        new float3(0f, 0f, 0f), //0
        new float3(1f, 0f, 0f), //1
        new float3(1f, 0.5f, 0f), //2
        new float3(0f, 0.5f, 0f), //3
        new float3(0f, 0.5f, 1f), //4
        new float3(1f, 0.5f, 1f), //5
        new float3(1f, 0f, 1f), //6
        new float3(0f, 0f, 1f), //7
    };
    public static readonly int[] SlabIsFullFaceTable = {
        0, 0, 0, 1, 0, 0
    };
    public static readonly int[] SlabConnectsToFullFaceTable = {
        1, 1, 0, 1, 1, 1
    };
    public static readonly float3[] CubeVertsTable = {
        new float3(0f, 0f, 0f), //0
        new float3(1f, 0f, 0f), //1
        new float3(1f, 1f, 0f), //2
        new float3(0f, 1f, 0f), //3
        new float3(0f, 1f, 1f), //4
        new float3(1f, 1f, 1f), //5
        new float3(1f, 0f, 1f), //6
        new float3(0f, 0f, 1f), //7
    };
    public static readonly int[] SquarePrismsFaceTable = {
        5, 4, 7, 5, 7, 6,
        0, 2, 1, 0, 3, 2,
        2, 3, 4, 2, 4, 5,
        0, 6, 7, 0, 1, 6,
        1, 2, 5, 1, 5, 6,
        0, 7, 4, 0, 4, 3,
    };



    public static readonly int[] VertsPosToUVsTable = {
        0, 1, 0, 1, 0, 2, 0, 2, 2, 1, 2, 1
    };
    public static readonly int[] FlipXUVsTable = {
        1, 0, 0, 0, 0, 1
    };
    public static readonly int3[] FaceNormalTable = {
        new int3(0, 0, 1),
        new int3(0, 0, -1),
        new int3(0, 1, 0),
        new int3(0, -1, 0),
        new int3(1, 0, 0),
        new int3(-1, 0, 0),
    };
}
