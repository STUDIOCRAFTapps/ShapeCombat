using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;

[RequireComponent(typeof(Camera))]
public class WorldEditor : MonoBehaviour {

    [Header("References")]
    public Transform grid;
    public Transform[] cursors;
    public Transform dragCursor;
    public EditorInventory editorInventory;

    [Header("Parameters")]
    public float defaultDistance = 4f;
    public float maxDistance = 4f;
    public float lineWidth = 0.02f;

    private CursorMode cursorMode = CursorMode.OnTop;

    public int selectedTileAsset = 0;
    private int selectedModel = 0;
    private bool isDragging;
    private bool isPlaneDraggingMode;
    private bool dragCancelled = false;
    private Vector3Int dragEndDelta;
    private float3 previousCameraAxis;
    private Vector3Int cursorPosAtDragStart;
    private byte initialRotCode;
    private byte currentRotCode = 0;

    public TilePrefab selectedTilePrefab;

    private Camera cam;
    private void Start () {
        cam = GetComponent<Camera>();
    }

    void Update () {

        // Position and rotate grid acording to editor movement
        PrepareGrid();

        bool isCursorOverTilePrefab = IsCursorOverTilePrefab(out TilePrefab tilePrefab);
        if((selectedTilePrefab || isCursorOverTilePrefab) && !isDragging) {
            HideAllCursors();

            if(selectedTilePrefab == null) {
                WrapCursorOverTilePrefab(tilePrefab);

                if(Input.GetMouseButtonUp(0)) {
                    selectedTilePrefab = tilePrefab;
                }
                return;
            }
            WrapCursorOverTilePrefab(selectedTilePrefab);

            if(Input.GetKeyUp(KeyCode.Space)) {
                selectedTilePrefab = null;
            }

            return;
        }
        

        #region Mode/Model Selection

        // [X]   : Inset cursor, clear tile
        // [Alt] : Inset curor, replace to selectedTileAsset
        cursorMode = Input.GetKey(KeyCode.X) ? CursorMode.InsetClear : (Input.GetKey(KeyCode.C) ? CursorMode.InsetReplace : CursorMode.OnTop);

        // Hold [C] + Scroll to select different model
        if(Input.GetKey(KeyCode.V)) {
            selectedModel = (int)Mathf.Repeat(selectedModel + Input.mouseScrollDelta.y, cursors.Length);
        }

        // Hold Tab to enter plane drag mode
        isPlaneDraggingMode = Input.GetKey(KeyCode.Tab);
        #endregion

        #region Cursor
        // Raycast to find a position for the cursor and determine if it should be shown
        bool showCursor = GetCursorPos(
            insetRaycast: cursorMode == CursorMode.InsetClear || cursorMode == CursorMode.InsetReplace, 
            out Vector3Int cursorPos, 
            out Vector3 inCellPos, 
            out Vector3 hitNormal);

        // Figure out cursor rotation based on various data
        Vector3 cursorRot = GetCursorRot(
            useAltSelect: Input.GetKey(KeyCode.R),
            inCellPos, hitNormal);

        // Get rotation code from euler rotation
        currentRotCode = GetRotByteFromEuler(cursorRot);

        // Position and rotate cursor
        PrepareCursor(showCursor, cursorPos, cursorRot);
        #endregion

        #region Copy Material
        if(Input.GetMouseButtonDown(2)) {
            bool copyMatCursor = GetCursorPos(
                insetRaycast: true,
                out Vector3Int copyMatCursorPos,
                out Vector3 cpIn,
                out Vector3 cpNor);

            if(copyMatCursor) {
                if(World.inst.TryGetVoxelTile(new int3(copyMatCursorPos.x, copyMatCursorPos.y, copyMatCursorPos.z), out TileData tileData)) {
                    if(tileData.assetId != ushort.MaxValue) {
                        editorInventory.SetSelectionOn(tileData.assetId);
                        selectedTileAsset = tileData.assetId;
                    }
                }
            }
        }
        #endregion

        if(Input.GetKeyDown(KeyCode.Space)) {
            dragCancelled = true;
            isDragging = false;
        }
        if(Input.GetMouseButtonDown(0) && !UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject() && showCursor) {
            cursorPosAtDragStart = cursorPos;
            initialRotCode = currentRotCode;
        }
        if(Input.GetMouseButton(0) & (cursorPosAtDragStart != cursorPos || !showCursor) && !isDragging && showCursor && !dragCancelled) {
            isDragging = true;
        }
        if(isPlaneDraggingMode && isDragging) {
            if(math.any(previousCameraAxis != GetTrueCameraAxis())) {
                previousCameraAxis = GetTrueCameraAxis();
                dragEndDelta = Vector3Int.zero;
            }
            dragEndDelta += Vector3Int.RoundToInt(previousCameraAxis * Input.mouseScrollDelta.y);
            cursorPos += dragEndDelta;
        }
        Vector3Int rectMin = new Vector3Int(Mathf.Min(cursorPosAtDragStart.x, cursorPos.x), Mathf.Min(cursorPosAtDragStart.y, cursorPos.y), Mathf.Min(cursorPosAtDragStart.z, cursorPos.z));
        Vector3Int rectMax = new Vector3Int(Mathf.Max(cursorPosAtDragStart.x, cursorPos.x), Mathf.Max(cursorPosAtDragStart.y, cursorPos.y), Mathf.Max(cursorPosAtDragStart.z, cursorPos.z));
        if(isDragging && showCursor && !dragCancelled) {
            dragCursor.position = rectMin;
            dragCursor.localScale = rectMax - rectMin + Vector3Int.one;
            dragCursor.eulerAngles = Vector3.zero;
        }
        

        if(Input.GetMouseButtonUp(0) && dragCancelled) {
            dragCancelled = false;
        } else if(Input.GetMouseButtonUp(0) && showCursor) {
            if(!isDragging) {
                ExecuteTileActionAt(cursorPos.x, cursorPos.y, cursorPos.z, currentRotCode);
            } else {
                isDragging = false;
                for(int x = rectMin.x; x < rectMax.x + 1; x++) {
                    for(int y = rectMin.y; y < rectMax.y + 1; y++) {
                        for(int z = rectMin.z; z < rectMax.z + 1; z++) {
                            ExecuteTileActionAt(x, y, z, initialRotCode);
                        }
                    }
                }
            }
        }
    }






    // Positions the grid at the right place (follows the player's X and Z position)
    public void PrepareGrid () {
        if(!isPlaneDraggingMode || !isDragging) {
            grid.eulerAngles = Vector3.right * 90f;
            grid.position = new Vector3(Mathf.Round(transform.position.x / 8f) * 8f, 0f, Mathf.Round(transform.position.z / 8f) * 8f);

            return;
        }

        switch(GetCameraAxis()) {
            case 0:
            grid.eulerAngles = Vector3.up * 90f;
            grid.position = new Vector3(cursorPosAtDragStart.x + 1f, Mathf.Round(transform.position.y / 8f) * 8f, Mathf.Round(transform.position.z / 8f) * 8f);
            break;
            case 1:
            grid.eulerAngles = Vector3.right * 90f;
            grid.position = new Vector3(Mathf.Round(transform.position.x / 8f) * 8f, cursorPosAtDragStart.y + 1f, Mathf.Round(transform.position.z / 8f) * 8f);
            break;
            case 2:
            grid.eulerAngles = Vector3.zero;
            grid.position = new Vector3(Mathf.Round(transform.position.x / 8f) * 8f, Mathf.Round(transform.position.y / 8f) * 8f, cursorPosAtDragStart.z + 1f);
            break;
            case 3:
            grid.eulerAngles = Vector3.up * 90f;
            grid.position = new Vector3(cursorPosAtDragStart.x, Mathf.Round(transform.position.y / 8f) * 8f, Mathf.Round(transform.position.z / 8f) * 8f);
            break;
            case 4:
            grid.eulerAngles = Vector3.right * 90f;
            grid.position = new Vector3(Mathf.Round(transform.position.x / 8f) * 8f, cursorPosAtDragStart.y, Mathf.Round(transform.position.z / 8f) * 8f);
            break;
            case 5:
            grid.eulerAngles = Vector3.zero;
            grid.position = new Vector3(Mathf.Round(transform.position.x / 8f) * 8f, Mathf.Round(transform.position.y / 8f) * 8f, cursorPosAtDragStart.z);
            break;
        }
    }


    // Figures out if mouse hit anything and where it hit to place the cursor 
    public bool GetCursorPos (bool insetRaycast, out Vector3Int cursorPos, out Vector3 inCellPos, out Vector3 hitNormal) {

        // If geometry was hit
        Ray ray = cam.ScreenPointToRay(Input.mousePosition);
        if(Physics.Raycast(ray, out RaycastHit hit) && !(isPlaneDraggingMode && isDragging)) {

            Vector3 hitPos = hit.point + (hit.normal * 0.05f * (insetRaycast ? -1 : 1));
            cursorPos = Vector3Int.FloorToInt(hitPos);
            inCellPos = hitPos - cursorPos;
            hitNormal = hit.normal;

            return true;

        // If no geometry was hit
        } else {

            Plane gridPlane = new Plane();
            Vector3 normal = Vector3.up;

            // In plane dragging mode, the only interactable plane is facing away from the prevalent cam axis
            if(!(isPlaneDraggingMode && isDragging)) {
                gridPlane = new Plane(Vector3.up, 0f);
                normal = ((ray.origin.y < 0f) ? Vector3.down : Vector3.up);
            } else {
                int cameraAxis = GetCameraAxis();
                switch(cameraAxis) {
                    case 0:
                    gridPlane = new Plane(Vector3.right, cursorPosAtDragStart);
                    normal = Vector3.right;
                    break;
                    case 3:
                    gridPlane = new Plane(Vector3.left, cursorPosAtDragStart + Vector3.right);
                    normal = Vector3.left;
                    break;
                    case 1:
                    gridPlane = new Plane(Vector3.up, cursorPosAtDragStart);
                    normal = Vector3.up;
                    break;
                    case 4:
                    gridPlane = new Plane(Vector3.down, cursorPosAtDragStart + Vector3.up);
                    normal = Vector3.down;
                    break;
                    case 2:
                    gridPlane = new Plane(Vector3.forward, cursorPosAtDragStart);
                    normal = Vector3.forward;
                    break;
                    case 5:
                    gridPlane = new Plane(Vector3.back, cursorPosAtDragStart + Vector3.forward);
                    normal = Vector3.back;
                    break;
                }
            }


            if(gridPlane.Raycast(ray, out float dist)) {

                // Figure out intersection position
                Vector3 hitPos = ray.origin + ray.direction * dist + (normal * 0.05f * (insetRaycast ? -1 : 1));
                cursorPos = Vector3Int.FloorToInt(hitPos);
                inCellPos = hitPos - cursorPos;
                hitNormal = normal;
                return dist < maxDistance;

            } else {
                cursorPos = Vector3Int.zero;
                inCellPos = Vector3.zero;
                hitNormal = Vector3.zero;
                return false;
            }
        }
    }


    // Prepare cursor
    public void PrepareCursor (bool showCursor, Vector3 cursorPos, Vector3 eulerRotation) {

        // Only show selected cursor, if not dragging
        for(int i = 0; i < cursors.Length; i++) {
            if(cursors[i].gameObject.activeSelf != (i == selectedModel && !isDragging && showCursor)) {
                cursors[i].gameObject.SetActive(i == selectedModel && !isDragging && showCursor);
            }
        }

        // Set dragging cursor active if dragging only
        if(dragCursor.gameObject.activeSelf != (isDragging && showCursor && !dragCancelled)) {
            dragCursor.gameObject.SetActive(isDragging && showCursor && !dragCancelled);
        }
        
        // Rotate and position correctly the selected model only
        cursors[selectedModel].eulerAngles = eulerRotation;
        cursors[selectedModel].position = cursorPos + Vector3.one * 0.5f;

        // Scale line width to keep same size no matter the distance
        float distance = Vector3.Distance(cam.transform.position, cursors[selectedModel].transform.position);
        float width = distance * lineWidth;
        foreach(LineRenderer line in cursors[selectedModel].GetComponentsInChildren(typeof(LineRenderer))) {
            line.widthMultiplier = width;
        }

        // Scale dragging cursor while taking into account the different point's position
        foreach(LineRenderer line in dragCursor.GetComponentsInChildren(typeof(LineRenderer))) {
            float startDistance = Vector3.Distance(cam.transform.position, Vector3.Scale(line.GetPosition(0), dragCursor.localScale) + dragCursor.position) * lineWidth;
            float endDistance = Vector3.Distance(cam.transform.position, Vector3.Scale(line.GetPosition(line.positionCount - 1), dragCursor.localScale) + dragCursor.position) * lineWidth;
            line.startWidth = startDistance;
            line.endWidth = endDistance;
            line.widthMultiplier = 1f;
        }
    }


    // Wraps cursor over given tile prefab
    public void WrapCursorOverTilePrefab (TilePrefab tilePrefab) {
        Transform tPtr = tilePrefab.transform;
        BoxCollider tPCol = tilePrefab.GetComponent<BoxCollider>();

        dragCursor.position = tPtr.position + tPCol.center - tPCol.size * 0.5f;
        dragCursor.localScale = tPCol.size;
        dragCursor.eulerAngles = tPtr.eulerAngles;
    }


    // Hides all cursors
    public void HideAllCursors () {

        // Only show selected cursor, if not dragging
        for(int i = 0; i < cursors.Length; i++) {
            if(cursors[i].gameObject.activeSelf != false) {
                cursors[i].gameObject.SetActive(false);
            }
        }

        // Set dragging cursor active if dragging only
        if(dragCursor.gameObject.activeSelf != true) {
            dragCursor.gameObject.SetActive(true);
        }
        
        // Scale dragging cursor while taking into account the different point's position
        foreach(LineRenderer line in dragCursor.GetComponentsInChildren(typeof(LineRenderer))) {
            float startDistance = Vector3.Distance(cam.transform.position, Vector3.Scale(line.GetPosition(0), dragCursor.localScale) + dragCursor.position) * lineWidth;
            float endDistance = Vector3.Distance(cam.transform.position, Vector3.Scale(line.GetPosition(line.positionCount - 1), dragCursor.localScale) + dragCursor.position) * lineWidth;
            line.startWidth = startDistance;
            line.endWidth = endDistance;
            line.widthMultiplier = 1f;
        }
    }


    // Figure out cursor rotation
    public Vector3 GetCursorRot (bool useAltSelect, Vector3 inCellPos, Vector3 normal) {
        Vector3 rotationEuler = Vector3.zero;

        switch(selectedModel) {

            case (int)ModelType.HalfCube:
            if(useAltSelect) {
                if(Mathf.Approximately(normal.y, 1f)) {
                    rotationEuler = new Vector3(0f, GetRotFromSplit(inCellPos.x, inCellPos.z), 90f);
                } else if(Mathf.Approximately(normal.y, -1f)) {
                    rotationEuler = new Vector3(0f, GetRotFromSplit(inCellPos.x, inCellPos.z), 90f);
                } else if(Mathf.Approximately(normal.x, 1f)) {
                    rotationEuler = new Vector3(GetRotFromSplit(inCellPos.z, inCellPos.y) - 90f, 0f, 0f);
                } else if(Mathf.Approximately(normal.x, -1f)) {
                    rotationEuler = new Vector3(GetRotFromSplit(inCellPos.z, inCellPos.y) - 90f, 0f, 0f);
                } else if(Mathf.Approximately(normal.z, 1f)) {
                    rotationEuler = new Vector3(0f, 0f, GetRotFromSplit(inCellPos.x, 1f - inCellPos.y) + 90f);
                } else if(Mathf.Approximately(normal.z, -1f)) {
                    rotationEuler = new Vector3(0f, 0f, GetRotFromSplit(inCellPos.x, 1f - inCellPos.y) + 90f);
                }
            } else {
                if(Mathf.Approximately(normal.y, 1f)) {
                    rotationEuler = new Vector3(0f, 0f, 0f);
                } else if(Mathf.Approximately(normal.y, -1f)) {
                    rotationEuler = new Vector3(0f, 0f, 180f);
                } else if(Mathf.Approximately(normal.x, 1f)) {
                    rotationEuler = new Vector3(0f, 0f, 270f);
                } else if(Mathf.Approximately(normal.x, -1f)) {
                    rotationEuler = new Vector3(0f, 0f, 90f);
                } else if(Mathf.Approximately(normal.z, 1f)) {
                    rotationEuler = new Vector3(90f, 0f, 0f);
                } else if(Mathf.Approximately(normal.z, -1f)) {
                    rotationEuler = new Vector3(270f, 0f, 0f);
                }
            }
            break;

            case (int)ModelType.QuaterCube:
            if(!useAltSelect) {
                if(Mathf.Approximately(normal.y, 1f)) {
                    rotationEuler = new Vector3(0f, GetRotFromSplit(inCellPos.x, inCellPos.z), 90f); //got it
                } else if(Mathf.Approximately(normal.y, -1f)) {
                    rotationEuler = new Vector3(0f, GetRotFromSplit(inCellPos.x, inCellPos.z), 180f); // got it
                } else if(Mathf.Approximately(normal.x, 1f)) {
                    rotationEuler = new Vector3(GetRotFromSplit(inCellPos.z, inCellPos.y) - 90f, 0f, 0f); //got it
                } else if(Mathf.Approximately(normal.x, -1f)) {
                    rotationEuler = new Vector3(GetRotFromSplit(inCellPos.z, 1f - inCellPos.y) + 90f, 180f, 0f); //all wrong
                } else if(Mathf.Approximately(normal.z, 1f)) {
                    rotationEuler = new Vector3(GetRotFromSplit(1f - inCellPos.x, 1f - inCellPos.y) - 90f, 90f, 180f); //x flip
                } else if(Mathf.Approximately(normal.z, -1f)) {
                    rotationEuler = new Vector3(GetRotFromSplit(inCellPos.x, inCellPos.y) - 90f, 90f, 0f); //y flip
                }
            } else {
                if(Mathf.Approximately(normal.y, 1f)) {
                    rotationEuler = new Vector3(90f, GetRotFromQuartile(inCellPos.x, inCellPos.z), 0f);
                } else if(Mathf.Approximately(normal.y, -1f)) {
                    rotationEuler = new Vector3(90f, GetRotFromQuartile(inCellPos.x, inCellPos.z), 0f);
                } else if(Mathf.Approximately(normal.x, 1f)) {
                    rotationEuler = new Vector3(0f, 90f, GetRotFromQuartile(inCellPos.y, 1 - inCellPos.z));
                } else if(Mathf.Approximately(normal.x, -1f)) {
                    rotationEuler = new Vector3(0f, 90f, GetRotFromQuartile(inCellPos.y, 1 - inCellPos.z));
                } else if(Mathf.Approximately(normal.z, 1f)) {
                    rotationEuler = new Vector3(0f, 0f, GetRotFromQuartile(inCellPos.y, inCellPos.x));
                } else if(Mathf.Approximately(normal.z, -1f)) {
                    rotationEuler = new Vector3(0f, 0f, GetRotFromQuartile(inCellPos.y, inCellPos.x));
                }
            }
            break;

            case (int)ModelType.EigthCube:
            if((inCellPos.y < .5f && !useAltSelect) || (inCellPos.y >= .5f && useAltSelect)) {
                rotationEuler = new Vector3(0f, GetRotFromQuartile(inCellPos.x, inCellPos.z), 0f);
            } else {
                rotationEuler = new Vector3(90f, GetRotFromQuartile(inCellPos.x, inCellPos.z), 0f);
            }
            break;

            case (int)ModelType.StairTwoStep:
            if(!useAltSelect) {
                if(Mathf.Approximately(normal.y, 1f)) {
                    rotationEuler = new Vector3(0f, GetRotFromSplit(inCellPos.x, inCellPos.z), 90f); //got it
                } else if(Mathf.Approximately(normal.y, -1f)) {
                    rotationEuler = new Vector3(0f, GetRotFromSplit(inCellPos.x, inCellPos.z), 180f); // got it
                } else if(Mathf.Approximately(normal.x, 1f)) {
                    rotationEuler = new Vector3(GetRotFromSplit(inCellPos.z, inCellPos.y) - 90f, 0f, 0f); //got it
                } else if(Mathf.Approximately(normal.x, -1f)) {
                    rotationEuler = new Vector3(GetRotFromSplit(inCellPos.z, 1f - inCellPos.y) + 90f, 180f, 0f); //all wrong
                } else if(Mathf.Approximately(normal.z, 1f)) {
                    rotationEuler = new Vector3(GetRotFromSplit(1f - inCellPos.x, 1f - inCellPos.y) - 90f, 90f, 180f); //x flip
                } else if(Mathf.Approximately(normal.z, -1f)) {
                    rotationEuler = new Vector3(GetRotFromSplit(inCellPos.x, inCellPos.y) - 90f, 90f, 0f); //y flip
                }
            } else {
                if(Mathf.Approximately(normal.y, 1f)) {
                    rotationEuler = new Vector3(90f, GetRotFromQuartile(inCellPos.x, inCellPos.z), 0f);
                } else if(Mathf.Approximately(normal.y, -1f)) {
                    rotationEuler = new Vector3(90f, GetRotFromQuartile(inCellPos.x, inCellPos.z), 0f);
                } else if(Mathf.Approximately(normal.x, 1f)) {
                    rotationEuler = new Vector3(0f, 90f, GetRotFromQuartile(inCellPos.y, 1 - inCellPos.z));
                } else if(Mathf.Approximately(normal.x, -1f)) {
                    rotationEuler = new Vector3(0f, 90f, GetRotFromQuartile(inCellPos.y, 1 - inCellPos.z));
                } else if(Mathf.Approximately(normal.z, 1f)) {
                    rotationEuler = new Vector3(0f, 0f, GetRotFromQuartile(inCellPos.y, inCellPos.x));
                } else if(Mathf.Approximately(normal.z, -1f)) {
                    rotationEuler = new Vector3(0f, 0f, GetRotFromQuartile(inCellPos.y, inCellPos.x));
                }
            }
            break;
        }

        return rotationEuler;
    }


    // Either place tile, clear tile, or replace tile based on the different modes and selected material
    public void ExecuteTileActionAt (int x, int y, int z, byte usedRotCode) {
        if(cursorMode == CursorMode.InsetClear) {
            World.inst.ClearVoxelTile(new int3(x, y, z));
        } else {
            World.inst.SetVoxelTile(new int3(x, y, z), (ushort)selectedTileAsset, (byte)selectedModel, usedRotCode);
        }
    }


    // Get Camera Axis (Either XYZ -> 012, -XYZ -> 345)
    int GetCameraAxis () {
        if(math.abs(transform.forward.x) >= math.abs(transform.forward.y) && math.abs(transform.forward.x) >= math.abs(transform.forward.z)) {
            return transform.forward.x > 0 ? 0 : 3;
        } else if(math.abs(transform.forward.y) >= math.abs(transform.forward.z)) {
            return transform.forward.y > 0 ? 1 : 4;
        } else {
            return transform.forward.z > 0 ? 2 : 5;
        }
    }


    // Check if cursor is over tile prefab
    public bool IsCursorOverTilePrefab (out TilePrefab tilePrefab) {
        Ray ray = cam.ScreenPointToRay(Input.mousePosition);
        if(Physics.Raycast(ray, out RaycastHit hit) && !isDragging) {
            
            if(hit.collider.tag == "TilePrefab") {
                tilePrefab = hit.collider.GetComponent<TilePrefab>();
                return true;
            } else {
                tilePrefab = null;
                return false;
            }
        }
        tilePrefab = null;
        return false;
    }

    // Get Camera Axis
    float3 GetTrueCameraAxis () {
        if(math.abs(transform.forward.x) >= math.abs(transform.forward.y) && math.abs(transform.forward.x) >= math.abs(transform.forward.z)) {
            return transform.forward.x > 0 ? new float3(1, 0, 0) : new float3(-1, 0, 0);
        } else if(math.abs(transform.forward.y) >= math.abs(transform.forward.z)) {
            return transform.forward.y > 0 ? new float3(0, 1, 0) : new float3(0, -1, 0);
        } else {
            return transform.forward.z > 0 ? new float3(0, 0, 1) : new float3(0, 0, -1);
        }
    }

    // Get Camera Axis
    float3 GetPlaneCameraAxis () {
        if(math.abs(transform.forward.x) >= math.abs(transform.forward.y) && math.abs(transform.forward.x) >= math.abs(transform.forward.z)) {
            return transform.forward.x > 0 ? new float3(1, 0, 0) : new float3(-1, 0, 0);
        } else if(math.abs(transform.forward.y) >= math.abs(transform.forward.z)) {
            return transform.forward.y > 0 ? new float3(0, 1, 0) : new float3(0, -1, 0);
        } else {
            return transform.forward.z > 0 ? new float3(0, 0, 1) : new float3(0, 0, -1);
        }
    }

    #region Utils
    private static float GetRotFromQuartile (float x, float y) {
        if(x < .5f) {
            if(y < .5f)
                return 0f;
            else if(y >= .5f)
                return 90f;
        } else if (x >= .5f) {
            if(y < .5f)
                return 270f;
            else if(y >= .5f)
                return 180f;
        }
        return 0f;
    }

    private static float GetRotFromSplit (float x, float y) {
        float _x = x * 2f - 1f;
        float _y = y * 2f - 1f;

        if(_x > math.abs(_y)) {
            return 0f;
        } else if(-_x > math.abs(_y)) {
            return 180f;
        } else if(_y > math.abs(_x)) {
            return 270f;
        } else if(-_y > math.abs(_x)) {
            return 90f;
        }
        return 0f;
    }

    private static byte GetRotByteFromEuler (Vector3 euler) {
        byte rotX = (byte)(Mathf.Repeat(Mathf.RoundToInt(Mathf.Repeat(euler.x, 360f) / 90f), 360f));
        byte rotY = (byte)(Mathf.Repeat(Mathf.RoundToInt(Mathf.Repeat(euler.y, 360f) / 90f), 360f));
        byte rotZ = (byte)(Mathf.Repeat(Mathf.RoundToInt(Mathf.Repeat(euler.z, 360f) / 90f), 360f));
        return (byte)(rotX | (rotY << 2) | (rotZ << 4));
    }

    private static int3 Vec3IntToInt3 (Vector3Int p) => new int3(p.x, p.y, p.z);
    #endregion
}

public enum CursorMode {
    OnTop,
    InsetReplace,
    InsetClear
}