using UnityEngine.InputSystem;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UnitRenderingController : MonoBehaviour
{
    // Original Unit Prefab
    public GameObject originalUnitPrefab;

    public InputAction playerControls;
    public int rows = 10, columns = 10;
    public int unitOffset = 5;
    public float boundsSize = 1000.0f;
    private int currentInstanceCount=100;

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
    private bool canToggleBakedAnimations = false;

    private bool canToggleMethods = true;

    private void OnEnable() {
        playerControls.Enable();
    }

    void Start()
    {
        currentInstanceCount = 100;
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
                transforms[index].localScale = Vector3.one * Random.Range(1.5f, 2.5f);
            }
        }

        render = new BakedAnimationRenderer();
        render.Initialize(currentInstanceCount, unitMeshes[0], unitMaterials[0], transforms, animationObjects, 0);
        
    }

    // Update is called once per frame
    private void Update()
    {
        if(isUsingBakedAnimations)
        {
            // Disable all AnimatorController objects once
            if(canToggleBakedAnimations)
            {
                for(int i = 0; i < unitObjects.Length; i++)
                {
                    unitObjects[i].SetActive(false);
                }
                canToggleBakedAnimations = false;
            }

            // Render animated mesh instances
            render?.RenderAnimatedMeshInstanced(bounds);
        }
        else
        {
            if(canToggleBakedAnimations == false)
            {
                // Disable all AnimatorController objects
                for(int i = 0; i < unitObjects.Length; i++)
                {
                    unitObjects[i].SetActive(false);
                }

                // Enable only the current instance count of AnimatorController objects
                for(int i = 0; i < currentInstanceCount; i++)
                {
                    unitObjects[i].SetActive(true);
                }

                canToggleBakedAnimations = true;

            }
        }
        
        // Toggle between using baked animations and not using baked animations
        if(playerControls.ReadValue<float>() > 0.0f && canToggleMethods)
        {
            isUsingBakedAnimations = !isUsingBakedAnimations;
            canToggleMethods = false;
        }
        else if(playerControls.ReadValue<float>() <= 0.0f)
        {
            canToggleMethods = true;
        }

        // Quit the application when 'ESC' is pressed
        if (Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            Application.Quit();
        }
    }

    // Release any manually allocated memory on application end
    void OnDisable()
    {
        render?.ReleaseBuffers();
        playerControls.Disable();
    }

    // Update the currently rendered instance count based on the GUI user input
    public void UpdateInstanceCount(GameObject canvas)
    {
        Slider slider = canvas.GetComponentInChildren<Slider>();
        TextMeshProUGUI currentCountText = canvas.GetComponentsInChildren<TextMeshProUGUI>()[2];
        currentInstanceCount = (int)slider.value;
        currentCountText.text = currentInstanceCount.ToString();
        render?.ReleaseBuffers();
        render?.Initialize(currentInstanceCount, unitMeshes[0], unitMaterials[0], transforms, animationObjects, 0);

        // Disable all AnimatorController objects
        for(int i = 0; i < unitObjects.Length; i++)
        {
            unitObjects[i].SetActive(false);
        }

        // Enable only the current instance count of AnimatorController objects
        for(int i = 0; i < currentInstanceCount; i++)
        {
            unitObjects[i].SetActive(true);
        }
    }
}

