using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
//using System;

public class VoxelGridMeshBound
{
    #region Public fields
    public Voxel[,,] Voxels;
    public Vector3Int GridSize;
    public readonly float VoxelSize;
    public Corner[,,] Corners;
    public Face[][,,] Faces = new Face[3][,,];
    public Edge[][,,] Edges = new Edge[3][,,];
    public Vector3 Origin;
    public Vector3 Corner;
    //List o placed blocks
    public List<Voxel> JointVoxels = new List<Voxel>();
    #endregion
    #region private fields
    private bool _showVoxels = false;
    private GameObject _goVoxelPrefab;

    #endregion
    #region Public dynamic getters
    /// <summary>
    /// Return the voxels in a flat list rather than a threedimensional array
    /// </summary>
    public IEnumerable<Voxel> FlattenedVoxels
    {
        get
        {
            for (int x = 0; x < GridSize.x; x++)
                for (int y = 0; y < GridSize.y; y++)
                    for (int z = 0; z < GridSize.z; z++)
                        yield return Voxels[x, y, z];
        }
    }
    public Voxel GetVoxelByIndex(Vector3Int index) => Voxels[index.x, index.y, index.z];
    /// <summary>
    /// Return all blocks that are not allready place in the grid
    /// </summary>

    #endregion

    #region constructor
    /// <summary>
    /// Constructor for the voxelgrid object. To be called in the Building manager. Origin set to 0,0,0
    /// </summary>
    /// <param name="gridDimensions">The dimensions of the grid</param>
    /// <param name="voxelSize">The size of one voxel</param>
    public VoxelGridMeshBound(Vector3Int gridDimensions, float voxelSize)
    {
        GridSize = gridDimensions;
        _goVoxelPrefab = Resources.Load("Prefabs/VoxelCube") as GameObject;
        VoxelSize = voxelSize;
        Origin = Vector3.zero;
        CreateVoxelGrid();
    }
    /// <summary>
    /// Constructor for the voxelgrid object. To be called in the Building manager
    /// </summary>
    /// <param name="gridDimensions">The dimensions of the grid</param>
    /// <param name="voxelSize">The size of one voxel</param>
    public VoxelGridMeshBound(Vector3Int gridDimensions, float voxelSize, Vector3 origin)
    {
        GridSize = gridDimensions;
        _goVoxelPrefab = Resources.Load("Prefabs/VoxelCube") as GameObject;
        VoxelSize = voxelSize;
        Origin = origin;
        CreateVoxelGrid();
    }
    /// <summary>
    /// Generate the voxelgrid from public Voxel(Vector3Int index, List<Vector3Int> possibleDirections)
    /// </summary>
    private void CreateVoxelGrid()
    {
        Voxels = new Voxel[GridSize.x, GridSize.y, GridSize.z];
        for (int x = 0; x < GridSize.x; x++)
        {
            for (int y = 0; y < GridSize.y; y++)
            {
                for (int z = 0; z < GridSize.z; z++)
                {
                    //Voxels[x, y, z] = new Voxel(new Vector3Int(x, y, z), _goVoxelPrefab, this);
                    Voxels[x, y, z] = new Voxel(new Vector3Int(x, y, z), _goVoxelPrefab, this);
                }
            }
        }
        MakeFaces();
        MakeCorners();
        MakeEdges();
    }
    #endregion
    #region Grid elements constructors

    /// <summary>
    /// Creates the Faces of each <see cref="Voxel"/>
    /// </summary>
    private void MakeFaces()
    {
        // make faces
        Faces[0] = new Face[GridSize.x + 1, GridSize.y, GridSize.z];
        for (int x = 0; x < GridSize.x + 1; x++)
            for (int y = 0; y < GridSize.y; y++)
                for (int z = 0; z < GridSize.z; z++)
                {
                    Faces[0][x, y, z] = new Face(x, y, z, Axis.X, this);
                }
        Faces[1] = new Face[GridSize.x, GridSize.y + 1, GridSize.z];
        for (int x = 0; x < GridSize.x; x++)
            for (int y = 0; y < GridSize.y + 1; y++)
                for (int z = 0; z < GridSize.z; z++)
                {
                    Faces[1][x, y, z] = new Face(x, y, z, Axis.Y, this);
                }
        Faces[2] = new Face[GridSize.x, GridSize.y, GridSize.z + 1];
        for (int x = 0; x < GridSize.x; x++)
            for (int y = 0; y < GridSize.y; y++)
                for (int z = 0; z < GridSize.z + 1; z++)
                {
                    Faces[2][x, y, z] = new Face(x, y, z, Axis.Z, this);
                }
    }
    /// <summary>
    /// Creates the Corners of each Voxel
    /// </summary>
    private void MakeCorners()
    {
        Corner = new Vector3(Origin.x - VoxelSize / 2, Origin.y - VoxelSize / 2, Origin.z - VoxelSize / 2);
        Corners = new Corner[GridSize.x + 1, GridSize.y + 1, GridSize.z + 1];
        for (int x = 0; x < GridSize.x + 1; x++)
            for (int y = 0; y < GridSize.y + 1; y++)
                for (int z = 0; z < GridSize.z + 1; z++)
                {
                    Corners[x, y, z] = new Corner(new Vector3Int(x, y, z), this);
                }
    }
    /// <summary>
    /// Creates the Edges of each Voxel
    /// </summary>
    private void MakeEdges()
    {
        Edges[2] = new Edge[GridSize.x + 1, GridSize.y + 1, GridSize.z];
        for (int x = 0; x < GridSize.x + 1; x++)
            for (int y = 0; y < GridSize.y + 1; y++)
                for (int z = 0; z < GridSize.z; z++)
                {
                    Edges[2][x, y, z] = new Edge(x, y, z, Axis.Z, this);
                }
        Edges[0] = new Edge[GridSize.x, GridSize.y + 1, GridSize.z + 1];
        for (int x = 0; x < GridSize.x; x++)
            for (int y = 0; y < GridSize.y + 1; y++)
                for (int z = 0; z < GridSize.z + 1; z++)
                {
                    Edges[0][x, y, z] = new Edge(x, y, z, Axis.X, this);
                }
        Edges[1] = new Edge[GridSize.x + 1, GridSize.y, GridSize.z + 1];
        for (int x = 0; x < GridSize.x + 1; x++)
            for (int y = 0; y < GridSize.y; y++)
                for (int z = 0; z < GridSize.z + 1; z++)
                {
                    Edges[1][x, y, z] = new Edge(x, y, z, Axis.Y, this);
                }
    }
    #endregion

    #region Block functionality
    //Old Block Combinatorial stuff
    #endregion

    #region Grid operations
    /// <summary>
    /// Get the Faces of the <see cref="VoxelGrid"/>
    /// </summary>
    /// <returns>All the faces</returns>
    public IEnumerable<Face> GetFaces()
    {
        for (int n = 0; n < 3; n++)
        {
            int xSize = Faces[n].GetLength(0);
            int ySize = Faces[n].GetLength(1);
            int zSize = Faces[n].GetLength(2);

            for (int x = 0; x < xSize; x++)
                for (int y = 0; y < ySize; y++)
                    for (int z = 0; z < zSize; z++)
                    {
                        yield return Faces[n][x, y, z];
                    }
        }
    }

    /// <summary>
    /// Get the Voxels of the <see cref="VoxelGrid"/>
    /// </summary>
    /// <returns>All the Voxels</returns>
    public IEnumerable<Voxel> GetVoxels()
    {
        for (int x = 0; x < GridSize.x; x++)
            for (int y = 0; y < GridSize.y; y++)
                for (int z = 0; z < GridSize.z; z++)
                {
                    yield return Voxels[x, y, z];
                }
    }

    /// <summary>
    /// Get the Corners of the <see cref="VoxelGrid"/>
    /// </summary>
    /// <returns>All the Corners</returns>
    public IEnumerable<Corner> GetCorners()
    {
        for (int x = 0; x < GridSize.x + 1; x++)
            for (int y = 0; y < GridSize.y + 1; y++)
                for (int z = 0; z < GridSize.z + 1; z++)
                {
                    yield return Corners[x, y, z];
                }
    }

    /// <summary>
    /// Get the Edges of the <see cref="VoxelGrid"/>
    /// </summary>
    /// <returns>All the edges</returns>
    public IEnumerable<Edge> GetEdges()
    {
        for (int n = 0; n < 3; n++)
        {
            int xSize = Edges[n].GetLength(0);
            int ySize = Edges[n].GetLength(1);
            int zSize = Edges[n].GetLength(2);

            for (int x = 0; x < xSize; x++)
                for (int y = 0; y < ySize; y++)
                    for (int z = 0; z < zSize; z++)
                    {
                        yield return Edges[n][x, y, z];
                    }
        }
    }

    public void DisableInsideBoundingMesh()
    {
        foreach (var voxel in GetVoxels())
        {
            if (BoundingMesh.IsInsideCentre(voxel)) voxel.Status = VoxelState.Dead;
        }
    }

    public void DisableOutsideBoundingMesh()
    {
        foreach (var voxel in GetVoxels())
        {
            if (!BoundingMesh.IsInsideCentre(voxel)) voxel.Status = VoxelState.Dead;
        }
    }

    #endregion

}
