using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UnitRenderingController : MonoBehaviour
{
    public int rows = 10, columns = 10;
    public int unitOffset = 5;

    // Each mesh must must only have 1 mesh (subMeshIndex of 0)
    public Mesh[] unitMeshes;
    public Material[] unitMaterials;
    public int subMeshIndex = 0;
    
    BakedAnimationRenderer render;
    GameObject[] unitObjects;
    Transform[] transforms;

    // Start is called before the first frame update
    void Start()
    {
        unitObjects = new GameObject[rows*columns];
        transforms = new Transform[rows*columns];
        for(int i = 0; i < unitObjects.Length; i++)
        {
            unitObjects[i] = new GameObject();
            transforms[i] = unitObjects[i].transform;
            transforms[i].position = new Vector4(Random.Range(0,100), 0, Random.Range(0, 100), 0);
            transforms[i].rotation = Quaternion.Euler(new Vector3(0, Random.Range(0, 360), 0));
        }

        render = new BakedAnimationRenderer();
        render.Initialize(rows*columns, unitMeshes[0], unitMaterials[0]);
    }

    // Update is called once per frame
    void Update()
    {
        render.RenderMeshInstanced(transforms, new Bounds(Vector3.zero, Vector3.one * 1000.0f));
    }

    void OnDisable()
    {
        render.ReleaseBuffers();
    }
}

