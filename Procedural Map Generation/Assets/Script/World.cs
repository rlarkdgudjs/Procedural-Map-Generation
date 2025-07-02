using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using static UnityEngine.Mesh;


public class World : MonoBehaviour
{
    public Material material;
    public BlockType[] blockTypes;
    public BiomeData biome;

    //private Chunk[,] chunks = new Chunk[VoxelData.WorldSizeInChunks, VoxelData.WorldSizeInChunks];
    private Dictionary<ChunkCoord, Chunk> chunks = new Dictionary<ChunkCoord, Chunk>();
    public Transform player;
    public Vector3 spawnPosition;

 
    List<ChunkCoord> currentActiveChunkList = new List<ChunkCoord>();
    private List<ChunkCoord> prevActiveChunkList = new List<ChunkCoord>();
    private List<Chunk> chunksToCreate = new List<Chunk>();
    private bool isCreatingChunks = false;

    // �÷��̾��� ���� ������ ��ġ
    private (int,int) prevPlayerCoord;
    // �÷��̾��� ���� ������ ��ġ
    private (int,int) currentPlayerCoord;

    public int setting = 0;

    #region Thread
    private readonly Queue<Action> mainThreadActions = new Queue<Action>();
    public void EnqueueMainThreadAction(Action action)
    {

        lock (mainThreadActions)
        {
            mainThreadActions.Enqueue(action);
        }
    }

    public void RequestChunkGeneration(ChunkCoord coord)
    {

        ThreadPool.QueueUserWorkItem((_) =>
        {
            byte[,,] voxelMap = new byte[VoxelData.ChunkWidth, VoxelData.ChunkHeight, VoxelData.ChunkWidth];
            for (int y = 0; y < VoxelData.ChunkHeight; y++)
            {
                for (int x = 0; x < VoxelData.ChunkWidth; x++)
                {
                    for (int z = 0; z < VoxelData.ChunkWidth; z++)
                    {
                        Vector3 worldPos = new Vector3(x + coord.x * VoxelData.ChunkWidth, y, z + coord.z * VoxelData.ChunkWidth);
                        voxelMap[x, y, z] = GetBlockType(worldPos);
                    }
                }
            }
            Vector3 chunkOrig = new Vector3(coord.x * VoxelData.ChunkWidth, 0f, coord.z * VoxelData.ChunkWidth);
            ChunkMeshData meshData = ChunkMeshBuilder.Build(voxelMap, this,chunkOrig);

            EnqueueMainThreadAction(() =>
            {
                
                Chunk chunk = new Chunk(coord, this, voxelMap, meshData);
                chunks[coord] = chunk; // ûũ�� ��ųʸ��� �߰�
                chunk.Init();
            });
        });
    }
    #endregion
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    [Space]
    public int seed = 0; // �õ尪 (������ ������ ���)
    private void Start()
    {
        UnityEngine.Random.InitState(seed); // �õ尪 �ʱ�ȭ
        InitPositions();
         // ���� ����
        //GenerateWorld(); // �ʿ� X (UpdateChunksInViewRange()���� ����)
    }

    private void Update()
    {
        currentPlayerCoord = GetChunkCoordFromWorldPos(player.position);
        lock (mainThreadActions)
        {
            while (mainThreadActions.Count > 0)
            {
                var action = mainThreadActions.Dequeue();
                action?.Invoke();
            }
        }
        // �÷��̾ ûũ ��ġ�� �̵��� ���, �þ� ���� ����
        if (!currentPlayerCoord.Equals(prevPlayerCoord))
        {

            UpdateChunksInViewRange();
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

    //private void CreateNewChunk(int x, int z,bool generate)
    //{
    //    ChunkCoord coord = new ChunkCoord(x, z);
    //    if (!chunks.ContainsKey(coord)) // ûũ�� �̹� �����Ǿ� �ִ��� Ȯ��
    //    {
    //        Chunk newChunk = new Chunk(coord, this, voxelMap, meshData);
    //        chunks.Add(coord, newChunk); // ûũ�� ��ųʸ��� �߰�
    //    }
        
    //}

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
    //private bool IsBlockInWorld(in Vector3 pos)
    //{

    //    return pos.x >= 0 && pos.x < VoxelData.WorldSizeInVoxels &&
    //           pos.y >= 0 && pos.y < VoxelData.ChunkHeight &&
    //           pos.z >= 0 && pos.z < VoxelData.WorldSizeInVoxels;
    //}
    private bool IsBlockInWorld(Vector3 pos)
    {
        return pos.y >= 0 && pos.y < VoxelData.ChunkHeight;
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
    private (int,int) GetChunkCoordFromWorldPos(in Vector3 worldPos)
    {
        int x = (int)(worldPos.x / VoxelData.ChunkWidth);
        int z = (int)(worldPos.z / VoxelData.ChunkWidth);
        return (x, z);
    }

    private void InitPositions()
    {
        spawnPosition = new Vector3(0.5f, VoxelData.ChunkHeight, 0.5f);
        player.position = spawnPosition;

        prevPlayerCoord = (-1, -1);
        currentPlayerCoord = GetChunkCoordFromWorldPos(player.position);
    }


    /// <summary> �þ߹��� ���� ûũ ���� </summary>
    private void UpdateChunksInViewRange()
    {
        var location = GetChunkCoordFromWorldPos(player.position);
        ChunkCoord centerCoord = new ChunkCoord(location.Item1,location.Item2);
        prevPlayerCoord = currentPlayerCoord; // ���� ��ǥ ����
        int viewDist = VoxelData.ViewDistanceInChunks;
        

        // Ȱ�� ��� : ���� -> �������� �̵�
        List<ChunkCoord> prevActiveChunkList = new List<ChunkCoord>(currentActiveChunkList);
        currentActiveChunkList.Clear();

        for (int x = centerCoord.x-viewDist; x < centerCoord.x + viewDist; x++)
        {
            for (int z = centerCoord.z - viewDist; z < centerCoord.z + viewDist; z++)
            {
                ChunkCoord coord = new ChunkCoord(x, z);


                // �þ� ���� ���� ûũ�� �������� ���� ������ ���� ���, ���� ����
                if (!chunks.ContainsKey(coord)) // ûũ�� ���� ������
                {
                    chunks.Add(coord, null); // �ڸ��� ���� ��������
                    RequestChunkGeneration(coord); // �����忡�� ûũ ���� ��û
                }
                else if (chunks[coord]!=null&&!chunks[coord].IsActive)
                {
                    chunks[coord].IsActive = true;
                }

                currentActiveChunkList.Add(coord);

                for (int i = 0; i < prevActiveChunkList.Count; i++)
                {

                    if (prevActiveChunkList[i].Equals(coord))
                        prevActiveChunkList.RemoveAt(i);

                }
            }

            
        }
        foreach (ChunkCoord c in prevActiveChunkList)
        {
            if(chunks.TryGetValue(c, out Chunk chunk))
            {
                    if(chunk != null) // ûũ�� �����ϰ� Ȱ��ȭ�Ǿ� �ִٸ�
                    chunk.IsActive = false; // ��Ȱ��ȭ
                
            }
            
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


