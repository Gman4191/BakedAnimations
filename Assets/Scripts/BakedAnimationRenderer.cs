using UnityEngine;

public struct unitInfo
{
    public Vector3 position;
    public Vector3 rotation;
    public Vector3 scale;
    public float currentAnimation;
    public float animationLength;
    public bool isLooping;
    public float time;
}

public class BakedAnimationRenderer
{
    // Number of animated objects
    private int instanceCount;

    // Arguments for instanced indirect rendering
    private ComputeBuffer argsBuffer;

    // Positions of each mesh to render
    private Vector3[] positions;
    private ComputeBuffer positionBuffer;

    // Rotations of each mesh to render
    private Vector3[] rotations;
    private ComputeBuffer rotationBuffer;

    // Material with the "PlayShaderInstanced" shader
    private Material instanceMaterial;

    // The mesh that will be animated and rendered
    private Mesh mesh;

    // The index of the mesh used in the mesh data
    private int subMeshIndex = 0;

    public void Initialize(int _instanceCount, Mesh instanceMesh, Material animationMaterial, int _subMeshIndex = 0)
    {
        instanceCount = _instanceCount;
        
        positions = new Vector3[instanceCount];
        rotations = new Vector3[instanceCount];

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

        instanceMaterial = animationMaterial;
        subMeshIndex     = _subMeshIndex;
        mesh             = instanceMesh;
    }

    public void RenderMeshInstanced(Transform[] transforms, Bounds bounds)
    {
        // Render meshes only when the renderer data has been initialized
        if(mesh == null)
            return;

        UpdateBuffers(transforms);

        Graphics.DrawMeshInstancedIndirect(mesh, subMeshIndex, instanceMaterial, bounds, argsBuffer);
    }

    private void UpdateBuffers(Transform[] transforms)
    {
        for(int i = 0; i < transforms.Length; i++)
        {
            positions[i] = transforms[i].position;
            rotations[i] = transforms[i].rotation.eulerAngles;
        }

        // Update the position buffer
        positionBuffer?.Release();

        positionBuffer = new ComputeBuffer(positions.Length, sizeof(float) * 3);
        positionBuffer.SetData(positions);
        instanceMaterial.SetBuffer("_PositionBuffer", positionBuffer);

        // Update the rotation buffer
        rotationBuffer?.Release();

        rotationBuffer = new ComputeBuffer(rotations.Length, sizeof(float) * 3);
        rotationBuffer.SetData(rotations);
        instanceMaterial.SetBuffer("_RotationBuffer", rotationBuffer);
    }

    // Release the allocated memory used by the animation renderer
    public void ReleaseBuffers()
    {
        positionBuffer?.Release();
        positionBuffer = null;

        rotationBuffer?.Release();
        rotationBuffer = null;

        argsBuffer?.Release();
        argsBuffer = null;
    }
}
