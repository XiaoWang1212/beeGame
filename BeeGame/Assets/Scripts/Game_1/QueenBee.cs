using UnityEngine;

public class QueenBee : Bee
{
    public override bool IsQueen => true;
    protected override void Start()
    {
        base.Start();
        // 蜂后移動較慢
        speed *= 0.8f;
        
        Debug.Log($"蜂后 Start 完成 - ID: {BeeID}, IsQueen: {IsQueen}");
    }

    public void InitializeAsQueen(int id, float difficulty = 0f)
    {
        BeeID = id;
        
        // 確保組件初始化
        InitializeComponents();
        SetupQueenAppearance();
        
        Debug.Log($"初始化蜂后 - ID: {BeeID}, IsQueen: {IsQueen}, Collider: {beeCollider != null}");
    }

    // 重寫移動參數設置，蜂后移動更慢
    public override void SetMovementParameters(float newSpeed, float radius, Vector3 center)
    {
        // 蜂后移動速度減慢
        base.SetMovementParameters(newSpeed * 0.8f, radius, center);
        Debug.Log($"蜂后 {BeeID} 設置移動參數 - 速度: {speed}");
    }

    protected override void SetupAppearance()
    {
        SetupQueenAppearance();
    }

    private void SetupQueenAppearance()
    {
        // 蜂后稍微大一點
        originalScale = transform.localScale;
    }

    // 重寫 OnMouseDown 確保點擊事件正常
    protected override void OnMouseDown()
    {
        Debug.Log($"蜂后被點擊 - ID: {BeeID}, IsQueen: {IsQueen}, Collider enabled: {beeCollider != null && beeCollider.enabled}");
        
        if (FindQueenGameManager.Instance != null && beeCollider != null && beeCollider.enabled)
        {
            FindQueenGameManager.Instance.PlayerGuessedBee(this);
        }
        else
        {
            Debug.LogWarning($"蜂后點擊失敗 - GameManager: {FindQueenGameManager.Instance != null}, Collider: {beeCollider != null}, Enabled: {beeCollider?.enabled}");
        }
    }

    protected override void OnMouseEnter()
    {
        if (beeCollider != null && beeCollider.enabled)
        {
            originalScale = transform.localScale;
            transform.localScale = originalScale * (hoverScaleMultiplier + 0.1f);
            
            Debug.Log($"滑鼠進入蜂后 - ID: {BeeID}");
        }
    }

    protected override void OnMouseExit()
    {
        if (beeCollider != null && beeCollider.enabled)
        {
            transform.localScale = originalScale;
            Debug.Log($"滑鼠離開蜂后 - ID: {BeeID}");
        }
    }
}