using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;

[BurstCompile]
public struct TimeIncrementJob : IJobParallelFor
{
    public NativeArray<objectInfo> _objectInformationArray;
    public float deltaTime;
    public void Execute(int index)
    {
        float objTime = _objectInformationArray[index].time + deltaTime;
        objectInfo obj = new objectInfo
        {
            position         = _objectInformationArray[index].position,
            rotation         = _objectInformationArray[index].rotation,
            scale            = _objectInformationArray[index].scale,
            currentAnimation = _objectInformationArray[index].currentAnimation,
            animationLength  = _objectInformationArray[index].animationLength,
            animationScale   = _objectInformationArray[index].animationScale,
            isLooping        = _objectInformationArray[index].isLooping,
            time             = objTime,
        };
        _objectInformationArray[index] = obj;
    }
}
