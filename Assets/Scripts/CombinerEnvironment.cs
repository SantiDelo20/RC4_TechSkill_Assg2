using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Unity.MLAgents;
using UnityEngine.UI;
//For DenseGrid void generators
using DenseGrid;
//QuickGraph
using QuickGraph;
using QuickGraph.Algorithms;
using System;

public class CombinerEnvironment : MonoBehaviour
{
    #region training buttons
    public bool TrainingWithVoidGrid;
    public bool TrainingWithVoxelGrid;
    #endregion

    #region Fields and Properties

    // 01.1 The VoxelGrid and its size
    public VoxelGrid VoxelGrid { get; private set; }
    Vector3Int _gridSize;

    #region private button bools

    private bool _toggleVoids = true;
    private bool _toggleVoidsGo = true;
    private bool _drawWithVoxels = false;
    private bool _drawWithVoids = false;
    private bool _erase = false;
    private bool _eraseGrid = false;
    private bool _drawVoids = false;
    private bool _genNewGrid = true;

    #endregion

 
    // 01.2 The Agent that operates on this evironment
    CombinerAgent _agent;

    // 01.3 The array that contains all the components of the environment
    Component[,,] _components;

    // 01.4 The selected component
    Component _selected;

    // 01.05 The Text object to display the current Void Ratio
    [SerializeField]
    Text _voidRatio;

    [SerializeField]
    Text _errasing;

    #endregion

    #region VoidGrid
    //GUI
    Rect _windowRect = new Rect(40, 300, 250, 400);
    GUISkin _skin = null;
   
    //GUIStyle _style = new GUIStyle();

    string _voxelSize = "0.96";
    int _animatedCount;

    Grid3d _grid3D = null;
    Vector3Int _grid3dSize;
    GameObject _voids;

    List<(DenseGrid.Voxel, float)> _orderedVoxels = new List<(DenseGrid.Voxel, float)>();

    #endregion

    #region Unity Standard Methods

    void Start()
    {
        _voids = GameObject.Find("Voids");
        if (TrainingWithVoidGrid == true)
        {
            CreateVoidGrid();
            _drawWithVoids = true;
        }
        if (TrainingWithVoxelGrid == true)
        {
            CreateVoxelGrid();
            _drawWithVoxels = true;
        }
    }

    #region GUI
    void OnGUI()
    {
        GUI.skin = _skin;
        GUI.skin.label.fontSize = 20; //not working
        //_skin = Resources.Load<GUISkin>($"GUI/GUISkin1");
        //_skin.fontSize = 20;

        _windowRect = GUI.Window(0, _windowRect, WindowFunction, string.Empty);
    }

    void WindowFunction(int windowID)
    {
        int i = 1;
        int s = 50;

        _voxelSize = GUI.TextField(new Rect(s / 2, s * i++, 200, 40), _voxelSize);

        if (GUI.Button(new Rect(s / 2, s * i++, 200, 40), "Generate"))
            GrowVoxels(); //GrowVoxels()

        if (GUI.Button(new Rect(s / 2, s * i++, 200, 40), "Purge ALL Voxels"))
            ErraseVoxels();

        if (_toggleVoids != GUI.Toggle(new Rect(s / 2, s * i++, 200, 40), _toggleVoids, "Show UserInput voids"))
            ToggleVoids(!_toggleVoids);
        if (_toggleVoidsGo != GUI.Toggle(new Rect(s / 2, s * i++, 200, 40), _toggleVoidsGo, "Show GameObject voids"))
            ToggleVoidsGo(!_toggleVoidsGo);
    }
    #endregion

    void Update()
    {
        if(_drawWithVoxels == true)
        {
            // 17 Draw the voxels using Drawing
            while (_genNewGrid == true)
            {
                CreateVoxelGrid();
            }
            DrawVoxels();
        }
        
        if (_drawWithVoids == true)
        {

            DrawVoxels();
   
        }

        if (_erase != true)
        {
            _errasing.text = $"Selecting Components";

            if (Input.GetMouseButtonDown(0))
            {
                // 21 Select component by clicking
                SelectComponent();
                if (_selected != null)
                {
                    print("Component Selected");
                }
                else
                {
                    print("No component selected");
                }
            }
        }
        else
        {
            _errasing.text = $"Erasing Components";

            if (Input.GetMouseButtonDown(0))
            {
                EraseRaycast();
                if (_selected != null)
                {
                    print("Component Cleared");
                }
                else
                {
                    print("No component selected");
                }
            }
        }

        // 22 Use 1 to change the state of the component
        //if (Input.GetKeyDown(KeyCode.Alpha1))
        //{
        //    if (_selected != null)
        //    {
        //        _selected.ChangeState(1);
        //    }
        //}

        // 23 Use 0 to change the state of the component
        //if (Input.GetKeyDown(KeyCode.Alpha0))
        //{
        //    if (_selected != null)
        //    {
        //        _selected.ChangeState(0);
        //    }
        //}

        // 76 Show the Void Ratio if text has been assigned
        if (_voidRatio != null)
        {
            _voidRatio.text = $"Current Void Ratio: {GetVoidRatio().ToString("F2")}";
        }

        // 77 Use A to unfreeze the Agent
        if (Input.GetKeyDown(KeyCode.A))
        {
            _agent.UnfreezeAgent();
        }
    }

    #endregion

    #region Private Methods

    private void GrowVoxels()
    {
        _drawWithVoids = true;

        CreateVoidGrid();

        ToggleVoids(false);
    }

    /// <summary>
    /// Toggle voids, game object and selections
    /// </summary>
    /// <param name="toggle"></param>
    void ToggleVoids(bool toggle)
    {
        
        _drawVoids = toggle;
        _toggleVoids = toggle;
    }
    void ToggleVoidsGo(bool toggle)
    {
        foreach (var r in _voids.GetComponentsInChildren<Renderer>())
        {
            r.enabled = toggle;
            r.GetComponent<MeshCollider>().enabled = toggle;
        }

        _drawVoids = toggle;
        _toggleVoidsGo = toggle;
    }
    /// <summary>
    /// Erasing to the ground
    /// </summary>
    private void ErraseVoxels()
    {
        int voxelCount = VoxelGrid.Voxels.Length;
        print(voxelCount);
        for (int x = 0; x < _gridSize.x; x++)
        {
            for (int y = 0; y < _gridSize.x; y++)
            {
                for (int z = 0; z < _gridSize.x; z++)
                {
                    VoxelGrid.Voxels[x, y, z] = null;
                }
            }
        }
        Debug.Log("_grid cleared");

        _eraseGrid = false;
    }
    /// <summary>
    /// Create a matrix xyz grid
    /// </summary>
    private void CreateVoxelGrid()
    {
        // 02 Create the base VoxelGrid
        _gridSize = new Vector3Int(5, 10, 5);
        VoxelGrid = new VoxelGrid(_gridSize, transform.position, 1f);

        // 03 Create the array that will store the environment's components
        _components = new Component[_gridSize.x, _gridSize.y, _gridSize.z];

        // 04 Get the Agent from the hierarchy
        _agent = transform.Find("CombinerAgent").GetComponent<CombinerAgent>();

        // 05 Get the Component prefab from resources
        var componentPrefab = Resources.Load<GameObject>("Prefabs/Component");

        // 06 Cycle through the VoxelGrid and create components
        for (int x = 0; x < _gridSize.x; x++)
        {
            for (int y = 0; y < _gridSize.y; y++)
            {
                for (int z = 0; z < _gridSize.z; z++)
                {
                    // 07 Get the Voxel
                    var voxel = VoxelGrid.Voxels[x, y, z];
                    // 08 Create new component at voxel
                    var newComponentGO = Instantiate(componentPrefab, voxel.Index + transform.position, Quaternion.identity, transform);
                    // 09 Name component based on location
                    newComponentGO.name = $"Component_{x}_{y}_{z}";
                    // 10 Get the Component from the GameObject
                    var newComponent = newComponentGO.GetComponent<Component>();
                    // 11 Store the component at the components array
                    _components[x, y, z] = newComponent;
                    // 12 Assign the Voxel to the Component
                    newComponent.SetVoxel(voxel);
                    // 13 Set the state of the Component as 0 (empty state)
                    newComponent.ChangeState(0);
                }
            }
        }
        _genNewGrid = false;
    }

    /// <summary>
    /// Trying to make more complex aggregations. Method From https://github.com/ADRC4/Voxel
    /// </summary>
    private void CreateVoidGrid()
    {
        var colliders = _voids
                      .GetComponentsInChildren<MeshCollider>()
                      .ToArray();

        var voxelSize = float.Parse(_voxelSize);
        _grid3D = Grid3d.MakeGridWithVoids(colliders, voxelSize);

        //Get the Bbox gridsize, Basically the Bounding box property
        _grid3dSize = _grid3D.Size; //works

        VoxelGrid = new VoxelGrid(_grid3dSize, transform.position, 1f);

        //03 Create the array that will store the environment's components
        _components = new Component[_grid3dSize.x, _grid3dSize.y, _grid3dSize.z];

        // 04 Get the Agent from the hierarchy
        _agent = transform.Find("CombinerAgent").GetComponent<CombinerAgent>();

        // 05 Get the Component prefab from resources
        var componentPrefab = Resources.Load<GameObject>("Prefabs/Component");

        
        for (int x = 0; x < _grid3dSize.x; x++)
        {
            for (int y = 0; y < _grid3dSize.y; y++)
            {
                for (int z = 0; z < _grid3dSize.z; z++)
                {
                    // Get the voxel
                    var voxelVoidGrid = _grid3D.Voxels[x, y, z];//is it here where we should find the void properties?

                    var voxel = VoxelGrid.Voxels[x, y, z];//Seems to be the thing that works with the component

                    if (voxelVoidGrid.IsActive)//Non Voids check //filter works, //places components out of the Voids.
                    {
                        //If not in a gameobject void, create a component
                        var newComponentGO = Instantiate(componentPrefab, voxel.Index + transform.position, Quaternion.identity, transform);

                        newComponentGO.name = $"Component_{x}_{y}_{z}";

                        var newComponent = newComponentGO.GetComponent<Component>();

                        _components[x, y, z] = newComponent;
                        newComponent.SetVoxel(voxel);
                        newComponent.ChangeState(0);
                        Debug.Log($"Placed Component_{x}_{y}_{z}");

                        voxel.IsVoid = false;
                        //Check is possition 
                    }
                    else //Void objects
                    {
                        //Voxels without component still render as transparent, also if theres no component the agent buggs out

                        var newComponentGO = Instantiate(componentPrefab, voxel.Index + transform.position, Quaternion.identity, transform);

                        newComponentGO.name = $"Component_{x}_{y}_{z}";
                        var newComponent = newComponentGO.GetComponent<Component>();
                        _components[x, y, z] = newComponent;
                        newComponent.SetVoxel(voxel);
                        newComponent.ChangeState(0);

                        Debug.Log("Voxel NotActive");
                        voxel.IsVoid = true;
                        voxel.IsOccupied = true;
                    }
                }
            }
        }
        _genNewGrid = false;
    }

    //14 Create the DrawVoxels method
    /// <summary>
    /// Uses <see cref="Drawing"/> to draw voxels without gameobjects
    /// </summary>
    private void DrawVoxels()
    {
        // 15 Iterate through all voxles
        foreach (var voxel in VoxelGrid.Voxels)
        {
            // 16 Draw voxel if it is not occupied
            if (!voxel.IsOccupied)
            {
                Drawing.DrawTransparentCube(((Vector3)voxel.Index * VoxelGrid.VoxelSize) + transform.position, VoxelGrid.VoxelSize);
            }
            // Draw Void voxel if requested
            else
            {
                if (_toggleVoids == true)
                {
                    if (voxel.IsVoid)
                    {
                        Drawing.DrawTransparentCubeVoids(((Vector3)voxel.Index * VoxelGrid.VoxelSize) + transform.position, VoxelGrid.VoxelSize);
                    }
                }
            }

        }
    }

    /// <summary>
    /// Added method, manual erase collider box toggle
    /// </summary>
    private void EraseRaycast()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            Transform objectHit = hit.transform;

            if (objectHit.CompareTag("Component"))
            {
                _selected = objectHit.gameObject.GetComponent<Component>();
                _selected.GetComponent<BoxCollider>().enabled = false;
                _selected.ClearComponent();
                _selected.Voxel.IsVoid = true;

                string infoB = _selected.ToString();//prints component name with coordinates
                print(infoB);
                
            }
        }
    }

    //19 Create the method to select component by clicking
    /// <summary>
    /// Select a component and assign Agent position with mouse click
    /// </summary>
    private void SelectComponent()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            Transform objectHit = hit.transform;

            if (objectHit.CompareTag("Component"))
            {
                // 20 Assign clicked component to the selected variable
                _selected = objectHit.GetComponent<Component>();

                // 75 Set the position of the agent at the clicked voxel
                var pos = objectHit.transform.localPosition;
                Vector3Int posInt = new Vector3Int((int)pos.x, (int)pos.y, (int)pos.z);
                _agent.GoToVoxel(posInt);
            }
        }
        else
        {
            _selected = null;
        }
    }

    #endregion

    #region Public Methods

    // 37.1 Create method to the component at a given voxel
    /// <summary>
    /// Get the component located at a <see cref="Voxel"/>
    /// </summary>
    /// <param name="voxel"></param>
    /// <returns>The <see cref="Component"/> at the voxel</returns>
    public Component GetComponentAtVoxel(Voxel voxel)
    {
        return _components[voxel.Index.x, voxel.Index.y, voxel.Index.z];
    }

    // 31 Create method to reset the environment
    /// <summary>
    /// Resets the environment by setting all components' state as 0
    /// </summary>
    public void ResetEnvironment()
    {
        foreach (var component in _components)
        {
            component.ChangeState(0);
        }
    }

    // 55 Create method to read the void ratio
    /// <summary>
    /// Calculate the ratio of voids in the current configuration, modded to not count intended voids or voids that shoud stay as voids.
    /// </summary>
    /// <returns>The ratio as a float</returns>
    public float GetVoidRatio()
    {
        if (_drawWithVoxels == true)
        {
            // 56 Calculate the amount of voids
            float nonAccessVoids = VoxelGrid.GetVoxels().Count(v => v.IsVoid);
            float voidCount = VoxelGrid.GetVoxels().Count(v => !v.IsOccupied);
            return voidCount / ((_gridSize.x * _gridSize.y * _gridSize.z) - nonAccessVoids);//Add correction for manual non fillable voids. (nonAccessVoids)
        }
        if (_drawWithVoids == true)
        {
            // 56 Calculate the amount of voids
            float nonAccessVoids = VoxelGrid.GetVoxels().Count(v => v.IsVoid);
            float voidCount = VoxelGrid.GetVoxels().Count(v => !v.IsOccupied);
            return voidCount / ((_grid3dSize.x * _grid3dSize.y * _grid3dSize.z) - nonAccessVoids);//Add correction for manual non fillable voids. (nonAccessVoids)
        }
        else
        {
            return 0;
        }
    }

    #endregion

    #region button bools
    //Grid Options
    public void StartRegularGrid()
    {
        _drawWithVoxels = true;
        _drawWithVoids = false;
        _genNewGrid = true;
        Debug.Log("Generate matrix grid");
    }

    public void StartVoidGrid()
    {
        _drawWithVoids = true;
        _drawWithVoxels = false;
        _genNewGrid = true;
        CreateVoidGrid();
        //_eraseGrid = true;

        Debug.Log("Generating Around Voids");
    }

    //Errase buttons
    public void StartEraseRayCast()
    {
        _erase = true;
        Debug.Log("Erase set to true");
    }

    public void StopEraseRayCast()
    {
        _erase = false;
        Debug.Log("Erase set to false");
    }

    //Draw options
    public void ShowVoidVoxels()
    {
        _drawVoids = true;
        Debug.Log("DrawingVoids");
    }
    public void HideVoidVoxels()
    {
        _drawVoids = false;
        Debug.Log("HideVoids");
    }

    //Star the agent
    public void ReleaseAgent()
    {
        _agent.UnfreezeAgent();
    }
    #endregion
}
