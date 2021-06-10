using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Collections;

public class JobHandleWithData<T> where T : IJob {
    public JobHandle JobHandle { get; set; }
    public T JobData { get; set; }
}

public class PathFindingJobHandleWithData {
    public JobHandle handle { get; set; }
    public PathfindingJob data { get; set; }
    public PathfindingManager.OnPathRequestFinished callOnceEnded { get; set; }
}

public struct PathfindingJob : IJob {

    // Context
    [ReadOnly] public NativeHashMap<int3, BlitableArray<TileData>> _chunkDataNative;
    [ReadOnly] public int3 startPos;
    [ReadOnly] public int3 endPos;
    [ReadOnly] public int chunkSize;

    // Processing
    public NativeBinaryHeap<NodeCost> _open;
    public NativeHashMap<int3, NodeCost> _closed;

    // Output
    public NativeArray<float3> _path;
    public NativeArray<int> _results;


    public void Execute () {
        _results[0] = 0; //IsValid
        _results[1] = 0; //NodeCount


        bool isStartClear = CheckIfAreaClear(startPos);
        bool isEndClear = CheckIfTileAir(endPos);

        if(!isStartClear || !isEndClear) {
            //return;
        }

        if(math.all(startPos == endPos)) {
            _results[0] = 1;
            _results[1] = 2;
            _path[0] = startPos;
            _path[1] = endPos;
            return;
        }

        NodeCost currentNode = new NodeCost(startPos, startPos);
        currentNode.hCost = NodeDistance(startPos.xz, endPos.xz);
        _open.Add(currentNode);

        int limit = _closed.Capacity;
        int stepIndex = 0;
        bool destinationReached = false;

        int smallestNodeCost = int.MaxValue;
        int3 smallestNodePos = int3.zero;

        while(_open.Count > 0) {
            currentNode = _open.RemoveFirst();

            if(!_closed.TryAdd(currentNode.idx, currentNode)) { // Why stop in this case? -- No more cells to explore?
                break;
            } else {
                if(currentNode.hCost < smallestNodeCost) {
                    smallestNodeCost = currentNode.hCost;
                    smallestNodePos = currentNode.idx;
                }
            }
            if(math.all(currentNode.idx == endPos)) { // We reached our destination
                destinationReached = true;
                break;
            }

            // Check all neighboors
            for(int xC = -1; xC <= 1; xC++) {
                for(int zC = -1; zC <= 1; zC++) {
                    int neighboorType = math.abs(xC) + math.abs(zC);
                    if(neighboorType == 0)
                        continue;

                    int3 newIdx = currentNode.idx + new int3(xC, 0, zC);

                    // Handle diagonal space checks
                    if(neighboorType == 2) {
                        bool diagClear = CheckIfAreaClear(newIdx);
                        bool sideClearA = CheckIfAreaClear(currentNode.idx + new int3(xC, 0, 0));
                        bool sideClearB = CheckIfAreaClear(currentNode.idx + new int3(0, 0, zC));

                        // All places must be clear to go diagonally
                        if(!diagClear || !sideClearA || !sideClearB) {
                            continue;
                        }

                    //Handle axis space checks
                    } else {
                        // If the neighboor is only available up or down, raise the node
                        // Check if the space hasn't been opened yet and if it's free.
                        bool mainClear = CheckIfAreaClear(newIdx);
                        bool downClear = CheckIfAreaClearDown(newIdx);
                        bool upClear = CheckIfAreaClearUp(newIdx) && CheckIfAreaClearToGoUp(currentNode.idx);
                        if(!mainClear && !downClear && !upClear) { // No place clear
                            continue;
                        } else if(!mainClear && upClear) { // Up only is clear
                            newIdx.y++;
                        } else if(!mainClear && downClear) { // Down only is clear
                            newIdx.y--;
                        }
                    }

                    if(_closed.TryGetValue(newIdx, out _)) {
                        continue;
                    }
                    NodeCost newCost = new NodeCost(newIdx, currentNode.idx);

                    int newGCost = currentNode.gCost + NodeDistance(currentNode.idx.xz, newIdx.xz);

                    newCost.gCost = newGCost;
                    newCost.hCost = NodeDistance(newIdx.xz, endPos.xz);



                    int oldIdx = _open.IndexOf(newCost);
                    if(oldIdx >= 0) {
                        if(newGCost < _open[oldIdx].gCost) {
                            _open.RemoveAt(oldIdx);
                            _open.Add(newCost);

                        }
                    } else {
                        if(_open.Count < _open.Capacity) {
                            _open.Add(newCost);
                        } else {
                            break;
                        }
                    }
                }
            }

            limit--;
            stepIndex++;
            if(limit == -1) {
                break;
            }
        }

        if(!destinationReached) {
            if(_closed.TryGetValue(smallestNodePos, out NodeCost item)) {
                currentNode = item;
            } else {
                _results[0] = 0; //IsValid
                return;
            }
        }

        _results[0] = 1; //IsValid
        int retraceIndex = 0;
        while(!math.all(currentNode.idx == currentNode.origin)) {
            _path[retraceIndex] = currentNode.idx;
            retraceIndex++;
            if(!_closed.TryGetValue(currentNode.origin, out NodeCost next)) {
                _results[0] = 1;
                _results[1] = retraceIndex; //NodeCount
                return;
            }
            currentNode = next;
        }
        _results[1] = retraceIndex; //NodeCount
    }

    int NodeDistance (int2 nodeA, int2 nodeB) {
        int2 d = nodeA - nodeB;
        int distx = math.abs(d.x);
        int disty = math.abs(d.y);

        if(distx > disty)
            return 14 * disty + 10 * (distx - disty);
        else
            return 14 * distx + 10 * (disty - distx);
    }

    bool CheckIfAreaClear (int3 p) {
        return !CheckIfTileAir(p + new int3(0, -1, 0)) && CheckIfTileAir(p) && CheckIfTileAir(p + new int3(0, 1, 0));
    }

    bool CheckIfAreaClearToGoUp (int3 p) {
        return !CheckIfTileAir(p + new int3(0, -1, 0)) && CheckIfTileAir(p) && CheckIfTileAir(p + new int3(0, 1, 0)) && CheckIfTileAir(p + new int3(0, 2, 0));
    }

    bool CheckIfAreaClearUp (int3 p) {
        return !CheckIfTileAir(p) && CheckIfTileAir(p + new int3(0, 1, 0)) && CheckIfTileAir(p + new int3(0, 2, 0));
    }

    bool CheckIfAreaClearDown (int3 p) {
        return !CheckIfTileAir(p + new int3(0, -2, 0)) && CheckIfTileAir(p + new int3(0, -1, 0)) && CheckIfTileAir(p) && CheckIfTileAir(p + new int3(0, 1, 0));
    }

    bool CheckIfTileAir (int3 p) {
        ChunkToWorldLocal(p, out int3 chunkPos, out int3 localPos);

        if(_chunkDataNative.TryGetValue(chunkPos, out BlitableArray<TileData> data)) {
            int index = IndexUtilities.XyzToIndex(localPos, chunkSize, chunkSize);
            if(data[index].IsAirTile)
                return data[index].IsAirTile;
            else
                return data[index].model == (byte)ModelType.EigthCube;
        } else {
            return true;
        }
    }

    void ChunkToWorldLocal (int3 p, out int3 chunkPos, out int3 localPos) {
        chunkPos = (int3)math.floor((float3)p / chunkSize);
        localPos = p - chunkPos * chunkSize;
    }
}
