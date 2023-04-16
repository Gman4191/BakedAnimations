using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BakedAnimationRenderer
{
    private int instanceCount;
    private ComputeBuffer argsBuffer;
    private Vector4[] positions;
    private ComputeBuffer positionBuffer;
    private Vector3[] rotations;
    private ComputeBuffer rotationBuffer;
    private Material instanceMaterial;
    private Mesh mesh;
    private int subMeshIndex;

    public void Initialize(int _instanceCount, Mesh instanceMesh, Material animationMaterial, int _subMeshIndex = 0)
    {
        instanceCount = _instanceCount;
        
        positions = new Vector4[instanceCount];
        rotations = new Vector3[instanceCount];

        uint[] args = new uint[5] {0, 0, 0, 0, 0};
        argsBuffer = new ComputeBuffer(1, sizeof(uint) * args.Length, ComputeBufferType.IndirectArguments);
        args[0] = (uint)instanceMesh.GetIndexCount(_subMeshIndex);
        args[1] = (uint)(instanceCount);
        args[2] = (uint)instanceMesh.GetIndexStart(_subMeshIndex);
        args[3] = (uint)instanceMesh.GetBaseVertex(_subMeshIndex);

        instanceMaterial = animationMaterial;
        subMeshIndex = _subMeshIndex;
        mesh = instanceMesh;
        
    }

    public void RenderMeshInstanced(Transform[] transforms, Bounds bounds)
    {
        if(mesh == null)
            return;

        UpdateBuffers(mesh, transforms);

        Graphics.DrawMeshInstancedIndirect(mesh, subMeshIndex, instanceMaterial, bounds, argsBuffer);
    }

    // Update the compute buffers
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

    public void ReleaseBuffers()
    {
        Debug.Log("Released Memory");
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
