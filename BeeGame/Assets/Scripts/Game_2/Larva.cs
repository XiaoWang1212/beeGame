using UnityEngine;

public class Larva : MonoBehaviour
{
    [Header("幼蟲設置")]
    public Sprite larvaSprite;
    public Color larvaColor = Color.white;

    private SpriteRenderer spriteRenderer;
    private CircleCollider2D larvaCollider;
    private int larvaID;
    private bool isActive = true;
    private bool isPickedUp = false;
    private Vector3 originalPosition;
    private Vector3 originalScale;

    public bool IsActive => isActive && !isPickedUp;
    public int LarvaID => larvaID;

    // 獲取當前應該使用的鑷子
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

    public void Initialize(int id = 0)
    {
        larvaID = id;
        originalPosition = transform.position;
        originalScale = transform.localScale;
        SetupVisuals();
        SetupCollider();
        
        Debug.Log($"幼蟲 {larvaID} 初始化完成，位置: {originalPosition}");
    }

    private void SetupVisuals()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer == null)
        {
            spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
        }

        if (larvaSprite != null)
        {
            spriteRenderer.sprite = larvaSprite;
        }
        else
        {
            spriteRenderer.sprite = CreateLarvaSprite();
        }

        spriteRenderer.color = larvaColor;
    }

    private void SetupCollider()
    {
        larvaCollider = GetComponent<CircleCollider2D>();
        if (larvaCollider == null)
        {
            larvaCollider = gameObject.AddComponent<CircleCollider2D>();
        }

        larvaCollider.radius = 0.5f;
        larvaCollider.isTrigger = false;
        
        Debug.Log($"幼蟲 {larvaID} Collider 設置完成");
    }

    void OnMouseDown()
    {
        var gameManager = QueenRearingGameManager.Instance;
        if (gameManager == null) return;

        Debug.Log($"幼蟲 {larvaID} 被點擊，當前工具: {gameManager.GetSelectedTool()}, 教學模式: {(Game_2.TutorialManager.Instance != null && Game_2.TutorialManager.Instance.IsTutorialActive)}");

        // 檢查是否有鑷子被選中
        if (gameManager.GetSelectedTool() == QueenRearingGameManager.Tool.Tweezers && IsActive)
        {
            var tweezers = GetCurrentTweezers(); // 使用當前模式對應的鑷子
            Debug.Log($"獲取到的鑷子: {(tweezers != null ? tweezers.name : "null")}");
            
            if (tweezers != null)
            {
                Debug.Log($"鑷子狀態 - IsSelected: {tweezers.IsSelected}, HasLarva: {tweezers.HasLarva}");
                
                if (tweezers.IsSelected)
                {
                    bool result = tweezers.TryPickupLarva(this);
                    Debug.Log($"嘗試夾取幼蟲結果: {result}");
                    
                    // 通知教學系統幼蟲被移動
                    if (result && Game_2.TutorialManager.Instance != null && Game_2.TutorialManager.Instance.IsTutorialActive)
                    {
                        // 這裡先不通知，等幼蟲真正放到杯子裡再通知
                        Debug.Log("幼蟲被鑷子夾取，等待放置到杯子");
                    }
                }
                else
                {
                    Debug.Log("鑷子沒有被選中");
                }
            }
            else
            {
                Debug.LogError($"無法獲取鑷子引用！教學模式: {(Game_2.TutorialManager.Instance != null && Game_2.TutorialManager.Instance.IsTutorialActive)}");
            }
        }
        else if (gameManager.GetSelectedTool() != QueenRearingGameManager.Tool.Tweezers)
        {
            Debug.Log("請先選擇鑷子工具才能夾取幼蟲");
        }
        else if (!IsActive)
        {
            Debug.Log("幼蟲已被收集，無法再次夾取");
        }
    }

    void OnMouseEnter()
    {
        if (IsActive)
        {
            var gameManager = QueenRearingGameManager.Instance;
            if (gameManager != null && gameManager.GetSelectedTool() == QueenRearingGameManager.Tool.Tweezers)
            {
                var tweezers = GetCurrentTweezers(); // 使用當前模式對應的鑷子
                if (tweezers != null && tweezers.IsSelected && !tweezers.HasLarva)
                {
                    if (Game_2.CursorManager.Instance != null)
                    {
                        Game_2.CursorManager.Instance.SetHandCursor();
                    }

                    Debug.Log($"鑷子懸停在幼蟲 {larvaID} 上 - 顯示 hand cursor");
                }
            }
        }
    }

    void OnMouseExit()
    {
        if (IsActive)
        {
            var gameManager = QueenRearingGameManager.Instance;
            if (gameManager != null && gameManager.GetSelectedTool() == QueenRearingGameManager.Tool.Tweezers)
            {
                var tweezers = GetCurrentTweezers(); // 使用當前模式對應的鑷子
                if (tweezers != null && tweezers.IsSelected)
                {
                    if (tweezers.HasLarva)
                    {
                        if (Game_2.CursorManager.Instance != null)
                        {
                            Game_2.CursorManager.Instance.SetHandCursor();
                        }
                    }
                    else
                    {
                        if (Game_2.CursorManager.Instance != null)
                        {
                            Game_2.CursorManager.Instance.SetDefaultCursor();
                        }
                    }
                    return;
                }
            }

            if (Game_2.CursorManager.Instance != null)
            {
                Game_2.CursorManager.Instance.SetDefaultCursor();
            }

            Debug.Log($"鑷子離開幼蟲 {larvaID}");
        }
    }

    public void SetPickedUp(bool pickedUp)
    {
        isPickedUp = pickedUp;
        if (pickedUp)
        {
            larvaCollider.enabled = false;
        }
        else
        {
            larvaCollider.enabled = true;
        }
        
        Debug.Log($"幼蟲 {larvaID} 拾取狀態設為: {pickedUp}");
    }

    public void CollectToCup(SpecialCup cup)
    {
        isActive = false;
        isPickedUp = false;

        transform.position = cup.GetLarvaPosition();

        // 通知教學系統和遊戲管理器
        if (Game_2.TutorialManager.Instance != null && Game_2.TutorialManager.Instance.IsTutorialActive)
        {
            // 通知教學系統幼蟲已移動
            var gameManager = QueenRearingGameManager.Instance;
            if (gameManager != null)
            {
                gameManager.OnLarvaMoved(); // 這會增加 collectedLarvae 並通知教學系統
            }
        }

        Debug.Log($"幼蟲 {larvaID} 被收集到杯子 {cup.CupID}");
    }

    public void ReturnToOrigin()
    {
        isPickedUp = false;
        larvaCollider.enabled = true;
        transform.position = originalPosition;
        Debug.Log($"幼蟲 {larvaID} 回到原位");
    }

    public void ResetLarva()
    {
        isActive = true;
        isPickedUp = false;
        larvaCollider.enabled = true;
        transform.position = originalPosition;
        transform.SetParent(null);
        transform.localScale = originalScale;
        Debug.Log($"幼蟲 {larvaID} 已重置");
    }

    private Sprite CreateLarvaSprite()
    {
        int size = 32;
        Texture2D texture = new Texture2D(size, size);
        Vector2 center = new Vector2(size / 2f, size / 2f);

        for (int x = 0; x < size; x++)
        {
            for (int y = 0; y < size; y++)
            {
                float distance = Vector2.Distance(new Vector2(x, y), center);
                if (distance <= size / 2f)
                {
                    texture.SetPixel(x, y, Color.white);
                }
                else
                {
                    texture.SetPixel(x, y, Color.clear);
                }
            }
        }

        texture.Apply();
        return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100);
    }
}