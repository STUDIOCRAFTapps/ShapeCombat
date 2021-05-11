using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;
using System.IO;

public class WorldSerializer : MonoBehaviour {
    public string assetSerializationPath;
    public string currentMapName;
    private const string worldExtension = "dat";

    /* 
     * The world file is composed of:
     * Chunks
     * -- Chunks count
     * -- -- Chunk pos
     * -- -- -- Chunk pos X
     * -- -- -- Chunk pos Y
     * -- -- -- Chunk pos Z
     * -- -- Tile data
     * -- -- Tile Prefabs count
     * -- -- -- Prefab data
     */

    public void DeserializeWorld () {
        string worldFilePath = Path.Combine(Application.dataPath, assetSerializationPath, $"{currentMapName}.{worldExtension}");

        if(!File.Exists(worldFilePath))
            return;

        using(FileStream fs = new FileStream(worldFilePath, FileMode.Open))
        using(BinaryReader reader = new BinaryReader(fs)) {
            int chunkCount = reader.ReadInt32();

            for(int i = 0; i < chunkCount; i++) {
                int3 chunkPosition = new int3(reader.ReadInt32(), reader.ReadInt32(), reader.ReadInt32());

                ChunkData chunkData = new ChunkData();
                chunkData.DeserializeChunkData(reader);
                World.inst.CreateChunk(chunkPosition, chunkData);
            }
        }
    }

    public void SerializeWorld () {
        string worldFilePath = Path.Combine(Application.dataPath, assetSerializationPath, $"{currentMapName}.{worldExtension}");

        using(FileStream fs = new FileStream(worldFilePath, FileMode.Create))
        using(BinaryWriter writer = new BinaryWriter(fs)) {
            writer.Write(World.inst.chunks.Count);

            foreach(KeyValuePair<int3, Chunk> kvp in World.inst.chunks) {
                writer.Write(kvp.Value.position.x);
                writer.Write(kvp.Value.position.y);
                writer.Write(kvp.Value.position.z);

                kvp.Value.chunkData.SerializeChunkData(writer);
            }
        }
    }
}
