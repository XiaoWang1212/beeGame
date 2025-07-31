using UnityEngine;

public class GooseFeather : MonoBehaviour
{
    [Header("鵝毛設置")]
    public Sprite normalSprite;
    public Sprite honeySprite;
    public Color normalColor = Color.white;
    public Color hoverColor = Color.yellow;

    [Header("移動設置")]
    public float moveSpeed = 10f;
    public Vector3 mouseOffset = new Vector3(1f, 1f, 0f);

    private SpriteRenderer featherRenderer;
    private SpriteRenderer borderRenderer;
    private bool isSelected = false;
    private bool hasHoney = false;
    private bool isMoving = false;
    private Vector3 originalPosition;
    private Quaternion originalRotation;
    private Camera mainCamera;

    public bool IsSelected => isSelected;
    public bool HasHoney => hasHoney;

    public void Initialize()
    {
        originalPosition = transform.position;
        originalRotation = transform.rotation;
        mainCamera = Camera.main;
        SetupVisuals();
        SetupBorder();
        SetupCollider();
    }

    private void SetupVisuals()
    {
        featherRenderer = GetComponent<SpriteRenderer>();
        if (featherRenderer == null)
        {
            featherRenderer = gameObject.AddComponent<SpriteRenderer>();
        }

        featherRenderer.sprite = normalSprite;
        featherRenderer.color = normalColor;
    }

    private void SetupBorder()
    {
        // 先清理可能存在的舊邊框
        Transform existingBorder = transform.Find("Border");
        if (existingBorder != null)
        {
            DestroyImmediate(existingBorder.gameObject);
        }

        // 創建邊框物件
        GameObject borderGO = new GameObject("Border");
        borderGO.transform.SetParent(transform);
        borderGO.transform.localPosition = Vector3.zero;
        borderGO.transform.localRotation = Quaternion.identity;
        borderGO.transform.localScale = Vector3.one * 1.03f;

        borderRenderer = borderGO.AddComponent<SpriteRenderer>();

        // 使用相同的 Sprite
        if (featherRenderer.sprite != null)
        {
            borderRenderer.sprite = featherRenderer.sprite;
        }

        // 設置邊框顏色（黃色 hover 效果）
        borderRenderer.color = new Color(hoverColor.r, hoverColor.g, hoverColor.b, 1f);

        // 確保邊框在羽毛後面
        borderRenderer.sortingLayerName = featherRenderer.sortingLayerName;
        borderRenderer.sortingOrder = featherRenderer.sortingOrder - 1;

        // 預設隱藏
        borderRenderer.enabled = false;

        Debug.Log("邊框設置完成 - hover 效果準備就緒");
    }

    private void SetupCollider()
    {
        // 確保 Collider 存在且啟用
        Collider2D collider = GetComponent<Collider2D>();
        if (collider != null)
        {
            collider.enabled = true;
            Debug.Log($"羽毛 Collider 設置完成 - 類型: {collider.GetType().Name}");
        }
        else
        {
            Debug.LogWarning("羽毛沒有 Collider！請在 Inspector 中添加 Polygon Collider 2D");
        }
    }

    void Update()
    {
        if (isMoving && isSelected)
        {
            FollowMouse();
        }
    }

    private void FollowMouse()
    {
        Vector3 mouseWorldPos = mainCamera.ScreenToWorldPoint(Input.mousePosition);
        mouseWorldPos.z = 0;
        Vector3 targetPos = mouseWorldPos + mouseOffset;

        transform.position = Vector3.Lerp(transform.position, targetPos, moveSpeed * Time.deltaTime);
        transform.rotation = originalRotation;
    }

    // 使用 Unity 的滑鼠事件系統（和蜂蜜罐一樣）
    void OnMouseEnter()
    {
        // 只有在未選中狀態才顯示 hover 效果
        if (!isSelected)
        {
            ShowBorder(true);
            Debug.Log("滑鼠進入羽毛 - 顯示黃色邊框");
        }
    }

    void OnMouseExit()
    {
        // 只有在未選中狀態才隱藏 hover 效果
        if (!isSelected)
        {
            ShowBorder(false);
            Debug.Log("滑鼠離開羽毛 - 隱藏邊框");
        }
    }

    void OnMouseDown()
    {
        // 只有在未選中狀態或需要切換工具時才能被選擇
        if (!isSelected && QueenRearingGameManager.Instance.CanSelectTool())
        {
            SelectFeather();
            Debug.Log("選擇羽毛");
        }
        else if (!isSelected && QueenRearingGameManager.Instance.selectedTool != QueenRearingGameManager.Tool.None)
        {
            // 切換工具
            QueenRearingGameManager.Instance.SelectTool(QueenRearingGameManager.Tool.GooseFeather);
            SelectFeather();
            Debug.Log("切換到羽毛工具");
        }
    }

    public void ReturnToOriginWithHoney()
    {
        isSelected = false;
        isMoving = false;
        transform.position = originalPosition;
        transform.rotation = originalRotation;

        // 保留蜂蜜狀態，不重置 hasHoney 和 Sprite
        ShowBorder(false);

        // 重新啟用 Collider
        Collider2D collider = GetComponent<Collider2D>();
        if (collider != null)
        {
            collider.enabled = true;
            Debug.Log("鵝毛保留蜂蜜狀態回原位時啟用 Collider");
        }

        // 不通知 GameManager 取消工具選擇，因為要保留狀態
        Debug.Log("鵝毛保留蜂蜜狀態回到原位");
    }

    private void SelectFeather()
    {
        isSelected = true;
        isMoving = true;

        // 選中時隱藏 hover 邊框
        ShowBorder(false);

        Collider2D collider = GetComponent<Collider2D>();
        if (collider != null)
        {
            collider.enabled = false;
            Debug.Log("鵝毛選中時禁用 Collider");
        }

        // 通知 GameManager
        if (QueenRearingGameManager.Instance != null)
        {
            QueenRearingGameManager.Instance.selectedTool = QueenRearingGameManager.Tool.GooseFeather;
        }

        Debug.Log("鵝毛被選擇 - Collider 保持啟用以測試 hover");
    }

    public void DipInHoney()
    {
        hasHoney = true;

        if (honeySprite != null)
        {
            featherRenderer.sprite = honeySprite;

            // 更新邊框圖片
            if (borderRenderer != null)
            {
                borderRenderer.sprite = honeySprite;
            }
        }

        Debug.Log("鵝毛沾了蜂蜜");
    }

    public void ApplyToCup()
    {
        ResetFeather();
        Debug.Log("鵝毛塗抹完成，已重置可再次使用");
    }

    public void ReturnToOrigin()
    {
        isSelected = false;
        isMoving = false;
        transform.position = originalPosition;
        transform.rotation = originalRotation;
        featherRenderer.color = normalColor;
        ShowBorder(false);

        // 重新啟用 Collider
        Collider2D collider = GetComponent<Collider2D>();
        if (collider != null)
        {
            collider.enabled = true;
            Debug.Log("鵝毛回原位時啟用 Collider");
        }

        // 通知 GameManager
        // if (QueenRearingGameManager.Instance != null)
        // {
        //     QueenRearingGameManager.Instance.selectedTool = QueenRearingGameManager.Tool.None;
        // }
        Debug.Log("鵝毛回到原位");
    }

    public void ResetFeather()
    {
        isSelected = false;
        isMoving = false;
        hasHoney = false;
        transform.position = originalPosition;
        transform.rotation = originalRotation;

        // 恢復原始圖片
        if (normalSprite != null)
        {
            featherRenderer.sprite = normalSprite;
            if (borderRenderer != null)
            {
                borderRenderer.sprite = normalSprite;
            }
        }

        featherRenderer.color = normalColor;
        ShowBorder(false);

        // 重新啟用 Collider
        Collider2D collider = GetComponent<Collider2D>();
        if (collider != null)
        {
            collider.enabled = true;
            Debug.Log("鵝毛重置時啟用 Collider");
        }

        QueenRearingGameManager.Instance.SelectTool(QueenRearingGameManager.Tool.GooseFeather);

        Debug.Log("鵝毛已完全重置");
    }

    private void ShowBorder(bool show)
    {
        if (borderRenderer != null)
        {
            borderRenderer.enabled = show;

            if (show)
            {
                // 使用黃色 hover 效果
                borderRenderer.color = new Color(hoverColor.r, hoverColor.g, hoverColor.b, 1f);
            }

            Debug.Log($"邊框 {(show ? "顯示" : "隱藏")} - 黃色 hover 效果");
        }
        else
        {
            Debug.LogWarning("borderRenderer 為 null！");
        }
    }
}