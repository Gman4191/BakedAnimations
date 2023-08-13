using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UnitRenderingController : MonoBehaviour
{
    public int rows = 10, columns = 10;
    public int unitOffset = 5;
    public float boundsSize = 1000.0f;

    // Each mesh must must only have 1 mesh (subMeshIndex of 0)
    public Mesh[] unitMeshes;
    public Material[] unitMaterials;

    BakedAnimationRenderer render;
    GameObject[] unitObjects;
    Transform[] transforms;
    Bounds bounds;

    // Start is called before the first frame update
    void Start()
    {
        bounds      = new Bounds(Vector3.zero, Vector3.one * boundsSize);
        unitObjects = new GameObject[rows*columns];
        transforms  = new Transform[rows*columns];

        for(int x = 0; x < rows; x++)
        {
            for(int y = 0; y < columns; y++)
            {
                int index = x*(rows-(rows-columns)) + y;
                unitObjects[index] = new GameObject();
                transforms[index] = unitObjects[index].transform;
                transforms[index].position = new Vector3(x*unitOffset, 0, y*unitOffset);
                transforms[index].rotation = Quaternion.Euler(new Vector3(0, Random.Range(0, 360), 0));
            }
        }

        render = new BakedAnimationRenderer();
        render.Initialize(rows*columns, unitMeshes[0], unitMaterials[0], 0);
    }

    // Update is called once per frame
    private void Update()
    {
        render.RenderMeshInstanced(transforms, bounds);
    }

    void OnDisable()
    {
        render.ReleaseBuffers();
    }
}

