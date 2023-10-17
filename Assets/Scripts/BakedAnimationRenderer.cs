using System;
using UnityEngine;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine.Jobs;

public struct objectInfo
{
    public Vector3 position;
    public Vector3 rotation;
    public Vector3 scale;
    public float currentAnimation;
    public float animationLength;
    public float animationScale;
    public uint isLooping;
    public float time;
}

public class BakedAnimationRenderer
{
    private const int OBJECT_BUFFER_SIZE = sizeof(float) * 13 + sizeof(uint);
    private const int THREAD_GROUP_SIZE  = 32;

    // Number of animated objects
    private int numberOfInstances;

    // Arguments for instanced indirect rendering
    private ComputeBuffer argsBuffer;

    // Object information
    private NativeArray<objectInfo> objectInformationArray;
    private ComputeBuffer objectInformationBuffer;

    // Transform Access to be used in transform assignment
    private TransformAccessArray transformAccessArray;

    // Material with the "PlayShaderInstanced" shader
    private Material instanceMaterial;

    // The mesh that will be animated and rendered
    private Mesh mesh;

    // The index of the mesh used in the mesh data
    private int subMeshIndex = 0;

    // Generated stacked Vertex Animation Textures
    private Texture2D stackedPositionTexture;
    private Texture2D stackedNormalTexture;

    // Length in seconds of the animations
    private NativeArray<float> animationLengthsInSecs;

    // Starting offset into stacked textures on the Y dimension for each animation 
    private NativeArray<float> animationStartOffsets;

    // Length of the animations within texture space scaled within the stacked textures
    private NativeArray<float> animationEndOffsets;
    
    // Starting times in seconds of the current animations being played by instanced objects
    private NativeArray<float> currentAnimationStartTimes;

    // Initialize mesh and object instance data
    public void Initialize(int numberOfInstances, Mesh instanceMesh, Material animationMaterial,
                           Transform[] transforms, AnimationObject[] animationObjects, int _subMeshIndex = 0)
    {
        if (instanceMesh == null)
        {
            throw new ArgumentNullException("instanceMesh", "InstanceMesh cannot be null.");
        }

        this.numberOfInstances = numberOfInstances;

        // Initialize the arguments buffer with mesh data and the number of instances to render
        uint[] args = new uint[5] {instanceMesh.GetIndexCount(_subMeshIndex),
                                   (uint)numberOfInstances,
                                   instanceMesh.GetIndexStart(_subMeshIndex),
                                   instanceMesh.GetBaseVertex(_subMeshIndex), 0};

        argsBuffer = new ComputeBuffer(1, args.Length * sizeof(uint), ComputeBufferType.IndirectArguments);
        argsBuffer.SetData(args);

        instanceMaterial = animationMaterial;
        subMeshIndex     = _subMeshIndex;
        mesh             = instanceMesh;

        GenerateAndSetStackedTextures(animationObjects);
        InitializeAnimationMetaData  (animationObjects);
        InitializeObjectInformation  ();

        // Initialize the thread safe transform access array
        transformAccessArray = new TransformAccessArray(transforms);
    }

    // Call every frame, updates the object information of the instanced meshes
    public void RenderAnimatedMeshInstanced(Bounds bounds)
    {
        if(mesh == null)
            return;

        UpdateBuffers();
        HandleAnimations();
        IncrementTime();
        Graphics.DrawMeshInstancedIndirect(mesh, subMeshIndex, instanceMaterial, bounds, argsBuffer);
    }

    private void UpdateBuffers()
    {
        AssignTransforms assignmentJob = new AssignTransforms
        {
            _objectInfos       = objectInformationArray,
            _numberOfInstances = numberOfInstances
        };

        JobHandle jobHandle = assignmentJob.Schedule(transformAccessArray);
        jobHandle.Complete(); 

        // Update the object information buffer
        objectInformationBuffer?.Release();
        objectInformationBuffer = new ComputeBuffer(objectInformationArray.Length, OBJECT_BUFFER_SIZE);
        objectInformationBuffer.SetData(objectInformationArray);
        instanceMaterial.SetBuffer("_ObjectInfoBuffer", objectInformationBuffer);
    }

    private void GenerateAndSetStackedTextures(AnimationObject[] animationObjects)
    {
        stackedPositionTexture = GenerateStackedTexture.GenerateStackedPositionTexture(animationObjects);
        stackedNormalTexture = GenerateStackedTexture.GenerateStackedNormalTexture(animationObjects);

        // Store the textures in VRAM
        instanceMaterial.SetTexture("_PosTex", stackedPositionTexture);
        instanceMaterial.SetTexture("_NmlTex", stackedNormalTexture);
    }

    private void InitializeObjectInformation()
    {
        objectInformationArray     = new NativeArray<objectInfo>(numberOfInstances, Allocator.Persistent);
        currentAnimationStartTimes = new NativeArray<float>     (numberOfInstances, Allocator.Persistent);

        for(int i = 0; i < objectInformationArray.Length; i++)
        {
            int randomIndex = UnityEngine.Random.Range(0, animationLengthsInSecs.Length);
            objectInfo obj  = new objectInfo
            {
                isLooping        = 0,
                currentAnimation = animationStartOffsets[randomIndex],
                animationScale   = animationEndOffsets[randomIndex],
                animationLength  = animationLengthsInSecs[randomIndex],
                time             = 0.0f
            };

            currentAnimationStartTimes[i] = Time.timeSinceLevelLoad;
            objectInformationArray[i]     = obj;
        }
    }

    // Initialize data structures to store VAT (Vertex Animation Texture) animation metadata.
    private void InitializeAnimationMetaData(AnimationObject[] animationObjects)
    {
        // Define the starting offset, end offset, and length of each animation.
        animationStartOffsets  = new NativeArray<float>(animationObjects.Length, Allocator.Persistent);
        animationEndOffsets    = new NativeArray<float>(animationObjects.Length, Allocator.Persistent);
        animationLengthsInSecs = new NativeArray<float>(animationObjects.Length, Allocator.Persistent);

        for(int currentIndex = 0; currentIndex < animationObjects.Length; currentIndex++)
        {
            // Calculate the end position of an animation in texture space within the stacked VATs
            animationEndOffsets[currentIndex] = (float)(animationObjects[currentIndex].positionTexture.height-1) / (float)(stackedPositionTexture.height);
            
            for(int prevIndex = 0; prevIndex < currentIndex; prevIndex++)
            {
                // Set the animation start offset as the sum of previous starting offsets
                if(prevIndex != currentIndex)
                    animationStartOffsets[currentIndex] += (float)(animationObjects[prevIndex].positionTexture.height) / (float)(stackedPositionTexture.height); 
            }

            // Copy the animation lengths in seconds
            animationLengthsInSecs[currentIndex] = animationObjects[currentIndex].animationLength;
        }
    }

    // Release the allocated memory used by the animation renderer
    public void ReleaseBuffers()
    {
        objectInformationArray.Dispose();
        objectInformationBuffer?.Release();
        objectInformationBuffer = null;
        argsBuffer?.Release();
        argsBuffer = null;
        animationStartOffsets.Dispose();
        animationLengthsInSecs.Dispose();
        animationEndOffsets.Dispose();
        currentAnimationStartTimes.Dispose();
        transformAccessArray.Dispose();
    }

    // Handle Non-Looping animations
    private void HandleAnimations()
    {
        LoopHandlingJob loopHandlingJob = new LoopHandlingJob
        {
            _objectInfos                = objectInformationArray,
            _currentAnimationStartTimes = currentAnimationStartTimes,
            _animationStartOffsets      = animationStartOffsets,
            _animationEndOffsets        = animationEndOffsets,
            _animationLengthsInSecs     = animationLengthsInSecs,
            currentTime                 = Time.timeSinceLevelLoad
        };

        JobHandle jobHandle = loopHandlingJob.Schedule(numberOfInstances, THREAD_GROUP_SIZE);
        jobHandle.Complete();
    }

    // Increment each animated objects time value
    private void IncrementTime()
    {
        TimeIncrementJob timeIncrementJob = new TimeIncrementJob
        {
            _objectInformationArray = objectInformationArray,
            deltaTime               = Time.deltaTime
        };

        JobHandle jobHandle = timeIncrementJob.Schedule(numberOfInstances, THREAD_GROUP_SIZE);
        jobHandle.Complete();
    }
}
