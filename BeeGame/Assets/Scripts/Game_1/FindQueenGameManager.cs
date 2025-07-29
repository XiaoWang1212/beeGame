using UnityEngine;
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

    [Header("Grid Generation Settings")]
    [Tooltip("網格原點世界座標")]
    public Vector3 gridOrigin = Vector3.zero;

    [Tooltip("格子大小")]
    public float cellSize = 1.0f;

    [Tooltip("網格寬度（格子數量）")]
    public int gridWidth = 10;

    [Tooltip("網格高度（格子數量）")]
    public int gridHeight = 8;

    [Tooltip("格子之間的間距")]
    public float cellSpacing = 0.1f;

    [Header("UI References")]
    public TextMeshProUGUI hintText;
    public TextMeshProUGUI roundInfoText;
    public TextMeshProUGUI timerText;
    public GameObject restartButton;

    [Header("Game Settings")]
    public float roundTimeLimit = 30.0f;
    public float wrongGuessTimePenalty = 5.0f;
    public bool enableDebugMode = false;

    [HideInInspector]
    public List<Vector3> honeycombCellWorldPositions;

    // 私有變數
    private List<Bee> activeBees = new List<Bee>();
    private Bee queenBee;
    private int currentRound = 0;
    private int score = 0;
    private float currentRoundTimeRemaining;
    private bool roundActive = false;
    private bool gameInitialized = false;

    // 回合配置
    private readonly Dictionary<int, RoundConfig> roundConfigs = new Dictionary<int, RoundConfig>
    {
        {1, new RoundConfig(20, 0.0f, "蜂后看起來明顯比其他蜜蜂更大且顏色更深。")},
        {2, new RoundConfig(30, 0.5f, "蜂后的體型稍大，但顏色與其他蜜蜂接近。")},
        {3, new RoundConfig(40, 1.0f, "蜂后與其他蜜蜂幾乎沒有視覺差異，需要仔細觀察。")}
    };

    // 內部類別：回合配置
    [System.Serializable]
    public class RoundConfig
    {
        public int numBees;
        public float difficulty;
        public string hint;

        public RoundConfig(int bees, float diff, string hintText)
        {
            numBees = bees;
            difficulty = diff;
            hint = hintText;
        }
    }

    #region Unity生命週期
    void Awake()
    {
        InitializeSingleton();
    }

    void Start()
    {
        InitializeGame();
    }

    void Update()
    {
        if (roundActive)
        {
            UpdateRoundTimer();
        }

        // 調試功能
        if (enableDebugMode)
        {
            if (Input.GetKeyDown(KeyCode.Space) && queenBee != null)
            {
                Debug.Log("強制觸發蜂后猜測（調試用）");
                PlayerGuessedBee(queenBee);
            }
        }
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

        // 檢查相機設置
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
            Debug.LogError("找不到主相機！請確保場景中有標籤為 MainCamera 的相機。");
            return;
        }

        // 檢查是否有 Physics2DRaycaster
        var raycaster = mainCamera.GetComponent<Physics2DRaycaster>();
        if (raycaster == null)
        {
            Debug.Log("主相機沒有 Physics2DRaycaster，正在添加...");
            mainCamera.gameObject.AddComponent<Physics2DRaycaster>();
        }

        // 檢查相機設置
        Debug.Log($"相機設置檢查完成 - 位置: {mainCamera.transform.position}, 正交: {mainCamera.orthographic}");
    }

    private bool ValidateComponents()
    {
        bool isValid = true;

        if (beePrefab == null)
        {
            Debug.LogError("BeePrefab 未設置！");
            isValid = false;
        }

        if (queenBeePrefab == null)
        {
            Debug.LogError("QueenBeePrefab 未設置！");
            isValid = false;
        }

        if (hintText == null || roundInfoText == null || timerText == null)
        {
            Debug.LogError("UI 組件未完全設置！");
            isValid = false;
        }

        return isValid;
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
                ) + gridOrigin;

                honeycombCellWorldPositions.Add(cellPosition);
            }
        }

        if (enableDebugMode)
        {
            Debug.Log($"生成了 {honeycombCellWorldPositions.Count} 個長方形格點 ({gridWidth}x{gridHeight})");
        }
    }
    #endregion

    #region 遊戲流程控制
    public void StartGame()
    {
        if (!gameInitialized) return;

        ResetGameState();
        UpdateUI("歡迎來到蜂后尋找遊戲！", "你的任務是找到蜂后。", "");

        if (restartButton != null)
            restartButton.SetActive(false);

        Invoke(nameof(StartNextRound), 2f);
    }

    public void StartNextRound()
    {
        Debug.Log($"=== StartNextRound 開始 ===");
        Debug.Log($"當前回合: {currentRound}");
        Debug.Log($"回合配置總數: {roundConfigs.Count}");
        
        if (currentRound >= roundConfigs.Count)
        {
            Debug.Log("所有回合完成，結束遊戲");
            EndGame(true);
            return;
        }

        currentRound++;
        Debug.Log($"進入回合: {currentRound}");
        PrepareNewRound();
    }

    private void PrepareNewRound()
    {
        Debug.Log($"=== PrepareNewRound 開始 ===");
        Debug.Log($"回合 {currentRound} 準備中...");
        
        ClearBees();

        if (!roundConfigs.ContainsKey(currentRound))
        {
            Debug.LogError($"找不到回合 {currentRound} 的配置！");
            return;
        }

        var config = roundConfigs[currentRound];
        Debug.Log($"回合配置 - 蜜蜂數: {config.numBees}, 難度: {config.difficulty}");

        UpdateRoundUI(config);
        SpawnBees(config.numBees, config.difficulty);
        StartRoundTimer();
        
        Debug.Log($"回合 {currentRound} 準備完成");
    }

    private void ResetGameState()
    {
        currentRound = 0;
        score = 0;
        roundActive = false;
        ClearBees();
    }

    private void UpdateRoundUI(RoundConfig config)
    {
        string roundInfo = $"回合: {currentRound} / {roundConfigs.Count}\n蜜蜂數量: {config.numBees}";
        string hint = $"蜂后提示 ({GetDifficultyText(config.difficulty)} 差異):\n{config.hint}";

        UpdateUI(hint, roundInfo, "");
    }

    private void StartRoundTimer()
    {
        currentRoundTimeRemaining = roundTimeLimit;
        roundActive = true;
        SetBeesClickable(true);
        UpdateTimerUI();
    }
    #endregion

    #region 蜜蜂管理
    private void SpawnBees(int count, float difficultyLevel)
    {
        if (!ValidateSpawnConditions(count)) return;

        activeBees = new List<Bee>();
        var chosenPositions = GetRandomBeePositions(count);

        // 先生成普通蜜蜂
        for (int i = 0; i < count - 1; i++)
        {
            Vector3 spawnPos = chosenPositions[i];
            GameObject beeGo = Instantiate(beePrefab, spawnPos, Quaternion.identity, transform);

            Bee bee = beeGo.GetComponent<Bee>();
            if (bee == null)
            {
                bee = beeGo.AddComponent<Bee>();
            }

            bee.Initialize(i + 1);

            // 設置移動參數
            float speed = Random.Range(1.5f, 2.5f);
            float radius = Random.Range(1f, 2f);
            bee.SetMovementParameters(speed, radius);

            activeBees.Add(bee);
        }

        // 生成蜂后
        Vector3 queenPos = chosenPositions[count - 1];
        GameObject queenGo = Instantiate(queenBeePrefab, queenPos, Quaternion.identity, transform);

        QueenBee queenBee = queenGo.GetComponent<QueenBee>();
        if (queenBee == null)
        {
            queenBee = queenGo.AddComponent<QueenBee>();
        }

        queenBee.InitializeAsQueen(count, difficultyLevel);

        // 設置蜂后移動參數
        float queenSpeed = Random.Range(1.2f, 2.0f);
        float queenRadius = Random.Range(0.8f, 1.5f);
        queenBee.SetMovementParameters(queenSpeed, queenRadius);

        activeBees.Add(queenBee);
        this.queenBee = queenBee;

        Debug.Log($"生成完成 - 普通蜜蜂: {count - 1} 隻, 蜂后: 1 隻");
    }

    private bool ValidateSpawnConditions(int count)
    {
        if (honeycombCellWorldPositions.Count < count)
        {
            Debug.LogError($"蜂巢格子不足！需要 {count} 個，但只有 {honeycombCellWorldPositions.Count} 個");
            EndGame(false);
            return false;
        }
        return true;
    }

    private List<Vector3> GetRandomBeePositions(int count)
    {
        return honeycombCellWorldPositions
            .OrderBy(x => Random.value)
            .Take(count)
            .ToList();
    }

    private void ClearBees()
    {
        foreach (var bee in activeBees)
        {
            if (bee != null)
            {
                Destroy(bee.gameObject);
            }
        }
        activeBees.Clear();
        queenBee = null;
    }

    private void SetBeesClickable(bool clickable)
    {
        foreach (var bee in activeBees)
        {
            if (bee != null)
            {
                bee.SetClickable(clickable);
            }
        }
    }
    #endregion

    #region 玩家互動
    public void PlayerGuessedBee(Bee guessedBee)
    {
        Debug.Log($"=== PlayerGuessedBee 開始 ===");
        Debug.Log($"roundActive: {roundActive}");
        Debug.Log($"guessedBee: {guessedBee?.name}");
        Debug.Log($"guessedBee.BeeID: {guessedBee?.BeeID}");
        Debug.Log($"guessedBee.IsQueen: {guessedBee?.IsQueen}");
        Debug.Log($"guessedBee 類型: {guessedBee?.GetType().Name}");
        
        if (!roundActive || guessedBee == null) 
        {
            Debug.LogError($"無法處理猜測 - roundActive: {roundActive}, guessedBee: {guessedBee}");
            return;
        }

        Debug.Log($"玩家猜測蜜蜂 - ID: {guessedBee.BeeID}, 是蜂后: {guessedBee.IsQueen}, 類型: {guessedBee.GetType().Name}");

        SetBeesClickable(false);

        if (guessedBee.IsQueen)
        {
            Debug.Log("✅ 正確！這是蜂后！");
            HandleCorrectGuess();
        }
        else
        {
            Debug.Log("❌ 錯誤！這不是蜂后！");
            HandleWrongGuess();
        }
    }

    private void HandleCorrectGuess()
    {
        Debug.Log($"=== HandleCorrectGuess 開始 ===");
        Debug.Log($"當前分數: {score}");
        Debug.Log($"當前回合: {currentRound}");
        
        score++;
        roundActive = false;

        string message = "找到了蜂后！準備進入下一回合...";
        hintText.text = message;
        
        Debug.Log($"✅ 正確猜測處理完成 - 新分數: {score}");
        Debug.Log($"2秒後將呼叫 StartNextRound");

        Invoke(nameof(StartNextRound), 2f);
    }

    private void HandleWrongGuess()
    {
        currentRoundTimeRemaining -= wrongGuessTimePenalty;
        currentRoundTimeRemaining = Mathf.Max(0, currentRoundTimeRemaining);

        hintText.text = $"這不是蜂后！懲罰 {wrongGuessTimePenalty} 秒。";
        UpdateTimerUI();

        Debug.Log($"錯誤猜測 - 剩餘時間: {currentRoundTimeRemaining}");

        if (currentRoundTimeRemaining <= 0)
        {
            EndGame(false);
        }
        else
        {
            Invoke(nameof(ReEnableBeeClick), 1.0f);
        }
    }

    private void ReEnableBeeClick()
    {
        if (roundActive && currentRoundTimeRemaining > 0)
        {
            SetBeesClickable(true);
            var config = roundConfigs[currentRound];
            hintText.text = $"蜂后提示 ({GetDifficultyText(config.difficulty)} 差異):\n{config.hint}";
        }
    }
    #endregion

    #region 計時器
    private void UpdateRoundTimer()
    {
        currentRoundTimeRemaining -= Time.deltaTime;
        UpdateTimerUI();

        if (currentRoundTimeRemaining <= 0)
        {
            currentRoundTimeRemaining = 0;
            UpdateTimerUI();
            EndGame(false);
        }
    }

    private void UpdateTimerUI()
    {
        if (timerText != null)
        {
            timerText.text = $"時間: {Mathf.Max(0, currentRoundTimeRemaining):F1}s";
        }
    }
    #endregion

    #region 遊戲結束
    private void EndGame(bool success)
    {
        roundActive = false;
        SetBeesClickable(false);
        ClearBees();

        string resultText = success ? "恭喜！你成功找到了所有蜂后！" : "遊戲結束！";
        string scoreText = $"最終得分: {score} / {roundConfigs.Count}";

        UpdateUI(scoreText, resultText, "");

        if (restartButton != null)
            restartButton.SetActive(true);
    }

    public void OnRestartButtonClicked()
    {
        StartGame();
    }
    #endregion

    #region UI 更新
    private void UpdateUI(string hint, string roundInfo, string timer)
    {
        if (hintText != null) hintText.text = hint;
        if (roundInfoText != null) roundInfoText.text = roundInfo;
        if (timerText != null) timerText.text = timer;
    }
    #endregion

    #region 輔助方法
    private string GetDifficultyText(float difficulty)
    {
        if (difficulty < 0.3f) return "明顯";
        if (difficulty < 0.7f) return "中等";
        return "微弱";
    }

    public float GetCurrentRoundDifficulty()
    {
        return currentRound > 0 && roundConfigs.ContainsKey(currentRound)
            ? roundConfigs[currentRound].difficulty
            : 0f;
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