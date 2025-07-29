using UnityEngine;

public class Bee : MonoBehaviour
{
    [Header("Bee Properties")]
    public int BeeID { get; protected set; }
    public virtual bool IsQueen => false;

    [Header("Appearance Settings")]
    public Color beeColor = Color.yellow;
    public float beeScale = 1.0f;
    public float hoverScaleMultiplier = 1.1f;

    [Header("Movement Settings")]
    public float moveSpeed = 2f;
    public float wanderRadius = 1.5f;
    public Vector2 directionChangeTimeRange = new Vector2(1.5f, 3.5f);

    protected SpriteRenderer spriteRenderer;
    protected Collider2D beeCollider;
    protected AudioSource audioSource;
    
    // 移動相關變數
    protected Vector3 targetPosition;
    protected Vector3 homePosition;
    protected float directionTimer;
    protected bool isMoving = true;
    protected Vector3 originalScale = Vector3.one;

    protected virtual void Awake()
    {
        InitializeComponents();
    }

    protected virtual void Start()
    {
        homePosition = transform.position;
        SetupAppearance();
        SetNewRandomTarget();
    }

    protected virtual void Update()
    {
        if (isMoving)
        {
            HandleMovement();
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
            beeCollider = gameObject.AddComponent<Collider2D>();
        }

        // 確保 Collider 正確設置
        if (beeCollider is BoxCollider2D boxCollider)
        {
            boxCollider.isTrigger = false;
            boxCollider.size = new Vector2(1f, 1f);
        }

        audioSource = GetComponent<AudioSource>();
    }

    public virtual void Initialize(int id)
    {
        BeeID = id;
        homePosition = transform.position;
        SetupAppearance();
        
        Debug.Log($"初始化普通蜜蜂 - ID: {BeeID}");
    }

    protected virtual void SetupAppearance()
    {
        if (spriteRenderer != null)
        {
            spriteRenderer.color = beeColor;
            transform.localScale = Vector3.one * beeScale;
            originalScale = transform.localScale;
        }
    }

    protected virtual void HandleMovement()
    {
        transform.position = Vector3.MoveTowards(transform.position, targetPosition, moveSpeed * Time.deltaTime);
        directionTimer += Time.deltaTime;

        float directionChangeTime = Random.Range(directionChangeTimeRange.x, directionChangeTimeRange.y);
        
        if (Vector3.Distance(transform.position, targetPosition) < 0.1f || directionTimer >= directionChangeTime)
        {
            SetNewRandomTarget();
            directionTimer = 0f;
        }
    }

    protected virtual void SetNewRandomTarget()
    {
        Vector2 randomDirection = Random.insideUnitCircle * wanderRadius;
        Vector3 newTarget = homePosition + new Vector3(randomDirection.x, randomDirection.y, 0);
        targetPosition = ClampToHoneycombBounds(newTarget);
    }

    protected Vector3 ClampToHoneycombBounds(Vector3 position)
    {
        if (FindQueenGameManager.Instance != null &&
            FindQueenGameManager.Instance.honeycombCellWorldPositions.Count > 0)
        {
            var positions = FindQueenGameManager.Instance.honeycombCellWorldPositions;
            
            float minX = float.MaxValue, maxX = float.MinValue;
            float minY = float.MaxValue, maxY = float.MinValue;
            
            foreach (var pos in positions)
            {
                if (pos.x < minX) minX = pos.x;
                if (pos.x > maxX) maxX = pos.x;
                if (pos.y < minY) minY = pos.y;
                if (pos.y > maxY) maxY = pos.y;
            }
            
            position.x = Mathf.Clamp(position.x, minX - 0.5f, maxX + 0.5f);
            position.y = Mathf.Clamp(position.y, minY - 0.5f, maxY + 0.5f);
        }
        
        return position;
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
            
            Debug.Log($"滑鼠進入蜜蜂 - ID: {BeeID}");
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
    public virtual void SetClickable(bool clickable)
    {
        if (beeCollider != null)
        {
            beeCollider.enabled = clickable;
        }
    }

    public virtual void SetMoving(bool moving)
    {
        isMoving = moving;
    }

    public virtual void SetMovementParameters(float speed, float radius)
    {
        moveSpeed = speed;
        wanderRadius = radius;
    }
}