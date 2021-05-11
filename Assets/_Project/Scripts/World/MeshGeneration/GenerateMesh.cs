using Unity.Jobs;
using Unity.Burst;
using Unity.Collections;
using Unity.Mathematics;
using System.Runtime.CompilerServices;

[BurstCompile]
public struct VoxelMesherJob : IJobParallelFor {
    
    // Voxel asset data
    // Voxel meshing lookups

    [ReadOnly] public NativeHashMap<int3, BlitableArray<TileData>> _voxelData;
    [ReadOnly] public NativeArray<TileAssetJobData> _tileAssets;

    public int chunkSize;
    public int3 chunkPosition;
    public NativeCounter vertexCountCounter;

    [NativeDisableParallelForRestriction, WriteOnly] public NativeArray<MeshingVertexData> _vertices; // The generated vertices
    [NativeDisableParallelForRestriction, WriteOnly] public NativeArray<ushort> _triangles; // The generated triangles

    public void Execute (int index) {
        int3 localPos = IndexUtilities.IndexToXyz(index, chunkSize, chunkSize);
        int3 pos = localPos + chunkPosition * chunkSize;
        int3 chunkPos = new int3(
                (int)math.floor((float)pos.x / chunkSize),
                (int)math.floor((float)pos.y / chunkSize),
                (int)math.floor((float)pos.z / chunkSize));

        if(_voxelData.TryGetValue(chunkPos, out BlitableArray<TileData> mainChunkData)) {
            if(mainChunkData[index].assetId != ushort.MaxValue) {
                AppendModel(index, localPos, pos, mainChunkData[index].assetId, mainChunkData[index].model, mainChunkData[index].rotation);
            }
        }
    }

    public void AppendModel (int index, int3 localPos, int3 pos, ushort assetId, byte model, byte rotation) {
        byte rotX = (byte)(rotation & 0b11);
        byte rotY = (byte)((rotation >> 2) & 0b11);
        byte rotZ = (byte)((rotation >> 4) & 0b11);
        
        for(int i = 0; i < 6; i++) {
            int3 faceNormal = RotateCY(RotateCX(RotateCZ(MeshLookupTables.FaceNormalTable[i], rotZ), rotX), rotY);
            int rotatedFaceId = FaceNormalToFaceIndex(faceNormal);

            if(IsTileFullFace(pos + faceNormal, -faceNormal) && IsTileCollectToFullFace(model, i))
                continue;

            int textureIndex = _tileAssets[assetId].GetTextureIndex(rotatedFaceId);
            for(int t = 0; t < MeshLookupTables.TrisPerModelFace[model]; t++) {
                float3 vert0 = float3.zero, vert1 = float3.zero, vert2 = float3.zero;
                float3 rvert0 = float3.zero, rvert1 = float3.zero, rvert2 = float3.zero;
                
                if(model == 0) {
                    vert0 = MeshLookupTables.CubeVertsTable[MeshLookupTables.SquarePrismsFaceTable[0 + t * 3 + i * 6]];
                    vert1 = MeshLookupTables.CubeVertsTable[MeshLookupTables.SquarePrismsFaceTable[1 + t * 3 + i * 6]];
                    vert2 = MeshLookupTables.CubeVertsTable[MeshLookupTables.SquarePrismsFaceTable[2 + t * 3 + i * 6]];
                } else if(model == 1) {
                    vert0 = MeshLookupTables.SlabVertsTable[MeshLookupTables.SquarePrismsFaceTable[0 + t * 3 + i * 6]];
                    vert1 = MeshLookupTables.SlabVertsTable[MeshLookupTables.SquarePrismsFaceTable[1 + t * 3 + i * 6]];
                    vert2 = MeshLookupTables.SlabVertsTable[MeshLookupTables.SquarePrismsFaceTable[2 + t * 3 + i * 6]];
                } else if(model == 2) {
                    vert0 = MeshLookupTables.QuarterVertsTable[MeshLookupTables.SquarePrismsFaceTable[0 + t * 3 + i * 6]];
                    vert1 = MeshLookupTables.QuarterVertsTable[MeshLookupTables.SquarePrismsFaceTable[1 + t * 3 + i * 6]];
                    vert2 = MeshLookupTables.QuarterVertsTable[MeshLookupTables.SquarePrismsFaceTable[2 + t * 3 + i * 6]];
                }  else if(model == 3) {
                    vert0 = MeshLookupTables.EigthVertsTable[MeshLookupTables.SquarePrismsFaceTable[0 + t * 3 + i * 6]];
                    vert1 = MeshLookupTables.EigthVertsTable[MeshLookupTables.SquarePrismsFaceTable[1 + t * 3 + i * 6]];
                    vert2 = MeshLookupTables.EigthVertsTable[MeshLookupTables.SquarePrismsFaceTable[2 + t * 3 + i * 6]];
                } else if(model == 4) {
                    if(MeshLookupTables.StairsFaceTable[0 + t * 3 + i * 12] == -1)
                        break;

                    vert0 = MeshLookupTables.StairsVertsTable[MeshLookupTables.StairsFaceTable[0 + t * 3 + i * 12]];
                    vert1 = MeshLookupTables.StairsVertsTable[MeshLookupTables.StairsFaceTable[1 + t * 3 + i * 12]];
                    vert2 = MeshLookupTables.StairsVertsTable[MeshLookupTables.StairsFaceTable[2 + t * 3 + i * 12]];
                }
                rvert0 = vert0;
                rvert1 = vert1;
                rvert2 = vert2;
                vert0 = RotateY(RotateX(RotateZ(vert0, rotZ), rotX), rotY);
                vert1 = RotateY(RotateX(RotateZ(vert1, rotZ), rotX), rotY);
                vert2 = RotateY(RotateX(RotateZ(vert2, rotZ), rotX), rotY);

                float2 uv0 = new float2(vert0[MeshLookupTables.VertsPosToUVsTable[rotatedFaceId * 2]], vert0[MeshLookupTables.VertsPosToUVsTable[rotatedFaceId * 2 + 1]]);
                float2 uv1 = new float2(vert1[MeshLookupTables.VertsPosToUVsTable[rotatedFaceId * 2]], vert1[MeshLookupTables.VertsPosToUVsTable[rotatedFaceId * 2 + 1]]);
                float2 uv2 = new float2(vert2[MeshLookupTables.VertsPosToUVsTable[rotatedFaceId * 2]], vert2[MeshLookupTables.VertsPosToUVsTable[rotatedFaceId * 2 + 1]]);
                float3 normal = faceNormal;
                uv0.x = math.select(uv0.x, 1f - uv0.x, MeshLookupTables.FlipXUVsTable[rotatedFaceId] == 1);
                uv1.x = math.select(uv1.x, 1f - uv1.x, MeshLookupTables.FlipXUVsTable[rotatedFaceId] == 1);
                uv2.x = math.select(uv2.x, 1f - uv2.x, MeshLookupTables.FlipXUVsTable[rotatedFaceId] == 1);

                int triangleIndex = vertexCountCounter.Increment() * 3;
                _triangles[triangleIndex + 0] = (ushort)(triangleIndex + 0);
                _triangles[triangleIndex + 1] = (ushort)(triangleIndex + 1);
                _triangles[triangleIndex + 2] = (ushort)(triangleIndex + 2);
                _vertices[triangleIndex + 0] = new MeshingVertexData(localPos + vert0, normal, new float4(uv0, textureIndex, 0));
                _vertices[triangleIndex + 1] = new MeshingVertexData(localPos + vert1, normal, new float4(uv1, textureIndex, 0));
                _vertices[triangleIndex + 2] = new MeshingVertexData(localPos + vert2, normal, new float4(uv2, textureIndex, 0));
            }
        }
    }

    #region Connection
    public bool IsTileFullFace (int3 pos, int3 normal) {
        int3 chunkPos = new int3(
            (int)math.floor((float)pos.x / chunkSize),
            (int)math.floor((float)pos.y / chunkSize),
            (int)math.floor((float)pos.z / chunkSize));

        if(_voxelData.TryGetValue(chunkPos, out BlitableArray<TileData> chunkData)) {
            int index = IndexUtilities.XyzToIndex(pos - chunkPos * chunkSize, chunkSize, chunkSize);
            ushort assetId = chunkData[index].assetId;
            if(assetId == ushort.MaxValue) {
                return false;
            } else {
                byte rotX = (byte)(chunkData[index].rotation & 0b11);
                byte rotY = (byte)((chunkData[index].rotation >> 2) & 0b11);
                byte rotZ = (byte)((chunkData[index].rotation >> 4) & 0b11);

                //GOAL: Get rotated normal, take rotating operation, reverse the operation to get default face index

                int3 faceNormal = RotateRCZ(RotateRCX(RotateRCY(normal, rotY), rotX), rotZ);
                int face = FaceNormalToFaceIndex(faceNormal);

                if(chunkData[index].model == 0) {
                    return true;
                } else if(chunkData[index].model == 1) {
                    return MeshLookupTables.SlabIsFullFaceTable[face] == 1;
                } else if(chunkData[index].model == 4) {
                    return MeshLookupTables.StairsIsFullFaceTable[face] == 1;
                }
                return false;
            }
        } else {
            return false;
        }
    }

    public bool IsTileCollectToFullFace (int model, int face) {
        if(model == 0) {
            return true;
        } else if(model == 1) {
            return MeshLookupTables.SlabConnectsToFullFaceTable[face] == 1;
        } else if(model == 2) {
            return MeshLookupTables.QuarterConnectsToFullFaceTable[face] == 1;
        } else if(model == 3) {
            return MeshLookupTables.EigthConnectsToFullFaceTable[face] == 1;
        } else if(model == 4) {
            return MeshLookupTables.StairsConnectsToFullFaceTable[face] == 1;
        }
        return false;
    }
    #endregion

    #region Rotate Utils
    public float3 RotateZ (float3 v, byte rot) {
        if(rot == 0) {
            return v;
        } else if(rot == 3) {
            return new float3(v.y, 1f-v.x, v.z);
        } else if(rot == 2) {
            return new float3(1f-v.x, 1f-v.y, v.z);
        } else {
            return new float3(1f-v.y, v.x, v.z);
        }
    }

    public int3 RotateCZ (int3 v, byte rot) {
        if(rot == 0) {
            return v;
        } else if(rot == 3) {
            return new int3(v.y, -v.x, v.z);
        } else if(rot == 2) {
            return new int3(-v.x, -v.y, v.z);
        } else {
            return new int3(-v.y, v.x, v.z);
        }
    }

    public int3 RotateRCZ (int3 v, byte rot) {
        if(rot == 0) {
            return v;
        } else if(rot == 1) {
            return new int3(v.y, -v.x, v.z);
        } else if(rot == 2) {
            return new int3(-v.x, -v.y, v.z);
        } else {
            return new int3(-v.y, v.x, v.z);
        }
    }

    public float3 RotateY (float3 v, byte rot) {
        if(rot == 0) {
            return v;
        } else if(rot == 1) {
            return new float3(v.z, v.y, 1f-v.x);
        } else if(rot == 2) {
            return new float3(1f-v.x, v.y, 1f-v.z);
        } else {
            return new float3(1f-v.z, v.y, v.x);
        }
    }

    public int3 RotateCY (int3 v, byte rot) {
        if(rot == 0) {
            return v;
        } else if(rot == 1) {
            return new int3(v.z, v.y, -v.x);
        } else if(rot == 2) {
            return new int3(-v.x, v.y, -v.z);
        } else {
            return new int3(-v.z, v.y, v.x);
        }
    }

    public int3 RotateRCY (int3 v, byte rot) {
        if(rot == 0) {
            return v;
        } else if(rot == 3) {
            return new int3(v.z, v.y, -v.x);
        } else if(rot == 2) {
            return new int3(-v.x, v.y, -v.z);
        } else {
            return new int3(-v.z, v.y, v.x);
        }
    }

    public float3 RotateX (float3 v, byte rot) {
        if(rot == 0) {
            return v;
        } else if(rot == 3) {
            return new float3(v.x, v.z, 1f-v.y);
        } else if(rot == 2) {
            return new float3(v.x, 1f-v.y, 1f-v.z);
        } else {
            return new float3(v.x, 1f-v.z, v.y);
        }
    }

    public int3 RotateCX (int3 v, byte rot) {
        if(rot == 0) {
            return v;
        } else if(rot == 3) {
            return new int3(v.x, v.z, -v.y);
        } else if(rot == 2) {
            return new int3(v.x, -v.y, -v.z);
        } else {
            return new int3(v.x, -v.z, v.y);
        }
    }

    public int3 RotateRCX (int3 v, byte rot) {
        if(rot == 0) {
            return v;
        } else if(rot == 1) {
            return new int3(v.x, v.z, -v.y);
        } else if(rot == 2) {
            return new int3(v.x, -v.y, -v.z);
        } else {
            return new int3(v.x, -v.z, v.y);
        }
    }

    public int FaceNormalToFaceIndex (int3 faceNormal) {
        return math.select(math.select(math.select(math.select(math.select(5, 4, faceNormal.x == 1), 3, faceNormal.y == -1), 2, faceNormal.y == 1), 1, faceNormal.z == -1), 0, faceNormal.z == 1);
    }

    public static int GetFaceIndex (int3 faceDir) {
        return ToBit(faceDir.z, 0) +
               ToBit(faceDir.y, 2) +
               ToBit(faceDir.x, 4);
    }

    public static int ToBit (int n, int baseValue) {
        if(n == 0) {
            return 0;
        } else if(n == 1) {
            return baseValue + 1;
        } else {
            return baseValue;
        }
    }
    #endregion
}

