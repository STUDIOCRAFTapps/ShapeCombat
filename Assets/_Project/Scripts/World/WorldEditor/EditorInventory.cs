using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class EditorInventory : MonoBehaviour {

    public WorldEditor worldEditor;
    public Transform prefabParent;
    public RectTransform templateItem;
    public RectTransform templateItemPrefab;
    public RectTransform selectionIndicator;
    public RectTransform selectionIndicatorPrefab;
    private RectTransform[] tiles;
    private RectTransform[] tilePrefabs;

    void Start () {
        tiles = new RectTransform[World.inst.tileCollection.tileAssets.Length];
        tilePrefabs = new RectTransform[World.inst.tilePrefabCollection.tilePrefabs.Length];
        
        for(int i = 0; i < World.inst.tileCollection.tileAssets.Length; i++) {
            World.inst.tileCollection[i].GetPreviewSprites(out Sprite top, out Sprite side);

            RectTransform tile = Instantiate(templateItem, transform);
            tiles[i] = tile;
            tile.GetChild(0).GetComponent<Image>().sprite = top;
            tile.GetChild(1).GetComponent<Image>().sprite = side;
            tile.gameObject.SetActive(true);

            int index = i;
            tile.GetChild(1).GetComponent<Button>().onClick.AddListener(() => {
                SetSelectionOn(index);
                worldEditor.selectedTileAsset = index;
            });
        }

        for(int i = 0; i < World.inst.tilePrefabCollection.tilePrefabs.Length; i++) {

            RectTransform tile = Instantiate(templateItemPrefab, prefabParent);
            tilePrefabs[i] = tile;
            tile.GetChild(0).GetComponent<Image>().sprite = World.inst.tilePrefabCollection[i].icon;
            tile.gameObject.SetActive(true);

            int index = i;
            tile.GetChild(0).GetComponent<Button>().onClick.AddListener(() => {
                SetSelectionOnPrefab(index);
                worldEditor.selectedTilePrefabAsset = index;
            });
        }
        SetSelectionOn(0);
        SetSelectionOnPrefab(0);
    }

    public void SetSelectionOn (int i) {
        selectionIndicator.position = tiles[i].position - new Vector3(2f, -2f);
    }

    public void SetSelectionOnPrefab (int i) {
        selectionIndicatorPrefab.position = tilePrefabs[i].position - new Vector3(2f, -2f);
    }

    private void Update () {
        if(worldEditor.isVoxelMode != selectionIndicator.gameObject.activeSelf)
            selectionIndicator.gameObject.SetActive(worldEditor.isVoxelMode);
        if(!worldEditor.isVoxelMode != selectionIndicatorPrefab.gameObject.activeSelf)
            selectionIndicatorPrefab.gameObject.SetActive(!worldEditor.isVoxelMode);

        if(!Input.GetKey(KeyCode.V) && !Input.GetKey(KeyCode.Tab)) {
            if(Input.mouseScrollDelta.y != 0f && worldEditor.isVoxelMode) {
                worldEditor.selectedTileAsset = Mathf.RoundToInt(Mathf.Repeat(worldEditor.selectedTileAsset + Input.mouseScrollDelta.y, World.inst.tileCollection.tileAssets.Length));
                SetSelectionOn(worldEditor.selectedTileAsset);
            }
            if(Input.mouseScrollDelta.y != 0f && !worldEditor.isVoxelMode) {
                worldEditor.selectedTilePrefabAsset = Mathf.RoundToInt(Mathf.Repeat(worldEditor.selectedTilePrefabAsset + Input.mouseScrollDelta.y, World.inst.tilePrefabCollection.tilePrefabs.Length));
                SetSelectionOnPrefab(worldEditor.selectedTilePrefabAsset);
            }
        }
    }
}
