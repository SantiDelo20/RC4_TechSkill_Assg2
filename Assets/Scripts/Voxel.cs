using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public enum VoxelState { Dead = 0, Alive = 1, Available = 2 }

public class Voxel : IEquatable<Voxel>
{
    #region Public fields

    public Vector3Int Index;
    public List<Face> Faces = new List<Face>(6);
    public Vector3 Center => (Index + _voxelGrid.Origin) * _size;
    public bool IsActive;

    public bool IsOccupied;
    public bool IsVoid;
    public bool IsOrigin;

    public VoxelState voxelStatus;
    public GameObject _voxelGO;

    #endregion

    #region Private fields
    private List<Corner> _corners;
    private VoxelState _voxelStatus;
    private bool _showVoxel;
    #endregion

    #region Protected fields

    
    protected VoxelGrid _voxelGrid;
    protected VoxelGridMeshBound _voxelGridMesh;
    protected float _size;

    #endregion

    #region Contructors
    
    public bool ShowVoidVoxel
    {
        get
        {
            return _showVoxel;
        }
        set
        {
            _showVoxel = value;
            if (!value)
                _voxelGO.SetActive(value);
            else
                _voxelGO.SetActive(Status == VoxelState.Alive);
        }
    }
    

    /// <summary>
    /// Get and set the status of the voxel. When setting the status, the linked gameobject will be enable or disabled depending on the state.
    /// </summary>
    public VoxelState Status
    {
        get
        {
            return _voxelStatus;
        }
        set
        {
            _voxelGO?.SetActive(value == VoxelState.Alive && _showVoxel);
            _voxelStatus = value;
        }
    }
    

    /// <summary>
    /// Get the centre point of the voxel in worldspace
    /// </summary>
    public Vector3 Centre => _voxelGrid.Origin + (Vector3)Index * _voxelGrid.VoxelSize + Vector3.one * 0.5f * _voxelGrid.VoxelSize;



    /// <summary>
    /// Creates a regular voxel on a voxel grid
    /// </summary>
    /// <param name="index">The index of the Voxel</param>
    /// <param name="voxelgrid">The <see cref="VoxelGrid"/> this <see cref="Voxel"/> is attached to</param>
    /// <param name="voxelGameObject">The <see cref="GameObject"/> used on the Voxel</param>
    public Voxel(Vector3Int index, VoxelGrid voxelGrid, float sizeFactor = 1f)
    {
        Index = index;
        _voxelGrid = voxelGrid;
        _size = _voxelGrid.VoxelSize;
    }

    public Voxel(Vector3Int index, GameObject goVoxel, VoxelGridMeshBound grid)
    {
        _voxelGridMesh = grid;
        Index = index;
        _voxelGO = GameObject.Instantiate(goVoxel, Centre, Quaternion.identity);
        _voxelGO.GetComponent<VoxelTrigger>().TriggerVoxel = this;
        _voxelGO.transform.localScale = Vector3.one * _voxelGrid.VoxelSize * 0.95f;
        Status = VoxelState.Available;
    }

    public List<Corner> Corners
    {
        get
        {
            if (_corners == null)
            {
                _corners = new List<Corner>();
                _corners.Add(_voxelGrid.Corners[Index.x, Index.y, Index.z]);
                _corners.Add(_voxelGrid.Corners[Index.x + 1, Index.y, Index.z]);
                _corners.Add(_voxelGrid.Corners[Index.x, Index.y + 1, Index.z]);
                _corners.Add(_voxelGrid.Corners[Index.x, Index.y, Index.z + 1]);
                _corners.Add(_voxelGrid.Corners[Index.x + 1, Index.y + 1, Index.z]);
                _corners.Add(_voxelGrid.Corners[Index.x, Index.y + 1, Index.z + 1]);
                _corners.Add(_voxelGrid.Corners[Index.x + 1, Index.y, Index.z + 1]);
                _corners.Add(_voxelGrid.Corners[Index.x + 1, Index.y + 1, Index.z + 1]);
            }
            return _corners;
        }
    }

    /// <summary>
    /// Generic constructor, allows the use of inheritance
    /// </summary>
    public Voxel() { }

    #endregion

    #region Public methods

    /// <summary>
    /// Get the neighbouring voxels at each face, if it exists
    /// </summary>
    /// <returns>All neighbour voxels</returns>
    public IEnumerable<Voxel> GetFaceNeighbours()
    {
        int x = Index.x;
        int y = Index.y;
        int z = Index.z;
        var s = _voxelGrid.GridSize;

        if (x != s.x - 1) yield return _voxelGrid.Voxels[x + 1, y, z];
        if (x != 0) yield return _voxelGrid.Voxels[x - 1, y, z];

        if (y != s.y - 1) yield return _voxelGrid.Voxels[x, y + 1, z];
        if (y != 0) yield return _voxelGrid.Voxels[x, y - 1, z];

        if (z != s.z - 1) yield return _voxelGrid.Voxels[x, y, z + 1];
        if (z != 0) yield return _voxelGrid.Voxels[x, y, z - 1];
    }

    public Voxel[] GetFaceNeighboursArray()
    {
        Voxel[] result = new Voxel[6];

        int x = Index.x;
        int y = Index.y;
        int z = Index.z;
        var s = _voxelGrid.GridSize;

        if (x != s.x - 1) result[0] = _voxelGrid.Voxels[x + 1, y, z];
        else result[0] = null;

        if (x != 0) result[1] = _voxelGrid.Voxels[x - 1, y, z];
        else result[1] = null;

        if (y != s.y - 1) result[2] = _voxelGrid.Voxels[x, y + 1, z];
        else result[2] = null;

        if (y != 0) result[3] = _voxelGrid.Voxels[x, y - 1, z];
        else result[3] = null;

        if (z != s.z - 1) result[4] = _voxelGrid.Voxels[x, y, z + 1];
        else result[4] = null;

        if (z != 0) result[5] = _voxelGrid.Voxels[x, y, z - 1];
        else result[5] = null;

        return result;
    }

    /// <summary>
    /// Activates the visibility of this voxel
    /// </summary>
    public void ActivateVoxel(bool state)
    {
        IsActive = state;
    }
    #endregion

    #region Equality checks

    /// <summary>
    /// Checks if two Voxels are equal based on their Index
    /// </summary>
    /// <param name="other">The <see cref="Voxel"/> to compare with</param>
    /// <returns>True if the Voxels are equal</returns>
    public bool Equals(Voxel other)
    {
        return (other != null) && (Index == other.Index);
    }

    /// <summary>
    /// Get the HashCode of this <see cref="Voxel"/> based on its Index
    /// </summary>
    /// <returns>The HashCode as an Int</returns>
    public override int GetHashCode()
    {
        return Index.GetHashCode();
    }

    #endregion
}