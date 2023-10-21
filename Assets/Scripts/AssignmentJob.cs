using Unity.Burst;
using Unity.Collections;
using UnityEngine.Jobs;

[BurstCompile]
public struct AssignTransforms : IJobParallelForTransform
{
    public NativeArray<objectInfo> _objectInfos;
    public int                     _numberOfInstances;
    
    public void Execute(int i, TransformAccess transform)
    {
        if(i >=_numberOfInstances)
            return;
        
        objectInfo obj  = _objectInfos[i];
        obj.position    = transform.position;
        obj.rotation    = transform.rotation.eulerAngles;
        obj.scale       = transform.localScale;
        _objectInfos[i] = obj;
    }
}