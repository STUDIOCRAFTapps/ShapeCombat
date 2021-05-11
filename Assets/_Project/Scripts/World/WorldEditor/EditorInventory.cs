using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class EditorInventory : MonoBehaviour {

    public WorldEditor worldEditor;
    public RectTransform templateItem;
    public RectTransform selectionIndicator;
    private RectTransform[] tiles;

    void Start () {
        tiles = new RectTransform[World.inst.tileCollection.tileAssets.Length];
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
        SetSelectionOn(0);
    }

    private void SetSelectionOn (int i) {
        selectionIndicator.position = tiles[i].position - new Vector3(2f, -2f);
    }

    private void Update () {
        if(!Input.GetKey(KeyCode.V)) {
            if(Input.mouseScrollDelta.y != 0f) {
                worldEditor.selectedTileAsset = Mathf.RoundToInt(Mathf.Repeat(worldEditor.selectedTileAsset + Input.mouseScrollDelta.y, World.inst.tileCollection.tileAssets.Length));
                SetSelectionOn(worldEditor.selectedTileAsset);
            }
        }
    }
}
