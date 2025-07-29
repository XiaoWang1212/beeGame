using UnityEngine;

public class QueenBee : Bee
{
    public override bool IsQueen => true;

    [Header("Queen Specific Settings")]
    [SerializeField] private float difficultyLevel = 0f;
    [SerializeField] private Color easyQueenColor = new Color(1f, 0.3f, 0f, 1f);
    [SerializeField] private Color mediumQueenColor = new Color(1f, 0.5f, 0f, 1f);
    [SerializeField] private Color hardQueenColor = new Color(1f, 0.8f, 0.2f, 1f);
    [SerializeField] private float maxQueenScale = 1.2f;
    [SerializeField] private bool showCrown = true;
    [SerializeField] private bool showAura = true;

    private GameObject crown;
    private GameObject aura;
    private Coroutine auraCoroutine; // 添加協程引用

    protected override void Start()
    {
        base.Start();
        // 蜂后移動較慢
        moveSpeed *= 0.8f;
    }

    public void InitializeAsQueen(int id, float difficulty)
    {
        BeeID = id;
        difficultyLevel = difficulty;
        homePosition = transform.position;
        
        SetupQueenAppearance();
        
        Debug.Log($"初始化蜂后 - ID: {BeeID}, 難度: {difficultyLevel}, IsQueen: {IsQueen}");
    }

    protected override void SetupAppearance()
    {
        SetupQueenAppearance();
    }

    private void SetupQueenAppearance()
    {
        if (spriteRenderer == null) return;

        float queenVisibility = 1f - difficultyLevel;

        // 設置蜂后顏色
        Color queenColor = GetQueenColorByDifficulty();
        Color finalColor = Color.Lerp(Color.yellow, queenColor, queenVisibility);
        spriteRenderer.color = finalColor;

        // 設置蜂后大小
        float currentScale = Mathf.Lerp(1.0f, maxQueenScale, queenVisibility);
        transform.localScale = Vector3.one * currentScale;
        originalScale = transform.localScale;

        // 創建皇冠
        if (showCrown && queenVisibility > 0.3f)
        {
            CreateCrown(queenVisibility);
        }

        // 創建光暈
        if (showAura && queenVisibility > 0.5f)
        {
            CreateAura(queenVisibility);
        }

        Debug.Log($"蜂后外觀設置完成 - 顏色: {finalColor}, 大小: {currentScale}, 可見度: {queenVisibility}");
    }

    private Color GetQueenColorByDifficulty()
    {
        if (difficultyLevel < 0.3f)
            return easyQueenColor;
        else if (difficultyLevel < 0.7f)
            return mediumQueenColor;
        else
            return hardQueenColor;
    }

    private void CreateCrown(float visibility)
    {
        if (crown != null)
        {
            Destroy(crown);
        }

        crown = new GameObject("Crown");
        crown.transform.SetParent(transform);
        crown.transform.localPosition = new Vector3(0, 0.5f, 0);
        crown.transform.localScale = Vector3.one * 0.6f;

        SpriteRenderer crownRenderer = crown.AddComponent<SpriteRenderer>();
        crownRenderer.color = new Color(1f, 0.8f, 0f, visibility);
        crownRenderer.sortingOrder = spriteRenderer.sortingOrder + 1;

        CreateCrownSprite(crownRenderer);
        
        Debug.Log($"皇冠創建成功 - 可見度: {visibility}");
    }

    private void CreateAura(float visibility)
    {
        // 停止現有的光暈協程
        if (auraCoroutine != null)
        {
            StopCoroutine(auraCoroutine);
            auraCoroutine = null;
        }

        if (aura != null)
        {
            Destroy(aura);
        }

        aura = new GameObject("Aura");
        aura.transform.SetParent(transform);
        aura.transform.localPosition = Vector3.zero;
        aura.transform.localScale = Vector3.one * 1.5f;

        SpriteRenderer auraRenderer = aura.AddComponent<SpriteRenderer>();
        auraRenderer.color = new Color(1f, 1f, 0f, visibility * 0.3f);
        auraRenderer.sortingOrder = spriteRenderer.sortingOrder - 1;

        CreateAuraSprite(auraRenderer);
        
        // 啟動新的光暈協程
        auraCoroutine = StartCoroutine(PulseAura());
    }

    private void CreateCrownSprite(SpriteRenderer renderer)
    {
        int size = 20;
        Texture2D crownTexture = new Texture2D(size, size);

        // 清空背景
        for (int x = 0; x < size; x++)
        {
            for (int y = 0; y < size; y++)
            {
                crownTexture.SetPixel(x, y, Color.clear);
            }
        }

        // 創建皇冠形狀 - 使用更明顯的金色
        Color crownColor = new Color(1f, 0.8f, 0f, 1f);

        // 基座
        for (int x = 2; x < 18; x++)
        {
            for (int y = 2; y < 6; y++)
            {
                crownTexture.SetPixel(x, y, crownColor);
            }
        }

        // 左尖峰
        for (int x = 4; x < 8; x++)
        {
            for (int y = 6; y < 12; y++)
            {
                crownTexture.SetPixel(x, y, crownColor);
            }
        }

        // 中尖峰（最高）
        for (int x = 8; x < 12; x++)
        {
            for (int y = 6; y < 16; y++)
            {
                crownTexture.SetPixel(x, y, crownColor);
            }
        }

        // 右尖峰
        for (int x = 12; x < 16; x++)
        {
            for (int y = 6; y < 12; y++)
            {
                crownTexture.SetPixel(x, y, crownColor);
            }
        }

        crownTexture.Apply();

        Sprite crownSprite = Sprite.Create(
            crownTexture,
            new Rect(0, 0, size, size),
            new Vector2(0.5f, 0.5f),
            100
        );

        renderer.sprite = crownSprite;
    }

    private void CreateAuraSprite(SpriteRenderer renderer)
    {
        int size = 64;
        Texture2D auraTexture = new Texture2D(size, size);
        Vector2 center = new Vector2(size / 2f, size / 2f);

        for (int x = 0; x < size; x++)
        {
            for (int y = 0; y < size; y++)
            {
                float distance = Vector2.Distance(new Vector2(x, y), center);
                float alpha = Mathf.Clamp01(1f - (distance / (size / 2f)));
                alpha = Mathf.Pow(alpha, 2);
                
                auraTexture.SetPixel(x, y, new Color(1f, 1f, 0f, alpha));
            }
        }
        
        auraTexture.Apply();
        
        Sprite auraSprite = Sprite.Create(
            auraTexture,
            new Rect(0, 0, size, size),
            new Vector2(0.5f, 0.5f),
            100
        );
        
        renderer.sprite = auraSprite;
    }

    private System.Collections.IEnumerator PulseAura()
    {
        if (aura == null) yield break;
        
        Vector3 baseScale = aura.transform.localScale;
        SpriteRenderer auraRenderer = aura.GetComponent<SpriteRenderer>();
        
        if (auraRenderer == null) yield break;
        
        Color baseColor = auraRenderer.color;
        
        while (aura != null && auraRenderer != null)
        {
            // 檢查光暈和渲染器是否仍然存在
            if (aura == null || auraRenderer == null)
                break;

            float pulse = (Mathf.Sin(Time.time * 2f) + 1f) * 0.5f;
            
            // 安全地設置大小
            if (aura != null)
            {
                aura.transform.localScale = baseScale * (0.8f + pulse * 0.4f);
            }
            
            // 安全地設置顏色
            if (auraRenderer != null)
            {
                Color newColor = baseColor;
                newColor.a = baseColor.a * (0.5f + pulse * 0.5f);
                auraRenderer.color = newColor;
            }
            
            yield return null;
        }
        
        // 協程結束時清空引用
        auraCoroutine = null;
    }

    protected override void OnMouseEnter()
    {
        if (beeCollider.enabled)
        {
            originalScale = transform.localScale;
            transform.localScale = originalScale * (hoverScaleMultiplier + 0.1f);
            
            Debug.Log($"滑鼠進入蜂后 - ID: {BeeID}");
        }
    }

    protected override void OnMouseExit()
    {
        if (beeCollider.enabled)
        {
            transform.localScale = originalScale;
            Debug.Log($"滑鼠離開蜂后 - ID: {BeeID}");
        }
    }

    // 安全地清理資源
    private void OnDestroy()
    {
        // 停止光暈協程
        if (auraCoroutine != null)
        {
            StopCoroutine(auraCoroutine);
            auraCoroutine = null;
        }

        // 銷毀子物件
        if (crown != null) 
        {
            Destroy(crown);
            crown = null;
        }
        
        if (aura != null) 
        {
            Destroy(aura);
            aura = null;
        }
    }

    // 添加手動清理方法
    public void CleanupQueen()
    {
        // 停止光暈協程
        if (auraCoroutine != null)
        {
            StopCoroutine(auraCoroutine);
            auraCoroutine = null;
        }

        // 銷毀子物件
        if (crown != null) 
        {
            Destroy(crown);
            crown = null;
        }
        
        if (aura != null) 
        {
            Destroy(aura);
            aura = null;
        }
    }
}