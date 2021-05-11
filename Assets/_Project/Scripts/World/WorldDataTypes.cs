using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;
using System.IO;

public struct TileData {
    public ushort assetId;
    public byte model;
    public byte rotation;

    public TileData (ushort assetId, byte rotation, byte model) {
        this.assetId = assetId;
        this.rotation = rotation;
        this.model = model;
    }

    public static TileData ReadTile (BinaryReader reader) {
        ushort assetId = reader.ReadUInt16();
        if(assetId == ushort.MaxValue)
            return new TileData(ushort.MaxValue, 0, 0);
        TileAsset tileAsset = World.inst.tileCollection[assetId];
        byte model = reader.ReadByte();
        if(model == (byte)ModelType.Cube)
            return new TileData(assetId, 0, model);
        return new TileData(assetId, reader.ReadByte(), model);
    }

    public void WriteTile (BinaryWriter writer) {
        writer.Write(assetId);
        if(IsAirTile)
            return;
        writer.Write(model);
        if(model != (byte)ModelType.Cube)
            writer.Write(rotation);
    }

    public bool IsAirTile {
        get { return assetId == ushort.MaxValue; }
    }
}

public struct TilePrefabData {
    public ushort assetId;
    public float3 position;
    public float3 rotation;

    public TilePrefabData (ushort assetId, float3 position, float3 rotation) {
        this.assetId = assetId;
        this.position = position;
        this.rotation = rotation;
    }

    public static TilePrefabData ReadTilePrefab (BinaryReader reader) {
        ushort assetId = reader.ReadUInt16();
        float3 position = new float3(
            reader.ReadSingle(),
            reader.ReadSingle(),
            reader.ReadSingle());
        float3 rotation = new float3(
            reader.ReadSingle(),
            reader.ReadSingle(),
            reader.ReadSingle());
        return new TilePrefabData(assetId, position, rotation);
    }

    public void WriteTilePrefab (BinaryWriter writer) {
        writer.Write(assetId);
        writer.Write(position.x);
        writer.Write(position.y);
        writer.Write(position.z);
        writer.Write(rotation.x);
        writer.Write(rotation.y);
        writer.Write(rotation.z);
    }
}

public enum ModelType {
    Cube,
    HalfCube,
    QuaterCube,
    EigthCube,
    StairTwoStep
}
