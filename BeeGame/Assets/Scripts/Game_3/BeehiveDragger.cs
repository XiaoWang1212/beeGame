using UnityEngine;

namespace Game_3
{

    public class BeehiveDragger : MonoBehaviour
    {
        [Header("拖曳設置")]
        public float shakeIntensity = 5f;
        public float shakeProgressRate = 20f; // 每秒增加的抖動進度

        private Camera mainCamera;
        private Beehive currentBeehive;
        private bool isDragging = false;
        private Vector3 lastMousePosition;
        private Vector3 beehiveOriginalPos;

        void Start()
        {
            mainCamera = Camera.main;
        }

        void Update()
        {
            HandleDragging();
        }

        void HandleDragging()
        {
            if (Input.GetMouseButtonDown(0))
            {
                StartDrag();
            }
            else if (Input.GetMouseButton(0) && isDragging)
            {
                ContinueDrag();
            }
            else if (Input.GetMouseButtonUp(0) && isDragging)
            {
                EndDrag();
            }
        }

        void StartDrag()
        {
            Vector3 mousePos = mainCamera.ScreenToWorldPoint(Input.mousePosition);
            mousePos.z = 0;

            // 檢查是否點擊到蜂巢
            RaycastHit2D hit = Physics2D.Raycast(mousePos, Vector2.zero);
            if (hit.collider != null)
            {
                Beehive beehive = hit.collider.GetComponent<Beehive>();
                if (beehive != null && !beehive.IsComplete)
                {
                    currentBeehive = beehive;
                    isDragging = true;
                    lastMousePosition = mousePos;
                    beehiveOriginalPos = beehive.transform.position;
                    Debug.Log("開始拖曳蜂巢");
                }
            }
        }

        void ContinueDrag()
        {
            if (currentBeehive == null) return;

            Vector3 mousePos = mainCamera.ScreenToWorldPoint(Input.mousePosition);
            mousePos.z = 0;

            // 計算拖曳距離
            float dragDistance = Vector3.Distance(mousePos, lastMousePosition);

            // 只有上下拖曳才算抖動
            float verticalDrag = Mathf.Abs(mousePos.y - lastMousePosition.y);
            if (verticalDrag > 0.1f)
            {
                // 添加抖動進度
                currentBeehive.AddShakeProgress(shakeProgressRate * Time.deltaTime);

                // 視覺抖動效果
                Vector3 shakeOffset = Random.insideUnitCircle * shakeIntensity * 0.1f;
                currentBeehive.transform.position = beehiveOriginalPos + shakeOffset;
            }

            lastMousePosition = mousePos;
        }

        void EndDrag()
        {
            if (currentBeehive != null)
            {
                // 恢復蜂巢位置
                currentBeehive.transform.position = beehiveOriginalPos;
            }

            isDragging = false;
            currentBeehive = null;
            Debug.Log("結束拖曳");
        }
    }
}