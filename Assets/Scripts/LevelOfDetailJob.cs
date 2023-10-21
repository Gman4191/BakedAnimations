using Unity.Burst;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine.Jobs;

[BurstCompile]
public struct LevelOfDetailJob : IJobParallelForTransform
{
    public NativeList<int>.ParallelWriter _renderedIndices;
    public NativeArray<objectInfo>        _objectInformationArray;
    public float3                         _playerPosition;
    public float                          _threshold;
    public int                            _numberOfInstances;
    
    public void Execute(int index, TransformAccess transform)
    {
        if(index >= _numberOfInstances)
            return;

        objectInfo obj = _objectInformationArray[index];
        float3 delta   = (float3)transform.position - _playerPosition;
        float distance = math.dot(delta, delta);

        if(distance <= _threshold*_threshold)
        {
            obj.isRendered = 0;
            _renderedIndices.AddNoResize(index);
        }
        else
        {
            obj.isRendered = 1;
        }

        _objectInformationArray[index] = obj;
    }
}
