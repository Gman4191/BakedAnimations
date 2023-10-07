using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.InputSystem.Interactions;

public struct unitInfo
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
    // Number of animated objects
    private int instanceCount;

    // Arguments for instanced indirect rendering
    private ComputeBuffer argsBuffer;

    // Unit information
    public unitInfo[] unitInfos;
    private ComputeBuffer unitInfoBuffer;

    // Material with the "PlayShaderInstanced" shader
    private Material instanceMaterial;

    // The mesh that will be animated and rendered
    private Mesh mesh;

    // The index of the mesh used in the mesh data
    private int subMeshIndex = 0;

    // Generated stacked Vertex Animation Textures
    private Texture2D stackedPositionTexture;
    private Texture2D stackedNormalTexture;

    // Starting offset into stacked textures on the Y dimension for each animation 
    private float[] yOffsets;

    // Scale of each animation
    private float[] animationScales;

    public void Initialize(int _instanceCount, Mesh instanceMesh, Material animationMaterial, AnimationObject[] animationObjects, int _subMeshIndex = 0)
    {
        instanceCount = _instanceCount;

        // Initialize the arguments buffer with mesh data and the number of instances to render
        uint[] args = new uint[5] {0, 0, 0, 0, 0};

        if(instanceMesh != null)
        {
            args[0] = instanceMesh.GetIndexCount(_subMeshIndex);
            args[1] = (uint)instanceCount;
            args[2] = instanceMesh.GetIndexStart(_subMeshIndex);
            args[3] = instanceMesh.GetBaseVertex(_subMeshIndex);
        }

        argsBuffer  = new ComputeBuffer(1, args.Length * sizeof(uint), ComputeBufferType.IndirectArguments);
        argsBuffer.SetData(args);

        // Initialize the mesh and material data
        instanceMaterial = animationMaterial;
        subMeshIndex = _subMeshIndex;
        mesh = instanceMesh;

        // Generate the stacked Vertex Animation Textures
        stackedPositionTexture = GenerateStackedTexture.generateStackedPositionTexture(animationObjects);
        stackedNormalTexture = GenerateStackedTexture.generateStackedNormalTexture(animationObjects);

        // Store the textures in VRAM
        instanceMaterial.SetTexture("_PosTex", stackedPositionTexture);
        instanceMaterial.SetTexture("_NmlTex", stackedNormalTexture);

        // Initialize the y offsets and animation scales
        yOffsets = new float[animationObjects.Length];
        animationScales = new float[animationObjects.Length];

        for(int i = 0; i < animationObjects.Length; i++)
        {
            animationScales[i] = (float)(animationObjects[i].positionTexture.height-1) / (float)(stackedPositionTexture.height);
            for(int j = 0; j < i; j++)
            {
                if(j != i)
                    yOffsets[i] += (float)(animationObjects[j].positionTexture.height) / (float)(stackedPositionTexture.height); 
            }
            Debug.Log(yOffsets[i]);
            Debug.Log(animationScales[i]);
        }

        // Initialize unit information
        unitInfos = new unitInfo[instanceCount];

        for(int i = 0; i < unitInfos.Length; i++)
        {
            unitInfos[i].isLooping = 0;
            unitInfos[i].currentAnimation = yOffsets[1];
            unitInfos[i].animationScale = animationScales[1];
            unitInfos[i].animationLength  = animationObjects[1].animationLength;
            unitInfos[i].time = 0.0f;
        }
    }

    // Called every frame, updates the unit information of the instanced meshes
    public void RenderAnimatedMeshInstanced(Transform[] transforms, Bounds bounds)
    {
        // Render meshes only when the renderer data has been initialized
        if(mesh == null)
            return;

        // Update the unit information on the GPU
        UpdateBuffers(transforms);
        
        // Set the current time 
        Shader.SetGlobalFloat("_CustomTime", Time.timeSinceLevelLoad);
        Graphics.DrawMeshInstancedIndirect(mesh, subMeshIndex, instanceMaterial, bounds, argsBuffer);
    }

    private void UpdateBuffers(Transform[] transforms)
    {
        for(int i = 0; i < transforms.Length; i++)
        {
            unitInfos[i].position = transforms[i].position;
            unitInfos[i].rotation = transforms[i].rotation.eulerAngles;
            unitInfos[i].scale = transforms[i].localScale;
        }

        // Update the unit information buffer
        unitInfoBuffer?.Release();
        int unitBufferSize = sizeof(float) * 13 + sizeof(uint);
        unitInfoBuffer = new ComputeBuffer(unitInfos.Length, unitBufferSize);
        unitInfoBuffer.SetData(unitInfos);
        instanceMaterial.SetBuffer("_UnitInfoBuffer", unitInfoBuffer);
    }

    // Release the allocated memory used by the animation renderer
    public void ReleaseBuffers()
    {
        unitInfoBuffer?.Release();
        unitInfoBuffer = null;

        argsBuffer?.Release();
        argsBuffer = null;
    }
}
