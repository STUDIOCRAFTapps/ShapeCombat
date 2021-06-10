using System.Collections.Generic;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;


public class PathfindingManager : MonoBehaviour {

    public int maxPathLength = 256;
    public int maxOpenedCellsLength = 1024;
    public int maxClosedCellsLength = 256;

    public Transform target1;
    public Transform target2;

    private List<PathFindingJobHandleWithData> onGoingJobs;

    public static PathfindingManager inst;
    private void Awake () {
        inst = this;
    }

    private void Start () {
        onGoingJobs = new List<PathFindingJobHandleWithData>(8);
    }

    private void Update () {

        // Check if any of our job has been finished, in reverse order so we can remove them properly.
        for(int i = onGoingJobs.Count - 1; i >= 0; i--) {
            if(onGoingJobs[i].handle.IsCompleted) {
                onGoingJobs[i].handle.Complete();
                onGoingJobs[i].callOnceEnded(onGoingJobs[i].data._results[0] == 1, onGoingJobs[i].data._path, onGoingJobs[i].data._results[1]);
                onGoingJobs[i].data._path.Dispose();
                onGoingJobs[i].data._results.Dispose();
                onGoingJobs[i].data._open.Dispose();
                onGoingJobs[i].data._closed.Dispose();
                onGoingJobs.RemoveAt(i);
            }
        }
    }

    private void OnDestroy () {
        if(onGoingJobs == null)
            return;

        for(int i = onGoingJobs.Count - 1; i >= 0; i--) {
            onGoingJobs[i].handle.Complete();
            onGoingJobs[i].data._path.Dispose();
            onGoingJobs[i].data._results.Dispose();
            onGoingJobs[i].data._open.Dispose();
            onGoingJobs[i].data._closed.Dispose();
        }
        onGoingJobs.Clear();
    }



    public void RequestPath (int3 start, int3 end, OnPathRequestFinished onPathRequestFinished) {

        // Prepare job data
        PathfindingJob pathfindingJobData = new PathfindingJob() {
            chunkSize = World.inst.chunkSize,
            _path = new NativeArray<float3>(maxPathLength, Allocator.TempJob),
            _results = new NativeArray<int>(2, Allocator.TempJob),
            startPos = start,
            endPos = end,
            _chunkDataNative = World.inst.chunkDataNative,

            _closed = new NativeHashMap<int3, NodeCost>(maxClosedCellsLength, Allocator.TempJob),
            _open = new NativeBinaryHeap<NodeCost>(maxOpenedCellsLength, Allocator.TempJob),
        };

        // Prepare handle so proper function can be called once job is finished
        JobHandle jobHandle = pathfindingJobData.Schedule();

        // Add job with data to list of jobs to wait for them to finish
        onGoingJobs.Add(new PathFindingJobHandleWithData() {
            callOnceEnded = onPathRequestFinished,
            data = pathfindingJobData,
            handle = jobHandle
        });

    }


    public delegate void OnPathRequestFinished (bool isValid, NativeArray<float3> path, int nodeCount);
}
