using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Game_3
{
    public class ShakeAndBrushGameManager : MonoBehaviour
    {
        public static ShakeAndBrushGameManager Instance { get; private set; }

        [Header("遊戲設置")]
        public float gameTime = 60f;
        public GameObject beehivePrefab;
        public Transform centerPosition;
        public Transform rightPosition;
        public Transform leftPosition;

        [Header("拖曳設置")]
        public float shakeIntensity = 5f;
        public float shakeProgressRate = 20f;

        [Header("工具設置")]
        public Brush gameBrush; // 刷子引用

        [Header("UI")]
        public Slider shakeProgressBar;
        public TextMeshProUGUI progressText;
        public TextMeshProUGUI timerText;
        public TextMeshProUGUI scoreText;
        public TextMeshProUGUI instructionText;

        [Header("蜂巢縮放設置")]
        public float centerScale = 1.5f;
        public float sideScale = 1f;

        // 遊戲狀態控制
        public enum GamePhase
        {
            Shaking,    // 抖動階段
            Brushing    // 刷蜂階段
        }

        private GamePhase currentPhase = GamePhase.Shaking;
        private float currentTime;
        private int completedHives = 0;
        private bool gameActive = false;
        private Beehive currentHive;
        private Beehive nextHive;

        // 拖曳相關變數
        private Camera mainCamera;
        private bool isDragging = false;
        private Vector3 lastMousePosition;
        private Vector3 beehiveOriginalPos;

        void Awake()
        {
            Instance = this;
        }

        void Start()
        {
            mainCamera = Camera.main;
            SetupProgressBar();
            StartGame();
        }

        void SetupProgressBar()
        {
            if (shakeProgressBar != null)
            {
                shakeProgressBar.minValue = 0f;
                shakeProgressBar.maxValue = 1f;
                shakeProgressBar.value = 0f;
                shakeProgressBar.interactable = false;

                Debug.Log("Progress Slider 已設置完成");
            }
            else
            {
                Debug.LogError("shakeProgressBar 未設置！");
            }
        }

        void Update()
        {
            if (TutorialManager.Instance != null && TutorialManager.Instance.ShouldBlockGameplay())
            {
                return; // 教學期間不執行遊戲邏輯
            }

            if (gameActive)
            {
                // 根據階段決定允許的互動
                if (currentPhase == GamePhase.Shaking)
                {
                    HandleDragging(); // 只有抖動階段才允許拖曳
                }

                UpdateTimer();
                UpdateUI();
                CheckPhaseTransition(); // 檢查階段轉換
                CheckHiveCompletion();
            }
        }

        void CheckPhaseTransition()
        {
            if (currentHive == null) return;

            // 從抖動階段轉換到刷蜂階段
            if (currentPhase == GamePhase.Shaking && currentHive.IsShakeComplete)
            {
                SwitchToBrushingPhase();
            }
        }

        void SwitchToBrushingPhase()
        {
            currentPhase = GamePhase.Brushing;
            Debug.Log("切換到刷蜂階段");

            // 強制結束拖曳
            if (isDragging)
            {
                EndDrag();
            }

            // 啟用刷子（如果有引用）
            if (gameBrush != null)
            {
                gameBrush.SetPhaseActive(true);
            }

            UpdateInstruction("抖動完成！現在用刷子刷掉所有蜜蜂！");
        }

        void SwitchToShakingPhase()
        {
            currentPhase = GamePhase.Shaking;
            Debug.Log("切換到抖動階段");

            // 禁用刷子
            if (gameBrush != null)
            {
                gameBrush.SetPhaseActive(false);
            }
        }

        void HandleDragging()
        {
            // 只有在抖動階段才允許拖曳
            if (currentPhase != GamePhase.Shaking) return;

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

            // 檢查是否點擊到當前蜂巢
            if (currentHive != null && !currentHive.IsComplete)
            {
                Collider2D hiveCollider = currentHive.GetComponent<Collider2D>();
                if (hiveCollider != null && hiveCollider.bounds.Contains(mousePos))
                {
                    // 確保不是在刷蜂階段
                    if (currentPhase == GamePhase.Shaking)
                    {
                        isDragging = true;
                        lastMousePosition = mousePos;
                        beehiveOriginalPos = currentHive.transform.position;
                        Debug.Log("開始拖曳蜂巢");
                    }
                }
            }
        }

        void ContinueDrag()
        {
            if (currentHive == null || currentPhase != GamePhase.Shaking) return;

            Vector3 mousePos = mainCamera.ScreenToWorldPoint(Input.mousePosition);
            mousePos.z = 0;

            // 只有上下拖曳才算抖動
            float verticalDrag = Mathf.Abs(mousePos.y - lastMousePosition.y);
            if (verticalDrag > 0.1f)
            {
                // 添加抖動進度
                currentHive.AddShakeProgress(shakeProgressRate * Time.deltaTime);

                // 視覺抖動效果
                Vector3 shakeOffset = Random.insideUnitCircle * shakeIntensity * 0.1f;
                currentHive.transform.position = beehiveOriginalPos + shakeOffset;
            }

            lastMousePosition = mousePos;
        }

        void EndDrag()
        {
            if (currentHive != null)
            {
                // 恢復蜂巢位置
                currentHive.transform.position = beehiveOriginalPos;
            }

            isDragging = false;
            Debug.Log("結束拖曳");
        }

        void StartGame()
        {
            if (TutorialManager.Instance != null && TutorialManager.Instance.IsTutorialActive)
            {
                return; // 教學期間不開始遊戲
            }

            currentTime = gameTime;
            completedHives = 0;
            gameActive = true;
            currentPhase = GamePhase.Shaking; // 開始時是抖動階段

            // 重置進度條
            if (shakeProgressBar != null)
            {
                shakeProgressBar.value = 0f;
            }
            if (progressText != null)
            {
                progressText.text = "0%";
            }

            SpawnInitialHive();
            SwitchToShakingPhase(); // 確保開始時是抖動階段
        }

        void SpawnInitialHive()
        {
            GameObject hiveObj = Instantiate(beehivePrefab, centerPosition.position, Quaternion.identity);
            currentHive = hiveObj.GetComponent<Beehive>();

            // 設置中間位置的大小
            currentHive.transform.localScale = Vector3.one * centerScale;

            // 確保蜂巢有 Collider2D 用於拖曳檢測
            if (currentHive.GetComponent<Collider2D>() == null)
            {
                BoxCollider2D collider = currentHive.gameObject.AddComponent<BoxCollider2D>();
                collider.size = currentHive.beehiveSize;
            }

            PrepareNextHive();
        }

        void PrepareNextHive()
        {
            GameObject nextHiveObj = Instantiate(beehivePrefab, rightPosition.position, Quaternion.identity);
            nextHive = nextHiveObj.GetComponent<Beehive>();

            // 設置右邊位置的原始大小
            nextHive.transform.localScale = Vector3.one * sideScale;

            // 確保下一個蜂巢也有 Collider2D
            if (nextHive.GetComponent<Collider2D>() == null)
            {
                BoxCollider2D collider = nextHive.gameObject.AddComponent<BoxCollider2D>();
                collider.size = nextHive.beehiveSize;
            }
        }

        void CheckHiveCompletion()
        {
            if (currentHive != null && currentHive.IsComplete)
            {
                CompletedHive();
            }
        }

        void CompletedHive()
        {
            completedHives++;

            // 重置進度條為下一個蜂巢
            if (shakeProgressBar != null)
            {
                shakeProgressBar.value = 0f;
            }
            if (progressText != null)
            {
                progressText.text = "0%";
            }

            StartCoroutine(MoveHiveToLeft(currentHive));

            currentHive = nextHive;
            StartCoroutine(MoveHiveToCenter(currentHive));

            PrepareNextHive();

            // 重置到抖動階段
            SwitchToShakingPhase();
            UpdateInstruction("蜂巢完成！繼續處理下一個");
        }

        System.Collections.IEnumerator MoveHiveToLeft(Beehive hive)
        {
            Vector3 startPos = hive.transform.position;
            Vector3 targetPos = leftPosition.position;
            Vector3 startScale = hive.transform.localScale;
            Vector3 targetScale = Vector3.one * sideScale;

            float duration = 0.5f;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;

                hive.transform.position = Vector3.Lerp(startPos, targetPos, t);
                hive.transform.localScale = Vector3.Lerp(startScale, targetScale, t);

                yield return null;
            }

            Destroy(hive.gameObject);
        }

        System.Collections.IEnumerator MoveHiveToCenter(Beehive hive)
        {
            Vector3 startPos = hive.transform.position;
            Vector3 targetPos = centerPosition.position;
            Vector3 startScale = hive.transform.localScale;
            Vector3 targetScale = Vector3.one * centerScale;

            float duration = 0.5f;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;

                hive.transform.position = Vector3.Lerp(startPos, targetPos, t);
                hive.transform.localScale = Vector3.Lerp(startScale, targetScale, t);

                yield return null;
            }
        }

        void UpdateTimer()
        {
            currentTime -= Time.deltaTime;
            if (currentTime <= 0)
            {
                currentTime = 0;
                EndGame();
            }
        }

        void UpdateUI()
        {
            timerText.text = $"時間: {currentTime:F1}s";
            scoreText.text = $"完成: {completedHives}";

            if (currentHive != null)
            {
                float progress = currentHive.ShakeProgress;

                // 更新 Slider 的值
                if (shakeProgressBar != null)
                {
                    shakeProgressBar.value = progress;
                    UpdateProgressBarColor(progress);
                }

                // 更新百分比文字
                if (progressText != null)
                {
                    progressText.text = $"{(progress * 100):F0}%";
                }

                // 根據階段更新指示文字
                if (currentPhase == GamePhase.Shaking)
                {
                    UpdateInstruction($"上下拖曳蜂巢來抖蜂！({(progress * 100):F0}%)");
                }
                else if (currentPhase == GamePhase.Brushing)
                {
                    UpdateInstruction($"點擊刷子，然後刷掉所有蜜蜂！(剩餘 {currentHive.activeBees.Count} 隻)");
                }
            }
        }

        void UpdateProgressBarColor(float progress)
        {
            // 更新 Slider 的 Fill 區域顏色
            Image fillImage = shakeProgressBar.fillRect.GetComponent<Image>();
            if (fillImage != null)
            {
                if (progress >= 1f)
                {
                    fillImage.color = Color.green; // 100% 時變綠色
                }
                else if (progress >= 0.75f)
                {
                    fillImage.color = Color.yellow; // 75% 時變黃色
                }
                else if (progress >= 0.5f)
                {
                    fillImage.color = new Color(1f, 0.5f, 0f); // 50% 時變橙色
                }
                else
                {
                    fillImage.color = Color.red; // 開始時紅色
                }
            }

            // 可選：更新 Handle（滑塊）顏色
            Image handleImage = shakeProgressBar.handleRect.GetComponent<Image>();
            if (handleImage != null)
            {
                handleImage.color = fillImage.color;
            }
        }

        void UpdateInstruction(string text)
        {
            instructionText.text = text;
        }

        void EndGame()
        {
            gameActive = false;
            UpdateInstruction($"遊戲結束！完成了 {completedHives} 個蜂巢");
        }

        // 公開方法供其他腳本查詢當前階段
        public bool CanUseBrush()
        {
            return currentPhase == GamePhase.Brushing;
        }

        public bool CanShakeHive()
        {
            return currentPhase == GamePhase.Shaking;
        }

        public Beehive GetCurrentHive()
        {
            return currentHive;
        }
    }
}