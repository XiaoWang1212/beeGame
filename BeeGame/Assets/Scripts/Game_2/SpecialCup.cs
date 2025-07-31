using UnityEngine;
using System.Collections.Generic;

public class SpecialCup : MonoBehaviour
{
    [Header("杯子設置")]
    public Sprite normalCupSprite;          // 普通杯子圖片
    public Sprite honeyedCupSprite;         // 有蜂王乳的杯子圖片
    public Sprite larvaeCupSprite;          // 有幼蟲的杯子圖片（新增）

    public Color hoverColor = Color.yellow;
    public float interactionDistance = 2f;

    private SpriteRenderer cupRenderer;
    private SpriteRenderer borderRenderer;
    private List<Larva> containedLarvae = new List<Larva>();
    private int cupID;
    private bool hasRoyalJelly = false;
    private bool isHovering = false;

    public bool HasRoyalJelly => hasRoyalJelly;
    public int CupID => cupID;
    public bool HasLarvae => containedLarvae.Count > 0;


    void Start()
    {
        SetupVisuals(); // 先設置視覺效果
        SetupBorder();  // 再設置邊框
    }

    void Update()
    {
        CheckForFeatherInteraction();
    }

    public void Initialize(int id)
    {
        cupID = id;
        SetupVisuals();
        SetupBorder();
    }

    private void SetupVisuals()
    {
        cupRenderer = GetComponent<SpriteRenderer>();
        if (cupRenderer == null)
        {
            cupRenderer = gameObject.AddComponent<SpriteRenderer>();
        }

        // 設置初始圖片
        if (normalCupSprite != null)
        {
            cupRenderer.sprite = normalCupSprite;
        }
    }

    private void SetupBorder()
    {
        // 確保 cupRenderer 已經初始化
        if (cupRenderer == null)
        {
            Debug.LogError("cupRenderer 為 null！請先調用 SetupVisuals()");
            return;
        }

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
        borderGO.transform.localScale = Vector3.one * 1.05f;

        borderRenderer = borderGO.AddComponent<SpriteRenderer>();

        // 確保 cupRenderer.sprite 不為 null
        if (cupRenderer.sprite != null)
        {
            borderRenderer.sprite = cupRenderer.sprite;
        }
        else
        {
            Debug.LogWarning("cupRenderer.sprite 為 null，邊框將沒有圖片");
        }

        borderRenderer.color = hoverColor;
        borderRenderer.sortingOrder = cupRenderer.sortingOrder - 1;
        borderRenderer.enabled = false;

        Debug.Log($"杯子 {cupID} 邊框設置完成");
    }

    private void CheckForFeatherInteraction()
    {
        var gameManager = QueenRearingGameManager.Instance;
        if (gameManager == null) return;

        // 檢查是否有選中且沾了蜂蜜的鵝毛
        if (gameManager.GetSelectedTool() == QueenRearingGameManager.Tool.GooseFeather)
        {
            var feather = gameManager.gooseFeather;
            if (feather != null && feather.IsSelected && feather.HasHoney && !hasRoyalJelly)
            {
                float distance = Vector3.Distance(transform.position, feather.transform.position);

                // 距離檢測 hover 效果
                if (distance <= interactionDistance)
                {
                    if (!isHovering)
                    {
                        isHovering = true;
                        ShowBorder(true);
                        Debug.Log($"沾蜂蜜的鵝毛靠近杯子 {cupID}");
                    }

                    // 點擊檢測
                    if (Input.GetMouseButtonDown(0))
                    {
                        ApplyRoyalJelly();
                        feather.ApplyToCup(); // 讓鵝毛回到原位
                        Debug.Log($"杯子 {cupID} 已塗抹蜂王乳！");
                    }
                }
                else
                {
                    if (isHovering)
                    {
                        isHovering = false;
                        ShowBorder(false);
                    }
                }
            }
            else
            {
                if (isHovering)
                {
                    isHovering = false;
                    ShowBorder(false);
                }
            }
        }
        else
        {
            if (isHovering)
            {
                isHovering = false;
                ShowBorder(false);
            }
        }
    }

    public void ApplyRoyalJelly()
    {
        hasRoyalJelly = true;

        // 更新杯子圖片
        UpdateCupSprite();

        Debug.Log($"杯子 {cupID} 已塗抹蜂王乳，圖片已更換");
    }

    public void AddLarva()
    {
        if (!HasLarvae)
        {
            containedLarvae.Add(null); // 簡單記錄數量

            // 更新杯子圖片為有幼蟲的狀態
            UpdateCupSprite();

            Debug.Log($"杯子 {cupID} 添加幼蟲，目前數量: {containedLarvae.Count}");
        }
    }

    private void UpdateCupSprite()
    {
        Sprite targetSprite = null;

        // 決定要使用的圖片
        if (HasLarvae)
        {
            // 如果有幼蟲，使用幼蟲杯子圖片
            targetSprite = larvaeCupSprite;
        }
        else if (hasRoyalJelly)
        {
            // 如果有蜂王乳但沒幼蟲，使用蜂王乳杯子圖片
            targetSprite = honeyedCupSprite;
        }
        else
        {
            // 普通狀態
            targetSprite = normalCupSprite;
        }

        // 更新主圖片
        if (targetSprite != null && cupRenderer != null)
        {
            cupRenderer.sprite = targetSprite;
        }

        // 更新邊框圖片
        if (targetSprite != null && borderRenderer != null)
        {
            borderRenderer.sprite = targetSprite;
        }

        Debug.Log($"杯子 {cupID} 圖片已更新 - 有幼蟲: {HasLarvae}, 有蜂王乳: {hasRoyalJelly}");
    }

    public Vector3 GetLarvaPosition()
    {
        // 返回杯子內幼蟲應該放置的位置（中心位置）
        return transform.position + new Vector3(0, -0.2f, 0);
    }

    public void ResetCup()
    {
        hasRoyalJelly = false;
        containedLarvae.Clear();
        isHovering = false;

        // 更新圖片回到普通狀態
        UpdateCupSprite();
        ShowBorder(false);

        Debug.Log($"杯子 {cupID} 已重置");
    }

    // 保留原始的滑鼠事件作為備用
    void OnMouseEnter()
    {
        var gameManager = QueenRearingGameManager.Instance;
        if (gameManager != null && gameManager.GetSelectedTool() == QueenRearingGameManager.Tool.Tweezers)
        {
            var tweezers = gameManager.tweezers;
            // 只有當鑷子夾著幼蟲且杯子能接受幼蟲時才顯示邊框
            if (tweezers != null && tweezers.HasLarva && CanAcceptLarva())
            {
                ShowBorder(true);
                Debug.Log($"鑷子夾著蟲進入杯子 {cupID} - 顯示邊框");
            }
        }
        else if (gameManager != null && gameManager.GetSelectedTool() == QueenRearingGameManager.Tool.GooseFeather)
        {
            var feather = gameManager.gooseFeather;
            if (feather != null && feather.HasHoney && !hasRoyalJelly)
            {
                ShowBorder(true);
                Debug.Log($"鵝毛有蜂蜜進入杯子 {cupID} - 顯示邊框");
            }
        }
    }

    void OnMouseExit()
    {
        if (!isHovering) // 只有在距離檢測沒有觸發時才隱藏
        {
            ShowBorder(false);
        }
    }

    void OnMouseDown()
    {
        if (Input.GetMouseButtonDown(0))
        {
            var gameManager = QueenRearingGameManager.Instance;
            if (gameManager != null)
            {
                // 檢查是否選中了鵝毛工具
                if (gameManager.GetSelectedTool() == QueenRearingGameManager.Tool.GooseFeather)
                {
                    var feather = gameManager.gooseFeather;

                    // 只有當鵝毛被選中、有蜂蜜且杯子還沒塗抹過時才能塗抹
                    if (feather != null && feather.IsSelected && feather.HasHoney && !hasRoyalJelly)
                    {
                        ApplyRoyalJelly();
                        feather.ApplyToCup(); // 這會調用 ResetFeather()
                        Debug.Log($"杯子 {cupID} 已塗抹蜂王乳！");
                    }
                    else
                    {
                        // 調試信息：為什麼無法塗抹
                        if (feather == null)
                            Debug.Log("沒有找到鵝毛");
                        else if (!feather.IsSelected)
                            Debug.Log("鵝毛沒有被選中");
                        else if (!feather.HasHoney)
                            Debug.Log("鵝毛沒有沾蜂蜜，無法塗抹杯子");
                        else if (hasRoyalJelly)
                            Debug.Log("杯子已經塗抹過蜂王乳了");
                    }
                }
                else
                {
                    Debug.Log("沒有選中鵝毛工具，無法塗抹杯子");
                }
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

    // 調試用：顯示互動範圍
    void OnDrawGizmos()
    {
        if (!hasRoyalJelly) // 只有沒塗抹時才顯示範圍
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(transform.position, interactionDistance);
        }
    }

    public bool CanAcceptLarva()
    {
        return hasRoyalJelly && !HasLarvae;
    }

    public bool IsShowingBorder()
    {
        return borderRenderer != null && borderRenderer.enabled;
    }
}