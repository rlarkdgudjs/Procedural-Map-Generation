using System;
using System.Collections.Generic;
using UnityEngine;
using static UnityEditor.Searcher.SearcherWindow.Alignment;

public class ChunkCoord
{
    public int x;
    public int z;

    public ChunkCoord(int x, int z)
    {
        this.x = x;
        this.z = z;
    }
}
public class Chunk 
{
    private GameObject chunkObject; // ûũ�� ������ ��� ���ӿ�����Ʈ
    private MeshRenderer meshRenderer;
    private MeshFilter meshFilter;

    public ChunkCoord coord; // ûũ�� ��ǥ


    private byte[,,] voxelMap =
  new byte[VoxelData.ChunkWidth, VoxelData.ChunkHeight, VoxelData.ChunkWidth];

    int vertexIndex = 0;
    List<Vector3> vertices = new List<Vector3>();
    List<int> triangles = new List<int>();
    List<Vector2> uvs = new List<Vector2>();

    private World world;

    public Vector3 position
    {
        get { return chunkObject.transform.position; }
    }

    public bool IsActive
    {
        get => chunkObject.activeSelf;
        set => chunkObject.SetActive(value);
    }


    public Chunk(ChunkCoord coord,World world)
    {
        this.coord = coord;
        this.world = world;

        chunkObject = new GameObject();
        meshRenderer = chunkObject.AddComponent<MeshRenderer>();
        meshFilter = chunkObject.AddComponent<MeshFilter>();

        meshRenderer.material = this.world.material;
        chunkObject.transform.SetParent(world.transform);
        chunkObject.transform.position =
        new Vector3(coord.x * VoxelData.ChunkWidth, 0f, coord.z * VoxelData.ChunkWidth);
        chunkObject.name = $"Chunk [{coord.x}, {coord.z}]";

        PopulateVoxelMap();
        CreateMeshData();
        CreateMesh();
    }


    private void PopulateVoxelMap()
    {
        for (int y = 0; y < VoxelData.ChunkHeight; y++)
        {
            for (int x = 0; x < VoxelData.ChunkWidth; x++)
            {
                for (int z = 0; z < VoxelData.ChunkWidth; z++)
                {
                    voxelMap[x, y, z] = world.GetBlockType(new Vector3(x, y, z)+position);
                    //if (y == 4) { voxelMap[x, y, z] = 0; }
                }
            }
        }
    }

    private void CreateMeshData()
    {
        for (int y = 0; y < VoxelData.ChunkHeight; y++)
        {
            for (int x = 0; x < VoxelData.ChunkWidth; x++)
            {
                for (int z = 0; z < VoxelData.ChunkWidth; z++)
                {
                    AddVoxelDataToChunk(new Vector3(x, y, z));
                }
            }
        }
    }
    void CreateMesh()
    {
        Mesh mesh = new Mesh();
        mesh.vertices = vertices.ToArray();
        mesh.triangles = triangles.ToArray();
        mesh.uv = uvs.ToArray();
        mesh.RecalculateNormals();
        meshFilter.mesh = mesh;

    }
    private void AddVoxelDataToChunk(Vector3 pos)
    {
        // 6������ �� �׸���
        // p : -Z, +Z, +Y, -Y, -X, +X ������ �̷����, ť���� �� �鿡 ���� �ε���
        for (int p = 0; p < 6; p++)
        {
            // Face Check(���� �ٶ󺸴� �������� +1 �̵��Ͽ� Ȯ��)�� ���� �� 
            // Solid�� �ƴ� ��쿡�� ť���� ���� �׷������� �ϱ�
            // => ûũ�� �ܰ� �κи� ���� �׷�����, ���ο��� ���� �׷����� �ʵ���
            if (CheckVoxel(pos) && !CheckVoxel(pos + VoxelData.faceChecks[p]))
            {
                byte blockID = GetBlockID(pos);
                // �� ��(�ﰢ�� 2��) �׸���

                // 1. Vertex, UV 4�� �߰�
                for (int i = 0; i <= 3; i++)
                {
                    vertices.Add(VoxelData.voxelVerts[VoxelData.voxelTris[p, i]] + pos);
                    //uvs.Add(VoxelData.voxelUvs[i]);
                    
                }
                AddTextureUV(world.blockTypes[blockID].GetTextureID(p));
                // 2. Triangle�� ���ؽ� �ε��� 6�� �߰�
                triangles.Add(vertexIndex);
                triangles.Add(vertexIndex + 1);
                triangles.Add(vertexIndex + 2);

                triangles.Add(vertexIndex + 2);
                triangles.Add(vertexIndex + 1);
                triangles.Add(vertexIndex + 3);

                vertexIndex += 4;
            }
        }
    }
    private bool IsVoxelInChunk(int x , int y, int z)
    {
        if (x < 0 || x >= VoxelData.ChunkWidth - 1 ||
            y < 0 || y >= VoxelData.ChunkHeight - 1 ||
            z < 0 || z >= VoxelData.ChunkWidth - 1)
        {
            return false;
        }
        else
            return true;
    }

    private bool CheckVoxel(Vector3 pos)
    {
        int x = Mathf.FloorToInt(pos.x);
        int y = Mathf.FloorToInt(pos.y);
        int z = Mathf.FloorToInt(pos.z);

        // �� ������ ����� ���
        if (x < 0 || x > VoxelData.ChunkWidth - 1 ||
           y < 0 || y > VoxelData.ChunkHeight - 1 ||
           z < 0 || z > VoxelData.ChunkWidth - 1)
            return false;

        // voxelMap[]�� ���� blockTypes[]�� �ε����� ����Ͽ�,
        // ������ ��� Ÿ�Կ��� isSolid ���� �о�´�.
        return world.blockTypes[voxelMap[x, y, z]].isSolid;
    }
    

    /// <summary> �ؽ��� ��Ʋ�� ������ �ش��ϴ� ID�� �ؽ��İ� ��ġ�� UV�� uvs ����Ʈ�� �߰� </summary>
    private void AddTextureUV(int textureID)
    {
        // ��Ʋ�� ���� �ؽ��� ����, ���� ����
        (int w, int h) = (VoxelData.TextureAtlasWidth, VoxelData.TextureAtlasHeight);

        int x = textureID % w;
        int y = h - (textureID / w) - 1;

        AddTextureUV(x, y);
    }

    // (x, y) : (0, 0) ������ ���ϴ�
    /// <summary> �ؽ��� ��Ʋ�� ������ (x, y) ��ġ�� �ؽ��� UV�� uvs ����Ʈ�� �߰� </summary>
    private void AddTextureUV(int x, int y)
    {
        const float uvXBeginOffset = 0.005f;
        const float uvXEndOffset = 0.005f;
        const float uvYBeginOffset = 0.01f;
        const float uvYEndOffset = 0.01f;

        if (x < 0 || y < 0 || x >= VoxelData.TextureAtlasWidth || y >= VoxelData.TextureAtlasHeight)
            throw new IndexOutOfRangeException($"�ؽ��� ��Ʋ���� ������ ������ϴ� : [x = {x}, y = {y}]");

        float nw = VoxelData.NormalizedTextureAtlasWidth;
        float nh = VoxelData.NormalizedTextureAtlasHeight;

        float uvX = x * nw;
        float uvY = y * nh;

        // �ش� �ؽ����� uv�� LB-LT-RB-RT ������ �߰�
        uvs.Add(new Vector2(uvX + uvXBeginOffset, uvY + uvYBeginOffset));
        uvs.Add(new Vector2(uvX + uvXBeginOffset, uvY + nh - uvYEndOffset));
        uvs.Add(new Vector2(uvX + nw - uvXEndOffset, uvY + uvYBeginOffset));
        uvs.Add(new Vector2(uvX + nw - uvXEndOffset, uvY + nh - uvYEndOffset));
    }

    byte GetBlockID(in Vector3 pos)
    {
        return voxelMap[(int)pos.x, (int)pos.y, (int)pos.z];
    }
//    private bool IsSolid(in Vector3 pos)
//{
//    return world.IsBlockSolid(pos + WorldPos);
//}
}
