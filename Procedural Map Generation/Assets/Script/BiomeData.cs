using UnityEngine;

[CreateAssetMenu(fileName = "BiomeData", menuName = "Scriptable Objects/Voxel System/BiomeData")]
public class BiomeData : ScriptableObject
{
    public string biomeName; // 생물군계 이름
    public int solidGroindHeight; // 고체 지면 높이
    public int terrainHeightRange; // solidGroundHeight로 부터 증가할수있는 최대 높이값
    public float terrainScale;

    public Lode[] lodes;
}
[System.Serializable]
public class Lode
{
    public string loadName;
    public byte blockID;
    public int minHeight;
    public int maxHeight;
    public float scale; // 노이즈 스케일
    public float threshold; // 노이즈 임계값
    public float noiseOffset; // 노이즈 오프셋
}
