using UnityEngine;
using System.Collections.Generic;
using Game_2;

public class SpecialCup : MonoBehaviour
{
    [Header("杯子設置")]
    public Sprite normalCupSprite;
    public Sprite honeyedCupSprite;
    public Sprite larvaeCupSprite;

    public Color hoverColor = Color.yellow;
    public float interactionDistance = 2f;

    private SpriteRenderer cupRenderer;
    private SpriteRenderer borderRenderer;
    private List<Larva> containedLarvae = new List<Larva>();
    private int cupID;
    private bool hasRoyalJelly = false;
    private bool isHovering = false;

    public bool HasRoyalJelly => hasRoyalJelly;
    public bool HasLarva => containedLarvae.Count > 0;
    public int CupID => cupID;

    // 獲取當前應該使用的工具
    private GooseFeather GetCurrentGooseFeather()
    {
        var gameManager = QueenRearingGameManager.Instance;
        if (gameManager == null) return null;

        // 檢查是否在教學模式
        if (Game_2.TutorialManager.Instance != null && Game_2.TutorialManager.Instance.IsTutorialActive)
        {
            return gameManager.tutorialGooseFeather;
        }
        else
        {
            return gameManager.gooseFeather;
        }
    }

    private Tweezers GetCurrentTweezers()
    {
        var gameManager = QueenRearingGameManager.Instance;
        if (gameManager == null) return null;

        // 檢查是否在教學模式
        if (Game_2.TutorialManager.Instance != null && Game_2.TutorialManager.Instance.IsTutorialActive)
        {
            return gameManager.tutorialTweezers;
        }
        else
        {
            return gameManager.tweezers;
        }
    }

    void Start()
    {
        SetupVisuals();
        SetupBorder();
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

        if (normalCupSprite != null)
        {
            cupRenderer.sprite = normalCupSprite;
        }
    }

    private void SetupBorder()
    {
        if (cupRenderer == null)
        {
            Debug.LogError("cupRenderer 為 null！請先調用 SetupVisuals()");
            return;
        }

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

        if (cupRenderer.sprite != null)
        {
            borderRenderer.sprite = cupRenderer.sprite;
        }

        borderRenderer.color = hoverColor;
        borderRenderer.sortingLayerName = cupRenderer.sortingLayerName;
        borderRenderer.sortingLayerID = cupRenderer.sortingLayerID;
        borderRenderer.sortingOrder = cupRenderer.sortingOrder - 1;
        borderRenderer.enabled = false;

        Debug.Log($"杯子 {cupID} 邊框設置完成");
    }

    void OnMouseEnter()
    {
        var gameManager = QueenRearingGameManager.Instance;
        if (gameManager != null && gameManager.GetSelectedTool() == QueenRearingGameManager.Tool.Tweezers)
        {
            var tweezers = GetCurrentTweezers(); // 使用當前模式的鑷子
            if (tweezers != null && tweezers.HasLarva && CanAcceptLarva())
            {
                ShowBorder(true);

                if (Input.GetMouseButton(0))
                {
                    if (Game_2.CursorManager.Instance != null)
                    {
                        Game_2.CursorManager.Instance.SetGrabCursor();
                    }
                }
                else
                {
                    if (Game_2.CursorManager.Instance != null)
                    {
                        Game_2.CursorManager.Instance.SetHoverCursor();
                    }
                }

                Debug.Log($"鑷子夾著蟲進入杯子 {cupID} - 顯示邊框");
            }
        }
        else if (gameManager != null && gameManager.GetSelectedTool() == QueenRearingGameManager.Tool.GooseFeather)
        {
            var feather = GetCurrentGooseFeather(); // 使用當前模式的鵝毛
            if (feather != null && feather.HasHoney && !hasRoyalJelly)
            {
                ShowBorder(true);

                if (Input.GetMouseButton(0))
                {
                    if (Game_2.CursorManager.Instance != null)
                    {
                        Game_2.CursorManager.Instance.SetGrabCursor();
                    }
                }
                else
                {
                    if (Game_2.CursorManager.Instance != null)
                    {
                        Game_2.CursorManager.Instance.SetHoverCursor();
                    }
                }

                Debug.Log($"鵝毛有蜂蜜進入杯子 {cupID} - 顯示邊框");
            }
        }
    }

    void OnMouseExit()
    {
        if (!isHovering)
        {
            ShowBorder(false);

            if (Game_2.CursorManager.Instance != null)
            {
                Game_2.CursorManager.Instance.SetDefaultCursor();
            }
        }
    }

    void OnMouseDown()
    {
        if (Input.GetMouseButtonDown(0))
        {
            var gameManager = QueenRearingGameManager.Instance;
            if (gameManager != null)
            {
                Debug.Log($"杯子 {cupID} 被點擊，當前選擇的工具: {gameManager.GetSelectedTool()}");
                Debug.Log($"教學模式: {(Game_2.TutorialManager.Instance != null && Game_2.TutorialManager.Instance.IsTutorialActive)}");

                if (gameManager.GetSelectedTool() == QueenRearingGameManager.Tool.GooseFeather)
                {
                    var feather = GetCurrentGooseFeather(); // 使用當前模式的鵝毛
                    Debug.Log($"獲取到的鵝毛: {(feather != null ? feather.name : "null")}");

                    if (feather != null && feather.IsSelected && feather.HasHoney && !hasRoyalJelly)
                    {
                        ApplyRoyalJelly();
                        feather.ApplyToCup();

                        // 通知教學系統蜂王乳已製作
                        if (Game_2.TutorialManager.Instance != null && Game_2.TutorialManager.Instance.IsTutorialActive)
                        {
                            Game_2.TutorialManager.Instance.NotifyJellyApplied();
                            Debug.Log("通知教學系統：蜂王乳已製作");
                        }

                        Debug.Log($"杯子 {cupID} 已塗抹蜂王乳！");
                    }
                    else
                    {
                        // 調試信息
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

    public void ApplyRoyalJelly()
    {
        hasRoyalJelly = true;
        UpdateCupSprite();
        Debug.Log($"杯子 {cupID} 已塗抹蜂王乳，圖片已更換");
    }

    public void AddLarva()
    {
        if (!HasLarva)
        {
            containedLarvae.Add(null);
            UpdateCupSprite();
            Debug.Log($"杯子 {cupID} 添加幼蟲，目前數量: {containedLarvae.Count}");
        }
    }

    private void UpdateCupSprite()
    {
        Sprite targetSprite = null;

        if (HasLarva)
        {
            targetSprite = larvaeCupSprite;
        }
        else if (hasRoyalJelly)
        {
            targetSprite = honeyedCupSprite;
        }
        else
        {
            targetSprite = normalCupSprite;
        }

        if (targetSprite != null && cupRenderer != null)
        {
            cupRenderer.sprite = targetSprite;
        }

        if (targetSprite != null && borderRenderer != null)
        {
            borderRenderer.sprite = targetSprite;
        }

        Debug.Log($"杯子 {cupID} 圖片已更新 - 有幼蟲: {HasLarva}, 有蜂王乳: {hasRoyalJelly}");
    }

    public Vector3 GetLarvaPosition()
    {
        return transform.position + new Vector3(0, -0.2f, 0);
    }

    public void ResetCup()
    {
        hasRoyalJelly = false;
        containedLarvae.Clear();
        isHovering = false;
        UpdateCupSprite();
        ShowBorder(false);
        Debug.Log($"杯子 {cupID} 已重置");
    }

    private void ShowBorder(bool show)
    {
        if (borderRenderer != null)
        {
            borderRenderer.enabled = show;
        }
    }

    void OnDrawGizmos()
    {
        if (!hasRoyalJelly)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(transform.position, interactionDistance);
        }
    }

    public bool CanAcceptLarva()
    {
        return hasRoyalJelly && !HasLarva;
    }

    public bool IsShowingBorder()
    {
        return borderRenderer != null && borderRenderer.enabled;
    }
}