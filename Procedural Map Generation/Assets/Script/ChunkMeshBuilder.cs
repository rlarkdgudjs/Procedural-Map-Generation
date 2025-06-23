using NUnit.Framework;
using UnityEngine;
using System.Collections.Generic;
public class ChunkMeshData
{
    public List<Vector3> vertices = new();
    public List<int> triangles = new();
    public List<Vector2> uvs = new();
}

public static class ChunkMeshBuilder 
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public static ChunkMeshData Build(byte[,,] voxelMap, World world,Vector3 chunkOrig)
    {

        var data = new ChunkMeshData();
        int vertexIndex = 0;

        for (int y  = 0; y < VoxelData.ChunkHeight; y++)
        {
            for (int x = 0; x < VoxelData.ChunkWidth; x++)
            {
                for (int z = 0; z < VoxelData.ChunkWidth; z++)
                {
                    byte blockID = voxelMap[x, y, z];
                    if(!world.blockTypes[blockID].isSolid)
                        continue;
                    Vector3 pos = new Vector3(x, y, z);
                    for (int p = 0; p<6; p++)
                    {
                        // Check if the face is visible
                        if ( !IsVoxelSolid(pos + VoxelData.faceChecks[p],voxelMap,world,chunkOrig) && IsVoxelSolid(pos, voxelMap, world,chunkOrig))
                        {
                            // Add vertices for the face
                            for (int i = 0; i <= 3; i++)
                            {
                                data.vertices.Add(VoxelData.voxelVerts[VoxelData.voxelTris[p, i]] + pos);
                                
                            }
                            AddUVs(data.uvs, world.blockTypes[blockID].GetTextureID(p));
                            // Add texture UVs
                            data.triangles.Add(vertexIndex);
                            data.triangles.Add(vertexIndex + 1);
                            data.triangles.Add(vertexIndex + 2);
                            data.triangles.Add(vertexIndex + 2);
                            data.triangles.Add(vertexIndex + 1);
                            data.triangles.Add(vertexIndex + 3);
                            
                            vertexIndex += 4;
                        }
                    }
                }
            }
        }
        return data;
    }
    private static bool IsVoxelSolid(Vector3 pos, byte[,,] voxelMap, World world,Vector3 chunkOrig)
    {
        int x = (int)pos.x;
        int y = (int)pos.y;
        int z = (int)pos.z;

        if (x < 0 || x >= VoxelData.ChunkWidth ||
            y < 0 || y >= VoxelData.ChunkHeight ||
            z < 0 || z >= VoxelData.ChunkWidth)
        {
            return world.IsBlockSolid(pos+chunkOrig); // 외부 청크는 world에서 체크
        }

        return world.blockTypes[voxelMap[x, y, z]].isSolid;
    }

    private static void AddUVs(List<Vector2> uvs, int textureID)
    {
        // 기존 AddTextureUV처럼 작성
        int atlasWidth = VoxelData.TextureAtlasWidth;
        int atlasHeight = VoxelData.TextureAtlasHeight;
        float nw = VoxelData.NormalizedTextureAtlasWidth;
        float nh = VoxelData.NormalizedTextureAtlasHeight;

        int x = textureID % atlasWidth;
        int y = atlasHeight - (textureID / atlasWidth) - 1;

        float uvX = x * nw;
        float uvY = y * nh;

        const float offsetX = 0.005f, offsetY = 0.01f;

        uvs.Add(new Vector2(uvX + offsetX, uvY + offsetY));
        uvs.Add(new Vector2(uvX + offsetX, uvY + nh - offsetY));
        uvs.Add(new Vector2(uvX + nw - offsetX, uvY + offsetY));
        uvs.Add(new Vector2(uvX + nw - offsetX, uvY + nh - offsetY));
    }
}

