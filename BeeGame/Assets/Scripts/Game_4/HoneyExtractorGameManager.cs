using UnityEngine;
using System.Collections;
using TMPro;
using UnityEngine.UI;
using Game_4;

public class HoneyExtractorGameManager : MonoBehaviour
{
    public static HoneyExtractorGameManager Instance { get; private set; }

    [Header("UI References")]
    public TextMeshProUGUI timerText;
    public TextMeshProUGUI bucketCountText;
    public TextMeshProUGUI instructionText;

    [Header("Custom Progress Bar")]
    public Image progressBarFillImage;
    public RectTransform honeyJarIcon;
    public RectTransform progressBarContainer;

    [Header("Game Over Panel")]
    public GameObject gameOverPanel;
    public TextMeshProUGUI finalScoreText;
    public Button restartButton;

    [Header("Handle Settings")]
    public Transform handleTransform;
    public float rotationSpeed = 180f;
    public float requiredRotations = 20f;

    [Header("Bucket Shake Settings")]
    public Transform bucketTransform; // 桶子的 Transform
    public float shakeIntensity = 0.05f; // 晃動強度
    public float shakeSpeed = 10f; // 晃動速度

    [Header("Acceleration Settings")]
    public float baseRotationMultiplier = 1f; // 基礎旋轉倍數
    public float maxRotationMultiplier = 3f; // 最大旋轉倍數
    public float accelerationRate = 0.5f; // 加速度
    public float decelerationRate = 2f; // 減速度（變向時）

    [Header("Game Settings")]
    public float gameTimeLimit = 60f;
    public float handleRadius = 2f;

    [Header("Visual Effects")]
    public HoneyDropEffect honeyDropEffect; // 添加蜂蜜掉落效果引用
    public float dropEffectTriggerThreshold = 5f; // 觸發效果的旋轉閾值

    // 私有變數
    private bool gameActive = false;
    private float currentGameTime = 0f;
    private float remainingTime = 0f;
    private int completedBuckets = 0;

    // 把手相關變數
    private bool isDragging = false;
    private float currentProgress = 0f;
    private float totalRotation = 0f;
    private Vector3 lastMousePosition;
    private Camera mainCamera;

    // 新增：加速度和方向檢測變數
    private float currentRotationMultiplier = 1f;
    private float rotationDirection = 0f; // 1 = 順時鐘, -1 = 逆時鐘, 0 = 靜止
    private float lastRotationAngle = 0f;
    private float continuousRotationTime = 0f; // 連續轉動時間
    private Vector3 bucketOriginalPosition; // 桶子原始位置
    private float rotationSinceLastDrop = 0f;
    private float lastEffectTime = 0f;
    private float minEffectInterval = 0.5f; // 最小效果間隔

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    void Start()
    {
        mainCamera = Camera.main;

        // 記錄桶子原始位置
        if (bucketTransform != null)
        {
            bucketOriginalPosition = bucketTransform.position;
        }

        InitializeGame();
    }

    void Update()
    {
        // 檢查是否在教學中
        if (TutorialManager.Instance != null && TutorialManager.Instance.ShouldBlockGameplay())
        {
            return; // 教學期間不執行遊戲邏輯
        }

        if (gameActive)
        {
            if (isDragging)
            {
                HandleRotation(Camera.main.ScreenToWorldPoint(Input.mousePosition));
            }

            UpdateTimer();
            UpdateAcceleration(); // 更新加速度系統
        }

        HandleInput();
    }

    private void InitializeGame()
    {
        if (gameOverPanel != null)
            gameOverPanel.SetActive(false);

        if (restartButton != null)
            restartButton.onClick.AddListener(RestartGame);

        StartGame();
    }

    public void StartGame()
    {
        // 檢查是否有教學系統
        if (TutorialManager.Instance != null && TutorialManager.Instance.IsTutorialActive)
        {
            return; // 教學期間不開始遊戲
        }

        gameActive = true;
        currentGameTime = 0f;
        remainingTime = gameTimeLimit;
        completedBuckets = 0;
        currentProgress = 0f;
        totalRotation = 0f;
        isDragging = false;

        // 重置效果追蹤變數
        rotationSinceLastDrop = 0f;
        lastEffectTime = 0f;

        // 重置加速度系統
        currentRotationMultiplier = baseRotationMultiplier;
        rotationDirection = 0f;
        continuousRotationTime = 0f;

        // 重置桶子位置
        if (bucketTransform != null)
        {
            bucketTransform.position = bucketOriginalPosition;
        }

        // 停止所有蜂蜜效果
        if (honeyDropEffect != null)
        {
            honeyDropEffect.StopAllEffects();
        }

        UpdateUI();

        if (instructionText != null)
            instructionText.text = "按住滑鼠拖拽旋轉把手來搖蜜！持續轉動會加速！";

        Debug.Log("搖蜜機遊戲正式開始！");
    }

    private void UpdateTimer()
    {
        currentGameTime += Time.deltaTime;
        remainingTime = gameTimeLimit - currentGameTime;

        if (remainingTime <= 0f)
        {
            EndGame();
        }

        UpdateTimerUI();
    }

    private void HandleInput()
    {
        Vector3 mouseWorldPos = mainCamera.ScreenToWorldPoint(Input.mousePosition);
        mouseWorldPos.z = 0f;

        if (Input.GetMouseButtonDown(0))
        {
            if (IsMouseNearHandle(mouseWorldPos))
            {
                isDragging = true;
                lastMousePosition = mouseWorldPos;
            }
        }
        else if (Input.GetMouseButtonUp(0))
        {
            isDragging = false;
            // 停止轉動時開始減速（但不立即重置）
            // continuousRotationTime = 0f; // 移除這行
            rotationDirection = 0f;
        }

        if (isDragging && Input.GetMouseButton(0))
        {
            HandleRotation(mouseWorldPos);
        }
        // 移除整個 else 區塊，讓 UpdateAcceleration 處理所有加速/減速邏輯
    }

    private bool IsMouseNearHandle(Vector3 mouseWorldPos)
    {
        if (handleTransform == null) return false;

        float distance = Vector3.Distance(mouseWorldPos, handleTransform.position);
        return distance <= handleRadius;
    }

    private void HandleRotation(Vector3 currentMousePos)
    {
        if (handleTransform == null) return;

        Vector3 handleToMouse = currentMousePos - handleTransform.position;
        Vector3 handleToLastMouse = lastMousePosition - handleTransform.position;

        float crossProduct = Vector3.Cross(handleToLastMouse.normalized, handleToMouse.normalized).z;
        float dotProduct = Vector3.Dot(handleToLastMouse.normalized, handleToMouse.normalized);
        float rotationAngle = Mathf.Atan2(crossProduct, dotProduct) * Mathf.Rad2Deg;

        // 檢測方向變化
        float currentDirection = Mathf.Sign(rotationAngle);

        if (Mathf.Abs(rotationAngle) > 0.1f) // 只在有明顯旋轉時檢測
        {
            if (rotationDirection != 0f && currentDirection != rotationDirection)
            {
                // 方向改變，重置加速時間（速度會立即降到基礎值）
                continuousRotationTime = 0f;
                currentRotationMultiplier = baseRotationMultiplier;
                Debug.Log("方向改變，重置速度！");
            }

            rotationDirection = currentDirection;
        }

        // 計算有效旋轉角度（應用倍數）
        float effectiveRotation = Mathf.Abs(rotationAngle) * currentRotationMultiplier;
        totalRotation += effectiveRotation;

        // 追蹤自上次掉落效果以來的旋轉量
        rotationSinceLastDrop += effectiveRotation;

        // 旋轉把手視覺效果
        handleTransform.Rotate(0, 0, rotationAngle);

        // 檢查是否應該觸發蜂蜜掉落效果
        CheckForDropEffect();

        // 更新進度
        UpdateProgress();

        lastMousePosition = currentMousePos;
        lastRotationAngle = rotationAngle;
    }

    private void CheckForDropEffect()
    {
        // 檢查是否滿足觸發條件
        bool shouldTrigger = rotationSinceLastDrop >= dropEffectTriggerThreshold &&
                           Time.time - lastEffectTime >= minEffectInterval &&
                           currentRotationMultiplier > 1.5f; // 只在加速時觸發

        if (shouldTrigger && honeyDropEffect != null)
        {
            honeyDropEffect.TriggerDropEffect();
            rotationSinceLastDrop = 0f;
            lastEffectTime = Time.time;
            Debug.Log($"觸發蜂蜜掉落效果！速度倍數: {currentRotationMultiplier:F2}x");
        }
    }

    private void UpdateAcceleration()
    {
        if (isDragging && Mathf.Abs(lastRotationAngle) > 0.1f)
        {
            // 轉動中：持續增加速度
            continuousRotationTime += Time.deltaTime;

            // 線性加速：從1到2.5
            float targetMultiplier = baseRotationMultiplier + (continuousRotationTime * accelerationRate);
            currentRotationMultiplier = Mathf.Clamp(targetMultiplier, baseRotationMultiplier, maxRotationMultiplier);

            // 調試輸出
            if (Time.frameCount % 60 == 0)
            {
                Debug.Log($"轉動中 - 時間: {continuousRotationTime:F1}s, 目標速度: {targetMultiplier:F2}, 當前速度: {currentRotationMultiplier:F2}x");
            }
        }
        else
        {
            // 停止轉動：減速回到基礎值
            if (continuousRotationTime > 0f)
            {
                continuousRotationTime -= Time.deltaTime * decelerationRate;
                continuousRotationTime = Mathf.Max(0f, continuousRotationTime);

                // 根據剩餘時間計算速度
                float targetMultiplier = baseRotationMultiplier + (continuousRotationTime * accelerationRate);
                currentRotationMultiplier = Mathf.Clamp(targetMultiplier, baseRotationMultiplier, maxRotationMultiplier);

                // 調試輸出
                if (Time.frameCount % 30 == 0)
                {
                    Debug.Log($"減速中 - 剩餘時間: {continuousRotationTime:F1}s, 當前速度: {currentRotationMultiplier:F2}x");
                }
            }
        }

        // 即時更新UI顯示速度變化
        UpdateSpeedDisplay();
    }

    private void UpdateSpeedDisplay()
    {
        if (bucketCountText != null)
        {
            float progressPercent = currentProgress * 100f;
            bucketCountText.text = $"完成: {completedBuckets} 桶 | 速度: {currentRotationMultiplier:F2}x | 進度: {progressPercent:F0}%";
        }
    }

    private void UpdateBucketShake()
    {
        if (bucketTransform == null) return;

        if (isDragging && Mathf.Abs(lastRotationAngle) > 0.1f)
        {
            // 根據旋轉速度和倍數計算晃動強度
            float shakeAmount = shakeIntensity * currentRotationMultiplier;

            // 產生隨機晃動
            Vector3 shakeOffset = new Vector3(
                Mathf.Sin(Time.time * shakeSpeed) * shakeAmount,
                Mathf.Cos(Time.time * shakeSpeed * 1.3f) * shakeAmount * 0.5f,
                0f
            );

            bucketTransform.position = bucketOriginalPosition + shakeOffset;
        }
        else
        {
            // 逐漸回到原位
            bucketTransform.position = Vector3.Lerp(bucketTransform.position, bucketOriginalPosition, Time.deltaTime * 5f);
        }
    }

    private void UpdateProgress()
    {
        float rotationsCompleted = totalRotation / 360f;
        currentProgress = (rotationsCompleted % requiredRotations) / requiredRotations;

        UpdateCustomProgressBar(currentProgress);

        if (rotationsCompleted >= requiredRotations * (completedBuckets + 1))
        {
            CompleteBucket();
        }
    }

    private void UpdateCustomProgressBar(float progress)
    {
        progress = Mathf.Clamp01(progress);

        if (progressBarFillImage != null)
        {
            progressBarFillImage.fillAmount = progress;
        }

        if (honeyJarIcon != null && progressBarContainer != null)
        {
            float containerWidth = progressBarContainer.rect.width;
            float leftEdge = -containerWidth * 0.5f;
            float iconPositionX = leftEdge + (containerWidth * progress);

            honeyJarIcon.anchoredPosition = new Vector2(iconPositionX, honeyJarIcon.anchoredPosition.y);
        }
    }

    private void CompleteBucket()
    {
        completedBuckets++;
        currentProgress = 0f;

        // 完成桶子時觸發特殊蜂蜜效果
        if (honeyDropEffect != null)
        {
            honeyDropEffect.TriggerDropEffect();
        }

        // 完成桶子時的特殊效果
        if (bucketTransform != null)
        {
            StartCoroutine(BucketCompleteEffect());
        }

        UpdateCustomProgressBar(0f);

        Debug.Log($"完成第 {completedBuckets} 桶蜂蜜！當前倍數: {currentRotationMultiplier:F2}x");

        UpdateUI();
    }

    private IEnumerator BucketCompleteEffect()
    {
        // 完成時的特殊晃動效果
        float effectTime = 0.5f;
        float timer = 0f;

        while (timer < effectTime)
        {
            float shakeAmount = (effectTime - timer) * shakeIntensity * 2f;
            Vector3 randomShake = new Vector3(
                Random.Range(-shakeAmount, shakeAmount),
                Random.Range(-shakeAmount, shakeAmount),
                0f
            );

            bucketTransform.position = bucketOriginalPosition + randomShake;

            timer += Time.deltaTime;
            yield return null;
        }

        bucketTransform.position = bucketOriginalPosition;
    }

    private void UpdateUI()
    {
        UpdateSpeedDisplay(); // 使用新的即時更新方法
        UpdateCustomProgressBar(currentProgress);
    }

    private void UpdateTimerUI()
    {
        if (timerText != null)
        {
            int minutes = Mathf.FloorToInt(remainingTime / 60f);
            int seconds = Mathf.FloorToInt(remainingTime % 60f);
            timerText.text = $"時間: {minutes:00}:{seconds:00}";

            if (remainingTime <= 10f)
            {
                timerText.color = Color.red;
            }
            else if (remainingTime <= 30f)
            {
                timerText.color = Color.yellow;
            }
            else
            {
                timerText.color = Color.white;
            }
        }
    }

    private void EndGame()
    {
        gameActive = false;

        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(true);
        }

        if (finalScoreText != null)
        {
            finalScoreText.text = $"最終成績\n完成了 {completedBuckets} 桶蜂蜜！";
        }

        if (instructionText != null)
        {
            instructionText.text = "遊戲結束！";
        }

        Debug.Log($"搖蜜機遊戲結束！最終完成 {completedBuckets} 桶蜂蜜");
    }

    public void RestartGame()
    {
        if (gameOverPanel != null)
            gameOverPanel.SetActive(false);

        if (handleTransform != null)
        {
            handleTransform.rotation = Quaternion.identity;
        }

        StartGame();
    }

    void OnDrawGizmos()
    {
        if (handleTransform != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(handleTransform.position, handleRadius);
        }
    }
}