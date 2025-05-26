using UnityEngine;

public static class Noise
{
    public static float Get2DPerlin(in Vector2 position, float offset, float scale)
    {

        // ���� 0.1�� �����ִ� ���� : ���װ� �־
        return Mathf.PerlinNoise
        (
            (position.x + 0.1f) / VoxelData.ChunkWidth * scale + offset, (position.y + 0.1f) / VoxelData.ChunkWidth * scale + offset
        );
    }
}
