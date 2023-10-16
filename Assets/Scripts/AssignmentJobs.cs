using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine.Jobs;

public struct InitializeObjectInfos : IJobParallelFor
{
    public NativeArray<objectInfo> _objectInfos;
    public NativeArray<float> _yOffsets;
    public NativeArray<float> _animationLengths;
    public NativeArray<float> _animationScales;

    [BurstCompile]
    void IJobParallelFor.Execute(int i)
    {
        Unity.Mathematics.Random random = new Unity.Mathematics.Random((uint)i + 1);
        int randomIndex = random.NextInt(0, _animationLengths.Length);

        objectInfo obj = new objectInfo
        {
            isLooping = 0,
            currentAnimation = _yOffsets[randomIndex],
            animationScale = _animationScales[randomIndex],
            animationLength = _animationLengths[randomIndex]
        };

        _objectInfos[i] = obj;
    }
}

[BurstCompile]
public struct AssignTransforms : IJobParallelForTransform
{
    public NativeArray<objectInfo> _objectInfos;
    public int _instanceCount;
    
    public void Execute(int i, TransformAccess transform)
    {
        if(i >=_instanceCount)
            return;
        
        objectInfo obj = new objectInfo
        {
            position = transform.position,
            rotation = transform.rotation.eulerAngles,
            scale = transform.localScale
        };
        _objectInfos[i] = obj;
    }
}