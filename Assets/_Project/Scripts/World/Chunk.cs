using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;
using System.IO;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine.Rendering;



public class Chunk : System.IDisposable {

    public GameObject colliderObject;
    public MeshCollider meshCollider;

    public int3 position { private set; get; }
    public ChunkData chunkData { private set; get; }
    private Vector3 worldPosition;
    private Mesh mesh;
    private Material material;
    private int layer;
    private List<TilePrefab> tilePrefabs;
    public bool isDirty { private set; get; }

    public Chunk (Material material, int renderingLayer, int3 position) {
        tilePrefabs = new List<TilePrefab>();

        this.position = position;
        worldPosition = (float3)(position * World.inst.chunkSize);
        this.layer = renderingLayer;
        this.material = material;

        colliderObject = new GameObject($"Collider {position.ToString()}", typeof(MeshCollider));
        colliderObject.transform.SetParent(World.inst.transform);
        colliderObject.transform.position = worldPosition;
        meshCollider = colliderObject.GetComponent<MeshCollider>();
    }

    public void Dispose () {
        if(colliderObject != null)
            Object.Destroy(colliderObject);
        chunkData.Dispose();
    }

    #region Chunk Data, Mesh Updates and Rendering
    public void SetChunkData (ChunkData chunkData) {
        this.chunkData = chunkData;
    }
    
    public void OnChunkDataUpdated () {
        isDirty = true;
    }

    private void GenerateMesh () {

        // Prepare jobs collections and variables
        NativeCounter vertexCountCounter = new NativeCounter(Allocator.TempJob);
        int voxelCount = World.inst.chunkSize * World.inst.chunkSize * World.inst.chunkSize;
        int maxVertCount = chunkData.EstimateVertsCount();
        NativeArray<MeshingVertexData> _verts = new NativeArray<MeshingVertexData>(maxVertCount, Allocator.TempJob);
        NativeArray<ushort> _tris = new NativeArray<ushort>(maxVertCount, Allocator.TempJob);
        NativeHashMap<int3, BlitableArray<TileData>> _voxelData = World.inst.GetSurroundingChunks(position);

        // Construct and schedule job
        VoxelMesherJob mesherJob = new VoxelMesherJob() {
            chunkPosition = new int3(position.x, position.y, position.z),
            vertexCountCounter = vertexCountCounter,
            _vertices = _verts,
            _triangles = _tris,
            _voxelData = _voxelData,
            _tileAssets = World.inst.tileCollection.tileAssetJobDatas,
            chunkSize = World.inst.chunkSize
        };
        JobHandle mesherJobHandle = mesherJob.Schedule(voxelCount, 180);

        // Force it to complete immediately
        mesherJobHandle.Complete();

        // Build mesh
        if(mesh == null)
            mesh = new Mesh();
        int vertexCount = vertexCountCounter.Count * 3;
        SubMeshDescriptor subMesh = new SubMeshDescriptor(0, 0);
        mesh.SetVertexBufferParams(vertexCount, MeshingVertexData.VertexBufferMemoryLayout);
        mesh.SetIndexBufferParams(vertexCount, IndexFormat.UInt16);
        mesh.SetVertexBufferData(_verts, 0, 0, vertexCount, 0, MeshUpdateFlags.Default); //MeshUpdateFlags.DontValidateIndices
        mesh.SetIndexBufferData(_tris, 0, 0, vertexCount, MeshUpdateFlags.Default); //MeshUpdateFlags.DontValidateIndices
        mesh.subMeshCount = 1;
        subMesh.indexCount = vertexCount;
        mesh.SetSubMesh(0, subMesh);
        mesh.RecalculateBounds();

        // This should be done in parralelle instead
        if(mesh.vertexCount > 0) {
            Physics.BakeMesh(mesh.GetInstanceID(), false);
            meshCollider.enabled = true;
            meshCollider.sharedMesh = mesh;
        } else {
            meshCollider.enabled = false;
        }

        // Dispose unused garbage (could be totally reused but who cares lol)
        vertexCountCounter.Dispose();
        _verts.Dispose();
        _tris.Dispose();
        _voxelData.Dispose();

    }

    public void Update () {
        if(isDirty) {
            isDirty = false;
            GenerateMesh();
        }

        Graphics.DrawMesh(mesh, worldPosition, Quaternion.identity, material, layer);
    }
    #endregion


    #region Utils
    public void SetVoxelData (int3 worldPos, ushort assetId, byte rotation, byte model) {
        chunkData.SetTileData(worldPos - World.inst.chunkSize * position, new TileData(assetId, rotation, model));
        OnChunkDataUpdated();
    }

    public TileData GetVoxelData (int3 worldPos) {
        return chunkData.GetTileData(worldPos - World.inst.chunkSize * position);
    }
    #endregion
}



public class ChunkData : System.IDisposable {
    public BlitableArray<TileData> tilesData;
    public List<TilePrefabData> tilePrefabsData; 

    public ChunkData () {
        tilePrefabsData = new List<TilePrefabData>();
        tilesData = new BlitableArray<TileData>(World.inst.chunkSize * World.inst.chunkSize * World.inst.chunkSize, Allocator.Persistent);
        for(int i = 0; i < (World.inst.chunkSize * World.inst.chunkSize * World.inst.chunkSize); i++) {
            tilesData[i] = new TileData(ushort.MaxValue, 0, 0);
        }
    }

    public void Dispose () {
        tilesData.Dispose();
    }



    public void SerializeChunkData (BinaryWriter writer) {
        for(int i = 0; i < (World.inst.chunkSize * World.inst.chunkSize * World.inst.chunkSize); i++) {
            GetTileData(i).WriteTile(writer);
        }
        writer.Write(tilePrefabsData.Count);
        for(int i = 0; i < tilePrefabsData.Count; i++) {
            tilePrefabsData[i].WriteTilePrefab(writer);
        }
    }

    public void DeserializeChunkData (BinaryReader reader) {
        for(int i = 0; i < (World.inst.chunkSize * World.inst.chunkSize * World.inst.chunkSize); i++) {
            SetTileData(i, TileData.ReadTile(reader));
        }
        int prefabCount = reader.ReadInt32();
        for(int i = 0; i < tilePrefabsData.Count; i++) {
            tilePrefabsData.Add(TilePrefabData.ReadTilePrefab(reader));
        }
    }



    public bool IsEmpty () {
        for(int i = 0; i < (World.inst.chunkSize * World.inst.chunkSize * World.inst.chunkSize); i++) {
            if(tilesData[i].assetId != ushort.MaxValue)
                return false;
        }
        return true;
    }

    public int EstimateVertsCount () {
        int vertCount = 0;
        for(int i = 0; i < (World.inst.chunkSize * World.inst.chunkSize * World.inst.chunkSize); i++) {
            if(!tilesData[i].IsAirTile) {
                vertCount += TileAsset.getModelVertCount[tilesData[i].model];
            }
        }
        return vertCount;
    }



    public void SetTileData (int index, TileData tileData) {
        tilesData[index] = tileData;
    }

    public void SetTileData (int3 p, TileData tileData) {
        tilesData[GetTileIndex(p.x, p.y, p.z)] = tileData;
    }

    public void SetTileData (int x, int y, int z, TileData tileData) {
        tilesData[GetTileIndex(x, y, z)] = tileData;
    }

    public TileData GetTileData (int index) {
        return tilesData[index];
    }

    public TileData GetTileData (int3 p) {
        return tilesData[GetTileIndex(p.x, p.y, p.z)];
    }

    public TileData GetTileData (int x, int y, int z) {
        return tilesData[GetTileIndex(x, y, z)];
    }

    public int GetTileIndex (int x, int y, int z) {
        return x + y * (World.inst.chunkSize) + z * (World.inst.chunkSize * World.inst.chunkSize);
    }
}
