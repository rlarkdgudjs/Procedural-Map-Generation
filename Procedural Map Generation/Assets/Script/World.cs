using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class World : MonoBehaviour
{
    public Material material;
    public BlockType[] blockTypes;
    public BiomeData biome;

    private Chunk[,] chunks = new Chunk[VoxelData.WorldSizeInChunks, VoxelData.WorldSizeInChunks];

    public Transform player;
    public Vector3 spawnPosition;

 
    List<ChunkCoord> currentActiveChunkList = new List<ChunkCoord>();
    private List<ChunkCoord> prevActiveChunkList = new List<ChunkCoord>();
    private List<Chunk> chunksToCreate = new List<Chunk>();
    private bool isCreatingChunks = false;

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
        GenerateWorld(); // ���� ����
        //GenerateWorld(); // �ʿ� X (UpdateChunksInViewRange()���� ����)
    }

    private void Update()
    {
        currentPlayerCoord = GetChunkCoordFromWorldPos(player.position);

        // �÷��̾ ûũ ��ġ�� �̵��� ���, �þ� ���� ����
        if (!currentPlayerCoord.Equals(prevPlayerCoord))
        {
           
            UpdateChunksInViewRange(); 
        }
        if (chunksToCreate.Count > 0 && !isCreatingChunks)
            StartCoroutine("CreateChunks");

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
                chunks[x, z] = new Chunk(new ChunkCoord(x, z), this, true);

                //currentActiveChunkList.Add(chunks[x,z]);
            }
        }
    }

    IEnumerator CreateChunks()
    {
        isCreatingChunks = true;
        while (chunksToCreate.Count > 0)
        {
            chunksToCreate[0].Init();
            chunksToCreate.RemoveAt(0);
            yield return null;// ���� ���������� �Ѿ��
        }
        isCreatingChunks = false;
    }

    private void CreateNewChunk(int x, int z,bool generate)
    {
        chunks[x, z] = new Chunk(new ChunkCoord(x, z), this,generate);
    }

    // �ش� ��ġ�� ��� Ÿ���� ����
    //public byte GetBlockType(in Vector3 worldPos)
    //{

    //    if (!IsBlockInWorld(worldPos))
    //        return 0;

    //    // ���� 0������ ��ݾ�
    //    if (worldPos.y < 1)
    //        return 1;//Bedrock;

    //    // �� �� ǥ��
    //    if (worldPos.y >= VoxelData.ChunkHeight - 1)
    //    {

    //        float noise = Noise.Get2DPerlin(new Vector2(worldPos.x, worldPos.z), 0f, 0.1f);

    //        if (noise < 0.5f)
    //            return 2;//Grass;
    //        else
    //            return 3;//Sand;
    //    }
    //    // ǥ�� ~ ��ݾ� ���� : ������
    //    else
    //        return 4;//Stone;
    //}
    public byte GetBlockType(in Vector3 worldPos)
    {
        // NOTE : ��� ���� 0���� ũ�ų� ���� ������ Mathf.FloorToInt() �� �ʿ� ����

        int yPos = (int)worldPos.y;
        byte blockType = 0; // �⺻ ��� Ÿ�� (����)

        /* -----------------------------------------------
                            Immutable Pass
        ----------------------------------------------- */
        // ���� �� : ����
        if (!IsBlockInWorld(worldPos))
            return 0;

        // ���� 0�� ��ݾ�
        if (yPos == 0)
            return 1;

        /* -----------------------------------------------
                        Basic Terrain Pass
        ----------------------------------------------- */
        // noise : 0.0 ~ 1.0
        float noise = Noise.Get2DPerlin(new Vector2(worldPos.x, worldPos.z), 0f, biome.terrainScale);
        float terrainHeight = (int)(biome.terrainHeightRange * noise) + biome.solidGroindHeight;

        // terrainHeight : 0 ~ VoxelData.ChunkHeight(15)
        if (yPos > terrainHeight)
        {
            return 0;
        }

        // ����
        if (yPos == terrainHeight)
        {
            return 2;
        }
        // ����
        else if (terrainHeight - 4 < yPos && yPos < terrainHeight)
        {
            return 3;
        }
        else
        {
            blockType = 5;
            
        }
        /* --------------------------------------------- *
        *              Second Terrain Pass              *
        * --------------------------------------------- */

        if (blockType == 5)
        {
            foreach (var lode in biome.lodes)
            {
                if ( lode.minHeight < yPos && yPos < lode.maxHeight)
                {
                    // ������ �� : 0.0 ~ 1.0
                    if(Noise.Get3DPerlin(worldPos, lode.noiseOffset, lode.scale, lode.threshold))
                    {
                        blockType = lode.blockID;
                    }
                }
            }
        }
        return blockType;

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
        prevPlayerCoord = currentPlayerCoord; // ���� �������� �÷��̾� ��ǥ ����
        int viewDist = VoxelData.ViewDistanceInChunks;
        (int x, int z) viewMin = (coord.x - viewDist, coord.z - viewDist);
        (int x, int z) viewMax = (coord.x + viewDist, coord.z + viewDist);

        // Ȱ�� ��� : ���� -> �������� �̵�
        List<ChunkCoord> prevActiveChunkList = new List<ChunkCoord>(currentActiveChunkList);
        

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
                    CreateNewChunk(x, z,false);
                    currentChunk = chunks[x, z]; // ���� ������ ûũ ����
                    chunksToCreate.Add(currentChunk); // ûũ ���� ��⿭�� �߰�
                }
                else if (chunks[x,z].IsActive == false)
                {
                    chunks[x, z].IsActive = true;
                }
                currentActiveChunkList.Add(new ChunkCoord(x,z));

                for (int i = 0; i < prevActiveChunkList.Count; i++)
                {

                    if (prevActiveChunkList[i].Equals(new ChunkCoord(x, z)))
                        prevActiveChunkList.RemoveAt(i);

                }
            }

            
        }
        foreach (ChunkCoord c in prevActiveChunkList)
            chunks[c.x, c.z].IsActive = false;
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
