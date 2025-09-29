using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine.EventSystems;

public class FindQueenGameManager : MonoBehaviour
{
    public static FindQueenGameManager Instance { get; private set; }

    [Header("Prefabs")]
    public GameObject beePrefab;        // 普通蜜蜂 Prefab
    public GameObject queenBeePrefab;   // 蜂后 Prefab
    public GameObject hivePrefab;       // 新增：蜂巢 Prefab

    [Header("Hive Settings")]
    [Tooltip("每個蜂窩的蜜蜂數量")]
    public int beesPerHive = 15;

    [Tooltip("蜂窩總數")]
    public int totalHives = 10;

    [Tooltip("蜂后總數")]
    public int totalQueens = 3;

    [Tooltip("蜜蜂移動範圍")]
    public float beeMovementRadius = 3f; // 增加移動範圍，從 2f 改為 3f 或更大

    [Header("Hive Generation Settings")]
    [Tooltip("蜂巢生成區域寬度")]
    public float hiveAreaWidth = 10f;

    [Tooltip("蜂巢生成區域高度")]
    public float hiveAreaHeight = 8f;

    [Tooltip("蜂巢之間的最小距離")]
    public float minHiveDistance = 3f;

    [Tooltip("使用固定蜂巢位置")]
    public bool useFixedHivePosition = true;

    [Tooltip("固定蜂巢位置")]
    public Vector3 fixedHivePosition = new Vector3(0, -0.48f, 0);

    [Header("Grid Generation Settings")]
    [Tooltip("格子大小")]
    public float cellSize = 1.0f;

    [Tooltip("網格寬度（格子數量）")]
    public int gridWidth = 8;

    [Tooltip("網格高度（格子數量）")]
    public int gridHeight = 6;

    [Tooltip("格子之間的間距")]
    public float cellSpacing = 0.1f;

    [Header("Time Settings")]
    [Tooltip("遊戲時間限制（秒）")]
    public float gameTimeLimit = 60f;

    [Header("UI References")]
    public TextMeshProUGUI hiveInfoText;
    public TextMeshProUGUI timerText;
    public GameObject leftArrowButton;
    public GameObject rightArrowButton;
    public GameObject restartButton;

    [Header("Game Result Panel")]
    public GameObject gameResultPanel;
    public TextMeshProUGUI resultTitleText;
    public TextMeshProUGUI resultTimeText;

    [Header("Intro UI References")]
    public GameObject introPanel;
    public TextMeshProUGUI GameTitleText;
    public TextMeshProUGUI instructionText;
    public TextMeshProUGUI BeeText;
    public TextMeshProUGUI QueenBeeText;
    public TextMeshProUGUI LeftRightButtonText;
    public GameObject BeePrefabExample;
    public GameObject QueenBeePrefabExample;
    public GameObject LeftRightButtonExample;

    [Header("Countdown UI References")]
    public GameObject countdownPanel;        // 新增：倒數面板
    public TextMeshProUGUI countdownText;    // 新增：倒數文字

    [Header("Intro Effects")]
    public float instructionBlinkSpeed = 1.5f; // 閃爍速度
    public float instructionMinAlpha = 0.3f; // 最小透明度
    public float instructionMaxAlpha = 1f; // 最大透明度
    private bool isInstructionBlinking = false;
    private Coroutine instructionBlinkCoroutine = null;
    private bool gameStarted = false;
    private bool isCountingDown = false;

    [Header("Game Settings")]
    public bool enableDebugMode = false;

    [Header("Animation Settings")]
    [Tooltip("蜂窩切換動畫時間")]
    public float hiveTransitionDuration = 0.5f;

    [Tooltip("切換動畫類型")]
    public HiveTransitionType transitionType = HiveTransitionType.Slide;

    [Tooltip("動畫緩動曲線")]
    public AnimationCurve transitionCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("Camera Settings")]
    [Tooltip("是否自動移動相機到當前蜂窩")]
    public bool enableCameraAutoFocus = false; // 新增：控制相機自動聚焦

    [Header("Audio Settings")]
    [Tooltip("背景音樂播放器")]
    public AudioSource bgmAudioSource;

    [Tooltip("音效播放器")]
    public AudioSource audioSource;

    [Tooltip("選對蜂后的音效")]
    public AudioClip correctAnswerSound;

    [Tooltip("選錯蜜蜂的音效")]
    public AudioClip wrongAnswerSound;

    [Tooltip("按空白鍵開始的音效")]
    public AudioClip startButtonSound;

    [Tooltip("倒數音效 (3, 2, 1)")]
    public AudioClip countdownSound;

    [Tooltip("音樂淡出開始時間（秒）")]
    [Range(50f, 65f)]
    public float fadeOutStartTime = 60f;

    [Tooltip("音樂淡出持續時間（秒）")]
    [Range(5f, 15f)]
    public float fadeOutDuration = 10f;

    [Tooltip("音效音量")]
    [Range(0f, 1f)]
    public float masterVolume = 0.7f;

    [Tooltip("正確答案音效音量")]
    [Range(0f, 1f)]
    public float correctSoundVolume = 0.8f;

    [Tooltip("錯誤答案音效音量")]
    [Range(0f, 1f)]
    public float wrongSoundVolume = 0.3f;

    [Tooltip("開始按鍵音效音量")]
    [Range(0f, 1f)]
    public float startButtonVolume = 0.6f;

    [Tooltip("倒數音效音量")]
    [Range(0f, 1f)]
    public float countdownVolume = 0.5f;

    [HideInInspector]
    public List<Vector3> honeycombCellWorldPositions;

    // 私有變數
    private List<Hive> allHives = new List<Hive>();
    private List<Vector3> hivePositions = new List<Vector3>();
    private int currentHiveIndex = 0;
    private int foundQueensCount = 0;
    private List<int> queenHiveIndices = new List<int>();
    private bool gameInitialized = false;
    private bool gameActive = false;

    // 時間相關變數
    private float currentGameTime = 0f;
    private float remainingTime = 0f;

    private bool isTransitioning = false;
    private Coroutine currentTransition = null;
    private bool gameIsPaused = false;

    // 動畫類型枚舉
    public enum HiveTransitionType
    {
        Slide,         // 滑動
        Fade,          // 淡入淡出
        Scale,         // 縮放
        SlideAndFade   // 滑動+淡化
    }

    // 蜂窩類別
    [System.Serializable]
    public class Hive
    {
        public int hiveIndex;
        public List<Bee> bees = new List<Bee>();
        public bool hasQueen;
        public bool isActive;
        public GameObject hiveGameObject;
        public Vector3 hivePosition;

        public Hive(int index)
        {
            hiveIndex = index;
            hasQueen = false;
            isActive = false;
            hiveGameObject = null;
        }
    }

    #region Unity生命週期
    void Awake()
    {
        InitializeSingleton();
        SetupAudioSource(); // 新增：設置音效
    }

    void Start()
    {
        InitializeGame();

        // 初始時隱藏結果面板和倒數面板
        if (gameResultPanel != null)
            gameResultPanel.SetActive(false);

        if (countdownPanel != null)
            countdownPanel.SetActive(false);

        ShowIntroScreen();
    }

    void Update()
    {
        if (!gameStarted && !isCountingDown)
        {
            HandleIntroInput();
        }
        else if (gameStarted && !gameIsPaused)
        {
            HandleInput();
            UpdateTimer();
        }

        if (enableDebugMode && Input.GetKeyDown(KeyCode.Space) && gameStarted)
        {
            var currentHive = allHives[currentHiveIndex];
            var queen = currentHive.bees.FirstOrDefault(b => b.IsQueen);
            if (queen != null)
            {
                Debug.Log("強制觸發蜂后猜測（調試用）");
                PlayerGuessedBee(queen);
            }
        }

        // 點擊任意處關閉結果面板
        if (gameIsPaused && Input.GetMouseButtonDown(0))
        {
            HideGameResult();
        }
    }

    private void ShowIntroScreen()
    {
        gameStarted = false;
        isCountingDown = false;

        // 顯示介紹面板
        if (introPanel != null)
            introPanel.SetActive(true);

        // 隱藏遊戲 UI
        HideGameUI();

        // 設置介紹文字
        SetupIntroTexts();

        // 開始閃爍效果
        StartInstructionBlinking();

        Debug.Log("顯示遊戲介紹畫面");
    }

    private void SetupIntroTexts()
    {
        if (BeePrefabExample != null)
            BeePrefabExample.SetActive(true);

        if (QueenBeePrefabExample != null)
            QueenBeePrefabExample.SetActive(true);
    }

    private void HandleIntroInput()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            PlayStartButtonSound();

            StartGameCountdown();
        }
    }

    private void ShowGameUI()
    {
        // if (hintText != null) hintText.gameObject.SetActive(true);
        if (hiveInfoText != null) hiveInfoText.gameObject.SetActive(true);
        // if (foundQueensText != null) foundQueensText.gameObject.SetActive(true);
        if (timerText != null) timerText.gameObject.SetActive(true);
        if (leftArrowButton != null) leftArrowButton.SetActive(true);
        if (rightArrowButton != null) rightArrowButton.SetActive(true);

        // 隱藏範例蜜蜂
        if (BeePrefabExample != null)
            BeePrefabExample.SetActive(false);

        if (QueenBeePrefabExample != null)
            QueenBeePrefabExample.SetActive(false);
    }

    private void HideGameUI()
    {
        // if (hintText != null) hintText.gameObject.SetActive(false);
        if (hiveInfoText != null) hiveInfoText.gameObject.SetActive(false);
        // if (foundQueensText != null) foundQueensText.gameObject.SetActive(false);
        if (timerText != null) timerText.gameObject.SetActive(false);
        if (leftArrowButton != null) leftArrowButton.SetActive(false);
        if (rightArrowButton != null) rightArrowButton.SetActive(false);
        if (restartButton != null) restartButton.SetActive(false);

        // 隱藏所有蜂巢
        foreach (var hive in allHives)
        {
            if (hive.hiveGameObject != null)
                hive.hiveGameObject.SetActive(false);
        }
    }

    private void StartInstructionBlinking()
    {
        if (instructionText != null && !isInstructionBlinking)
        {
            isInstructionBlinking = true;
            instructionBlinkCoroutine = StartCoroutine(BlinkInstructionText());
        }
    }

    private void StopInstructionBlinking()
    {
        if (instructionBlinkCoroutine != null)
        {
            StopCoroutine(instructionBlinkCoroutine);
            instructionBlinkCoroutine = null;
        }

        isInstructionBlinking = false;

        if (instructionText != null)
        {
            Color textColor = instructionText.color;
            textColor.a = instructionMaxAlpha;
            instructionText.color = textColor;
        }
    }

    private IEnumerator BlinkInstructionText()
    {
        if (instructionText == null) yield break;

        while (isInstructionBlinking)
        {
            float alpha = Mathf.Lerp(instructionMinAlpha, instructionMaxAlpha,
                (Mathf.Sin(Time.time * instructionBlinkSpeed * Mathf.PI) + 1f) * 0.5f);

            // 應用透明度
            Color textColor = instructionText.color;
            textColor.a = alpha;
            instructionText.color = textColor;

            yield return null;
        }
    }

    private void StartGameCountdown()
    {
        if (isCountingDown) return;

        // 停止閃爍效果
        StopInstructionBlinking();

        // 隱藏介紹面板
        if (introPanel != null)
            introPanel.SetActive(false);

        // 開始倒數協程
        StartCoroutine(CountdownCoroutine());
    }

    private IEnumerator CountdownCoroutine()
    {
        isCountingDown = true;

        // 顯示您預先設置的倒數面板
        if (countdownPanel != null)
            countdownPanel.SetActive(true);

        // 倒數 3, 2, 1, スタート！
        string[] countdownTexts = { "3", "2", "1", "スタート！" };
        Color[] countdownColors = { Color.red, Color.yellow, Color.green, Color.cyan };

        PlayCountdownSound();

        for (int i = 0; i < countdownTexts.Length; i++)
        {
            if (countdownText != null)
            {
                countdownText.text = countdownTexts[i];
                countdownText.color = countdownColors[i];

                // 文字動畫效果
                StartCoroutine(CountdownTextAnimation(countdownText.transform));
            }


            yield return new WaitForSeconds(0.6f);
        }

        // 隱藏倒數面板
        if (countdownPanel != null)
            countdownPanel.SetActive(false);

        // 開始實際遊戲
        StartActualGame();
    }

    private IEnumerator CountdownTextAnimation(Transform textTransform)
    {
        Vector3 originalScale = Vector3.one;
        textTransform.localScale = Vector3.zero;

        float duration = 0.3f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;

            float scaleMultiplier = Mathf.Lerp(0f, 1.2f, t);
            if (t > 0.7f)
            {
                scaleMultiplier = Mathf.Lerp(1.2f, 1f, (t - 0.7f) / 0.3f);
            }

            textTransform.localScale = originalScale * scaleMultiplier;
            yield return null;
        }

        textTransform.localScale = originalScale;
    }

    private void StartActualGame()
    {
        isCountingDown = false;
        gameStarted = true;

        // 顯示遊戲 UI
        ShowGameUI();

        // 開始原本的遊戲邏輯
        if (!gameInitialized)
        {
            InitializeGame();
        }

        StartGame();

        PlayBackgroundMusic();

        Debug.Log("遊戲正式開始！");
    }

    void OnDrawGizmos()
    {
        DrawHoneycombGizmos();
    }
    #endregion

    #region 初始化
    private void InitializeSingleton()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void InitializeGame()
    {
        if (!ValidateComponents())
        {
            enabled = false;
            return;
        }

        CheckCameraSetup();
        GenerateHoneycombCellPositions();

        if (honeycombCellWorldPositions.Count == 0)
        {
            Debug.LogError("蜂巢格點生成失敗！請檢查設置。");
            enabled = false;
            return;
        }

        gameInitialized = true;
        StartGame();
    }

    private void CheckCameraSetup()
    {
        Camera mainCamera = Camera.main;
        if (mainCamera == null)
        {
            Debug.LogError("找不到主相機！");
            return;
        }

        var raycaster = mainCamera.GetComponent<Physics2DRaycaster>();
        if (raycaster == null)
        {
            mainCamera.gameObject.AddComponent<Physics2DRaycaster>();
        }
    }

    private bool ValidateComponents()
    {
        bool isValid = true;

        if (beePrefab == null || queenBeePrefab == null)
        {
            Debug.LogError("蜜蜂 Prefab 未設置！");
            isValid = false;
        }

        if (hivePrefab == null)
        {
            Debug.LogWarning("蜂巢 Prefab 未設置！將不會顯示蜂巢。");
        }

        // if (hintText == null || hiveInfoText == null || foundQueensText == null || timerText == null)
        // {
        //     Debug.LogError("UI 組件未完全設置！");
        //     isValid = false;
        // }

        if (leftArrowButton == null || rightArrowButton == null)
        {
            Debug.LogError("箭頭按鈕未設置！");
            isValid = false;
        }

        return isValid;
    }
    #endregion

    #region 時間管理
    private void UpdateTimer()
    {
        if (!gameActive || gameIsPaused) return; // 暫停時不更新計時器

        currentGameTime += Time.deltaTime;
        remainingTime = gameTimeLimit - currentGameTime;

        UpdateTimerUI();

        if (remainingTime <= 0f)
        {
            // 時間到，顯示結果面板
            int remainingQueens = totalQueens - foundQueensCount;
            ShowGameResult("時間切れ！", "女王蜂を見つけられませんでした", true); // 時間到！沒有找到女王蜂
        }
    }

    private void UpdateTimerUI()
    {
        if (timerText != null)
        {
            int minutes = Mathf.FloorToInt(remainingTime / 60f);
            int seconds = Mathf.FloorToInt(remainingTime % 60f);

            string timeText = $"残り時間: {minutes:00}:{seconds:00}"; // 日文：剩餘時間

            // 時間不足時變紅
            if (remainingTime <= 30f)
            {
                timerText.color = Color.red;
            }
            else if (remainingTime <= 60f)
            {
                timerText.color = Color.yellow;
            }
            else
            {
                timerText.color = Color.white;
            }

            timerText.text = timeText;
        }
    }

    private void ResetTimer()
    {
        currentGameTime = 0f;
        remainingTime = gameTimeLimit;

        if (timerText != null)
        {
            timerText.color = Color.white;
        }
    }
    #endregion

    #region 網格生成
    private void GenerateHoneycombCellPositions()
    {
        honeycombCellWorldPositions = new List<Vector3>();
        float adjustedCellSize = cellSize + cellSpacing;

        for (int x = 0; x < gridWidth; x++)
        {
            for (int y = 0; y < gridHeight; y++)
            {
                Vector3 cellPosition = new Vector3(
                    (x - gridWidth / 2f) * adjustedCellSize,
                    (y - gridHeight / 2f) * adjustedCellSize,
                    0
                );

                honeycombCellWorldPositions.Add(cellPosition);
            }
        }

        Debug.Log($"生成了 {honeycombCellWorldPositions.Count} 個格點 ({gridWidth}x{gridHeight})");
    }
    #endregion

    #region 蜂巢生成
    private void GenerateHivePositions()
    {
        hivePositions.Clear();

        if (useFixedHivePosition)
        {
            // 所有蜂巢都使用相同的固定位置
            for (int i = 0; i < totalHives; i++)
            {
                hivePositions.Add(fixedHivePosition);
            }
            Debug.Log($"使用固定位置 {fixedHivePosition} 生成了 {hivePositions.Count} 個蜂巢位置");
        }
        else
        {
            // 使用隨機生成的位置（原本的邏輯）
            for (int i = 0; i < totalHives; i++)
            {
                Vector3 newPosition = GetValidHivePosition();
                hivePositions.Add(newPosition);
            }
            Debug.Log($"隨機生成了 {hivePositions.Count} 個蜂巢位置");
        }
    }

    private Vector3 GetValidHivePosition()
    {
        Vector3 position;
        int attempts = 0;
        int maxAttempts = 100;

        do
        {
            // 在指定區域內隨機生成位置
            float x = Random.Range(-hiveAreaWidth / 2f, hiveAreaWidth / 2f);
            float y = Random.Range(-hiveAreaHeight / 2f, hiveAreaHeight / 2f);
            position = new Vector3(x, y, 0);

            attempts++;

            // 如果嘗試太多次，就放棄距離檢查
            if (attempts > maxAttempts)
            {
                Debug.LogWarning("蜂巢位置生成達到最大嘗試次數，可能會有重疊");
                break;
            }

        } while (!IsValidHivePosition(position));

        return position;
    }

    private bool IsValidHivePosition(Vector3 newPosition)
    {
        // 檢查與現有蜂巢的距離
        foreach (Vector3 existingPosition in hivePositions)
        {
            if (Vector3.Distance(newPosition, existingPosition) < minHiveDistance)
            {
                return false;
            }
        }
        return true;
    }
    #endregion

    #region 遊戲流程控制
    public void StartGame()
    {
        if (!gameInitialized) return;

        if (!gameStarted && !isCountingDown)
        {
            ShowIntroScreen();
            return;
        }

        ResetGameState();
        ResetTimer();

        GenerateHivePositions();
        GenerateHoneycombCellPositions();
        GenerateAllHives();

        ShowCurrentHive();
        gameActive = true;

        UpdateUI();

        if (restartButton != null)
            restartButton.SetActive(false);

        Debug.Log($"遊戲開始！時間限制: {gameTimeLimit} 秒");
    }


    private void ResetGameState()
    {
        currentHiveIndex = 0;
        foundQueensCount = 0;
        queenHiveIndices.Clear();
        ClearAllHives();
        hivePositions.Clear();
    }

    private void GenerateAllHives()
    {
        allHives = new List<Hive>();

        // 創建所有蜂窩
        for (int i = 0; i < totalHives; i++)
        {
            Hive newHive = new Hive(i);
            newHive.hivePosition = hivePositions[i];

            // 生成蜂巢 GameObject，設置旋轉為 Z軸 -90度
            if (hivePrefab != null)
            {
                // 設置旋轉：Z軸 90度
                Quaternion hiveRotation = Quaternion.Euler(0, 0, 90);
                newHive.hiveGameObject = Instantiate(hivePrefab, newHive.hivePosition, hiveRotation, transform);
                newHive.hiveGameObject.name = $"Hive_{i}";

                // 初始時隱藏蜂巢（除了第一個）
                newHive.hiveGameObject.SetActive(i == 0);
            }

            allHives.Add(newHive);
        }

        // 隨機選擇有蜂后的蜂窩
        queenHiveIndices = Enumerable.Range(0, totalHives)
            .OrderBy(x => Random.value)
            .Take(totalQueens)
            .ToList();

        Debug.Log($"蜂后將出現在蜂窩: {string.Join(", ", queenHiveIndices)}");

        // 為每個蜂窩生成蜜蜂
        for (int i = 0; i < totalHives; i++)
        {
            bool hasQueen = queenHiveIndices.Contains(i);
            GenerateHiveBees(allHives[i], hasQueen);
        }
    }

    private void GenerateHiveBees(Hive hive, bool hasQueen)
    {
        hive.hasQueen = hasQueen;

        // 創建一個蜜蜂容器，不受蜂巢縮放影響
        GameObject beeContainer = new GameObject($"BeeContainer_{hive.hiveIndex}");
        if (hive.hiveGameObject != null)
        {
            beeContainer.transform.SetParent(hive.hiveGameObject.transform);
            beeContainer.transform.localPosition = Vector3.zero;
            beeContainer.transform.localRotation = Quaternion.identity;
            beeContainer.transform.localScale = Vector3.one; // 保持 1:1 縮放
        }
        else
        {
            beeContainer.transform.SetParent(transform);
            beeContainer.transform.position = hive.hivePosition;
        }

        var chosenPositions = GetRandomBeePositionsAroundHive(hive.hivePosition, beesPerHive);

        // 生成普通蜜蜂
        int normalBeeCount = hasQueen ? beesPerHive - 1 : beesPerHive;

        for (int i = 0; i < normalBeeCount; i++)
        {
            Vector3 spawnPos = chosenPositions[i];
            GameObject beeGo = Instantiate(beePrefab, spawnPos, Quaternion.identity);

            // 將蜜蜂設為蜜蜂容器的子物件，而不是直接設為蜂巢的子物件
            beeGo.transform.SetParent(beeContainer.transform);

            Bee bee = beeGo.GetComponent<Bee>();
            beeGo.transform.localScale = Vector3.one * bee.beeScale; // 正常設置大小

            // 轉換為相對於容器的本地座標
            beeGo.transform.localPosition = beeContainer.transform.InverseTransformPoint(spawnPos);

            if (bee == null)
            {
                bee = beeGo.AddComponent<Bee>();
            }

            bee.Initialize(i + 1);
            SetupBeeMovement(bee, Vector3.zero);

            hive.bees.Add(bee);
        }

        // 生成蜂后
        if (hasQueen)
        {
            Vector3 queenPos = chosenPositions[normalBeeCount];
            GameObject queenGo = Instantiate(queenBeePrefab, queenPos, Quaternion.identity);

            queenGo.transform.SetParent(beeContainer.transform);

            Bee bee = queenGo.GetComponent<Bee>();
            queenGo.transform.localScale = Vector3.one * bee.beeScale * 1.2f; // 蜂后稍微大一點
            queenGo.transform.localPosition = beeContainer.transform.InverseTransformPoint(queenPos);

            QueenBee queenBee = queenGo.GetComponent<QueenBee>();
            if (queenBee == null)
            {
                queenBee = queenGo.AddComponent<QueenBee>();
            }

            queenBee.InitializeAsQueen(beesPerHive, 0.5f);
            SetupBeeMovement(queenBee, Vector3.zero);

            hive.bees.Add(queenBee);
        }

        Debug.Log($"蜂窩 {hive.hiveIndex} 生成完成 - 蜜蜂: {hive.bees.Count}, 有蜂后: {hasQueen}");
    }

    // 修改原有的 SetupBeeMovement 方法
    private void SetupBeeMovement(Bee bee, Vector3 relativeCenter)
    {
        float speed = Random.Range(1.5f, 2.5f);

        // 只設定速度和中心，保留 Prefab 的移動範圍設定
        bee.speed = speed;
        bee.movementCenter = relativeCenter;
        bee.useCustomMovementCenter = true;

        // 不要調用 SetMovementParameters，避免覆蓋 Prefab 設定
        bee.SetNewTargetAroundCenter(relativeCenter);
    }

    private List<Vector3> GetRandomBeePositionsAroundHive(Vector3 hiveCenter, int count)
    {
        List<Vector3> positions = new List<Vector3>();

        for (int i = 0; i < count; i++)
        {
            // 生成相對於蜂巢的位置
            Vector2 randomCircle = Random.insideUnitCircle * (beeMovementRadius * 0.8f);
            Vector3 relativePosition = new Vector3(randomCircle.x, randomCircle.y, 0);

            // 返回世界座標位置用於初始生成
            Vector3 worldPosition = hiveCenter + relativePosition;
            positions.Add(worldPosition);
        }

        return positions;
    }
    #endregion

    #region 蜂窩切換
    private void HandleInput()
    {
        if (!gameActive || isTransitioning) return; // 動畫期間禁止切換

        if (Input.GetKeyDown(KeyCode.LeftArrow))
        {
            SwitchToPreviousHive();
        }
        else if (Input.GetKeyDown(KeyCode.RightArrow))
        {
            SwitchToNextHive();
        }
    }

    public void SwitchToPreviousHive()
    {
        if (!gameActive || isTransitioning) return; // 動畫期間禁止切換

        int newIndex = (currentHiveIndex - 1 + totalHives) % totalHives;
        StartHiveTransition(newIndex, -1); // -1 表示向左
    }

    public void SwitchToNextHive()
    {
        if (!gameActive || isTransitioning) return; // 動畫期間禁止切換

        int newIndex = (currentHiveIndex + 1) % totalHives;
        StartHiveTransition(newIndex, 1); // 1 表示向右
    }

    private void StartHiveTransition(int newHiveIndex, int direction)
    {
        if (currentTransition != null)
        {
            StopCoroutine(currentTransition);
        }

        currentTransition = StartCoroutine(TransitionToHive(newHiveIndex, direction));
    }

    private IEnumerator TransitionToHive(int newHiveIndex, int direction)
    {
        isTransitioning = true;

        var oldHive = allHives[currentHiveIndex];
        var newHive = allHives[newHiveIndex];

        Debug.Log($"開始切換動畫：從蜂窩 {currentHiveIndex + 1} 到 {newHiveIndex + 1}");

        switch (transitionType)
        {
            case HiveTransitionType.Slide:
                yield return StartCoroutine(SlideTransition(oldHive, newHive, direction));
                break;
            case HiveTransitionType.Fade:
                yield return StartCoroutine(FadeTransition(oldHive, newHive));
                break;
            case HiveTransitionType.Scale:
                yield return StartCoroutine(ScaleTransition(oldHive, newHive));
                break;
            case HiveTransitionType.SlideAndFade:
                yield return StartCoroutine(SlideAndFadeTransition(oldHive, newHive, direction));
                break;
        }

        // 動畫完成後，確保狀態正確
        currentHiveIndex = newHiveIndex;

        // 隱藏舊蜂窩，顯示新蜂窩
        SetHiveActive(oldHive, false);
        SetHiveActive(newHive, true);

        // 關鍵：確保新蜂窩在正確位置，蜜蜂立即歸位
        SetHiveWorldPosition(newHive, newHive.hivePosition);

        UpdateUI();

        isTransitioning = false;
        currentTransition = null;

        Debug.Log($"切換動畫完成：當前蜂窩 {currentHiveIndex + 1}");
    }

    #region 動畫效果
    private IEnumerator SlideTransition(Hive oldHive, Hive newHive, int direction)
    {
        Camera cam = Camera.main;
        float slideDistance = cam.orthographicSize * cam.aspect * 2f;

        Vector3 newHiveStartPos = newHive.hivePosition + Vector3.right * (slideDistance * direction);
        Vector3 oldHiveEndPos = oldHive.hivePosition + Vector3.left * (slideDistance * direction);

        // 激活新蜂窝并设置初始位置
        SetHiveActive(newHive, true);
        if (newHive.hiveGameObject != null)
        {
            newHive.hiveGameObject.transform.position = newHiveStartPos;
        }

        float elapsed = 0f;
        while (elapsed < hiveTransitionDuration)
        {
            elapsed += Time.deltaTime;
            float t = transitionCurve.Evaluate(elapsed / hiveTransitionDuration);

            // 移動蜂巢，蜜蜂會自動跟著移動
            if (oldHive.hiveGameObject != null)
            {
                Vector3 oldCurrentPos = Vector3.Lerp(oldHive.hivePosition, oldHiveEndPos, t);
                oldHive.hiveGameObject.transform.position = oldCurrentPos;
            }

            if (newHive.hiveGameObject != null)
            {
                Vector3 newCurrentPos = Vector3.Lerp(newHiveStartPos, newHive.hivePosition, t);
                newHive.hiveGameObject.transform.position = newCurrentPos;
            }

            yield return null;
        }

        // 確保最終位置正確
        if (newHive.hiveGameObject != null)
        {
            newHive.hiveGameObject.transform.position = newHive.hivePosition;
        }

        if (oldHive.hiveGameObject != null)
        {
            oldHive.hiveGameObject.transform.position = oldHive.hivePosition;
        }
    }

    private IEnumerator FadeTransition(Hive oldHive, Hive newHive)
    {
        SetHiveActive(newHive, true);

        // 設置初始透明度
        SetHiveAlpha(newHive, 0f);

        float elapsed = 0f;
        while (elapsed < hiveTransitionDuration)
        {
            elapsed += Time.deltaTime;
            float t = transitionCurve.Evaluate(elapsed / hiveTransitionDuration);

            SetHiveAlpha(oldHive, 1f - t);
            SetHiveAlpha(newHive, t);

            yield return null;
        }

        // 確保最終透明度正確
        SetHiveAlpha(oldHive, 0f);
        SetHiveAlpha(newHive, 1f);
    }

    private IEnumerator ScaleTransition(Hive oldHive, Hive newHive)
    {
        SetHiveActive(newHive, true);

        // 設置初始縮放
        SetHiveScale(newHive, Vector3.zero);

        float elapsed = 0f;
        while (elapsed < hiveTransitionDuration)
        {
            elapsed += Time.deltaTime;
            float t = transitionCurve.Evaluate(elapsed / hiveTransitionDuration);

            SetHiveScale(oldHive, Vector3.one * (1f - t));
            SetHiveScale(newHive, Vector3.one * t);

            yield return null;
        }

        // 確保最終縮放正確
        SetHiveScale(oldHive, Vector3.zero);
        SetHiveScale(newHive, Vector3.one);
    }

    private IEnumerator SlideAndFadeTransition(Hive oldHive, Hive newHive, int direction)
    {
        Camera cam = Camera.main;
        float slideDistance = cam.orthographicSize * cam.aspect * 1.5f;

        Vector3 newHiveStartPos = newHive.hivePosition + Vector3.right * (slideDistance * direction);
        Vector3 oldHiveEndPos = oldHive.hivePosition + Vector3.left * (slideDistance * direction);

        SetHiveActive(newHive, true);
        SetHiveWorldPosition(newHive, newHiveStartPos);
        SetHiveAlpha(newHive, 0f);

        float elapsed = 0f;
        while (elapsed < hiveTransitionDuration)
        {
            elapsed += Time.deltaTime;
            float t = transitionCurve.Evaluate(elapsed / hiveTransitionDuration);

            // 移動
            Vector3 oldCurrentPos = Vector3.Lerp(oldHive.hivePosition, oldHiveEndPos, t);
            SetHiveWorldPosition(oldHive, oldCurrentPos);

            Vector3 newCurrentPos = Vector3.Lerp(newHiveStartPos, newHive.hivePosition, t);
            SetHiveWorldPosition(newHive, newCurrentPos);

            // 淡化
            SetHiveAlpha(oldHive, 1f - t);
            SetHiveAlpha(newHive, t);

            yield return null;
        }

        SetHiveWorldPosition(newHive, newHive.hivePosition);
        SetHiveAlpha(oldHive, 0f);
        SetHiveAlpha(newHive, 1f);
    }
    #endregion

    #region 蜂窩控制輔助方法
    private void SetHiveActive(Hive hive, bool active)
    {
        if (hive.hiveGameObject != null)
        {
            hive.hiveGameObject.SetActive(active);
        }

        foreach (var bee in hive.bees)
        {
            if (bee != null)
            {
                // bee.SetClickable(active && gameActive);

                if (active)
                {
                    // 只更新移動中心，保留 Prefab 的移動範圍設定
                    bee.movementCenter = Vector3.zero;
                    bee.useCustomMovementCenter = true;
                    bee.SetNewTargetAroundCenter(Vector3.zero);
                }
            }
        }

        hive.isActive = active;

        Debug.Log($"蜂窩 {hive.hiveIndex} 設置為 {(active ? "顯示" : "隱藏")}，蜜蜂數量: {hive.bees.Count}");
    }

    private void SetHiveWorldPosition(Hive hive, Vector3 worldPosition)
    {
        // 只需要移動蜂巢，蜜蜂會自動跟著移動
        if (hive.hiveGameObject != null)
        {
            hive.hiveGameObject.transform.position = worldPosition;
        }

        foreach (var bee in hive.bees)
        {
            if (bee != null)
            {
                bee.movementCenter = Vector3.zero;
                bee.useCustomMovementCenter = true;
                bee.SetNewTargetAroundCenter(Vector3.zero);
            }
        }
    }

    private void SetHiveAlpha(Hive hive, float alpha)
    {
        // 設置蜂巢透明度
        if (hive.hiveGameObject != null && hive.hiveGameObject.activeInHierarchy)
        {
            var renderers = hive.hiveGameObject.GetComponentsInChildren<SpriteRenderer>();
            foreach (var renderer in renderers)
            {
                Color color = renderer.color;
                color.a = alpha;
                renderer.color = color;
            }
        }

        // 設置蜜蜂透明度
        foreach (var bee in hive.bees)
        {
            if (bee != null && bee.gameObject.activeInHierarchy)
            {
                var renderers = bee.GetComponentsInChildren<SpriteRenderer>();
                foreach (var renderer in renderers)
                {
                    Color color = renderer.color;
                    color.a = alpha;
                    renderer.color = color;
                }
            }
        }
    }

    private void SetHiveScale(Hive hive, Vector3 scale)
    {
        // 設置蜂巢縮放
        if (hive.hiveGameObject != null && hive.hiveGameObject.activeInHierarchy)
        {
            hive.hiveGameObject.transform.localScale = scale;
        }

        // 設置蜜蜂縮放
        foreach (var bee in hive.bees)
        {
            if (bee != null && bee.gameObject.activeInHierarchy)
            {
                bee.transform.localScale = scale;
            }
        }
    }
    #endregion

    // 修改原有的 ShowCurrentHive 方法
    private void ShowCurrentHive()
    {
        // 如果不是在動畫中，直接切換
        if (!isTransitioning)
        {
            for (int i = 0; i < allHives.Count; i++)
            {
                bool isCurrentHive = (i == currentHiveIndex);
                SetHiveActive(allHives[i], isCurrentHive);
            }

            // 只有在啟用自動聚焦時才移動相機
            if (enableCameraAutoFocus && allHives[currentHiveIndex].hiveGameObject != null)
            {
                FocusCameraOnHive(allHives[currentHiveIndex].hivePosition);
            }

            Debug.Log($"顯示蜂窩 {currentHiveIndex + 1}/{totalHives}");
        }
    }

    // 相機聚焦也可以加上動畫
    private void FocusCameraOnHive(Vector3 hivePosition)
    {
        if (!enableCameraAutoFocus || currentTransition != null) return; // 檢查是否啟用相機聚焦

        Camera mainCamera = Camera.main;
        if (mainCamera != null)
        {
            StartCoroutine(SmoothCameraMove(mainCamera, hivePosition));
        }
    }

    private IEnumerator SmoothCameraMove(Camera camera, Vector3 targetPosition)
    {
        Vector3 startPosition = camera.transform.position;
        Vector3 endPosition = new Vector3(targetPosition.x, targetPosition.y, startPosition.z);

        float elapsed = 0f;
        float duration = 0.3f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0, 1, elapsed / duration);

            camera.transform.position = Vector3.Lerp(startPosition, endPosition, t);
            yield return null;
        }

        camera.transform.position = endPosition;
    }
    #endregion

    #region 音效系統
    private void SetupAudioSource()
    {
        // 設置 AudioSource 基本屬性
        audioSource.playOnAwake = false;
        audioSource.volume = masterVolume;
        audioSource.spatialBlend = 0f;

        bgmAudioSource.spatialBlend = 0f;

        Debug.Log("音效系統初始化完成");
    }

    private void PlayBackgroundMusic()
    {
        bgmAudioSource.Play();

        // 開始淡出協程
        StartCoroutine(BackgroundMusicFadeOut());
    }

    private void StopBackgroundMusic()
    {
        if (bgmAudioSource != null && bgmAudioSource.isPlaying)
        {
            bgmAudioSource.Stop();
            Debug.Log("停止背景音樂");
        }
    }

    private IEnumerator BackgroundMusicFadeOut()
    {
        // 等待到指定時間開始淡出
        yield return new WaitForSeconds(fadeOutStartTime);

        if (bgmAudioSource != null && bgmAudioSource.isPlaying)
        {
            float startVolume = bgmAudioSource.volume;
            float elapsed = 0f;

            Debug.Log($"開始淡出背景音樂 - 從音量 {startVolume} 淡出 {fadeOutDuration} 秒");

            while (elapsed < fadeOutDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / fadeOutDuration;

                // 使用平滑曲線淡出
                float currentVolume = Mathf.Lerp(startVolume, 0f, t * t); // 二次方曲線，更自然的淡出
                bgmAudioSource.volume = currentVolume;

                yield return null;
            }

            bgmAudioSource.volume = 0f;
            bgmAudioSource.Stop();
            Debug.Log("背景音樂淡出完成");
        }
    }

    private void PlayStartButtonSound()
    {
        if (audioSource != null && startButtonSound != null)
        {
            float finalVolume = masterVolume * startButtonVolume;
            audioSource.PlayOneShot(startButtonSound, finalVolume);
            Debug.Log($"播放開始按鍵音效 - 音量: {finalVolume}");
        }
        else
        {
            Debug.LogWarning("無法播放開始按鍵音效 - AudioSource或音效檔案未設置");
        }
    }

    private void PlayCountdownSound()
    {
        if (audioSource != null && countdownSound != null)
        {
            float finalVolume = masterVolume * countdownVolume;
            audioSource.PlayOneShot(countdownSound, finalVolume);
            Debug.Log($"播放倒數音效 - 音量: {finalVolume}");
        }
        else
        {
            Debug.LogWarning("無法播放倒數音效 - AudioSource或音效檔案未設置");
        }
    }

    private void PlayCorrectSound()
    {
        if (audioSource != null && correctAnswerSound != null)
        {
            float finalVolume = masterVolume * correctSoundVolume;
            audioSource.PlayOneShot(correctAnswerSound, finalVolume);
            Debug.Log("播放正確答案音效");
        }
        else
        {
            Debug.LogWarning("無法播放正確答案音效 - AudioSource或音效檔案未設置");
        }
    }

    private void PlayWrongSound()
    {
        if (audioSource != null && wrongAnswerSound != null)
        {
            float soundVolume = masterVolume * wrongSoundVolume;
            audioSource.PlayOneShot(wrongAnswerSound, soundVolume);
            Debug.Log("播放錯誤答案音效");
        }
        else
        {
            Debug.LogWarning("無法播放錯誤答案音效 - AudioSource或音效檔案未設置");
        }
    }

    [ContextMenu("測試背景音樂")]
    public void TestBackgroundMusic()
    {
        Debug.Log("測試背景音樂");
        PlayBackgroundMusic();
    }

    [ContextMenu("停止背景音樂")]
    public void TestStopBackgroundMusic()
    {
        Debug.Log("停止背景音樂");
        StopBackgroundMusic();
    }

    [ContextMenu("測試正確音效")]
    public void TestCorrectSound()
    {
        Debug.Log("測試正確答案音效");
        PlayCorrectSound();
    }

    [ContextMenu("測試錯誤音效")]
    public void TestWrongSound()
    {
        Debug.Log("測試錯誤答案音效");
        PlayWrongSound();
    }

    [ContextMenu("測試開始按鍵音效")]
    public void TestStartButtonSound()
    {
        Debug.Log("測試開始按鍵音效");
        PlayStartButtonSound();
    }

    // 公開方法，讓其他腳本可以調用
    public void PlaySoundEffect(bool isCorrect)
    {
        if (isCorrect)
        {
            PlayCorrectSound();
        }
        else
        {
            PlayWrongSound();
        }
    }
    #endregion

    #region 玩家互動
    public void PlayerGuessedBee(Bee guessedBee)
    {
        if (!gameActive || guessedBee == null) return;

        Debug.Log($"玩家猜測蜜蜂 - ID: {guessedBee.BeeID}, 是蜂后: {guessedBee.IsQueen}");

        if (guessedBee.IsQueen)
        {
            HandleCorrectGuess();
        }
        else
        {
            HandleWrongGuess();
        }
    }

    private void HandleCorrectGuess()
    {
        // 播放正確音效
        PlayCorrectSound();

        foundQueensCount++;

        // 移除找到的蜂后
        var currentHive = allHives[currentHiveIndex];
        var queen = currentHive.bees.FirstOrDefault(b => b.IsQueen);
        if (queen != null)
        {
            currentHive.bees.Remove(queen);
            Destroy(queen.gameObject);
            currentHive.hasQueen = false;
        }

        Debug.Log($"找到蜂后！已找到 {foundQueensCount}/{totalQueens} 隻");

        // 計算時間資訊
        float usedTime = currentGameTime;
        int usedMinutes = Mathf.FloorToInt(usedTime / 60f);
        int usedSeconds = Mathf.FloorToInt(usedTime % 60f);

        ShowGameResult("おめでとうございます！", // 恭喜！
        $"女王蜂を見つけました！\n所要時間: {usedMinutes:00}:{usedSeconds:00}", // 找到女王蜂！所需時間
        true);
    }

    private void HandleWrongGuess()
    {
        // 播放錯誤音效
        PlayWrongSound();

        Debug.Log("選錯了蜜蜂");

        // 可以在這裡加入視覺效果，比如蜜蜂閃紅光等
        // 或者讓蜜蜂稍微震動一下
    }

    // ...rest of existing methods...
    #endregion

    #region 遊戲結束
    private void EndGame(bool success)
    {
        gameActive = false;

        if (success)
        {
            float usedTime = currentGameTime;
            int minutes = Mathf.FloorToInt(usedTime / 60f);
            int seconds = Mathf.FloorToInt(usedTime % 60f);

            // 使用 hiveInfoText 顯示結果（因為沒有 hintText 了）
            if (hiveInfoText != null)
            {
                hiveInfoText.text = $"ゲームクリア！\n{minutes:00}:{seconds:00}"; // 遊戲通關！
            }
        }
        else
        {
            if (hiveInfoText != null)
            {
                hiveInfoText.text = "時間切れ！"; // 時間到！
            }
        }

        if (restartButton != null)
            restartButton.SetActive(true);
    }

    private void ShowGameResult(string title, string timeInfo, bool isGameEnd = false)
    {
        gameIsPaused = true;

        SetAllBeesMoving(false);

        if (gameResultPanel != null)
        {
            gameResultPanel.SetActive(true);
        }

        if (resultTitleText != null)
        {
            resultTitleText.text = title;

            // 根據結果設置顏色
            if (title.Contains("おめでとう") || title.Contains("女王蜂")) // 日文檢查
            {
                resultTitleText.color = Color.green;
            }
            else if (title.Contains("時間"))
            {
                resultTitleText.color = Color.red;
            }
            else
            {
                resultTitleText.color = Color.yellow;
            }
        }

        if (resultTimeText != null)
        {
            resultTimeText.text = timeInfo;
        }

        if (isGameEnd)
        {
            gameActive = false;

            if (restartButton != null)
                restartButton.SetActive(true);
        }

        Debug.Log($"遊戲暫停 - {title}");
    }

    // 新增：隱藏結果面板（繼續遊戲）
    private void HideGameResult()
    {
        if (!gameActive) return; // 遊戲已結束，不能繼續

        gameIsPaused = false;

        // 恢復蜜蜂移動
        SetAllBeesMoving(true);

        // 隱藏結果面板
        if (gameResultPanel != null)
        {
            gameResultPanel.SetActive(false);
        }

        UpdateUI(); // 更新UI顯示

        Debug.Log("遊戲繼續");
    }

    // 新增：控制所有蜜蜂移動的方法
    private void SetAllBeesMoving(bool isMoving)
    {
        foreach (var hive in allHives)
        {
            foreach (var bee in hive.bees)
            {
                if (bee != null)
                {
                    bee.SetMoving(isMoving);
                }
            }
        }
    }

    // 修改重新開始按鈕事件
    public void OnRestartButtonClicked()
    {
        gameIsPaused = false;
        gameStarted = false;

        StopInstructionBlinking();

        if (gameResultPanel != null)
            gameResultPanel.SetActive(false);

        ShowIntroScreen();
    }
    #endregion

    #region 蜜蜂管理
    private void ClearAllHives()
    {
        foreach (var hive in allHives)
        {
            // 清除蜜蜂
            foreach (var bee in hive.bees)
            {
                if (bee != null)
                {
                    Destroy(bee.gameObject);
                }
            }
            hive.bees.Clear();

            // 清除蜂巢
            if (hive.hiveGameObject != null)
            {
                Destroy(hive.hiveGameObject);
                hive.hiveGameObject = null;
            }
        }
        allHives.Clear();
    }

    // private void SetAllBeesClickable(bool clickable)
    // {
    //     foreach (var hive in allHives)
    //     {
    //         foreach (var bee in hive.bees)
    //         {
    //             if (bee != null)
    //             {
    //                 bee.SetClickable(clickable);
    //             }
    //         }
    //     }
    // }
    #endregion

    #region UI 更新
    private void UpdateUI()
    {
        // 刪除 hintText 和 foundQueensText 相關代碼

        if (hiveInfoText != null)
        {
            // 只顯示當前蜂巢編號 - 日文
            hiveInfoText.text = $"ハチの巣 {currentHiveIndex + 1}/{totalHives}"; // 蜂巢
        }

        // 計時器在 UpdateTimer() 方法中處理
    }
    #endregion

    #region 輔助方法
    public float GetCurrentRoundDifficulty()
    {
        return 0.5f; // 固定中等難度
    }

    private void DrawHoneycombGizmos()
    {
        if (honeycombCellWorldPositions != null && enableDebugMode)
        {
            Gizmos.color = Color.blue;
            foreach (Vector3 pos in honeycombCellWorldPositions)
            {
                Gizmos.DrawSphere(pos, 0.1f);
            }
        }
    }
    #endregion
}

