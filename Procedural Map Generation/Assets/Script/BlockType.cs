using System;
using UnityEngine;

[Serializable]
public class BlockType
{
    public string blockName;
    public bool isSolid;

    [Header("Texture IDs")]
    public int topFaceTextureID;
    public int frontFaceTextureID;
    public int backFaceTextureID;
    public int leftFaceTextureID;
    public int rightFaceTextureID;
    public int bottomFaceTextureID;
    // Order : Back, Front, Top, Bottom, Left, Right
    /// <summary> Face Index(0~5)에 해당하는 텍스쳐 ID 리턴 </summary>
    public int GetTextureID(int faceIndex)
    {
        switch (faceIndex)
        {
            case VoxelData.TopFace: return topFaceTextureID;
            case VoxelData.FrontFace: return frontFaceTextureID;
            case VoxelData.BackFace: return backFaceTextureID;
            case VoxelData.LeftFace: return leftFaceTextureID;
            case VoxelData.RightFace: return rightFaceTextureID;
            case VoxelData.BottomFace: return bottomFaceTextureID;

            default:
                throw new IndexOutOfRangeException($"Face Index must be in 0 ~ 5, but input : {faceIndex}");
        }
    }
}
