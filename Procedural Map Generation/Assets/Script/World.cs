using System.Collections.Generic;
using UnityEngine;

public class World : MonoBehaviour
{
    public Material material;
    public BlockType[] blockTypes;

    private Chunk[,] chunks = new Chunk[VoxelData.WorldSizeInChunks, VoxelData.WorldSizeInChunks];

    public Transform player;
    public Vector3 spawnPosition;

    // ���� �����ӿ� Ȱ��ȭ �Ǿ��� ûũ ���
    private List<Chunk> prevActiveChunkList = new List<Chunk>();
    // ���� �����ӿ� Ȱ��ȭ�� ûũ ���
    private List<Chunk> currentActiveChunkList = new List<Chunk>();

    // �÷��̾��� ���� ������ ��ġ
    private ChunkCoord prevPlayerCoord;
    // �÷��̾��� ���� ������ ��ġ
    private ChunkCoord currentPlayerCoord;

    public int setting = 0;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    [Space]
    public int seed = 0; // �õ尪 (������ ������ ���)
    private void Start()
    {
        Random.InitState(seed); // �õ尪 �ʱ�ȭ
        InitPositions();
        //GenerateWorld(); // �ʿ� X (UpdateChunksInViewRange()���� ����)
    }

    private void Update()
    {
        currentPlayerCoord = GetChunkCoordFromWorldPos(player.position);

        // �÷��̾ ûũ ��ġ�� �̵��� ���, �þ� ���� ����
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

    // �ش� ��ġ�� ��� Ÿ���� ����
    public byte GetBlockType(in Vector3 worldPos)
    {
        
        if (!IsBlockInWorld(worldPos))
            return 0;

        // ���� 0������ ��ݾ�
        if (worldPos.y < 1)
            return 1;//Bedrock;

        // �� �� ǥ��
        if (worldPos.y >= VoxelData.ChunkHeight - 1)
        {

            float noise = Noise.Get2DPerlin(new Vector2(worldPos.x, worldPos.z), 0f, 0.1f);
            
            if (noise < 0.5f)
                return 2;//Grass;
            else
                return 3;//Sand;
        }
        // ǥ�� ~ ��ݾ� ���� : ������
        else
            return 4;//Stone;
    }
    /// <summary> �ش� ��ġ�� ������ ���� ���� �ִ��� �˻� </summary>
    private bool IsBlockInWorld(in Vector3 pos)
    {
        
        return pos.x >= 0 && pos.x < VoxelData.WorldSizeInVoxels &&
               pos.y >= 0 && pos.y < VoxelData.ChunkHeight &&
               pos.z >= 0 && pos.z < VoxelData.WorldSizeInVoxels;
    }
    

    /// <summary> �ش� ��ġ�� ����� �ܴ����� �˻�</summary>
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


    /// <summary> �þ߹��� ���� ûũ ���� </summary>
    private void UpdateChunksInViewRange()
    {
        ChunkCoord coord = GetChunkCoordFromWorldPos(player.position);
        int viewDist = VoxelData.ViewDistanceInChunks;
        (int x, int z) viewMin = (coord.x - viewDist, coord.z - viewDist);
        (int x, int z) viewMax = (coord.x + viewDist, coord.z + viewDist);

        // Ȱ�� ��� : ���� -> �������� �̵�
        prevActiveChunkList = currentActiveChunkList;
        currentActiveChunkList = new List<Chunk>();

        for (int x = viewMin.x; x < viewMax.x; x++)
        {
            for (int z = viewMin.z; z < viewMax.z; z++)
            {
                // ûũ ��ǥ�� ���� ���� ���� �ִ��� �˻�
                if (IsChunkPosInWorld(x, z) == false)
                    continue;
                Chunk currentChunk = chunks[x, z];
                // �þ� ���� ���� ûũ�� �������� ���� ������ ���� ���, ���� ����
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
                chunk.IsActive = false; // ���� �����ӿ� Ȱ��ȭ�� ûũ�� ��Ȱ��ȭ
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
    //            // ûũ ��ǥ�� ���� ���� ���� �ִ��� �˻�
    //            if (IsChunkPosInWorld(x, z) == false)
    //                continue;

    //            // �þ� ���� ���� ûũ�� �������� ���� ������ ���� ���, ���� ����
    //            if (chunks[x, z] == null)
    //                CreateNewChunk(x, z);
    //        }
    //    }
    //}

}
