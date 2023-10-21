using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;

[BurstCompile]
public struct LoopHandlingJob : IJobParallelFor
{
    public NativeArray<float>            _currentAnimationStartTimes;
    public NativeArray<objectInfo>       _objectInfos;
    [ReadOnly] public NativeArray<float> _animationStartOffsets;
    [ReadOnly] public NativeArray<float> _animationEndOffsets;
    [ReadOnly] public NativeArray<float> _animationLengthsInSecs;
    [ReadOnly] public float              currentTime;

    public void Execute(int i)
    {
        if(_objectInfos[i].isLooping != 0)
            return;
        
        // If the current animation is ended, start the 
        if(currentTime >= _currentAnimationStartTimes[i] + _objectInfos[i].animationLength)
        {
            // Change the animation based on the animation index
            objectInfo obj       = _objectInfos[i];
            obj.isLooping        = 1;
            obj.currentAnimation = _animationStartOffsets[0];
            obj.animationScale   = _animationEndOffsets[0];
            obj.animationLength  = _animationLengthsInSecs[0];
            obj.time             = 0.0f;

            _currentAnimationStartTimes[i] = currentTime;
            _objectInfos[i]                = obj;
        }
    }
}
