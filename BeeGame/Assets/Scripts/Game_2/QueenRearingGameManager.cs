using UnityEngine;
using System.Collections.Generic;
using TMPro;
using UnityEngine.UI;
using Game_2;

public class QueenRearingGameManager : MonoBehaviour
{
    public static QueenRearingGameManager Instance { get; private set; }

    [Header("Cursor Manager")]
    public CursorManager cursorManager;

    [Header("現有遊戲物件 - 直接拖入場景中的物件")]
    public HoneycombGrid honeycombGrid;     // 巢脾（場景中現有的）
    public SpecialCup[] specialCups;       // 所有杯子（場景中現有的）
    public GooseFeather gooseFeather;      // 鵝毛（ingame 專用）
    public Tweezers tweezers;              // 鑷子（ingame 專用）
    public HoneyJar honeyJar;              // 蜂蜜罐（場景中現有的）

    [Header("教學專用工具引用")]
    public GooseFeather tutorialGooseFeather;  // 教學專用鵝毛
    public Tweezers tutorialTweezers;          // 教學專用鑷子
    public Larva tutorialLarva;

    [Header("遊戲設置")]
    public int totalLarvae = 5;            // 總幼蟲數量（改為5）
    public float gameTimeLimit = 60f;      // 遊戲時間限制（秒）

    [Header("UI 組件")]
    public TextMeshProUGUI timerText;
    public TextMeshProUGUI scoreText;
    public GameObject ingamePanel; // 新增：遊戲中的 UI Panel（包含 Timer 和 Score）
    public GameObject gameOverPanel;

    [Header("Game Over Panel UI")]
    public TextMeshProUGUI gameOverTitleText;     // 遊戲結束標題
    public TextMeshProUGUI gameOverMessageText;   // 遊戲結束訊息
    public TextMeshProUGUI remainingTimeText;     // 剩餘時間顯示

    [Header("Audio Settings")]
    [Tooltip("工具選擇音效")]
    public AudioClip toolSelectSound;

    [Tooltip("鵝毛沾蜜音效")]
    public AudioClip dipHoneySound;

    [Tooltip("鑷子夾取音效")]
    public AudioClip tweezersPickSound;

    [Tooltip("幼蟲放入杯子音效")]
    public AudioClip larvaDropSound;

    [Tooltip("成功收集音效")]
    public AudioClip successCollectSound;

    [Tooltip("時間警告音效")]
    public AudioClip timeWarningSound;

    [Tooltip("遊戲完成音效")]
    public AudioClip gameCompleteSound;

    private List<Larva> activeLarvae = new List<Larva>();
    private List<SpecialCup> cupsList = new List<SpecialCup>();

    private float currentTime;
    private int collectedLarvae = 0;
    private bool gameActive = false;
    private GameState currentState = GameState.SelectingTool;
    public Tool selectedTool = Tool.None;
    public int CollectedLarvae => collectedLarvae;

    // 添加教學模式控制
    private bool isTutorialMode = true;

    public enum GameState
    {
        SelectingTool,      // 選擇工具
        ApplyingRoyalJelly, // 塗抹蜂王乳
        CollectingLarvae,   // 收集幼蟲
        GameOver            // 遊戲結束
    }

    public enum Tool
    {
        None,
        GooseFeather,
        Tweezers
    }

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        // 預設隱藏 ingamePanel
        if (ingamePanel != null)
        {
            ingamePanel.SetActive(false);
        }

        // 檢查是否有 TutorialManager，如果有就啟動教學
        if (TutorialManager.Instance != null)
        {
            TutorialManager.Instance.StartTutorial();
        }
        else
        {
            // 沒有教學，直接開始遊戲
            SetTutorialMode(false);
            InitializeGame();
        }
    }

    void Update()
    {
        // 在教學模式下不更新計時器
        if (gameActive && !isTutorialMode)
        {
            UpdateTimer();
        }
    }

    private void InitializeGame()
    {
        SetupExistingObjects();

        currentTime = gameTimeLimit;
        collectedLarvae = 0;
        gameActive = true;
        currentState = GameState.SelectingTool;

        // 確保遊戲 UI 顯示（只有在非教學模式下）
        if (!isTutorialMode && ingamePanel != null)
        {
            ingamePanel.SetActive(true);
        }

        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(false);
        }

        UpdateUI();

        Debug.Log("遊戲初始化完成");
    }

    private void SetupExistingObjects()
    {
        // 初始化巢脾和幼蟲
        if (honeycombGrid != null)
        {
            honeycombGrid.Initialize(totalLarvae);
            activeLarvae = new List<Larva>(honeycombGrid.GetLarvae());
            Debug.Log($"找到 {activeLarvae.Count} 個幼蟲");
        }
        else
        {
            Debug.LogError("請將場景中的 HoneycombGrid 拖入 Inspector！");
        }

        // 設置杯子
        cupsList.Clear();
        if (specialCups != null && specialCups.Length > 0)
        {
            for (int i = 0; i < specialCups.Length; i++)
            {
                if (specialCups[i] != null)
                {
                    specialCups[i].Initialize(i);
                    cupsList.Add(specialCups[i]);
                }
            }
            Debug.Log($"找到 {cupsList.Count} 個杯子");
        }
        else
        {
            Debug.LogError("請將場景中的 SpecialCup 物件拖入 Inspector 的陣列中！");
        }

        // 根據當前模式初始化對應的工具
        if (isTutorialMode)
        {
            // 初始化教學工具
            if (tutorialGooseFeather != null)
            {
                tutorialGooseFeather.Initialize();
                Debug.Log("教學鵝毛初始化完成");
            }

            if (tutorialTweezers != null)
            {
                tutorialTweezers.Initialize();
                Debug.Log("教學鑷子初始化完成");
            }
        }
        else
        {
            // 初始化遊戲工具
            if (gooseFeather != null)
            {
                gooseFeather.Initialize();
                Debug.Log("遊戲鵝毛初始化完成");
            }

            if (tweezers != null)
            {
                tweezers.Initialize();
                Debug.Log("遊戲鑷子初始化完成");
            }
        }
    }

    // 獲取當前應該使用的工具引用
    private GooseFeather GetCurrentGooseFeather()
    {
        return isTutorialMode ? tutorialGooseFeather : gooseFeather;
    }

    private Tweezers GetCurrentTweezers()
    {
        return isTutorialMode ? tutorialTweezers : tweezers;
    }

    public Larva GetTutorialLarva()
    {
        return tutorialLarva;
    }

    public void SetTutorialMode(bool tutorialMode)
    {
        isTutorialMode = tutorialMode;

        if (tutorialMode)
        {
            // 教學模式：完全隱藏 ingamePanel
            if (ingamePanel != null)
            {
                ingamePanel.SetActive(false);
                Debug.Log("教學模式：隱藏 ingamePanel");
            }
        }
        else
        {
            // 正常遊戲模式：顯示 ingamePanel 和所有 UI
            if (ingamePanel != null)
            {
                ingamePanel.SetActive(true);
                Debug.Log("遊戲模式：顯示 ingamePanel");
            }

            if (timerText != null) timerText.gameObject.SetActive(true);
            if (scoreText != null) scoreText.gameObject.SetActive(true);

            Debug.Log("離開教學模式 - UI 已恢復");
        }
    }

    public Tool GetSelectedTool()
    {
        return selectedTool;
    }

    public void SelectTool(Tool tool)
    {
        // 如果選擇的是相同工具，直接返回
        if (selectedTool == tool) return;

        // 切換前先處理當前工具
        if (selectedTool != Tool.None)
        {
            HandleToolSwitch(selectedTool);
        }

        // 設置新工具
        selectedTool = tool;

        if (CursorManager.Instance != null)
        {
            CursorManager.Instance.SetDefaultCursor();
        }

        Debug.Log($"切換到工具: {tool} (教學模式: {isTutorialMode})");
    }

    private void HandleToolSwitch(Tool currentTool)
    {
        Debug.Log($"HandleToolSwitch 被調用 - 當前工具: {currentTool}, 教學模式: {isTutorialMode}");

        switch (currentTool)
        {
            case Tool.GooseFeather:
                var currentGooseFeather = GetCurrentGooseFeather();
                if (currentGooseFeather != null)
                {
                    try
                    {
                        if (currentGooseFeather.HasHoney)
                        {
                            currentGooseFeather.ReturnToOriginWithHoney();
                            Debug.Log("鵝毛有蜂蜜，保留狀態回到原位");
                        }
                        else
                        {
                            currentGooseFeather.ReturnToOrigin();
                            Debug.Log("鵝毛回到原位");
                        }
                    }
                    catch (System.Exception e)
                    {
                        Debug.LogError($"GooseFeather HandleToolSwitch 錯誤: {e.Message}");
                    }
                }
                else
                {
                    Debug.LogError($"當前鵝毛引用為 null！教學模式: {isTutorialMode}");
                }
                break;

            case Tool.Tweezers:
                var currentTweezers = GetCurrentTweezers();
                if (currentTweezers != null)
                {
                    try
                    {
                        currentTweezers.ReturnToOrigin();
                        Debug.Log("鑷子回到原位");
                    }
                    catch (System.Exception e)
                    {
                        Debug.LogError($"Tweezers HandleToolSwitch 錯誤: {e.Message}");
                    }
                }
                else
                {
                    Debug.LogError($"當前鑷子引用為 null！教學模式: {isTutorialMode}");
                }
                break;
        }
    }

    public void OnLarvaCollected()
    {
        collectedLarvae++;
        UpdateUI();

        if (collectedLarvae >= totalLarvae)
        {
            EndGame(true);
        }
    }

    private void CollectLarva(Larva larva, SpecialCup cup)
    {
        larva.CollectToCup(cup);
        cup.AddLarva();
        collectedLarvae++;

        if (tweezers != null)
            tweezers.UseTweezers();

        UpdateUI();

        // 檢查是否收集完所有幼蟲
        if (collectedLarvae >= totalLarvae)
        {
            EndGame(true);
        }
    }

    private bool AllCupsHaveRoyalJelly()
    {
        foreach (var cup in cupsList)
        {
            if (cup != null && !cup.HasRoyalJelly) return false;
        }
        return true;
    }

    private SpecialCup FindNearestCupWithRoyalJelly(Vector3 position)
    {
        SpecialCup nearest = null;
        float minDistance = float.MaxValue;

        foreach (var cup in cupsList)
        {
            if (cup != null && cup.HasRoyalJelly && !cup.HasLarva)
            {
                float distance = Vector3.Distance(position, cup.transform.position);
                if (distance < minDistance)
                {
                    minDistance = distance;
                    nearest = cup;
                }
            }
        }

        return nearest;
    }

    private void UpdateTimer()
    {
        currentTime -= Time.deltaTime;

        if (currentTime <= 0)
        {
            currentTime = 0;
            EndGame(false);
        }

        UpdateUI();
    }

    private void UpdateUI()
    {
        if (timerText != null)
        {
            timerText.text = $"時間: {currentTime:F1}s";
        }

        if (scoreText != null)
        {
            scoreText.text = $"収集: {collectedLarvae} / {totalLarvae}"; // 日文：收集
        }
    }

    private void EndGame(bool completed)
    {
        gameActive = false;
        currentState = GameState.GameOver;

        // 隱藏遊戲 UI
        if (ingamePanel != null)
        {
            ingamePanel.SetActive(false);
        }

        // 恢復預設 cursor
        if (CursorManager.Instance != null)
        {
            CursorManager.Instance.SetDefaultCursor();
        }

        // 顯示遊戲結束面板
        ShowGameOverPanel(completed);

        // UpdateInstructions(); // 刪除這行
    }

    private void ShowGameOverPanel(bool completed)
    {
        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(true);
        }

        // 計算時間
        int minutes = Mathf.FloorToInt(currentTime / 60f);
        int seconds = Mathf.FloorToInt(currentTime % 60f);

        if (completed)
        {
            // 成功完成所有幼蟲收集
            if (gameOverTitleText != null)
            {
                gameOverTitleText.text = "おめでとうございます！"; // 恭喜！
                gameOverTitleText.color = Color.green;
            }

            if (gameOverMessageText != null)
            {
                gameOverMessageText.text = "すべての卵の収集が完了しました！"; // 完成了所有幼蟲的收集！
            }

            if (remainingTimeText != null)
            {
                remainingTimeText.text = $"残り時間: {minutes:00}:{seconds:00}"; // 剩餘時間
                remainingTimeText.color = Color.green;
            }

            Debug.Log($"遊戲完成！剩餘時間: {minutes:00}:{seconds:00}");
        }
        else
        {
            // 時間到，沒有完成所有收集
            int remaining = totalLarvae - collectedLarvae;

            if (gameOverTitleText != null)
            {
                gameOverTitleText.text = "時間切れ！"; // 時間到！
                gameOverTitleText.color = Color.red;
            }

            if (gameOverMessageText != null)
            {
                gameOverMessageText.text = $"あと{remaining}匹の幼虫が残っています\n頑張りましょう！"; // 還剩下X隻幼蟲，加油！
            }

            if (remainingTimeText != null)
            {
                remainingTimeText.text = $"収集済み: {collectedLarvae} / {totalLarvae}"; // 已收集
                remainingTimeText.color = Color.yellow;
            }

            Debug.Log($"時間到！最終收集: {collectedLarvae}/{totalLarvae}，還剩 {remaining} 隻");
        }
    }

    public bool CanSelectTool()
    {
        return selectedTool == Tool.None;
    }

    public void SetSelectedTool(Tool tool)
    {
        selectedTool = tool;
    }

    public void StartGameAfterTutorial()
    {
        Debug.Log("教學結束，開始正式遊戲");

        // 離開教學模式，切換到遊戲工具
        SetTutorialMode(false);

        // 重置工具狀態
        selectedTool = Tool.None;

        // 初始化遊戲
        InitializeGame();
    }

    public bool HasLarvaInCup()
    {
        if (isTutorialMode)
        {
            // 教學模式：檢查教學杯子是否有幼蟲
            var tutorialManager = TutorialManager.Instance;
            if (tutorialManager != null && tutorialManager.tutorialSpecialCups != null)
            {
                foreach (var cup in tutorialManager.tutorialSpecialCups)
                {
                    if (cup != null && cup.HasLarva)
                    {
                        return true;
                    }
                }
            }
        }
        else
        {
            // 正常遊戲模式：檢查遊戲杯子
            if (specialCups != null)
            {
                foreach (var cup in specialCups)
                {
                    if (cup != null && cup.HasLarva)
                    {
                        return true;
                    }
                }
            }
        }
        return false;
    }

    public void OnLarvaMoved()
    {
        if (isTutorialMode)
        {
            collectedLarvae++;

            // 通知教學系統
            if (TutorialManager.Instance != null)
            {
                TutorialManager.Instance.NotifyLarvaMoved();
            }

            Debug.Log($"教學模式 - 幼蟲移動完成，總數: {collectedLarvae}");
        }
        else
        {
            // 正常遊戲模式
            OnLarvaCollected();
        }
    }
}