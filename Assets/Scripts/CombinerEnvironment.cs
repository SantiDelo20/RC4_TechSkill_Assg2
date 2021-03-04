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

public class CombinerEnvironment : MonoBehaviour
{
    #region Fields and Properties

    // 01.1 The VoxelGrid and its size
    public VoxelGrid VoxelGrid { get; private set; }
    Vector3Int _gridSize;

    private bool _errase = false;

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

    Grid3d _grid = null;
    string _voxelSize = "0.8";
    GameObject _voids;
    List<(DenseGrid.Voxel, float)> _orderedVoxels = new List<(DenseGrid.Voxel, float)>();

    #endregion

    #region Unity Standard Methods

    void Start()
    {
        // 02 Create the base VoxelGrid
        _gridSize = new Vector3Int(5, 5, 10);
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
    }


    void Update()
    {
        // 17 Draw the voxels using Drawing
        DrawVoxels();

        if (_errase != true)
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
            _errasing.text = $"Errasing Components";
            EraseRaycast();
        }

        // 18 Use mouse click to select a component
        /*
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
        */

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
        //-----------------xx

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
    /// Calculate the ratio of voids in the current configuration
    /// </summary>
    /// <returns>The ratio as a float</returns>
    public float GetVoidRatio()
    {
        // 56 Calculate the amount of voids
        float voidCount = VoxelGrid.GetVoxels().Count(v => !v.IsOccupied);
        return voidCount / (_gridSize.x * _gridSize.y * _gridSize.z);
    }

    /// <summary>
    /// Trying to make more complex aggregations. Method From https://github.com/ADRC4/Voxel
    /// </summary>
    void MakeGrid()
    {
        var colliders = _voids
                      .GetComponentsInChildren<MeshCollider>()
                      .ToArray();

        var voxelSize = float.Parse(_voxelSize);
        _grid = Grid3d.MakeGridWithVoids(colliders, voxelSize);
        
        var faces = _grid.GetFaces().Where(f => f.IsActive);
        var graphEdges = faces.Select(f => new TaggedEdge<DenseGrid.Voxel, DenseGrid.Face>(f.Voxels[0], f.Voxels[1], f));

        var graph = graphEdges.ToUndirectedGraph<DenseGrid.Voxel, TaggedEdge<DenseGrid.Voxel, DenseGrid.Face>>();

        var bottomSlab = _grid
                            .GetVoxels()
                            .Where(v => v.IsActive && v.Index.y == 0)
                            .ToList();

        var bottomCentroid = _grid.BBox.center;
        bottomCentroid.y = 0;

        var startVoxel = bottomSlab.MinBy(v => (v.Center - bottomCentroid).sqrMagnitude);
        var shortest = graph.ShortestPathsDijkstra(e => 1.0, startVoxel);

        _orderedVoxels = _grid
                          .GetVoxels()
                          .Where(v => v.IsActive)
                          .Select(v => (v, shortest(v, out var path) ? path.Count() + Random.value * 0.9f : float.MaxValue))
                          .OrderBy(p => p.Item2)
                          .Select(p => (p.v, p.Item2 / 30f))
                          .ToList();
    }
    /// <summary>
    /// Method to manualy errase voxels before the agent tries to aggregate patterns
    /// </summary>
    /// <param name="screenPosition"></param>


    #endregion

    #region public methods

    public void EraseRaycast()
    {
        //_errase = true;

        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            Transform objectHit = hit.transform;

            if (objectHit.CompareTag("Component"))
            {
                objectHit.GetComponent<Component>().voxel.voxelStatus = VoxelState.Dead;

                //var selected = objectHit.GetComponent<Component>();
                //selected.voxel.voxelStatus = VoxelState.Dead;
            }
        }
        /*
        while (_errase == true)
        {
            
        }
        */

    }

    //Errase buttons
    public void StartEraseRayCast()
    {
        _errase = true;
        Debug.Log("Erase set to true");
    }
    public void StopEraseRayCast()
    {
        _errase = false;
        Debug.Log("Erase set to false");
    }
    #endregion
}
