using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;

[BurstCompile]
public struct TimeIncrementJob : IJobParallelFor
{
    public NativeArray<objectInfo> _objectInformationArray;
    public float                   deltaTime;
    public void Execute(int index)
    {
        float objTime  = _objectInformationArray[index].time + deltaTime;
        objectInfo obj = _objectInformationArray[index];
        obj.time       = objTime;
        _objectInformationArray[index] = obj;
    }
}
