using UnityEngine;

public class HoneyJar : MonoBehaviour
{
    [Header("蜂蜜罐設置")]
    public Color normalColor = Color.white;
    public Color hoverColor = Color.yellow;

    private SpriteRenderer jarRenderer;
    private SpriteRenderer borderRenderer;

    void Start()
    {
        SetupVisuals();
        SetupBorder();
        SetupCollider(); // 確保有正確的 Collider
    }

    private void SetupVisuals()
    {
        jarRenderer = GetComponent<SpriteRenderer>();
        if (jarRenderer == null)
        {
            jarRenderer = gameObject.AddComponent<SpriteRenderer>();
        }
        jarRenderer.color = normalColor;
        
        // 確保 Sorting Order 夠高，不被其他物件擋住
        jarRenderer.sortingOrder = 10;
    }

    private void SetupBorder()
    {
        // 清理舊的邊框
        Transform existingBorder = transform.Find("Border");
        if (existingBorder != null)
        {
            DestroyImmediate(existingBorder.gameObject);
        }

        GameObject borderGO = new GameObject("Border");
        borderGO.transform.SetParent(transform);
        borderGO.transform.localPosition = Vector3.zero;
        borderGO.transform.localRotation = Quaternion.identity;
        borderGO.transform.localScale = Vector3.one * 1.03f;

        borderRenderer = borderGO.AddComponent<SpriteRenderer>();
        borderRenderer.sprite = jarRenderer.sprite;
        borderRenderer.color = hoverColor;
        borderRenderer.sortingOrder = jarRenderer.sortingOrder - 1;
        borderRenderer.enabled = false;
    }

    private void SetupCollider()
    {
        // 確保有 Collider 且大小正確
        Collider2D col = GetComponent<Collider2D>();
        if (col == null)
        {
            BoxCollider2D boxCol = gameObject.AddComponent<BoxCollider2D>();
            
            // 根據 Sprite 大小設置 Collider
            if (jarRenderer.sprite != null)
            {
                boxCol.size = jarRenderer.sprite.bounds.size;
            }
            else
            {
                boxCol.size = new Vector2(2f, 2f); // 預設大小
            }
            
            Debug.Log($"蜂蜜罐 Collider 大小: {boxCol.size}");
        }
    }

    // 滑鼠進入時 - 檢查是否有選中鵝毛且未沾蜂蜜
    void OnMouseEnter()
    {
        var gameManager = QueenRearingGameManager.Instance;
        if (gameManager != null && gameManager.GetSelectedTool() == QueenRearingGameManager.Tool.GooseFeather)
        {
            var feather = gameManager.gooseFeather;
            if (feather != null && feather.IsSelected && !feather.HasHoney)
            {
                ShowBorder(true);
                Debug.Log("鵝毛 hover 蜂蜜罐");
            }
        }
    }

    // 滑鼠離開時
    void OnMouseExit()
    {
        ShowBorder(false);
        Debug.Log("鵝毛離開蜂蜜罐");
    }

    // 點擊時 - 讓鵝毛沾蜂蜜
    void OnMouseDown()
    {
        var gameManager = QueenRearingGameManager.Instance;
        if (gameManager != null)
        {
            // 檢查是否選中了鵝毛工具
            if (gameManager.GetSelectedTool() == QueenRearingGameManager.Tool.GooseFeather)
            {
                var feather = gameManager.gooseFeather;
                if (feather != null && feather.IsSelected && !feather.HasHoney)
                {
                    feather.DipInHoney();
                    Debug.Log("鵝毛沾了蜂蜜！");
                }
                else if (feather != null && feather.HasHoney)
                {
                    Debug.Log("鵝毛已經有蜂蜜了");
                }
                else if (feather != null && !feather.IsSelected)
                {
                    Debug.Log("請先選擇鵝毛工具");
                }
            }
            else
            {
                Debug.Log("請先選擇鵝毛工具才能沾蜂蜜");
            }
        }
    }

    private void ShowBorder(bool show)
    {
        if (borderRenderer != null)
        {
            borderRenderer.enabled = show;
        }
    }

    // 調試用：顯示 Collider 邊界
    void OnDrawGizmos()
    {
        Collider2D col = GetComponent<Collider2D>();
        if (col != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireCube(transform.position + (Vector3)col.offset, col.bounds.size);
        }
    }
}