using UnityEngine.InputSystem;
using UnityEngine;

public class UnitRenderingController : MonoBehaviour
{
    // Original Unit Prefab
    public GameObject originalUnitPrefab;

    public InputAction playerControls;
    public int rows = 10, columns = 10;
    public int unitOffset = 5;
    public float boundsSize = 1000.0f;

    // Each mesh must must only have 1 mesh (subMeshIndex of 0)
    public Mesh[] unitMeshes;
    public Material[] unitMaterials;

    private BakedAnimationRenderer render;

    [SerializeField]
    private AnimationObject[] animationObjects;
    private GameObject[] unitObjects;
    private Transform[] transforms;
    private Bounds bounds;

    private bool isUsingBakedAnimations = false;
    private bool canToggle = true;

    private void OnEnable() {
        playerControls.Enable();
    }

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
                unitObjects[index] = GameObject.Instantiate(originalUnitPrefab, Vector3.zero, Quaternion.identity);
                transforms[index] = unitObjects[index].transform;
                transforms[index].position = new Vector3(x*unitOffset, 0, y*unitOffset);
                transforms[index].rotation = Quaternion.Euler(new Vector3(0, Random.Range(0, 360), 0));
                transforms[index].localScale = Vector3.one * Random.Range(.9f, 1.1f);
            }
        }

        render = new BakedAnimationRenderer();
        render.Initialize(rows*columns, unitMeshes[0], unitMaterials[0], animationObjects, 0);
        
    }

    // Update is called once per frame
    private void Update()
    {
        if(isUsingBakedAnimations)
        {
            for(int i = 0; i < unitObjects.Length; i++)
            {
                unitObjects[i].SetActive(false);
            }
            render?.RenderAnimatedMeshInstanced(transforms, bounds);
        }
        else
        {
            for(int i = 0; i < unitObjects.Length; i++)
            {
                unitObjects[i].SetActive(true);
            }
        }
        
        // Toggle between using baked animations and not using baked animations
        if(playerControls.ReadValue<float>() > 0.0f && canToggle)
        {
            isUsingBakedAnimations = !isUsingBakedAnimations;
            canToggle = false;
        }
        else
        {
            canToggle = true;
        }
    }

    void OnDisable()
    {
        render.ReleaseBuffers();
        playerControls.Disable();
    }
}

