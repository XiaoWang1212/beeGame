using UnityEngine;
using UnityEngine.EventSystems; // Required for PointerClick

public class HexCell : MonoBehaviour, IPointerClickHandler
{
    public Vector2Int GridCoordinates { get; private set; } // 儲存蜂巢格的邏輯座標 (例如：(0,0), (0,1) 等)
    private SpriteRenderer spriteRenderer; // 如果是2D遊戲，用於改變顏色

    // 可以用來表示是否有蜜蜂在上面
    public Bee CurrentBee { get; set; } = null;

    void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer == null)
        {
            Debug.LogWarning("HexCell: SpriteRenderer not found on this GameObject. Visual feedback might not work.");
        }
    }

    public void SetCoordinates(int q, int r) // 使用 Axial Coordinates q, r
    {
        GridCoordinates = new Vector2Int(q, r);
        // 可以選擇在這裡更新格子的名字以便在Hierarchy中識別
        gameObject.name = $"Hex_{q},{r}";
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (CurrentBee != null)
        {
            // 當蜂巢格被點擊時，通知遊戲管理器，玩家猜測了這隻蜜蜂
            FindQueenGameManager.Instance.PlayerGuessedBee(CurrentBee);
        }
        else
        {
            Debug.Log($"點擊了空的蜂巢格: {GridCoordinates}");
            // 可以給予玩家提示或懲罰
        }
    }

    // 視覺回饋：當滑鼠懸停時
    void OnMouseEnter()
    {
        if (spriteRenderer != null)
        {
            spriteRenderer.color = Color.yellow; // 懸停時變黃
        }
    }

    void OnMouseExit()
    {
        if (spriteRenderer != null)
        {
            spriteRenderer.color = Color.white; // 離開時恢復白色
        }
    }
}