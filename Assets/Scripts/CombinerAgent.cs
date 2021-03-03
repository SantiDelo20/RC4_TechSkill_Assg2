using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;

public class CombinerAgent : Agent
{
    #region Fields and Properties

    // 24.1 The voxel where the agent is currently
    private Voxel _voxelLocation;
    
    // 24.2 The component at the current location
    private Component _component;

    // 24.3 The VoxelGrid the agent is navigating
    private VoxelGrid _voxelGrid;

    // 24.4 The environment the agent belongs to
    private CombinerEnvironment _environment;

    // 24.5 The normalized position the agent is currently at
    private Vector3 _normalizedIndex;

    // 24.6 The target void ratio
    private float _voidRatioThreshold;

    // 24.7 Training booleans
    public bool Training;
    private bool _frozen;

    #endregion

    #region Unity standard methods

    // 25 Create Awake method
    private void Awake()
    {
        // 26 Read the environment from the hierarchy
        _environment = transform.parent.gameObject.GetComponent<CombinerEnvironment>();
    }

    #endregion

    #region MLAgents methods

    // 27.1 Create OnEpisodeBegin
    public override void OnEpisodeBegin()
    {
        // 28 Get the voxel grid from the environment
        _voxelGrid = _environment.VoxelGrid;

        // 29 Read the target initial void ratio
        _voidRatioThreshold = Academy.Instance.EnvironmentParameters.GetWithDefault("void_ratio", 0.055f);

        if (Training)
        {
            // 30 Unfreeze agent
            _frozen = false;

            // 32 Reset the environment
            _environment.ResetEnvironment();

            // 33 Find a new random position
            int x = Random.Range(0, _voxelGrid.GridSize.x);
            int y = Random.Range(0, _voxelGrid.GridSize.y);
            int z = Random.Range(0, _voxelGrid.GridSize.z);

            // 38 Move the agent to new random voxel
            GoToVoxel(new Vector3Int(x, y, z));
        }
        else
        {
            // 39 Freeze the agent
            _frozen = true;
            // 40 Move the agent to the origin voxel
            GoToVoxel(new Vector3Int(0, 0, 0));
        }
    }

    //27.2 Create OnActionReceived
    public override void OnActionReceived(float[] vectorAction)
    {
        // 41 Only move forward with action if agent is not frozen
        if (!_frozen)
        {
            // 42 Read actions as integers
            int movementAction = (int)vectorAction[0];
            int modifyAction = (int)vectorAction[1];

            // 43 Navigation actions [7], move the agent
            if (MoveAgent(movementAction))
            {
                // 50 If action was valid, add reward
                AddReward(0.0001f);
            }
            else
            {
                // 51 Otherwise, apply penalty
                AddReward(-0.0001f);
            }

            // 52 Modification action [16]
            if (_component.ChangeState(modifyAction))
            {
                // 53 If action was valid, add reward
                AddReward(0.0001f);
            }
            else
            {
                // 54 Otherwise, apply penalty
                AddReward(-0.0001f);
            }

            // 57 Check if the current void ratio obeys the target void ratio
            if (_environment.GetVoidRatio() <= _voidRatioThreshold)
            {
                // 58 Print result
                print($"Succeeded with {_voidRatioThreshold}");
                // 59 Reward agent
                AddReward(1f);
                // 60 End episode
                EndEpisode();
            }
        }
    }

    // 27.3 Create Heuristic
    public override void Heuristic(float[] actionsOut)
    {
        // 61 Use inputs to test agent movement actions
        if (Input.GetKeyDown(KeyCode.UpArrow)) actionsOut[0] = 1;
        else if (Input.GetKeyDown(KeyCode.DownArrow)) actionsOut[0] = 2;
        else if (Input.GetKeyDown(KeyCode.LeftArrow)) actionsOut[0] = 3;
        else if (Input.GetKeyDown(KeyCode.RightArrow)) actionsOut[0] = 4;
        else if (Input.GetKeyDown(KeyCode.E)) actionsOut[0] = 5;
        else if (Input.GetKeyDown(KeyCode.Q)) actionsOut[0] = 6;
        else actionsOut[0] = 0;

        // 62 Use inpits to change component state
        if (Input.GetKeyDown(KeyCode.Alpha1)) actionsOut[1] = 1;
        else if (Input.GetKeyDown(KeyCode.Alpha0)) actionsOut[1] = 0;
        else actionsOut[1] = 16;
    }

    // 27.4 Create CollectObservations
    public override void CollectObservations(VectorSensor sensor)
    {
        // Total of 12 Observations
        // 63 Normalized index of the agent [3 Observations]
        _normalizedIndex = new Vector3(
            _voxelLocation.Index.x / _voxelGrid.GridSize.x - 1,
            _voxelLocation.Index.y / _voxelGrid.GridSize.y - 1,
            _voxelLocation.Index.z / _voxelGrid.GridSize.z - 1);
        sensor.AddObservation(_normalizedIndex);

        // 64 Existance of face neighbours and its state (occupied or not) [6 Observations]
        var neighbours = _voxelLocation.GetFaceNeighboursArray();
        for (int i = 0; i < neighbours.Length; i++)
        {
            if (neighbours[i] != null)
            {
                // 65 If neighbour voxel is occupied
                if (neighbours[i].IsOccupied) sensor.AddObservation(1);
                // 66 If neighbour voxel is not occupied
                else sensor.AddObservation(2);
            }
            // 67 If neighbour voxel does not exist
            else sensor.AddObservation(0);
        }

        // 68 Current state of the current component [1 Observation]
        sensor.AddObservation(_component.State);

        // 69 Current occupation state of the current location [1 Observation]
        if (_voxelLocation.IsOccupied)
        {
            // 70 Is occupied and is origin of component
            if (_voxelLocation.IsOrigin) sensor.AddObservation(0);
            // 71 Is occupied but isn't origin of component
            else sensor.AddObservation(1);
        }
        // 72 Is not occupied
        else sensor.AddObservation(2);

        // 73 Ratio of voids [1 Observation]
        sensor.AddObservation(_environment.GetVoidRatio());
    }

    #endregion

    #region Private methods

    // 44 Create the method to move the agent
    /// <summary>
    /// Attempt to move the based on an integer action
    /// </summary>
    /// <param name="action">The action</param>
    /// <returns>The success of the attempt</returns>
    private bool MoveAgent(int action)
    {
        // 45 Create the vector and assign it's value based on the action input
        Vector3Int direction;

        // 46 Set direction based on action input
        if (action == 0) return true;
        else if (action == 1) direction = new Vector3Int(1, 0, 0);
        else if (action == 2) direction = new Vector3Int(-1, 0, 0);
        else if (action == 3) direction = new Vector3Int(0, 1, 0);
        else if (action == 4) direction = new Vector3Int(0, -1, 0);
        else if (action == 5) direction = new Vector3Int(0, 0, 1);
        else direction = new Vector3Int(0, 0, -1);

        // 47 Check if the resulting action keeps the agent within the grid
        Vector3Int destination = _voxelLocation.Index + direction;
        if (destination.x < 0 || destination.x >= _voxelGrid.GridSize.x ||
            destination.y < 0 || destination.y >= _voxelGrid.GridSize.y ||
            destination.z < 0 || destination.z >= _voxelGrid.GridSize.z)
        {
            // 48 Return false if action was invalid
            return false;
        }

        // 49 Move the agent to the destination
        GoToVoxel(destination);

        return true;
    }

    #endregion

    #region Public methods

    // 34 Create the method to move voxel to an index
    /// <summary>
    /// Move the agent to an index
    /// </summary>
    /// <param name="index">The target index position</param>
    public void GoToVoxel(Vector3Int index)
    {
        // 35 Get the target voxel
        var voxel = _voxelGrid.Voxels[index.x, index.y, index.z];
        _voxelLocation = voxel;
        
        // 36 Move agent game object to target position
        transform.localPosition = voxel.Index;
        
        // 37 Get the component at the target position -> create Method
        _component = _environment.GetComponentAtVoxel(_voxelLocation);
    }

    // 74 Create method to unfreeze agent
    /// <summary>
    /// Unfreezes the agent so it can proceed taking actions
    /// </summary>
    public void UnfreezeAgent()
    {
        _frozen = false;
    }

    #endregion
}