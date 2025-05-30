using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using UnityEngine;
using static UnityEditor.Searcher.SearcherWindow.Alignment;

public struct ChunkCoord : IEquatable<ChunkCoord>
{
    public int x;
    public int z;

    public ChunkCoord(int x, int z)
    {
        this.x = x;
        this.z = z;
    }
    public ChunkCoord(Vector3 pos)
    {

        int xCheck = Mathf.FloorToInt(pos.x);
        int zCheck = Mathf.FloorToInt(pos.z);

        x = xCheck / VoxelData.ChunkWidth;
        z = zCheck / VoxelData.ChunkWidth;

    }

    public override int GetHashCode()
    {
        // x와 z를 조합하여 해시코드 생성
        return HashCode.Combine(x, z);
    }
    public override bool Equals(object obj)
    {
        if (obj is ChunkCoord other)
            return Equals(other);
        return false; // 캐스팅 후 아래 메서드 호출
    }


    public bool Equals(ChunkCoord other)
    {
        // null 체크는 여기서만
        
        return this.x == other.x && this.z == other.z;
    }
}
public class Chunk 
{
    private GameObject chunkObject; // 청크가 생성될 대상 게임오브젝트
    private MeshRenderer meshRenderer;
    private MeshFilter meshFilter;

    public ChunkCoord coord; // 청크의 좌표


    private byte[,,] voxelMap =
  new byte[VoxelData.ChunkWidth, VoxelData.ChunkHeight, VoxelData.ChunkWidth];

    int vertexIndex = 0;
    List<Vector3> vertices = new List<Vector3>();
    List<int> triangles = new List<int>();
    List<Vector2> uvs = new List<Vector2>();

    private bool _isActive; // 청크가 활성화 상태인지 여부
    private World world;

    public Vector3 position
    {
        get { return chunkObject.transform.position; }
    }

    public bool IsActive
    {

        get { return _isActive; }
        set
        {

            _isActive = value;
            if (chunkObject != null)
                chunkObject.SetActive(value);

        }

    }
    public Chunk(ChunkCoord coord, World world, bool generate)
    {
        this.coord = coord;
        this.world = world;

        if (generate)
        {
            Init();
        }
    }

    public void Init()
    {
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
                    if (world.blockTypes[voxelMap[x, y, z]].isSolid)
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
        // 6방향의 면 그리기
        // p : -Z, +Z, +Y, -Y, -X, +X 순서로 이루어진, 큐브의 각 면에 대한 인덱스
        for (int p = 0; p < 6; p++)
        {
            // Face Check(면이 바라보는 방향으로 +1 이동하여 확인)를 했을 때 
            // Solid가 아닌 경우에만 큐브의 면이 그려지도록 하기
            // => 청크의 외곽 부분만 면이 그려지고, 내부에는 면이 그려지지 않도록
            if (CheckVoxel(pos) && !CheckVoxel(pos + VoxelData.faceChecks[p]))
            {
                byte blockID = GetBlockID(pos);
                // 각 면(삼각형 2개) 그리기

                // 1. Vertex, UV 4개 추가
                for (int i = 0; i <= 3; i++)
                {
                    vertices.Add(VoxelData.voxelVerts[VoxelData.voxelTris[p, i]] + pos);
                    //uvs.Add(VoxelData.voxelUvs[i]);
                    
                }
                AddTextureUV(world.blockTypes[blockID].GetTextureID(p));
                // 2. Triangle의 버텍스 인덱스 6개 추가
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

        // 맵 범위를 벗어나는 경우
        if (!IsVoxelInChunk(x, y, z))
            return world.IsBlockSolid(pos+position);

        // voxelMap[]의 값은 blockTypes[]의 인덱스로 사용하여,
        // 참조한 블록 타입에서 isSolid 값을 읽어온다.
        return world.blockTypes[voxelMap[x, y, z]].isSolid;
    }
    

    /// <summary> 텍스쳐 아틀라스 내에서 해당하는 ID의 텍스쳐가 위치한 UV를 uvs 리스트에 추가 </summary>
    private void AddTextureUV(int textureID)
    {
        // 아틀라스 내의 텍스쳐 가로, 세로 개수
        (int w, int h) = (VoxelData.TextureAtlasWidth, VoxelData.TextureAtlasHeight);

        int x = textureID % w;
        int y = h - (textureID / w) - 1;

        AddTextureUV(x, y);
    }

    // (x, y) : (0, 0) 기준은 좌하단
    /// <summary> 텍스쳐 아틀라스 내에서 (x, y) 위치의 텍스쳐 UV를 uvs 리스트에 추가 </summary>
    private void AddTextureUV(int x, int y)
    {
        const float uvXBeginOffset = 0.005f;
        const float uvXEndOffset = 0.005f;
        const float uvYBeginOffset = 0.01f;
        const float uvYEndOffset = 0.01f;

        if (x < 0 || y < 0 || x >= VoxelData.TextureAtlasWidth || y >= VoxelData.TextureAtlasHeight)
            throw new IndexOutOfRangeException($"텍스쳐 아틀라스의 범위를 벗어났습니다 : [x = {x}, y = {y}]");

        float nw = VoxelData.NormalizedTextureAtlasWidth;
        float nh = VoxelData.NormalizedTextureAtlasHeight;

        float uvX = x * nw;
        float uvY = y * nh;

        // 해당 텍스쳐의 uv를 LB-LT-RB-RT 순서로 추가
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
