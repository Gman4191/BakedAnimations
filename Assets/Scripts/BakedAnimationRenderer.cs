using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BakedAnimationRenderer
{
    // Number of animated objects
    private int instanceCount;

    // Arguments for instanced indirect rendering
    private ComputeBuffer argsBuffer;

    // Positions of each mesh to render
    private Vector4[] positions;
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
        
        positions = new Vector4[instanceCount];
        rotations = new Vector3[instanceCount];

        // Initialize the arguments buffer with mesh data and the number of instances to render
        uint[] args = new uint[5] {0, 0, 0, 0, 0};
        argsBuffer  = new ComputeBuffer(1, sizeof(uint) * args.Length, ComputeBufferType.IndirectArguments);
        args[0]     = (uint)instanceMesh.GetIndexCount(_subMeshIndex);
        args[1]     = (uint)(instanceCount);
        args[2]     = (uint)instanceMesh.GetIndexStart(_subMeshIndex);
        args[3]     = (uint)instanceMesh.GetBaseVertex(_subMeshIndex);

        
        instanceMaterial = animationMaterial;
        subMeshIndex     = _subMeshIndex;
        mesh             = instanceMesh;
        
    }

    public void RenderMeshInstanced(Transform[] transforms, Bounds bounds)
    {
        // Render meshes only when the renderer data has been initialized
        if(mesh == null)
            return;

        UpdateBuffers(mesh, transforms);

        Graphics.DrawMeshInstancedIndirect(mesh, subMeshIndex, instanceMaterial, bounds, argsBuffer);
    }

    // Send the position and rotation data to the instanced material's shader
    private void UpdateBuffers(in Mesh mesh, Transform[] transforms)
    {
        if(transforms.Length != instanceCount)
        {
            Debug.LogError("Error: Unequal instances compared to provided transforms");
            return;
        }

        for(int i = 0; i < transforms.Length; i++)
        {
            positions[i] = transforms[i].position;
            rotations[i] = transforms[i].rotation.eulerAngles;
        }

        // Update the position buffer
        if(positionBuffer != null)
            positionBuffer.Release();

        positionBuffer = new ComputeBuffer(positions.Length, sizeof(float) * 4);
        positionBuffer.SetData(positions);
        instanceMaterial.SetBuffer("_PositionBuffer", positionBuffer);

        // Update the rotation buffer
        if(rotationBuffer != null)
            rotationBuffer.Release();

        rotationBuffer = new ComputeBuffer(rotations.Length, sizeof(float) * 3);
        rotationBuffer.SetData(rotations);
        instanceMaterial.SetBuffer("_RotationBuffer", rotationBuffer);
    }

    // Release the allocated memory used by the animation renderer
    public void ReleaseBuffers()
    {
        if (positionBuffer != null)
            positionBuffer.Release();
        positionBuffer = null;

        if (rotationBuffer != null)
            rotationBuffer.Release();
        rotationBuffer = null;

        if (argsBuffer != null)
            argsBuffer.Release();
        argsBuffer = null;
    }
}
