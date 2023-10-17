using Unity.Burst;
using Unity.Collections;
using UnityEngine.Jobs;

[BurstCompile]
public struct AssignTransforms : IJobParallelForTransform
{
    public NativeArray<objectInfo> _objectInfos;
    public int _numberOfInstances;
    
    public void Execute(int i, TransformAccess transform)
    {
        if(i >=_numberOfInstances)
            return;
        
        objectInfo obj = new objectInfo
        {
            position         = transform.position,
            rotation         = transform.rotation.eulerAngles,
            scale            = transform.localScale,
            currentAnimation = _objectInfos[i].currentAnimation,
            animationLength  = _objectInfos[i].animationLength,
            animationScale   = _objectInfos[i].animationScale,
            isLooping        = _objectInfos[i].isLooping,
            time             = _objectInfos[i].time
        };
        _objectInfos[i] = obj;
    }
}