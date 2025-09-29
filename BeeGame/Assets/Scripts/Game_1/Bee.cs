using UnityEngine;

public class Bee : MonoBehaviour
{
    [Header("Bee Properties")]
    public int BeeID { get; protected set; }
    public virtual bool IsQueen => false;

    [Header("Appearance Settings")]
    public float beeScale = 1.0f;
    public float hoverScaleMultiplier = 1.1f;

    [Header("Movement Settings")]
    public float moveSpeed = 2f;
    public float wanderRadius = 1.5f;
    public Vector2 directionChangeTimeRange = new Vector2(1.5f, 3.5f);

    [Header("Movement Area Settings")]
    [Tooltip("水平移動範圍（左右）")]
    public float horizontalMovementRadius = 3f; // 增加左右範圍

    [Tooltip("垂直移動範圍（上下）")]
    public float verticalMovementRadius = 1.5f; // 保持上下範圍

    [Tooltip("使用長方形移動範圍")]
    public bool useRectangularMovement = true; // 改名更清楚

    [Header("Rotation Settings")]
    [Tooltip("蜜蜂是否朝向移動方向")]
    public bool faceMovementDirection = true;

    [Tooltip("蜜蜂預設朝向（度數，0=右，90=上，180=左，270=下）")]
    public float defaultFacingAngle = 90f; // 預設向上

    [Tooltip("旋轉平滑速度")]
    public float rotationSpeed = 180f; // 每秒旋轉180度

    [Header("Rest Settings")]
    [Tooltip("休息機率（0-1，1表示100%機率）")]
    public float restProbability = 0.2f; // 20%機率休息

    [Tooltip("休息時間範圍（秒）")]
    public Vector2 restTimeRange = new Vector2(1f, 2f);

    [Tooltip("移動多少次後可能休息")]
    public int moveCountBeforeRest = 3;

    [Tooltip("休息時是否停止旋轉")]
    public bool stopRotationDuringRest = true;

    protected SpriteRenderer spriteRenderer;
    protected Collider2D beeCollider;
    protected AudioSource audioSource;

    // 移動相關變數
    protected Vector3 targetPosition;
    public Vector3 movementCenter;
    protected float directionTimer;
    protected bool isMoving = true;
    protected Vector3 originalScale = Vector3.one;
    public float speed = 2f;
    protected float movementRadius = 1.5f;
    public bool useCustomMovementCenter = false;

    // 旋轉相關變數
    protected Vector3 lastPosition;
    protected float targetRotation;

    // 新增休息相關變數
    protected bool isResting = false;
    protected float restTimer = 0f;
    protected float restDuration = 0f;
    protected int moveCounter = 0; // 計算移動次數

    protected virtual void Awake()
    {
        InitializeComponents();
    }

    protected virtual void Start()
    {
        if (!useCustomMovementCenter)
        {
            movementCenter = transform.position;
        }

        // 初始化位置記錄
        lastPosition = transform.position;

        // 設置初始旋轉
        if (faceMovementDirection)
        {
            transform.rotation = Quaternion.Euler(0, 0, defaultFacingAngle);
            targetRotation = defaultFacingAngle;
        }

        SetupAppearance();
        SetNewRandomTarget();
    }

    protected virtual void Update()
    {
        if (isMoving)
        {
            HandleMovement();
            HandleRotation();
        }
    }

    protected virtual void InitializeComponents()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer == null)
        {
            Debug.LogWarning($"Bee {name}: SpriteRenderer not found!");
        }

        beeCollider = GetComponent<Collider2D>();
        if (beeCollider == null)
        {
            beeCollider = gameObject.AddComponent<BoxCollider2D>();
        }

        // 明確啟用 Collider
        if (beeCollider != null)
        {
            beeCollider.enabled = true;
            beeCollider.isTrigger = false;

            if (beeCollider is BoxCollider2D boxCollider)
            {
                boxCollider.size = new Vector2(1f, 1f);
            }

            Debug.Log($"蜜蜂 {BeeID} Collider 初始化: enabled={beeCollider.enabled}, isTrigger={beeCollider.isTrigger}");
        }

        audioSource = GetComponent<AudioSource>();
    }

    public virtual void Initialize(int id)
    {
        BeeID = id;

        ForceEnableCollider();

        SetupAppearance();

        Debug.Log($"初始化普通蜜蜂 - ID: {BeeID}");
    }

    protected virtual void SetupAppearance()
    {
        if (spriteRenderer != null)
        {
            // 不再設置顏色，使用原始 sprite
            transform.localScale = Vector3.one * beeScale;
            originalScale = transform.localScale;
        }
    }

    protected virtual void HandleMovement()
    {
        // 如果正在休息，不移動
        if (isResting)
        {
            HandleResting();
            return;
        }

        Vector3 previousPosition;

        // 使用 localPosition 進行移動（相對於父物件）
        if (transform.parent != null)
        {
            previousPosition = transform.localPosition;
            transform.localPosition = Vector3.MoveTowards(transform.localPosition, targetPosition, speed * Time.deltaTime);
            directionTimer += Time.deltaTime;

            float directionChangeTime = Random.Range(directionChangeTimeRange.x, directionChangeTimeRange.y);

            if (Vector3.Distance(transform.localPosition, targetPosition) < 0.1f || directionTimer >= directionChangeTime)
            {
                // 到達目標點，檢查是否要休息
                CheckForRest();
                
                if (!isResting) // 如果沒有開始休息，才設置新目標
                {
                    SetNewRandomTarget();
                    directionTimer = 0f;
                }
            }

            // 更新移動方向
            if (faceMovementDirection && !isResting)
            {
                Vector3 movementDirection = transform.localPosition - previousPosition;
                UpdateFacingDirection(movementDirection);
            }
        }
        else
        {
            // 如果沒有父物件，使用世界座標
            previousPosition = transform.position;
            transform.position = Vector3.MoveTowards(transform.position, targetPosition, speed * Time.deltaTime);
            directionTimer += Time.deltaTime;

            float directionChangeTime = Random.Range(directionChangeTimeRange.x, directionChangeTimeRange.y);

            if (Vector3.Distance(transform.position, targetPosition) < 0.1f || directionTimer >= directionChangeTime)
            {
                // 到達目標點，檢查是否要休息
                CheckForRest();
                
                if (!isResting) // 如果沒有開始休息，才設置新目標
                {
                    SetNewRandomTarget();
                    directionTimer = 0f;
                }
            }

            // 更新移動方向
            if (faceMovementDirection && !isResting)
            {
                Vector3 movementDirection = transform.position - previousPosition;
                UpdateFacingDirection(movementDirection);
            }
        }
    }

    protected virtual void CheckForRest()
    {
        moveCounter++;
        
        // 移動了足夠次數後，有機率休息
        if (moveCounter >= moveCountBeforeRest)
        {
            float randomValue = Random.Range(0f, 1f);
            
            if (randomValue <= restProbability)
            {
                StartResting();
            }
            
            moveCounter = 0; // 重置計數器
        }
    }

    protected virtual void StartResting()
    {
        isResting = true;
        restDuration = Random.Range(restTimeRange.x, restTimeRange.y);
        restTimer = 0f;
        
        Debug.Log($"蜜蜂 {BeeID} 開始休息 {restDuration:F1} 秒");
        
        // 可選：休息時的視覺效果
        OnStartResting();
    }

    protected virtual void HandleResting()
    {
        restTimer += Time.deltaTime;
        
        // 休息時間結束
        if (restTimer >= restDuration)
        {
            EndResting();
        }
        
        // 休息時可能會有輕微的動畫（可選）
        HandleRestAnimation();
    }

    protected virtual void EndResting()
    {
        isResting = false;
        restTimer = 0f;
        
        Debug.Log($"蜜蜂 {BeeID} 結束休息，繼續移動");
        
        // 設置新的移動目標
        SetNewRandomTarget();
        directionTimer = 0f;
        
        // 可選：結束休息時的視覺效果
        OnEndResting();
    }

    protected virtual void OnStartResting()
    {
        // 可以在這裡添加休息開始時的視覺效果
        // 例如：改變顏色、播放動畫等
    }

    protected virtual void OnEndResting()
    {
        // 可以在這裡添加休息結束時的視覺效果
    }

    protected virtual void HandleRestAnimation()
    {
        // 可以在這裡添加休息時的輕微動畫
        // 例如：輕微的上下浮動、緩慢旋轉等
        
        if (stopRotationDuringRest)
        {
            // 休息時停止旋轉，保持當前角度
            return;
        }
        
        // 或者可以添加很慢的旋轉
        // float slowRotation = 10f * Time.deltaTime; // 很慢的旋轉
        // transform.Rotate(0, 0, slowRotation);
    }

    protected virtual void UpdateFacingDirection(Vector3 movementDirection)
    {
        // 只有在實際移動時才更新方向
        if (movementDirection.magnitude > 0.01f)
        {
            // 計算移動角度
            float angle = Mathf.Atan2(movementDirection.y, movementDirection.x) * Mathf.Rad2Deg;

            // 調整角度以符合預設朝向
            // 如果預設是向上(90度)，我們需要加90度
            angle += (defaultFacingAngle - 0f);

            targetRotation = angle;
        }
    }

    protected virtual void HandleRotation()
    {
        if (faceMovementDirection && !isResting)
        {
            // 平滑旋轉到目標角度
            float currentAngle = transform.eulerAngles.z;

            // 處理角度差異（考慮360度循環）
            float angleDifference = Mathf.DeltaAngle(currentAngle, targetRotation);

            if (Mathf.Abs(angleDifference) > 1f) // 只有角度差異大於1度時才旋轉
            {
                float rotationStep = rotationSpeed * Time.deltaTime;
                float newAngle = currentAngle + Mathf.Sign(angleDifference) * Mathf.Min(rotationStep, Mathf.Abs(angleDifference));
                transform.rotation = Quaternion.Euler(0, 0, newAngle);
            }
        }
        else if (isResting && !stopRotationDuringRest)
        {
            // 休息時的旋轉處理（如果允許的話）
            HandleRestAnimation();
        }
    }

    public virtual void SetNewTargetAroundCenter(Vector3 center)
    {
        movementCenter = center;

        Vector3 newTarget;

        if (useRectangularMovement)
        {
            // 使用長方形範圍 - 簡單且可預測
            float randomX = Random.Range(-horizontalMovementRadius, horizontalMovementRadius);
            float randomY = Random.Range(-verticalMovementRadius, verticalMovementRadius);

            Vector3 rectangularOffset = new Vector3(randomX, randomY, 0);
            newTarget = movementCenter + rectangularOffset;
        }
        else
        {
            // 使用原來的圓形範圍
            Vector2 randomDirection = Random.insideUnitCircle * movementRadius;
            newTarget = movementCenter + new Vector3(randomDirection.x, randomDirection.y, 0);
        }

        targetPosition = newTarget;
        directionTimer = 0f;

        Debug.Log($"蜜蜂 {BeeID} 設置新目標: {targetPosition} (範圍: {horizontalMovementRadius}x{verticalMovementRadius})");
    }

    // 修改 SetNewRandomTarget 方法，使用統一的邏輯
    protected virtual void SetNewRandomTarget()
    {
        SetNewTargetAroundCenter(movementCenter);
    }

    // 滑鼠互動
    protected virtual void OnMouseDown()
    {
        Debug.Log($"點擊了蜜蜂 - ID: {BeeID}, 類型: {GetType().Name}");

        if (FindQueenGameManager.Instance != null && beeCollider.enabled)
        {
            FindQueenGameManager.Instance.PlayerGuessedBee(this);
        }
    }

    protected virtual void OnMouseEnter()
    {
        if (beeCollider.enabled)
        {
            originalScale = transform.localScale;
            transform.localScale = originalScale * hoverScaleMultiplier;
        }
    }

    protected virtual void OnMouseExit()
    {
        if (beeCollider.enabled)
        {
            transform.localScale = originalScale;
        }
    }

    // 公開方法
    // public virtual void SetClickable(bool clickable)
    // {
    //     if (beeCollider != null)
    //     {
    //         beeCollider.enabled = clickable;
    //     }
    // }

    public virtual void SetMoving(bool moving)
    {
        isMoving = moving;
    }

    // 主要的移動參數設置方法
    public virtual void SetMovementParameters(float newSpeed, float radius, Vector3 center)
    {
        this.speed = newSpeed;
        this.movementRadius = radius;
        this.movementCenter = center;
        this.useCustomMovementCenter = true;

        // 如果使用長方形移動，根據半徑調整範圍
        if (useRectangularMovement)
        {
            horizontalMovementRadius = radius * 1.5f; // 左右是半徑的1.5倍
            verticalMovementRadius = radius * 0.8f;   // 上下是半徑的0.8倍
        }

        Debug.Log($"蜜蜂 {BeeID} 設置移動參數 - 速度: {speed}, 長方形範圍: {horizontalMovementRadius}x{verticalMovementRadius}");

        SetNewTargetAroundCenter(center);
    }

    // 新增：直接設置長方形範圍的方法
    public virtual void SetRectangularMovementParameters(float newSpeed, float horizontalRange, float verticalRange, Vector3 center)
    {
        this.speed = newSpeed;
        this.horizontalMovementRadius = horizontalRange;
        this.verticalMovementRadius = verticalRange;
        this.movementCenter = center;
        this.useCustomMovementCenter = true;
        this.useRectangularMovement = true;

        Debug.Log($"蜜蜂 {BeeID} 設置長方形移動 - 水平: {horizontalRange}, 垂直: {verticalRange}");

        SetNewTargetAroundCenter(center);
    }

    // 新增：專門設置橢圓形移動參數的方法
    public virtual void SetEllipticalMovementParameters(float newSpeed, float horizontalRadius, float verticalRadius, Vector3 center)
    {
        this.speed = newSpeed;
        this.horizontalMovementRadius = horizontalRadius;
        this.verticalMovementRadius = verticalRadius;
        this.movementCenter = center;
        this.useCustomMovementCenter = true;

        Debug.Log($"蜜蜂 {BeeID} 設置橢圓移動參數 - 速度: {speed}, 水平: {horizontalRadius}, 垂直: {verticalRadius}");

        SetNewTargetAroundCenter(center);
    }

    // 新增：設置朝向選項
    public virtual void SetFacingDirection(bool enableFacing, float defaultAngle = 90f)
    {
        faceMovementDirection = enableFacing;
        defaultFacingAngle = defaultAngle;

        if (enableFacing)
        {
            transform.rotation = Quaternion.Euler(0, 0, defaultFacingAngle);
            targetRotation = defaultFacingAngle;
        }
        else
        {
            transform.rotation = Quaternion.identity;
        }
    }

    public virtual void ForceEnableCollider()
    {
        if (beeCollider == null)
        {
            InitializeComponents();
        }

        if (beeCollider != null)
        {
            beeCollider.enabled = true;
            Debug.Log($"蜜蜂 {BeeID} 強制啟用 Collider: {beeCollider.enabled}");
        }
        else
        {
            Debug.LogError($"蜜蜂 {BeeID} 無法找到或創建 Collider！");
        }
    }

    // 新增：強制結束休息（可能在遊戲事件中需要）
    public virtual void ForceEndRest()
    {
        if (isResting)
        {
            EndResting();
        }
    }

    // 新增：檢查是否正在休息
    public virtual bool IsResting()
    {
        return isResting;
    }

    // 新增：設置休息參數
    public virtual void SetRestParameters(float probability, Vector2 timeRange, int moveCount)
    {
        restProbability = Mathf.Clamp01(probability);
        restTimeRange = timeRange;
        moveCountBeforeRest = Mathf.Max(1, moveCount);
        
        Debug.Log($"蜜蜂 {BeeID} 設置休息參數 - 機率: {restProbability}, 時間: {restTimeRange.x}-{restTimeRange.y}s, 移動次數: {moveCountBeforeRest}");
    }
}