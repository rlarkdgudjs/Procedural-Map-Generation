using UnityEngine;

public class World : MonoBehaviour
{
    public Material material;
    public BlockType[] blockTypes;

    private Chunk[,] chunks = new Chunk[VoxelData.WorldSizeInChunks, VoxelData.WorldSizeInChunks];

    public Transform player;
    public Vector3 spawnPosition;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    private void Start()
    {
        InitPositions();
        GenerateWorld();
    }

    private void Update()
    {
        UpdateChunksInViewRange();
    }
    private void GenerateWorld()
    {
        int center = VoxelData.WorldSizeInChunks / 2;
        int viewMin = center - VoxelData.ViewDistanceInChunks;
        int viewMax = center + VoxelData.ViewDistanceInChunks;

        for (int x = viewMin; x < viewMax; x++)
        {
            for (int z = viewMin; z < viewMax; z++)
            {
                CreateNewChunk(x, z);
            }
        }
    }

    private void CreateNewChunk(int x, int z)
    {
        chunks[x, z] = new Chunk(new ChunkCoord(x, z), this);
    }

    // 해당 위치의 블록 타입을 결정
    public byte GetBlockType(in Vector3 worldPos)
    {
        if (!IsBlockInWorld(worldPos))
            return 0;

        if (worldPos.y >= VoxelData.ChunkHeight - 1)
            return 1;
        else
            return 2;
    }
    /// <summary> 해당 위치의 복셀이 월드 내에 있는지 검사 </summary>
    private bool IsBlockInWorld(in Vector3 pos)
    {
        return pos.x >= 0 && pos.x < VoxelData.WorldSizeInVoxels &&
               pos.y >= 0 && pos.y < VoxelData.WorldSizeInVoxels &&
               pos.z >= 0 && pos.z < VoxelData.ChunkHeight;
    }
    

    /// <summary> 해당 위치의 블록이 단단한지 검사</summary>
    public bool IsBlockSolid(in Vector3 worldPos)
    {
        return blockTypes[GetBlockType(worldPos)].isSolid;
    }

    private bool IsChunkPosInWorld(int x, int z)
    {
        return x >= 0 && x < VoxelData.WorldSizeInChunks &&
               z >= 0 && z < VoxelData.WorldSizeInChunks;
    }
    private ChunkCoord GetChunkCoordFromWorldPos(in Vector3 worldPos)
    {
        int x = (int)(worldPos.x / VoxelData.ChunkWidth);
        int z = (int)(worldPos.z / VoxelData.ChunkWidth);
        return new ChunkCoord(x, z);
    }

    private void InitPositions()
    {
        spawnPosition = new Vector3(
            VoxelData.WorldSizeInVoxels * 0.5f,
            VoxelData.ChunkHeight,
            VoxelData.WorldSizeInVoxels * 0.5f
        ); 
        player.position = spawnPosition;
    }
   

    /// <summary> 시야범위 내의 청크 생성 </summary>
    private void UpdateChunksInViewRange()
    {
        ChunkCoord coord = GetChunkCoordFromWorldPos(player.position);
        int viewDist = VoxelData.ViewDistanceInChunks;
        (int x, int z) viewMin = (coord.x - viewDist, coord.z - viewDist);
        (int x, int z) viewMax = (coord.x + viewDist, coord.z + viewDist);

        for (int x = viewMin.x; x < viewMax.x; x++)
        {
            for (int z = viewMin.z; z < viewMax.z; z++)
            {
                // 청크 좌표가 월드 범위 내에 있는지 검사
                if (IsChunkPosInWorld(x, z) == false)
                    continue;

                // 시야 범위 내에 청크가 생성되지 않은 영역이 있을 경우, 새로 생성
                if (chunks[x, z] == null)
                    CreateNewChunk(x, z);
            }
        }
    }
}
