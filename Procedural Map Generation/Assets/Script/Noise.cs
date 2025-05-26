using UnityEngine;

public static class Noise
{
    public static float Get2DPerlin(in Vector2 position, float offset, float scale)
    {

        // 각자 0.1을 더해주는 이유 : 버그가 있어서
        return Mathf.PerlinNoise
        (
            (position.x + 0.1f) / VoxelData.ChunkWidth * scale + offset, (position.y + 0.1f) / VoxelData.ChunkWidth * scale + offset
        );
    }
}
