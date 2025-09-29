using System;
using UnityEngine;

public class Tweezers : MonoBehaviour
{
    [Header("鑷子設置")]
    public Sprite tweezersSprite; // 添加這個欄位讓您在 Inspector 中設置
    public Color normalColor = Color.white; // 改為白色，讓您的 Sprite 正常顯示
    public Color hoverColor = Color.yellow;

    [Header("移動設置")]
    public float moveSpeed = 10f;
    public Vector3 mouseOffset = new Vector3(1f, 1f, 0f);
    public Vector3 larvaOffset = new Vector3(0f, -0.3f, 0f); // 調整幼蟲相對於鑷子的位置


    private SpriteRenderer tweezersRenderer;
    private SpriteRenderer borderRenderer;
    private bool isSelected = false;
    private bool isDragging = false;
    private Vector3 originalPosition;
    private Camera mainCamera;
    private Larva currentLarva;

    public bool IsSelected => isSelected;
    public bool HasLarva => currentLarva != null;

    void Start()
    {
        Initialize();
    }

    void Update()
    {
        if (isSelected)
        {
            FollowMouse();

            // 檢查是否正在拖動
            if (Input.GetMouseButton(0))
            {
                // 只有在有 Larva 且正在拖動時才變成 grab cursor
                if (currentLarva != null)
                {
                    if (Game_2.CursorManager.Instance != null)
                    {
                        Game_2.CursorManager.Instance.SetGrabCursor();
                    }

                    if (!isDragging)
                    {
                        isDragging = true;
                        Debug.Log("開始拖曳幼蟲 - 切換到 grab cursor");
                    }
                }
            }
            else
            {
                if (isDragging)
                {
                    isDragging = false;

                    // 在這裡處理放下邏輯
                    if (currentLarva != null)
                    {
                        HandleDropLarva();
                    }
                    else
                    {
                        // 檢查是否需要設置適當的 cursor
                        SetAppropiateCursor();
                    }
                }
            }

            // 如果夾住幼蟲，讓幼蟲跟著移動
            if (currentLarva != null)
            {
                currentLarva.transform.position = transform.position + larvaOffset;
            }
        }
    }

    public void Initialize()
    {
        originalPosition = transform.position;
        mainCamera = Camera.main;
        SetupVisuals();
        SetupBorder();
        SetupCollider();
    }

    private void SetAppropiateCursor()
    {
        if (currentLarva != null)
        {
            // 有夾住 Larva 但沒在拖動，顯示手張開
            if (Game_2.CursorManager.Instance != null)
            {
                Game_2.CursorManager.Instance.SetHandCursor();
            }
        }
        else
        {
            // 沒有夾住 Larva，檢查是否懸停在可抓取的 Larva 上
            CheckForLarvaHover();
        }
    }

    private void CheckForLarvaHover()
    {
        Vector3 mouseWorldPos = mainCamera.ScreenToWorldPoint(Input.mousePosition);
        mouseWorldPos.z = 0;

        RaycastHit2D hit = Physics2D.Raycast(mouseWorldPos, Vector2.zero);

        if (hit.collider != null)
        {
            Larva larva = hit.collider.GetComponent<Larva>();
            if (larva != null && larva.IsActive)
            {
                // 懸停在可抓取的 Larva 上，顯示手張開
                if (Game_2.CursorManager.Instance != null)
                {
                    Game_2.CursorManager.Instance.SetHandCursor();
                }
                return;
            }
        }

        // 沒有懸停在可抓取的物件上，顯示預設 cursor
        if (Game_2.CursorManager.Instance != null)
        {
            Game_2.CursorManager.Instance.SetDefaultCursor();
        }
    }

    private void HandleDropLarva()
    {
        if (currentLarva == null) return;

        bool successfulDrop = false;

        // 直接檢查滑鼠位置下的杯子（不用 OverlapCircle）
        Vector3 mouseWorldPos = mainCamera.ScreenToWorldPoint(Input.mousePosition);
        mouseWorldPos.z = 0;

        // 使用 Raycast 檢查滑鼠位置下的物件
        RaycastHit2D hit = Physics2D.Raycast(mouseWorldPos, Vector2.zero);

        if (hit.collider != null)
        {
            SpecialCup cup = hit.collider.GetComponent<SpecialCup>();
            if (cup != null && cup.CanAcceptLarva())
            {
                // 檢查杯子是否正在顯示 hover 效果（邊框）
                if (IsCupShowingHover(cup))
                {
                    // 成功放入杯子 - 直接移動到杯子位置，不用動畫
                    Vector3 cupPosition = cup.GetLarvaPosition();
                    currentLarva.transform.position = cupPosition;

                    currentLarva.CollectToCup(cup);
                    cup.AddLarva();
                    currentLarva = null;
                    isDragging = false;

                    QueenRearingGameManager.Instance.OnLarvaCollected();
                    Debug.Log($"幼蟲成功放入杯子 {cup.CupID}");
                    successfulDrop = true;
                }
                else
                {
                    Debug.Log($"杯子 {cup.CupID} 沒有顯示 hover 效果，無法放置");
                }
            }
            else if (cup != null)
            {
                // 詳細的失敗原因
                if (!cup.HasRoyalJelly)
                {
                    Debug.Log($"杯子 {cup.CupID} 沒有蜂王乳，無法放入幼蟲");
                }
                else if (cup.HasLarva)
                {
                    Debug.Log($"杯子 {cup.CupID} 已經有幼蟲了，無法放入更多");
                }
            }
        }

        if (!successfulDrop)
        {
            // 放置失敗，幼蟲直接回原位，不用動畫
            ReturnLarvaToOrigin();
            Debug.Log("放置失敗，幼蟲回到原位");
        }

        isDragging = false;

        SetAppropiateCursor();
    }

    private bool IsCupShowingHover(SpecialCup cup)
    {
        return cup.IsShowingBorder();
    }

    private void SetupVisuals()
    {
        tweezersRenderer = GetComponent<SpriteRenderer>();
        if (tweezersRenderer == null)
        {
            tweezersRenderer = gameObject.AddComponent<SpriteRenderer>();
        }

        // 使用您設置的 Sprite，如果沒有則使用程式生成的
        if (tweezersSprite != null)
        {
            tweezersRenderer.sprite = tweezersSprite;
        }
        else
        {
            tweezersRenderer.sprite = CreateTweezersSprite();
        }

        tweezersRenderer.color = normalColor;
    }

    private void SetupBorder()
    {
        // 先清理可能存在的舊邊框
        Transform existingBorder = transform.Find("Border");
        if (existingBorder != null)
        {
            DestroyImmediate(existingBorder.gameObject);
        }

        GameObject borderGO = new GameObject("Border");
        borderGO.transform.SetParent(transform);
        borderGO.transform.localPosition = Vector3.zero;
        borderGO.transform.localRotation = Quaternion.identity;
        borderGO.transform.localScale = Vector3.one * 1.05f;

        borderRenderer = borderGO.AddComponent<SpriteRenderer>();

        // 使用相同的 Sprite
        borderRenderer.sprite = tweezersRenderer.sprite;
        borderRenderer.color = hoverColor;
        borderRenderer.sortingOrder = tweezersRenderer.sortingOrder - 1;
        borderRenderer.enabled = false;

        Debug.Log("鑷子邊框設置完成");
    }

    private void SetupCollider()
    {
        Collider2D collider = GetComponent<Collider2D>();
        if (collider != null)
        {
            collider.enabled = true;
            collider.isTrigger = false; // 確保不是 Trigger
            Debug.Log($"鑷子 Collider 設置完成 - 類型: {collider.GetType().Name}, 啟用: {collider.enabled}, Trigger: {collider.isTrigger}");
        }
        else
        {
            // 如果沒有 Collider，自動添加一個
            BoxCollider2D boxCollider = gameObject.AddComponent<BoxCollider2D>();
            boxCollider.enabled = true;
            boxCollider.isTrigger = false;

            // 根據 Sprite 大小自動調整 Collider 大小
            if (tweezersRenderer != null && tweezersRenderer.sprite != null)
            {
                Bounds spriteBounds = tweezersRenderer.sprite.bounds;
                boxCollider.size = spriteBounds.size;
            }
            else
            {
                boxCollider.size = new Vector2(1f, 0.3f); // 預設大小
            }

            Debug.Log("自動為鑷子添加了 BoxCollider2D");
        }
    }

    private void FollowMouse()
    {
        Vector3 mouseWorldPos = mainCamera.ScreenToWorldPoint(Input.mousePosition);
        mouseWorldPos.z = 0;
        Vector3 targetPos = mouseWorldPos + mouseOffset;

        transform.position = Vector3.Lerp(transform.position, targetPos, moveSpeed * Time.deltaTime);
    }

    // 使用 Unity 的滑鼠事件系統
    void OnMouseEnter()
    {
        if (!isSelected)
        {
            ShowBorder(true); // 進入時顯示邊框

            // 切換到 hover cursor
            if (Game_2.CursorManager.Instance != null)
            {
                Game_2.CursorManager.Instance.SetHoverCursor();
            }

            Debug.Log("滑鼠進入鑷子 - 顯示黃色邊框和 hover cursor");
        }
    }

    void OnMouseExit()
    {
        if (!isSelected)
        {
            ShowBorder(false); // 離開時隱藏邊框

            // 恢復預設 cursor
            if (Game_2.CursorManager.Instance != null)
            {
                Game_2.CursorManager.Instance.SetDefaultCursor();
            }

            Debug.Log("滑鼠離開鑷子 - 隱藏邊框和恢復預設 cursor");
        }
    }

    void OnMouseDown()
    {
        var gameManager = QueenRearingGameManager.Instance;
        bool isTutorial = (Game_2.TutorialManager.Instance != null && Game_2.TutorialManager.Instance.IsTutorialActive);

        Debug.Log($"鑷子被點擊 - isSelected: {isSelected}, 當前工具: {gameManager?.GetSelectedTool()}, 教學模式: {isTutorial}");

        if (gameManager == null) return;

        if (!isSelected && gameManager.CanSelectTool())
        {
            SelectTweezers();

            // 通知教學系統鑷子被選中
            if (isTutorial && Game_2.TutorialManager.Instance != null)
            {
                Game_2.TutorialManager.Instance.NotifyTweezersInteraction();
                Debug.Log("通知教學系統：鑷子已互動");
            }
        }
        else if (!isSelected && gameManager.GetSelectedTool() != QueenRearingGameManager.Tool.None)
        {
            // 切換工具
            gameManager.SelectTool(QueenRearingGameManager.Tool.Tweezers);
            SelectTweezers();

            // 通知教學系統鑷子被選中
            if (isTutorial && Game_2.TutorialManager.Instance != null)
            {
                Game_2.TutorialManager.Instance.NotifyTweezersInteraction();
                Debug.Log("通知教學系統：鑷子已互動");
            }

            Debug.Log("切換到鑷子工具");
        }
        else if (isSelected)
        {
            Debug.Log("鑷子已經被選中，可以開始拖曳操作");
        }
    }

    public void SelectTweezers()
    {
        isSelected = true;
        ShowBorder(false);

        SetAppropiateCursor();

        QueenRearingGameManager.Instance.SelectTool(QueenRearingGameManager.Tool.Tweezers);
        Debug.Log("鑷子被選擇");
    }

    public void ReturnToOrigin()
    {
        ReturnLarvaToOrigin();

        isSelected = false;
        isDragging = false;
        transform.position = originalPosition;
        tweezersRenderer.color = normalColor;
        ShowBorder(false);

        // 恢復預設 cursor
        if (Game_2.CursorManager.Instance != null)
        {
            Game_2.CursorManager.Instance.SetDefaultCursor();
        }

        // 確保 Collider 啟用
        Collider2D collider = GetComponent<Collider2D>();
        if (collider != null)
        {
            collider.enabled = true;
            Debug.Log("鑷子回原位時確保 Collider 啟用");
        }

        Debug.Log("鑷子回到原位 - 恢復預設 cursor");
    }

    public void ResetTweezers()
    {
        ReturnLarvaToOrigin();

        isSelected = false;
        isDragging = false;
        transform.position = originalPosition;
        tweezersRenderer.color = normalColor;
        ShowBorder(false);

        // 恢復預設 cursor
        if (Game_2.CursorManager.Instance != null)
        {
            Game_2.CursorManager.Instance.SetDefaultCursor();
        }

        // 確保 Collider 啟用
        Collider2D collider = GetComponent<Collider2D>();
        if (collider != null)
        {
            collider.enabled = true;
            Debug.Log("鑷子重置時確保 Collider 啟用");
        }

        // 只在重置時才清除工具狀態
        if (QueenRearingGameManager.Instance != null)
        {
            QueenRearingGameManager.Instance.selectedTool = QueenRearingGameManager.Tool.None;
        }

        Debug.Log("鑷子已重置 - 恢復預設 cursor");
    }

    private void ShowBorder(bool show)
    {
        if (borderRenderer != null)
        {
            borderRenderer.enabled = show;

            if (show)
            {
                borderRenderer.color = hoverColor;
            }

            Debug.Log($"鑷子邊框 {(show ? "顯示" : "隱藏")}");
        }
        else
        {
            Debug.LogWarning("borderRenderer 為 null！");
        }
    }

    public bool TryPickupLarva(Larva larva)
    {
        if (currentLarva == null && isSelected && larva.IsActive)
        {
            currentLarva = larva;
            larva.SetPickedUp(true);

            // 立即將幼蟲移動到鑷子位置
            larva.transform.position = transform.position + larvaOffset;

            if (Game_2.CursorManager.Instance != null)
            {
                Game_2.CursorManager.Instance.SetHandCursor();
            }

            Debug.Log("鑷子夾住了幼蟲");
            return true;
        }
        return false;
    }

    private void ReturnLarvaToOrigin()
    {
        if (currentLarva != null)
        {
            currentLarva.ReturnToOrigin();
            currentLarva = null;
        }
        isDragging = false;

        SetAppropiateCursor();
    }

    // 保留這個方法作為備用
    private Sprite CreateTweezersSprite()
    {
        // ... 原有的程式碼保持不變 ...
        int width = 64;
        int height = 16;
        Texture2D texture = new Texture2D(width, height);

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                Color pixelColor = Color.clear;

                if ((y >= height / 2 - 2 && y <= height / 2 + 2) ||
                    (x < 8 && (y == 0 || y == height - 1)))
                {
                    pixelColor = Color.gray;
                }

                texture.SetPixel(x, y, pixelColor);
            }
        }

        texture.Apply();
        return Sprite.Create(texture, new Rect(0, 0, width, height), new Vector2(0.1f, 0.5f), 100);
    }

    public void UseTweezers()
    {
        if (isSelected)
        {
            ShowBorder(false); // 使用時隱藏邊框
            Debug.Log("鑷子正在使用中");
        }
        else
        {
            Debug.LogWarning("鑷子未被選擇，無法使用");
        }
    }

    // 新增：強制重新啟用互動
    public void ForceEnableInteraction()
    {
        Collider2D collider = GetComponent<Collider2D>();
        if (collider != null)
        {
            collider.enabled = true;
            Debug.Log("強制啟用鑷子互動");
        }
    }
}