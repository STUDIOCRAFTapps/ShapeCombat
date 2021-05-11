using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "TileAsset", menuName = "Custom/TileAsset", order = -1)]
public class TileAsset : ScriptableObject {

    new public string name;
    public TexturingType texturingType;
    public Sprite[] textures;

    [HideInInspector] public int[] textureIndexToAtlasIndex;

    public readonly static int[] getModelVertCount = {
        36,
        36,
        36,
        36,
        72
    };

    public TileAssetJobData GetTileAssetJobData () {
        TileAssetJobData data = new TileAssetJobData(
            texturingType,
            textureIndexToAtlasIndex);

        return data;
    }

    public void GetPreviewSprites (out Sprite top, out Sprite side) {
        top = null;
        side = null;
        switch(texturingType) {
            case TexturingType.AllSame:
            top = textures[0];
            side = textures[0];
            break;
            case TexturingType.TopAndSideAndBottom:
            top = textures[0];
            side = textures[1];
            break;
            case TexturingType.TopBottomAndSide:
            top = textures[1];
            side = textures[0];
            break;
            case TexturingType.AllSeperatedXYZ:
            top = textures[2];
            side = textures[0];
            break;
        }
    }
}

public enum TexturingType {
    AllSame,
    TopAndSideAndBottom,
    TopBottomAndSide,
    AllSeperatedXYZ
}

public struct TileAssetJobData {
    public byte texturingType;
    public ushort textureIndex0;
    public ushort textureIndex1;
    public ushort textureIndex2;
    public ushort textureIndex3;
    public ushort textureIndex4;
    public ushort textureIndex5;
    public ushort textureIndex6;
    public ushort textureIndex7;

    public TileAssetJobData (TexturingType texturingType, params int[] textureIndices) : this() {
        this.texturingType = (byte)texturingType;

        switch(texturingType) {
            case TexturingType.AllSame:
            SetTextureIndex(0, textureIndices[0]);
            SetTextureIndex(1, textureIndices[0]);
            SetTextureIndex(2, textureIndices[0]);
            SetTextureIndex(3, textureIndices[0]);
            SetTextureIndex(4, textureIndices[0]);
            SetTextureIndex(5, textureIndices[0]);
            break;
            case TexturingType.TopAndSideAndBottom:
            SetTextureIndex(0, textureIndices[1]);
            SetTextureIndex(1, textureIndices[1]);
            SetTextureIndex(2, textureIndices[0]);
            SetTextureIndex(3, textureIndices[2]);
            SetTextureIndex(4, textureIndices[1]);
            SetTextureIndex(5, textureIndices[1]);
            break;
            case TexturingType.TopBottomAndSide:
            SetTextureIndex(0, textureIndices[0]);
            SetTextureIndex(1, textureIndices[0]);
            SetTextureIndex(2, textureIndices[1]);
            SetTextureIndex(3, textureIndices[1]);
            SetTextureIndex(4, textureIndices[0]);
            SetTextureIndex(5, textureIndices[0]);
            break;
            case TexturingType.AllSeperatedXYZ:
            SetTextureIndex(0, textureIndices[0]);
            SetTextureIndex(1, textureIndices[1]);
            SetTextureIndex(2, textureIndices[2]);
            SetTextureIndex(3, textureIndices[3]);
            SetTextureIndex(4, textureIndices[4]);
            SetTextureIndex(5, textureIndices[5]);
            break;
        }
    }

    public void SetTextureIndex (int index, int value) {
        SetTextureIndex(index, (ushort)value);
    }

    public void SetTextureIndex (int index, ushort value) {
        switch(index) {
            case 0:
            textureIndex0 = value;
            break;
            case 1:
            textureIndex1 = value;
            break;
            case 2:
            textureIndex2 = value;
            break;
            case 3:
            textureIndex3 = value;
            break;
            case 4:
            textureIndex4 = value;
            break;
            case 5:
            textureIndex5 = value;
            break;
            case 6:
            textureIndex6 = value;
            break;
            case 7:
            textureIndex7 = value;
            break;
        }
    }

    public ushort GetTextureIndex (int index) {
        switch(index) {
            case 0:
            return textureIndex0;
            case 1:
            return textureIndex1;
            case 2:
            return textureIndex2;
            case 3:
            return textureIndex3;
            case 4:
            return textureIndex4;
            case 5:
            return textureIndex5;
            case 6:
            return textureIndex6;
            case 7:
            return textureIndex7;
        }
        return 0;
    }
}
