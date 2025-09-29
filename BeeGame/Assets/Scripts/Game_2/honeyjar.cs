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
        SetupCollider();
    }

    private void SetupVisuals()
    {
        jarRenderer = GetComponent<SpriteRenderer>();
        if (jarRenderer == null)
        {
            jarRenderer = gameObject.AddComponent<SpriteRenderer>();
        }
        jarRenderer.color = normalColor;
        jarRenderer.sortingOrder = 10;
    }

    private void SetupBorder()
    {
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
        borderRenderer.sortingLayerName = jarRenderer.sortingLayerName;
        borderRenderer.sortingLayerID = jarRenderer.sortingLayerID;
        borderRenderer.sortingOrder = jarRenderer.sortingOrder - 1;
        borderRenderer.enabled = false;
    }

    private void SetupCollider()
    {
        Collider2D col = GetComponent<Collider2D>();
        if (col == null)
        {
            BoxCollider2D boxCol = gameObject.AddComponent<BoxCollider2D>();

            if (jarRenderer.sprite != null)
            {
                boxCol.size = jarRenderer.sprite.bounds.size;
            }
            else
            {
                boxCol.size = new Vector2(2f, 2f);
            }

            Debug.Log($"蜂蜜罐 Collider 大小: {boxCol.size}");
        }
    }

    // 獲取當前應該使用的鵝毛
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

    void OnMouseEnter()
    {
        var gameManager = QueenRearingGameManager.Instance;
        if (gameManager != null && gameManager.GetSelectedTool() == QueenRearingGameManager.Tool.GooseFeather)
        {
            var feather = GetCurrentGooseFeather();
            if (feather != null && feather.IsSelected && !feather.HasHoney)
            {
                ShowBorder(true);

                if (Game_2.CursorManager.Instance != null)
                {
                    Game_2.CursorManager.Instance.SetHoverCursor();
                }

                Debug.Log("鵝毛 hover 蜂蜜罐");
            }
        }
    }

    void OnMouseExit()
    {
        ShowBorder(false);

        if (Game_2.CursorManager.Instance != null)
        {
            Game_2.CursorManager.Instance.SetDefaultCursor();
        }

        Debug.Log("鵝毛離開蜂蜜罐");
    }

    void OnMouseDown()
    {
        var gameManager = QueenRearingGameManager.Instance;
        if (gameManager != null)
        {
            Debug.Log($"蜂蜜罐被點擊，當前選擇的工具: {gameManager.GetSelectedTool()}");
            Debug.Log($"教學模式: {(Game_2.TutorialManager.Instance != null && Game_2.TutorialManager.Instance.IsTutorialActive)}");

            // 檢查是否選中了鵝毛工具
            if (gameManager.GetSelectedTool() == QueenRearingGameManager.Tool.GooseFeather)
            {
                var feather = GetCurrentGooseFeather();
                Debug.Log($"獲取到的鵝毛: {(feather != null ? feather.name : "null")}");

                if (feather != null)
                {
                    Debug.Log($"鵝毛狀態 - IsSelected: {feather.IsSelected}, HasHoney: {feather.HasHoney}");

                    if (feather.IsSelected && !feather.HasHoney)
                    {
                        feather.DipInHoney();

                        // 通知教學系統蜂蜜已沾取
                        if (Game_2.TutorialManager.Instance != null && Game_2.TutorialManager.Instance.IsTutorialActive)
                        {
                            Game_2.TutorialManager.Instance.NotifyHoneyDipped();
                            Debug.Log("通知教學系統：蜂蜜已沾取");
                        }

                        if (Game_2.CursorManager.Instance != null)
                        {
                            Game_2.CursorManager.Instance.SetDefaultCursor();
                        }

                        Debug.Log("鵝毛沾了蜂蜜！");
                    }
                    else if (feather.HasHoney)
                    {
                        Debug.Log("鵝毛已經有蜂蜜了");
                    }
                    else if (!feather.IsSelected)
                    {
                        Debug.Log("請先選擇鵝毛工具");
                    }
                }
                else
                {
                    Debug.LogError("無法獲取鵝毛引用！");
                }
            }
            else
            {
                Debug.Log("請先選擇鵝毛工具才能沾蜂蜜");
            }
        }
        else
        {
            Debug.LogError("QueenRearingGameManager.Instance 為 null！");
        }
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
        Collider2D col = GetComponent<Collider2D>();
        if (col != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireCube(transform.position + (Vector3)col.offset, col.bounds.size);
        }
    }
}