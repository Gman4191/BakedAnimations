using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UnitRendering : MonoBehaviour
{
    public int rows = 10, columns = 10;
    public int unitOffset = 5;

    // Each mesh must must only have 1 mesh (subMeshIndex of 0)
    public Mesh[] unitMeshes;
    public Material[] unitMaterials;
    public int subMeshIndex = 0;

    private Vector4[] unitPositions;
    private ComputeBuffer positionBuffer;
    private Vector3[] unitRotations;
    private ComputeBuffer rotationBuffer;
    private ComputeBuffer argsBuffer;
    private uint[][] args;
    // Data corresponding to a specific unit
    struct UnitData
    {
        bool  isLooping;
        float time;
        uint  currentAnimation;
    };

    // Start is called before the first frame update
    void Start()
    {
        args = new uint[unitMeshes.Length][];
        for(int i = 0; i < args.Length; i++)
        {
            args[i] = new uint[5] {0, 0, 0, 0, 0};
        }
        argsBuffer = new ComputeBuffer(1, args[0].Length*sizeof(uint), ComputeBufferType.IndirectArguments);

        // Indirect arguments
        for(int i = 0; i < unitMeshes.Length; i++)
        {
            if(unitMeshes[i] != null)
            {
                args[i][0] = (uint)unitMeshes[i].GetIndexCount(subMeshIndex);
                args[i][1] = (uint)(rows*columns);
                args[i][2] = (uint)unitMeshes[i].GetIndexStart(subMeshIndex);
                args[i][3] = (uint)unitMeshes[i].GetBaseVertex(subMeshIndex);
            }
            else
            {
                args[i][0] = args[i][1] = args[i][2] = args[i][3] = 0;
            }
        }

        // TEMPORARILY ONLY USING 1 ARGS BUFFER
        argsBuffer.SetData(args[0]);
        unitPositions = new Vector4[rows*columns];
        for(int x = 0; x < rows; x++)
        {
            for(int y = 0; y < columns; y++)
            {
                unitPositions[x*rows + y] = new Vector4(((x + y) % rows) * unitOffset, 0, x * unitOffset, 0);
            }
        }
        unitRotations = new Vector3[rows*columns];    

        if(unitMeshes.Length != unitMaterials.Length)
            Debug.Log("Mismatched unit Mesh/Material Error");
    }

    // Update is called once per frame
    void Update()
    {
        UpdateBuffers();

        for(int i = 0; i < unitMeshes.Length; i++)
        {
            Graphics.DrawMeshInstancedIndirect(unitMeshes[i], subMeshIndex, unitMaterials[i], new Bounds(Vector3.zero, new Vector3(1000.0f, 1000.0f, 1000.0f)), argsBuffer);
        }
    }

    // Update the compute buffers
    private void UpdateBuffers()
    {
        if(unitMeshes == null)
        {
            Debug.Log("Unit Mesh Null Reference");
            return;
        }

        // Update the position buffer
        if(positionBuffer != null)
            positionBuffer.Release();

        positionBuffer = new ComputeBuffer(unitPositions.Length, sizeof(float) * 4);
        positionBuffer.SetData(unitPositions);
        unitMaterials[0].SetBuffer("_PositionBuffer", positionBuffer);

        // Update the rotation buffer
        if(rotationBuffer != null)
            rotationBuffer.Release();

        rotationBuffer = new ComputeBuffer(unitRotations.Length, sizeof(float) * 3);

        for(int x = 0; x < rows; x++)
        {
            for(int y = 0; y < columns; y++)
            {
                unitRotations[x*rows + y] = new Vector3(0, (x+y) * 15, 0);
            }
        }  
        rotationBuffer.SetData(unitRotations);
        unitMaterials[0].SetBuffer("_RotationBuffer", rotationBuffer);
    }

    void OnDisable()
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
