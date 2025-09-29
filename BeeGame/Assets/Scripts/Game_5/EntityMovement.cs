using UnityEngine;

public class EntityMovement : MonoBehaviour
{
    private Vector2 targetPosition;
    private float moveSpeed;
    private bool hasTarget = false;
    private bool isOutOfBounds = false;

    [Header("Sprite Direction Settings")]
    public bool flipSpriteBasedOnDirection = true; // 是否根據方向翻轉圖片
    public bool originalFacingLeft = true; // 原始圖片是否朝左
    
    [Header("Flight Pattern Settings")]
    public bool useRandomFlight = true; // 是否使用隨機飛行
    public float waveAmplitude = 1f; // 波浪振幅
    public float waveFrequency = 2f; // 波浪頻率
    public float randomChangeInterval = 0.5f; // 隨機改變方向的間隔
    public float directionChangeSpeed = 3f; // 方向改變的速度
    
    private SpriteRenderer spriteRenderer;
    private Vector2 lastPosition;
    private bool isInitialized = false;
    
    // 隨機飛行相關變數
    private Vector2 baseDirection; // 基本移動方向
    private Vector2 currentDirection; // 當前實際移動方向
    private float flightTimer = 0f;
    private float lastDirectionChangeTime = 0f;
    private float currentWaveOffset = 0f;
    private Vector2 randomOffset = Vector2.zero;
    private Vector2 lastFramePosition;

    void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        lastPosition = transform.position;
        lastFramePosition = transform.position;
        
        // 如果沒有 SpriteRenderer，嘗試從子物件獲取
        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        }
        
        isInitialized = true;
        
        // 初始化隨機飛行參數
        InitializeRandomFlight();
    }

    void InitializeRandomFlight()
    {
        // 隨機化波浪參數
        waveAmplitude = Random.Range(0.5f, 2f);
        waveFrequency = Random.Range(1f, 3f);
        randomChangeInterval = Random.Range(0.3f, 0.8f);
        currentWaveOffset = Random.Range(0f, 2f * Mathf.PI);
    }

    public void SetTargetAndSpeed(Vector2 target, float speed)
    {
        targetPosition = target;
        moveSpeed = speed;
        hasTarget = true;
        isOutOfBounds = false;

        // 計算基本移動方向
        Vector2 currentPos = transform.position;
        baseDirection = (target - currentPos).normalized;
        currentDirection = baseDirection;

        // 計算移動方向並設置圖片翻轉
        if (flipSpriteBasedOnDirection && isInitialized)
        {
            UpdateSpriteDirection(currentDirection);
        }
    }

    void Update()
    {
        if (hasTarget && !isOutOfBounds)
        {
            flightTimer += Time.deltaTime;
            
            // 更新飛行路徑
            if (useRandomFlight)
            {
                UpdateRandomFlight();
            }
            else
            {
                // 直線飛行
                currentDirection = baseDirection;
            }

            // 移動實體
            Vector2 currentPosition = transform.position;
            Vector2 movement = currentDirection * moveSpeed * Time.deltaTime;
            transform.position = currentPosition + movement;

            // 檢查方向改變並更新圖片翻轉
            if (flipSpriteBasedOnDirection && isInitialized)
            {
                Vector2 actualMovement = (Vector2)transform.position - lastFramePosition;
                if (actualMovement.magnitude > 0.01f)
                {
                    UpdateSpriteDirectionBasedOnMovement(actualMovement.normalized);
                }
            }

            lastFramePosition = transform.position;

            // 檢查是否到達目標或超出螢幕邊界
            if (Vector2.Distance(transform.position, targetPosition) < 0.5f || IsOutOfScreen())
            {
                DestroyEntity();
            }
        }
    }

    void UpdateRandomFlight()
    {
        // 計算到目標的基本方向
        Vector2 currentPos = transform.position;
        Vector2 toTarget = (targetPosition - currentPos).normalized;
        
        // 添加正弦波動效果（S型飛行）
        Vector2 perpendicular = new Vector2(-toTarget.y, toTarget.x); // 垂直於目標方向的向量
        float waveValue = Mathf.Sin(flightTimer * waveFrequency + currentWaveOffset) * waveAmplitude;
        Vector2 waveOffset = perpendicular * waveValue;

        // 添加隨機方向改變
        if (Time.time - lastDirectionChangeTime > randomChangeInterval)
        {
            lastDirectionChangeTime = Time.time;
            randomChangeInterval = Random.Range(0.3f, 0.8f); // 下次改變時間隨機
            
            // 隨機偏移方向
            float randomAngle = Random.Range(-45f, 45f) * Mathf.Deg2Rad;
            Vector2 randomDirection = new Vector2(
                Mathf.Cos(randomAngle), 
                Mathf.Sin(randomAngle)
            );
            randomOffset = Vector2.Lerp(randomOffset, randomDirection * 0.5f, Time.deltaTime * directionChangeSpeed);
        }

        // 組合所有方向因素
        Vector2 combinedDirection = toTarget + waveOffset + randomOffset;
        
        // 平滑過渡到新方向
        currentDirection = Vector2.Lerp(currentDirection, combinedDirection.normalized, Time.deltaTime * directionChangeSpeed);
    }

    private void UpdateSpriteDirection(Vector2 direction)
    {
        if (spriteRenderer == null) return;

        // 計算是否需要翻轉
        bool shouldFaceLeft = direction.x < 0;
        
        // 根據原始圖片朝向決定翻轉邏輯
        if (originalFacingLeft)
        {
            // 原始圖片朝左，向右移動時需要翻轉
            spriteRenderer.flipX = !shouldFaceLeft;
        }
        else
        {
            // 原始圖片朝右，向左移動時需要翻轉
            spriteRenderer.flipX = shouldFaceLeft;
        }

        Debug.Log($"{gameObject.name}: 飛行方向 {direction.x:F2}, 原始朝左: {originalFacingLeft}, FlipX: {spriteRenderer.flipX}");
    }

    private void UpdateSpriteDirectionBasedOnMovement(Vector2 movementDirection)
    {
        if (spriteRenderer == null) return;

        // 只有在有明顯水平移動時才更新方向
        if (Mathf.Abs(movementDirection.x) > 0.1f)
        {
            bool shouldFaceLeft = movementDirection.x < 0;
            
            // 根據原始圖片朝向決定翻轉邏輯
            if (originalFacingLeft)
            {
                // 原始圖片朝左，向右移動時需要翻轉
                spriteRenderer.flipX = !shouldFaceLeft;
            }
            else
            {
                // 原始圖片朝右，向左移動時需要翻轉
                spriteRenderer.flipX = shouldFaceLeft;
            }
        }
    }

    private bool IsOutOfScreen()
    {
        Vector2 screenBounds = Camera.main.ScreenToWorldPoint(new Vector3(Screen.width, Screen.height, Camera.main.transform.position.z));
        Vector3 pos = transform.position;
        
        return pos.x < -screenBounds.x - 2f || pos.x > screenBounds.x + 2f || 
               pos.y < -screenBounds.y - 2f || pos.y > screenBounds.y + 2f;
    }

    private void DestroyEntity()
    {
        if (!isOutOfBounds)
        {
            isOutOfBounds = true;
            // 延遲銷毀以避免 Editor 錯誤
            Invoke(nameof(SafeDestroy), 0.1f);
        }
    }

    private void SafeDestroy()
    {
        if (gameObject != null)
        {
            Destroy(gameObject);
        }
    }

    void OnBecameInvisible()
    {
        // 當物件離開相機視野時自動銷毀
        if (hasTarget)
        {
            DestroyEntity();
        }
    }

    // 公開方法供外部設置
    public void SetSpriteDirection(bool facingLeft)
    {
        originalFacingLeft = facingLeft;
    }

    public void SetFlipEnabled(bool enabled)
    {
        flipSpriteBasedOnDirection = enabled;
    }

    public void SetRandomFlightEnabled(bool enabled)
    {
        useRandomFlight = enabled;
    }

    public void SetFlightParameters(float amplitude, float frequency, float changeSpeed)
    {
        waveAmplitude = amplitude;
        waveFrequency = frequency;
        directionChangeSpeed = changeSpeed;
    }

    // 手動設置翻轉
    public void SetFlipX(bool flip)
    {
        if (spriteRenderer != null)
        {
            spriteRenderer.flipX = flip;
        }
    }

    // 調試用：顯示飛行路徑
    void OnDrawGizmos()
    {
        if (hasTarget && useRandomFlight)
        {
            // 顯示當前方向
            Gizmos.color = Color.green;
            Gizmos.DrawRay(transform.position, currentDirection * 2f);
            
            // 顯示基本方向
            Gizmos.color = Color.blue;
            Gizmos.DrawRay(transform.position, baseDirection * 1.5f);
            
            // 顯示目標
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(targetPosition, 0.3f);
        }
    }
}