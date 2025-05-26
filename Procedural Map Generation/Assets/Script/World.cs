using System.Collections.Generic;
using UnityEngine;

public class World : MonoBehaviour
{
    public Material material;
    public BlockType[] blockTypes;

    private Chunk[,] chunks = new Chunk[VoxelData.WorldSizeInChunks, VoxelData.WorldSizeInChunks];

    public Transform player;
    public Vector3 spawnPosition;

    // 이전 프레임에 활성화 되었던 청크 목록
    private List<Chunk> prevActiveChunkList = new List<Chunk>();
    // 현재 프레임에 활성화된 청크 목록
    private List<Chunk> currentActiveChunkList = new List<Chunk>();

    // 플레이어의 이전 프레임 위치
    private ChunkCoord prevPlayerCoord;
    // 플레이어의 현재 프레임 위치
    private ChunkCoord currentPlayerCoord;

    public int setting = 0;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    [Space]
    public int seed = 0; // 시드값 (노이즈 생성에 사용)
    private void Start()
    {
        Random.InitState(seed); // 시드값 초기화
        InitPositions();
        //GenerateWorld(); // 필요 X (UpdateChunksInViewRange()에서 수행)
    }

    private void Update()
    {
        currentPlayerCoord = GetChunkCoordFromWorldPos(player.position);

        // 플레이어가 청크 위치를 이동한 경우, 시야 범위 갱신
        if (!prevPlayerCoord.Equals(currentPlayerCoord))
            UpdateChunksInViewRange();

        prevPlayerCoord = currentPlayerCoord;
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

        // 높이 0까지는 기반암
        if (worldPos.y < 1)
            return 1;//Bedrock;

        // 맨 위 표면
        if (worldPos.y >= VoxelData.ChunkHeight - 1)
        {

            float noise = Noise.Get2DPerlin(new Vector2(worldPos.x, worldPos.z), 0f, 0.1f);
            
            if (noise < 0.5f)
                return 2;//Grass;
            else
                return 3;//Sand;
        }
        // 표면 ~ 기반암 사이 : 돌멩이
        else
            return 4;//Stone;
    }
    /// <summary> 해당 위치의 복셀이 월드 내에 있는지 검사 </summary>
    private bool IsBlockInWorld(in Vector3 pos)
    {
        
        return pos.x >= 0 && pos.x < VoxelData.WorldSizeInVoxels &&
               pos.y >= 0 && pos.y < VoxelData.ChunkHeight &&
               pos.z >= 0 && pos.z < VoxelData.WorldSizeInVoxels;
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

        prevPlayerCoord = new ChunkCoord(-1, -1);
        currentPlayerCoord = GetChunkCoordFromWorldPos(player.position);
    }


    /// <summary> 시야범위 내의 청크 생성 </summary>
    private void UpdateChunksInViewRange()
    {
        ChunkCoord coord = GetChunkCoordFromWorldPos(player.position);
        int viewDist = VoxelData.ViewDistanceInChunks;
        (int x, int z) viewMin = (coord.x - viewDist, coord.z - viewDist);
        (int x, int z) viewMax = (coord.x + viewDist, coord.z + viewDist);

        // 활성 목록 : 현재 -> 이전으로 이동
        prevActiveChunkList = currentActiveChunkList;
        currentActiveChunkList = new List<Chunk>();

        for (int x = viewMin.x; x < viewMax.x; x++)
        {
            for (int z = viewMin.z; z < viewMax.z; z++)
            {
                // 청크 좌표가 월드 범위 내에 있는지 검사
                if (IsChunkPosInWorld(x, z) == false)
                    continue;
                Chunk currentChunk = chunks[x, z];
                // 시야 범위 내에 청크가 생성되지 않은 영역이 있을 경우, 새로 생성
                if (chunks[x, z] == null)
                { 
                    CreateNewChunk(x, z);
                    currentChunk = chunks[x,z];
                }
                else if (chunks[x,z].IsActive == false)
                {
                    chunks[x, z].IsActive = true;
                }
                currentActiveChunkList.Add(currentChunk);

                if (prevActiveChunkList.Contains(currentChunk))
                {
                  
                    prevActiveChunkList.Remove(currentChunk);
                }
            }

            foreach (var chunk in prevActiveChunkList)
            {
                chunk.IsActive = false; // 이전 프레임에 활성화된 청크는 비활성화
            }
        }
    }
    //private void UpdateChunksInViewRange2()
    //{
    //    ChunkCoord coord = GetChunkCoordFromWorldPos(player.position);
    //    int viewDist = VoxelData.ViewDistanceInChunks;
    //    (int x, int z) viewMin = (coord.x - viewDist, coord.z - viewDist);
    //    (int x, int z) viewMax = (coord.x + viewDist, coord.z + viewDist);

    //    for (int x = viewMin.x; x < viewMax.x; x++)
    //    {
    //        for (int z = viewMin.z; z < viewMax.z; z++)
    //        {
    //            // 청크 좌표가 월드 범위 내에 있는지 검사
    //            if (IsChunkPosInWorld(x, z) == false)
    //                continue;

    //            // 시야 범위 내에 청크가 생성되지 않은 영역이 있을 경우, 새로 생성
    //            if (chunks[x, z] == null)
    //                CreateNewChunk(x, z);
    //        }
    //    }
    //}

}
