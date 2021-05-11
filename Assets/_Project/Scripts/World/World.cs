using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;
using Unity.Collections;

public class World : MonoBehaviour {

    [Header("Parameters")]
    public int chunkSize = 16;
    public int renderingLayers = 0;

    [Header("References")]
    public Material material;
    public TileAssetCollection tileCollection;
    public TilePrefabAssetCollection tilePrefabCollection;
    new public Camera camera;
    public WorldSerializer worldSerializer;
    
    public Dictionary<int3, Chunk> chunks;
    

    public static World inst;
    void Awake () {
        inst = this;
        tileCollection.Init();
    }

    private void Start () {
        material.SetTexture("_MainTex", tileCollection.textures);
        chunks = new Dictionary<int3, Chunk>();
        LoadWorld();
    }

    private void Update () {
        foreach(KeyValuePair<int3, Chunk> kvp in chunks) {
            kvp.Value.Update();
        }

        if(Input.GetKeyDown(KeyCode.Alpha0)) {
            SaveWorld();
        }
    }

    private void OnDestroy () {
        foreach(KeyValuePair<int3, Chunk> kvp in chunks) {
            kvp.Value.Dispose(); // VERY IMPORTANT. UNSAFE MEMORY - WOULDN'T BE RELEASED OTHERWISE
        }
        tileCollection.Dispose();
    }


    #region Update Chunks/Tile
    public void ClearVoxelTile (int3 p) {
        SetVoxelTile(p, ushort.MaxValue, 0);
    }

    public void SetVoxelTile (int3 p, ushort assetId, byte model, byte rotation = 0) {
        int3 chunkPos = WorldToChunk(p);
        if(chunks.TryGetValue(chunkPos, out Chunk chunk)) {
            chunk.SetVoxelData(p, assetId, rotation, model);
        } else {
            CreateEmptyChunk(chunkPos);
            chunks[chunkPos].SetVoxelData(p, assetId, rotation, model);
        }

        UpdateSurroundingChunks(p, chunkPos);
    }

    public void UpdateSurroundingChunks (int3 p, int3 chunkPos) {
        HashSet<int3> updatedChunks = new HashSet<int3>();
        updatedChunks.Add(chunkPos);

        for(int z = -1; z <= 1; z++) {
            for(int y = -1; y <= 1; y++) {
                for(int x = -1; x <= 1; x++) {
                    int3 updatePos = p + new int3(x, y, z);
                    int3 cPos = WorldToChunk(updatePos);
                    if(chunks.TryGetValue(cPos, out Chunk chunk) && !updatedChunks.Contains(cPos)) {
                        updatedChunks.Add(cPos);
                        chunk.OnChunkDataUpdated();
                    }
                }
            }
        }
    }

    public NativeHashMap<int3, BlitableArray<TileData>> GetSurroundingChunks (int3 chunkPosition) {
        NativeHashMap<int3, BlitableArray<TileData>> surroundingChunks = new NativeHashMap<int3, BlitableArray<TileData>>(27, Allocator.Persistent);
        for(int z = -1; z <= 1; z++) {
            for(int y = -1; y <= 1; y++) {
                for(int x = -1; x <= 1; x++) {
                    int3 cPos = chunkPosition + new int3(x, y, z);
                    if(chunks.TryGetValue(cPos, out Chunk chunk)) {
                        surroundingChunks.Add(cPos, chunk.chunkData.tilesData);
                    }
                }
            }
        }
        return surroundingChunks;
    }
    #endregion

    #region Load/Save World & Create/Delete Chunks
    private void LoadWorld () {
        worldSerializer.DeserializeWorld();
        foreach(KeyValuePair<int3, Chunk> kvp in chunks) {
            kvp.Value.OnChunkDataUpdated();
        }
    }

    private void SaveWorld () {
        worldSerializer.SerializeWorld();
    }

    public void CreateChunk (int3 position, ChunkData chunkData) {
        Chunk chunk = new Chunk(material, renderingLayers, new int3(position.x, position.y, position.z));
        chunks.Add(position, chunk);
        chunk.SetChunkData(chunkData);
    }

    public void CreateEmptyChunk (int3 position) {
        Chunk chunk = new Chunk(material, renderingLayers, new int3(position.x, position.y, position.z));
        chunk.SetChunkData(new ChunkData());
        chunks.Add(position, chunk);
    }

    public void DeleteChunk (int3 position) {
        if(chunks.TryGetValue(position, out Chunk value)) {
            chunks.Remove(position);
        }
    }



    public static int3 WorldToChunk (int3 world) {
        return (int3)math.floor((float3)world / inst.chunkSize);
    }
    #endregion
}
