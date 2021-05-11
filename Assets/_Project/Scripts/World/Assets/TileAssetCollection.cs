using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Collections;

[CreateAssetMenu(fileName = "TileAssetCollection", menuName = "Custom/TileAssetCollection", order = -1)]
public class TileAssetCollection : ScriptableObject, System.IDisposable {

    public TileAsset[] tileAssets;
    public Texture2DArray textures;
    public NativeArray<TileAssetJobData> tileAssetJobDatas;

    private const int tileSize = 16;

    public void Init () {
        int textureCount = 0;
        foreach(TileAsset tile in tileAssets) {
            textureCount += tile.textures.Length;
        }

        textures = new Texture2DArray(tileSize, tileSize, textureCount, TextureFormat.ARGB32, false);
        textures.filterMode = FilterMode.Point;

        // Creates a dictionnairy that checks for potential texture duplicates to merge them.
        Dictionary<int, int> spriteAssetIdToTextureAtlas = new Dictionary<int, int>();

        // Creates of atlas of textures
        int textureIndex = 0;
        Color[] colorBuffer = new Color[tileSize * tileSize];
        foreach(TileAsset tile in tileAssets) {
            tile.textureIndexToAtlasIndex = new int[tile.textures.Length];
            for(int t = 0; t < tile.textures.Length; t++) {

                // Checking for potential texture duplicates
                if(spriteAssetIdToTextureAtlas.TryGetValue(tile.textures[t].GetInstanceID(), out int textureAtlasIndex)) {
                    tile.textureIndexToAtlasIndex[t] = textureAtlasIndex;
                } else {

                    //Writing the sprite to the atlas
                    WriteSpriteToColorBuffer(
                        colorBuffer, tileSize,
                        SpriteToTexture(tile.textures[t]).GetPixels(),
                        Mathf.CeilToInt(tile.textures[t].textureRect.width),
                        Mathf.CeilToInt(tile.textures[t].textureRect.height)
                    );
                    textures.SetPixels(colorBuffer, textureIndex);
                    tile.textureIndexToAtlasIndex[t] = textureIndex;
                    spriteAssetIdToTextureAtlas.Add(tile.textures[t].GetInstanceID(), textureIndex);
                    textureIndex++;
                }
            }
        }
        textures.Apply();

        tileAssetJobDatas = new NativeArray<TileAssetJobData>(tileAssets.Length, Allocator.Persistent);
        for(int i = 0; i < tileAssets.Length; i++) {
            tileAssetJobDatas[i] = tileAssets[i].GetTileAssetJobData();
        }
    }

    public void Dispose () {
        tileAssetJobDatas.Dispose();
    }

    public TileAsset this[int i] {
        get {
            return tileAssets[i];
        }
    }

    #region Utils
    public static Texture2D SpriteToTexture (Sprite sprite) {
        Texture2D texture = new Texture2D(Mathf.CeilToInt(sprite.textureRect.width), Mathf.CeilToInt(sprite.textureRect.height), TextureFormat.RGBA32, false);
        if(!texture.isReadable) {
            Debug.LogError($"The texture \"{texture.name}\" is not readable.");
        }
        Color[] newColors = sprite.texture.GetPixels(
            Mathf.FloorToInt(sprite.textureRect.x),
            Mathf.FloorToInt(sprite.textureRect.y),
            Mathf.CeilToInt(sprite.textureRect.width),
            Mathf.CeilToInt(sprite.textureRect.height)
        );
        texture.SetPixels(newColors);
        texture.Apply();
        return texture;
    }

    public static void WriteSpriteToColorBuffer (Color[] colorBuffer, int bufferWidth, Color[] sprite, int spriteWidth, int spriteHeight) {
        for(int x = 0; x < spriteWidth; x++) {
            for(int y = 0; y < spriteHeight; y++) {
                colorBuffer[x + y * bufferWidth] = sprite[x + y * spriteWidth];
            }
        }
    }
    #endregion
}
