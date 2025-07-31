using UnityEngine;

public class Larva : MonoBehaviour
{
    [Header("幼蟲設置")]
    public Sprite larvaSprite;
    public Color larvaColor = Color.white;
    // 移除 larvaSize，使用 Inspector 中設置的 Scale

    private SpriteRenderer spriteRenderer;
    private CircleCollider2D larvaCollider;
    private int larvaID;
    private bool isActive = true;
    private bool isPickedUp = false;
    private Vector3 originalPosition;
    private Vector3 originalScale; // 保存原始 Scale

    public bool IsActive => isActive && !isPickedUp;
    public int LarvaID => larvaID;

    public void Initialize(int id)
    {
        larvaID = id;
        originalPosition = transform.position;
        originalScale = transform.localScale; // 保存您設置的原始 Scale
        SetupVisuals();
        SetupCollider();
    }

    private void SetupVisuals()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer == null)
        {
            spriteRenderer = gameObject.AddComponent<SpriteRenderer>();
        }

        // 使用您設置的 Sprite，如果沒有則使用程式生成的
        if (larvaSprite != null)
        {
            spriteRenderer.sprite = larvaSprite;
        }
        else
        {
            spriteRenderer.sprite = CreateLarvaSprite();
        }

        spriteRenderer.color = larvaColor;
        // 不修改 Scale，保持您在 Inspector 中設置的值
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
    }

    void OnMouseDown()
    {
        // 檢查是否有鑷子被選中
        var gameManager = QueenRearingGameManager.Instance;
        if (gameManager.GetSelectedTool() == QueenRearingGameManager.Tool.Tweezers && IsActive)
        {
            var tweezers = gameManager.tweezers;
            if (tweezers != null && tweezers.IsSelected)
            {
                tweezers.TryPickupLarva(this);
            }
        }
    }

    public void SetPickedUp(bool pickedUp)
    {
        isPickedUp = pickedUp;
        if (pickedUp)
        {
            larvaCollider.enabled = false; // 禁用碰撞器避免重複點擊
        }
        else
        {
            larvaCollider.enabled = true;
        }
        // 不修改 Scale
    }

    public void CollectToCup(SpecialCup cup)
    {
        isActive = false;
        isPickedUp = false;

        // 直接移動到杯子位置，不用動畫
        transform.position = cup.GetLarvaPosition();
        
        // 不修改 Scale，保持原始大小
        Debug.Log($"幼蟲 {larvaID} 被收集到杯子 {cup.CupID}");
    }

    public void ReturnToOrigin()
    {
        isPickedUp = false;
        larvaCollider.enabled = true;
        transform.position = originalPosition;
        // 不修改 Scale，保持原始大小
        Debug.Log($"幼蟲 {larvaID} 回到原位");
    }

    public void ResetLarva()
    {
        isActive = true;
        isPickedUp = false;
        larvaCollider.enabled = true;
        transform.position = originalPosition;
        transform.SetParent(null);
        // 確保恢復到您設置的原始 Scale
        transform.localScale = originalScale;
        Debug.Log($"幼蟲 {larvaID} 已重置");
    }

    // 移除 OnMouseEnter 和 OnMouseExit 中的大小調整
    void OnMouseEnter()
    {
        if (IsActive)
        {
            // 不修改大小，可以添加其他 hover 效果，比如改變顏色
            // spriteRenderer.color = Color.yellow;
        }
    }

    void OnMouseExit()
    {
        if (IsActive)
        {
            // 恢復原始顏色
            // spriteRenderer.color = larvaColor;
        }
    }

    // ... 其他方法保持不變 ...

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