using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Component : MonoBehaviour
{
    // The voxel this component is assigned to
    Voxel _voxel;

    // The state of the component
    public int State { get; private set; }

    // The component's sections
    private GameObject[] _sections;

    #region Unity Standard Methods

    private void Awake()
    {
        // Create the sections array and populate it
        _sections = new GameObject[7];
        _sections[0] = transform.Find("0").gameObject;
        _sections[1] = transform.Find("Pos_X").gameObject;
        _sections[2] = transform.Find("Neg_X").gameObject;

        _sections[3] = transform.Find("Pos_Y").gameObject;
        _sections[4] = transform.Find("Neg_Y").gameObject;

        _sections[5] = transform.Find("Pos_Z").gameObject;
        _sections[6] = transform.Find("Neg_Z").gameObject;
    }

    #endregion

    #region Private Methods

    /// <summary>
    /// Tries to activate the sections based on a new state
    /// </summary>
    /// <param name="state">The new state</param>
    /// <param name="a">Section 1 of the state</param>
    /// <param name="b">Section 2 of the state</param>
    /// <returns>The success of the attempt</returns>
    private bool ActivateSections(int state, int a, int b = 0)
    {
        // Return if trying to change a voxel that is occupied but not the origin of the component
        if (_voxel.IsOccupied && !_voxel.IsOrigin) return false;

        // Get the neighbour voxels
        var faceNeighbours = _voxel.GetFaceNeighboursArray();
        // If the new state is not zero
        if (state != 0)
        {
            // Validate action checking if the neighbours that will be occupied are empty
            for (int i = 1; i < _sections.Length; i++)
            {
                if (i == a || i == b)
                {
                    // If tried to move into an occupied voxel, penalize and end, not changing the state
                    if (/*i != 0 && */faceNeighbours[i - 1] != null && faceNeighbours[i - 1].IsOccupied)
                    {
                        return false;
                    }
                }
            }

            // Set the origin voxel and tag as occupied
            _voxel.IsOccupied = true;
            _voxel.IsOrigin = true;

            for (int i = 0; i < _sections.Length; i++)
            {
                // Deactivate previous active sections
                if (_sections[i].activeSelf)
                {
                    _sections[i].SetActive(false);
                    if (i != 0 && faceNeighbours[i - 1] != null) faceNeighbours[i - 1].IsOccupied = false;
                }

                // Activate current sections
                if (i == 0 || i == a || i == b)
                {
                    _sections[i].SetActive(true);
                    if (i != 0 && faceNeighbours[i - 1] != null) faceNeighbours[i - 1].IsOccupied = true;
                }
            }
        }
        else
        {
            // If target state is 0, deactivate the occupied voxels
            _voxel.IsOccupied = false;
            _voxel.IsOrigin = false;
            for (int i = 0; i < _sections.Length; i++)
            {
                if (_sections[i].activeSelf)
                {
                    _sections[i].SetActive(false);
                    if (i != 0 && faceNeighbours[i - 1] != null) faceNeighbours[i - 1].IsOccupied = false;
                }
            }
        }
        
        // Assign the new state
        State = state;
        return true;
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// Set the reference voxel
    /// </summary>
    /// <param name="voxel"></param>
    public void SetVoxel(Voxel voxel)
    {
        _voxel = voxel;
    }

    /// <summary>
    /// Attempts to change the state of the component
    /// </summary>
    /// <param name="state">The new state</param>
    /// <returns>The result of the attempt</returns>
    public bool ChangeState(int state)
    {
        bool result = false;
        switch (state)
        {
            //Empty state and linear states
            case 0:
                result = ActivateSections(state, 0);
                break;
            case 1:
                result = ActivateSections(state, 1, 2);
                break;
            case 2:
                result = ActivateSections(state, 3, 4);
                break;
            case 3:
                result = ActivateSections(state, 5, 6);
                break;

            // XY plane states
            case 4:
                result = ActivateSections(state, 1, 3);
                break;
            case 5:
                result = ActivateSections(state, 1, 4);
                break;
            case 6:
                result = ActivateSections(state, 2, 3);
                break;
            case 7:
                result = ActivateSections(state, 2, 4);
                break;

            // ZY plane states
            case 8:
                result = ActivateSections(state, 3, 5);
                break;
            case 9:
                result = ActivateSections(state, 3, 6);
                break;
            case 10:
                result = ActivateSections(state, 4, 5);
                break;
            case 11:
                result = ActivateSections(state, 4, 6);
                break;

            // XZ plane states
            case 12:
                result = ActivateSections(state, 1, 5);
                break;
            case 13:
                result = ActivateSections(state, 1, 6);
                break;
            case 14:
                result = ActivateSections(state, 2, 5);
                break;
            case 15:
                result = ActivateSections(state, 2, 6);
                break;
            case 16:
                result = true;
                break;
        }

        foreach (var section in _sections)
        {
            section.GetComponent<MeshRenderer>().material = Resources.Load<Material>($"Materials/State_{State}");
        }
        
        return result;
    }

    #endregion
}
