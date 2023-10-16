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
}

public class BakedAnimationRenderer
{
    // Number of animated objects
    private int instanceCount;

    // Arguments for instanced indirect rendering
    private ComputeBuffer argsBuffer;

    // Object information
    private NativeArray<objectInfo> objectInfos;
    private ComputeBuffer objectInfoBuffer;

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
    

    // Initialize mesh and object instance data
    public void Initialize(int _instanceCount, Mesh instanceMesh, Material animationMaterial, Transform[] transforms, AnimationObject[] animationObjects, int _subMeshIndex = 0)
    {
        if(_instanceCount >= 0)
        {
            instanceCount = _instanceCount;
        }
        else
        {
            Debug.LogError("Instance count can not be negative.");
            return;
        }

        // Initialize the arguments buffer with mesh data and the number of instances to render
        uint[] args = new uint[5] {0, 0, 0, 0, 0};

        if(instanceMesh != null)
        {
            args[0] = instanceMesh.GetIndexCount(_subMeshIndex);
            args[1] = (uint)instanceCount;
            args[2] = instanceMesh.GetIndexStart(_subMeshIndex);
            args[3] = instanceMesh.GetBaseVertex(_subMeshIndex);
        }

        argsBuffer = new ComputeBuffer(1, args.Length * sizeof(uint), ComputeBufferType.IndirectArguments);
        argsBuffer.SetData(args);

        // Initialize the mesh and material data
        instanceMaterial = animationMaterial;
        subMeshIndex = _subMeshIndex;
        mesh = instanceMesh;

        // Stack the animation textures together into collective Vertex Animation Textures
        stackedPositionTexture = GenerateStackedTexture.generateStackedPositionTexture(animationObjects);
        stackedNormalTexture = GenerateStackedTexture.generateStackedNormalTexture(animationObjects);

        // Store the textures in VRAM
        instanceMaterial.SetTexture("_PosTex", stackedPositionTexture);
        instanceMaterial.SetTexture("_NmlTex", stackedNormalTexture);

        // Initialize arrays to store VAT (Vertex Animation Texture) animation metadata.
        // These arrays define the starting offset, scale, and length of each animation.
        animationStartOffsets = new NativeArray<float>(animationObjects.Length, Allocator.Persistent);
        animationEndOffsets = new NativeArray<float>(animationObjects.Length, Allocator.Persistent);
        animationLengthsInSecs = new NativeArray<float>(animationObjects.Length, Allocator.Persistent);

        for(int i = 0; i < animationObjects.Length; i++)
        {
            // Calculate the end position of an animation in texture space within the stacked VATs
            animationEndOffsets[i] = (float)(animationObjects[i].positionTexture.height-1) / (float)(stackedPositionTexture.height);
            for(int j = 0; j < i; j++)
            {
                // Set the animation start offset as the sum of previous starting offsets
                if(j != i)
                    animationStartOffsets[i] += (float)(animationObjects[j].positionTexture.height) / (float)(stackedPositionTexture.height); 
            }
            // Copy the animation lengths in seconds
            animationLengthsInSecs[i] = animationObjects[i].animationLength;
        }

        // Initialize unit information
        objectInfos = new NativeArray<objectInfo>(instanceCount, Allocator.Persistent);

        for(int i = 0; i < objectInfos.Length; i++)
        {
            int randomIndex = Random.Range(0, animationLengthsInSecs.Length);
            objectInfo obj = new objectInfo
            {
                isLooping = 0,
                currentAnimation = animationStartOffsets[randomIndex],
                animationScale = animationEndOffsets[randomIndex],
                animationLength = animationLengthsInSecs[randomIndex]
            };

            objectInfos[i] = obj;
        }

        // Initialize the thread safe transform access array
        transformAccessArray = new TransformAccessArray(transforms);
    }

    // Call every frame, updates the object information of the instanced meshes
    public void RenderAnimatedMeshInstanced(Bounds bounds)
    {
        // Render meshes only when the renderer data has been initialized
        if(mesh == null)
            return;

        // Update the object information on the GPU
        UpdateBuffers();

        
        
        Graphics.DrawMeshInstancedIndirect(mesh, subMeshIndex, instanceMaterial, bounds, argsBuffer);
    }

    private void UpdateBuffers()
    {
        AssignTransforms assignmentJob = new AssignTransforms
        {
            _objectInfos = objectInfos,
            _instanceCount = instanceCount
        };
        JobHandle jobHandle = assignmentJob.Schedule(transformAccessArray);
        jobHandle.Complete(); 

        // Update the object information buffer
        objectInfoBuffer?.Release();
        int unitBufferSize = sizeof(float) * 12 + sizeof(uint);
        objectInfoBuffer = new ComputeBuffer(objectInfos.Length, unitBufferSize);
        objectInfoBuffer.SetData(objectInfos);
        instanceMaterial.SetBuffer("_ObjectInfoBuffer", objectInfoBuffer);
    }

    // Release the allocated memory used by the animation renderer
    public void ReleaseBuffers()
    {
        objectInfoBuffer?.Release();
        objectInfoBuffer = null;

        argsBuffer?.Release();
        argsBuffer = null;

        objectInfos.Dispose();
        animationStartOffsets.Dispose();
        animationLengthsInSecs.Dispose();
        animationEndOffsets.Dispose();

        transformAccessArray.Dispose();
    }

    // Change the current animation of an object instance
    public void ChangeAnimation(int objectIndex, int animationIndex, uint isLooping = 0)
    {
        // Bounds check for animation index
        if(animationIndex >= 0 && animationIndex < animationLengthsInSecs.Length)
        {
            // Change the animation based on the animation index
            objectInfo obj = new objectInfo
            {
                isLooping = isLooping,
                currentAnimation = animationStartOffsets[animationIndex],
                animationScale = animationEndOffsets[animationIndex],
                animationLength  = animationLengthsInSecs[animationIndex]
            };
            objectInfos[objectIndex] = obj;
        }
    }
}
