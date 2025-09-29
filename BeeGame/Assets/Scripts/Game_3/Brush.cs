using UnityEngine;

namespace Game_3
{
    public class Brush : MonoBehaviour
    {
        [Header("刷子設置")]
        public Sprite brushSprite;
        public float brushRange = 1f;
        public float returnSpeed = 5f; // 回到原位的速度

        [Header("拖拽設置")]
        public Vector3 dragOffset = new Vector3(0.5f, -0.5f, 0f); // 刷子相對於鼠標的偏移
        public float mouseDetectionRadius = 0.3f; // 鼠標檢測蜜蜂的範圍

        private Camera mainCamera;
        private SpriteRenderer brushRenderer;
        private bool isDragging = false; // 改為拖拽狀態
        private bool isPhaseActive = false;
        private Collider2D brushCollider;
        private Vector3 originalPosition; // 記錄原始位置
        private Vector3 mouseWorldPosition; // 鼠標的世界座標

        void Start()
        {
            mainCamera = Camera.main;
            originalPosition = transform.position; // 記錄初始位置
            SetupVisuals();
            SetupCollider();
        }

        void SetupVisuals()
        {
            brushRenderer = GetComponent<SpriteRenderer>();
            if (brushRenderer == null)
                brushRenderer = gameObject.AddComponent<SpriteRenderer>();

            brushRenderer.sprite = brushSprite;

            // 初始時設為半透明，表示不可用
            SetBrushAppearance(false);
        }

        void SetupCollider()
        {
            brushCollider = GetComponent<Collider2D>();
            if (brushCollider == null)
            {
                brushCollider = gameObject.AddComponent<BoxCollider2D>();
            }
        }

        void Update()
        {
            // 只有在刷蜂階段才處理輸入
            if (isPhaseActive)
            {
                HandleInput();
            }

            // 如果不在拖拽狀態，慢慢回到原位
            if (!isDragging && Vector3.Distance(transform.position, originalPosition) > 0.1f)
            {
                transform.position = Vector3.MoveTowards(transform.position, originalPosition, returnSpeed * Time.deltaTime);
            }
        }

        void HandleInput()
        {
            // 更新鼠標世界座標
            UpdateMousePosition();

            if (Input.GetMouseButtonDown(0))
            {
                CheckBrushDragStart();
            }
            else if (Input.GetMouseButtonUp(0))
            {
                StopDragging();
            }

            if (isDragging)
            {
                FollowMouse();
                BrushBees(); // 拖拽過程中持續檢測鼠標位置的蜜蜂
            }
        }

        void UpdateMousePosition()
        {
            mouseWorldPosition = mainCamera.ScreenToWorldPoint(Input.mousePosition);
            mouseWorldPosition.z = 0;
        }

        void CheckBrushDragStart()
        {
            RaycastHit2D hit = Physics2D.Raycast(mouseWorldPosition, Vector2.zero);
            if (hit.collider != null && hit.collider == brushCollider)
            {
                // 檢查是否在正確的階段
                if (ShakeAndBrushGameManager.Instance.CanUseBrush())
                {
                    StartDragging();
                }
                else
                {
                    Debug.Log("還沒完成抖動，無法使用刷子！");
                }
            }
        }

        void StartDragging()
        {
            isDragging = true;
            Debug.Log("開始拖拽刷子");

            // 拖拽時的視覺效果
            if (brushRenderer != null)
            {
                brushRenderer.color = new Color(1f, 1f, 1f, 0.9f); // 稍微透明表示正在使用
            }
        }

        void StopDragging()
        {
            if (isDragging)
            {
                isDragging = false;
                Debug.Log("停止拖拽刷子，回到原位");

                // 恢復正常外觀
                SetBrushAppearance(isPhaseActive);
            }
        }

        void SetBrushAppearance(bool active)
        {
            if (brushRenderer != null)
            {
                if (active)
                {
                    brushRenderer.color = Color.white; // 可用時正常顏色
                }
                else
                {
                    brushRenderer.color = new Color(0.5f, 0.5f, 0.5f, 0.5f); // 不可用時灰色半透明
                }
            }
        }

        void FollowMouse()
        {
            // 刷子位置 = 鼠標位置 + 偏移
            transform.position = mouseWorldPosition + dragOffset;
        }

        void BrushBees()
        {
            // 再次確認是否在正確階段
            if (!ShakeAndBrushGameManager.Instance.CanUseBrush())
            {
                return;
            }

            // 只刷當前在中間的蜂巢上的蜜蜂
            var currentHive = ShakeAndBrushGameManager.Instance.GetCurrentHive();
            if (currentHive == null)
            {
                return;
            }

            // 在鼠標位置檢測蜜蜂，而不是刷子位置
            Collider2D[] hits = Physics2D.OverlapCircleAll(mouseWorldPosition, mouseDetectionRadius);

            foreach (var hit in hits)
            {
                if (hit == null) continue;

                Bee bee = hit.GetComponent<Bee>();
                if (bee != null)
                {
                    // 檢查這隻蜜蜂是否屬於當前的蜂巢
                    if (bee.ParentHive == currentHive) // 修正屬性名稱
                    {
                        bee.OnBrushed();
                        Debug.Log($"鼠標刷到蜜蜂！位置: {mouseWorldPosition}");
                    }
                }
            }
        }

        void OnDrawGizmosSelected()
        {
            // 畫刷子範圍（舊的，現在不用於檢測）
            Gizmos.color = Color.gray;
            Gizmos.DrawWireSphere(transform.position, brushRange);

            // 畫鼠標檢測範圍（實際用於檢測蜜蜂的範圍）
            if (isDragging)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawWireSphere(mouseWorldPosition, mouseDetectionRadius);
                
                // 畫鼠標到刷子的連線，顯示偏移
                Gizmos.color = Color.yellow;
                Gizmos.DrawLine(mouseWorldPosition, transform.position);
                
                // 在鼠標位置畫一個小點
                Gizmos.color = Color.blue;
                Gizmos.DrawWireSphere(mouseWorldPosition, 0.1f);
            }

            // 畫原始位置
            Gizmos.color = Color.green;
            Gizmos.DrawWireCube(originalPosition, Vector3.one * 0.2f);

            // 畫回到原位的線
            if (!isDragging && Vector3.Distance(transform.position, originalPosition) > 0.1f)
            {
                Gizmos.color = Color.blue;
                Gizmos.DrawLine(transform.position, originalPosition);
            }
        }

        // 在 Scene 視圖中也顯示調試資訊
        void OnDrawGizmos()
        {
            // 在拖拽時顯示即時的檢測範圍
            if (isDragging && Application.isPlaying)
            {
                Gizmos.color = new Color(1f, 0f, 0f, 0.3f); // 半透明紅色
                Gizmos.DrawSphere(mouseWorldPosition, mouseDetectionRadius);
            }
        }

        // 公開方法供遊戲管理器調用
        public bool IsDragging => isDragging;

        public void SetPhaseActive(bool active)
        {
            isPhaseActive = active;
            SetBrushAppearance(active);

            if (!active && isDragging)
            {
                StopDragging();
            }
        }

        public void ForceStopDragging()
        {
            StopDragging();
        }

        public void SetOriginalPosition(Vector3 position)
        {
            originalPosition = position;
        }

        // 新增方法用於調試
        [ContextMenu("Test Offset")]
        public void TestOffset()
        {
            if (Application.isPlaying)
            {
                UpdateMousePosition();
                transform.position = mouseWorldPosition + dragOffset;
                Debug.Log($"鼠標位置: {mouseWorldPosition}, 刷子位置: {transform.position}, 偏移: {dragOffset}");
            }
        }
    }
}