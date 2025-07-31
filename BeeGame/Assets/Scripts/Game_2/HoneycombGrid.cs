using UnityEngine;
using System.Collections.Generic;

public class HoneycombGrid : MonoBehaviour
{
    [Header("巢脾設置")]
    public int gridWidth = 8;
    public int gridHeight = 6;
    public float cellSize = 0.8f;
    public float cellSpacing = 0.1f;

    [Header("現有幼蟲 - 手動放置的幼蟲")]
    public Larva[] existingLarvae;  // 拖入場景中現有的幼蟲

    private List<Larva> larvae = new List<Larva>();
    private List<Vector3> cellPositions = new List<Vector3>();

    public void Initialize(int larvaCount)
    {
        GenerateCellPositions();
        SetupExistingLarvae();
    }

    private void GenerateCellPositions()
    {
        cellPositions.Clear();
        
        float adjustedCellSize = cellSize + cellSpacing;
        
        for (int x = 0; x < gridWidth; x++)
        {
            for (int y = 0; y < gridHeight; y++)
            {
                Vector3 cellPos = new Vector3(
                    (x - gridWidth / 2f) * adjustedCellSize,
                    (y - gridHeight / 2f) * adjustedCellSize,
                    0
                ) + transform.position;
                
                cellPositions.Add(cellPos);
            }
        }
    }

    private void SetupExistingLarvae()
    {
        larvae.Clear();
        
        if (existingLarvae != null && existingLarvae.Length > 0)
        {
            for (int i = 0; i < existingLarvae.Length; i++)
            {
                if (existingLarvae[i] != null)
                {
                    existingLarvae[i].Initialize(i);
                    larvae.Add(existingLarvae[i]);
                    Debug.Log($"初始化幼蟲 {i} 在位置: {existingLarvae[i].transform.position}");
                }
            }
        }
        else
        {
            Debug.LogError("請將場景中的幼蟲物件拖入 HoneycombGrid 的 Existing Larvae 陣列中！");
        }
        
        Debug.Log($"總共初始化了 {larvae.Count} 隻幼蟲");
    }

    public List<Larva> GetLarvae()
    {
        return new List<Larva>(larvae);
    }

    // 重置所有幼蟲
    public void ResetAllLarvae()
    {
        foreach (var larva in larvae)
        {
            if (larva != null)
            {
                larva.ResetLarva();
            }
        }
    }

    void OnDrawGizmos()
    {
        if (cellPositions != null)
        {
            Gizmos.color = Color.yellow;
            foreach (Vector3 pos in cellPositions)
            {
                Gizmos.DrawWireCube(pos, Vector3.one * cellSize);
            }
        }
    }
}