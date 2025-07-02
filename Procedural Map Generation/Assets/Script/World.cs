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

    // 플레이어의 이전 프레임 위치
    private (int,int) prevPlayerCoord;
    // 플레이어의 현재 프레임 위치
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
                chunks[coord] = chunk; // 청크를 딕셔너리에 추가
                chunk.Init();
            });
        });
    }
    #endregion
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    [Space]
    public int seed = 0; // 시드값 (노이즈 생성에 사용)
    private void Start()
    {
        UnityEngine.Random.InitState(seed); // 시드값 초기화
        InitPositions();
         // 월드 생성
        //GenerateWorld(); // 필요 X (UpdateChunksInViewRange()에서 수행)
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
        // 플레이어가 청크 위치를 이동한 경우, 시야 범위 갱신
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
            yield return null;// 다음 프레임으로 넘어가기
        }
        isCreatingChunks = false;
    }

    //private void CreateNewChunk(int x, int z,bool generate)
    //{
    //    ChunkCoord coord = new ChunkCoord(x, z);
    //    if (!chunks.ContainsKey(coord)) // 청크가 이미 생성되어 있는지 확인
    //    {
    //        Chunk newChunk = new Chunk(coord, this, voxelMap, meshData);
    //        chunks.Add(coord, newChunk); // 청크를 딕셔너리에 추가
    //    }
        
    //}

    // 해당 위치의 블록 타입을 결정
    //public byte GetBlockType(in Vector3 worldPos)
    //{

    //    if (!IsBlockInWorld(worldPos))
    //        return 0;

    //    // 높이 0까지는 기반암
    //    if (worldPos.y < 1)
    //        return 1;//Bedrock;

    //    // 맨 위 표면
    //    if (worldPos.y >= VoxelData.ChunkHeight - 1)
    //    {

    //        float noise = Noise.Get2DPerlin(new Vector2(worldPos.x, worldPos.z), 0f, 0.1f);

    //        if (noise < 0.5f)
    //            return 2;//Grass;
    //        else
    //            return 3;//Sand;
    //    }
    //    // 표면 ~ 기반암 사이 : 돌멩이
    //    else
    //        return 4;//Stone;
    //}
    public byte GetBlockType(in Vector3 worldPos)
    {
        // NOTE : 모든 값은 0보다 크거나 같기 때문에 Mathf.FloorToInt() 할 필요 없음

        int yPos = (int)worldPos.y;
        byte blockType = 0; // 기본 블록 타입 (공기)

        /* -----------------------------------------------
                            Immutable Pass
        ----------------------------------------------- */
        // 월드 밖 : 공기
        if (!IsBlockInWorld(worldPos))
            return 0;

        // 높이 0은 기반암
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

        // 지면
        if (yPos == terrainHeight)
        {
            return 2;
        }
        // 땅속
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
                    // 노이즈 값 : 0.0 ~ 1.0
                    if(Noise.Get3DPerlin(worldPos, lode.noiseOffset, lode.scale, lode.threshold))
                    {
                        blockType = lode.blockID;
                    }
                }
            }
        }
        return blockType;

    }
    /// <summary> 해당 위치의 복셀이 월드 내에 있는지 검사 </summary>
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


    /// <summary> 시야범위 내의 청크 생성 </summary>
    private void UpdateChunksInViewRange()
    {
        var location = GetChunkCoordFromWorldPos(player.position);
        ChunkCoord centerCoord = new ChunkCoord(location.Item1,location.Item2);
        prevPlayerCoord = currentPlayerCoord; // 기준 좌표 갱신
        int viewDist = VoxelData.ViewDistanceInChunks;
        

        // 활성 목록 : 현재 -> 이전으로 이동
        List<ChunkCoord> prevActiveChunkList = new List<ChunkCoord>(currentActiveChunkList);
        currentActiveChunkList.Clear();

        for (int x = centerCoord.x-viewDist; x < centerCoord.x + viewDist; x++)
        {
            for (int z = centerCoord.z - viewDist; z < centerCoord.z + viewDist; z++)
            {
                ChunkCoord coord = new ChunkCoord(x, z);


                // 시야 범위 내에 청크가 생성되지 않은 영역이 있을 경우, 새로 생성
                if (!chunks.ContainsKey(coord)) // 청크가 아직 없으면
                {
                    chunks.Add(coord, null); // 자리만 먼저 만들어놓고
                    RequestChunkGeneration(coord); // 스레드에서 청크 생성 요청
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
                    if(chunk != null) // 청크가 존재하고 활성화되어 있다면
                    chunk.IsActive = false; // 비활성화
                
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
    //            // 청크 좌표가 월드 범위 내에 있는지 검사
    //            if (IsChunkPosInWorld(x, z) == false)
    //                continue;

    //            // 시야 범위 내에 청크가 생성되지 않은 영역이 있을 경우, 새로 생성
    //            if (chunks[x, z] == null)
    //                CreateNewChunk(x, z);
    //        }
    //    }
    //}


