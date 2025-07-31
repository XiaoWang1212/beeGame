using UnityEngine;
using System.Collections.Generic;
using TMPro;
using UnityEngine.UI;

public class QueenRearingGameManager : MonoBehaviour
{
    public static QueenRearingGameManager Instance { get; private set; }

    [Header("現有遊戲物件 - 直接拖入場景中的物件")]
    public HoneycombGrid honeycombGrid;     // 巢脾（場景中現有的）
    public SpecialCup[] specialCups;       // 所有杯子（場景中現有的）
    public GooseFeather gooseFeather;      // 鵝毛（場景中現有的）
    public Tweezers tweezers;              // 鑷子（場景中現有的）

    [Header("遊戲設置")]
    public int totalLarvae = 20;           // 總幼蟲數量
    public float gameTimeLimit = 60f;      // 遊戲時間限制（秒）

    [Header("UI 組件")]
    public TextMeshProUGUI timerText;
    public TextMeshProUGUI scoreText;
    public TextMeshProUGUI instructionText;
    public Button restartButton;
    public GameObject gameOverPanel;

    // 遊戲狀態
    private List<Larva> activeLarvae = new List<Larva>();
    private List<SpecialCup> cupsList = new List<SpecialCup>();

    private float currentTime;
    private int collectedLarvae = 0;
    private bool gameActive = false;
    private GameState currentState = GameState.SelectingTool;
    public Tool selectedTool = Tool.None;

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
        InitializeGame();
    }

    void Update()
    {
        if (gameActive)
        {
            UpdateTimer();
            // HandleInput();
        }
    }

    private void InitializeGame()
    {
        SetupExistingObjects();

        currentTime = gameTimeLimit;
        collectedLarvae = 0;
        gameActive = true;
        currentState = GameState.SelectingTool;

        UpdateUI();
        UpdateInstructions();
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

        // 設置工具
        if (gooseFeather != null)
        {
            gooseFeather.Initialize();
            Debug.Log("鵝毛初始化完成");
        }
        else
        {
            Debug.LogError("請將場景中的 GooseFeather 物件拖入 Inspector！");
        }

        if (tweezers != null)
        {
            tweezers.Initialize();
            Debug.Log("鑷子初始化完成");
        }
        else
        {
            Debug.LogError("請將場景中的 Tweezers 物件拖入 Inspector！");
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
        Debug.Log($"切換到工具: {tool}");
    }

    private void HandleToolSwitch(Tool currentTool)
    {
        Debug.Log($"HandleToolSwitch 被調用 - 當前工具: {currentTool}");

        switch (currentTool)
        {
            case Tool.GooseFeather:
                Debug.Log("準備處理羽毛切換");
                if (gooseFeather != null)
                {
                    if (gooseFeather.HasHoney)
                    {
                        gooseFeather.ReturnToOriginWithHoney();
                        Debug.Log("鵝毛有蜂蜜，保留狀態回到原位");
                    }
                    else
                    {
                        gooseFeather.ReturnToOrigin();
                        Debug.Log("鵝毛回到原位");
                    }
                }
                else
                {
                    Debug.LogError("gooseFeather 引用為 null！");
                }
                break;

            case Tool.Tweezers:
                Debug.Log("準備處理鑷子切換");
                if (tweezers != null)
                {
                    tweezers.ReturnToOrigin();
                    Debug.Log("鑷子回到原位");
                }
                else
                {
                    Debug.LogError("tweezers 引用為 null！");
                }
                break;
        }

        Debug.Log($"HandleToolSwitch 完成 - 當前工具: {currentTool}");
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
            if (cup != null && cup.HasRoyalJelly && !cup.HasLarvae)
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
            scoreText.text = $"收集: {collectedLarvae} / {totalLarvae}";
        }
    }

    private void UpdateInstructions()
    {
        if (instructionText == null) return;

        switch (currentState)
        {
            case GameState.SelectingTool:
                if (AllCupsHaveRoyalJelly())
                {
                    instructionText.text = "選擇鑷子來收集幼蟲";
                }
                else
                {
                    instructionText.text = "選擇鵝毛來塗抹蜂王乳";
                }
                break;

            case GameState.ApplyingRoyalJelly:
                instructionText.text = "點擊杯子來塗抹蜂王乳";
                break;

            case GameState.CollectingLarvae:
                instructionText.text = "用鑷子夾取幼蟲到杯子裡";
                break;

            case GameState.GameOver:
                instructionText.text = "遊戲結束！";
                break;
        }
    }

    private void EndGame(bool completed)
    {
        gameActive = false;
        currentState = GameState.GameOver;

        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(true);
        }

        string resultText = completed ? "恭喜完成所有幼蟲收集！" : "時間到！";
        Debug.Log($"{resultText} 最終收集: {collectedLarvae}/{totalLarvae}");

        UpdateInstructions();
    }

    public void RestartGame()
    {
        // 重置遊戲狀態
        collectedLarvae = 0;
        currentTime = gameTimeLimit;
        gameActive = true;
        currentState = GameState.SelectingTool;
        selectedTool = Tool.None;

        // 重置所有杯子
        foreach (var cup in cupsList)
        {
            if (cup != null)
            {
                cup.ResetCup(); // 需要在 SpecialCup 中添加這個方法
            }
        }

        // 重置所有幼蟲
        foreach (var larva in activeLarvae)
        {
            if (larva != null)
            {
                larva.ResetLarva(); // 需要在 Larva 中添加這個方法
            }
        }

        // 重置工具
        if (gooseFeather != null)
        {
            gooseFeather.ResetFeather(); // 需要在 GooseFeather 中添加這個方法
        }

        if (tweezers != null)
        {
            tweezers.ResetTweezers(); // 需要在 Tweezers 中添加這個方法
        }

        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(false);
        }

        UpdateUI();
        UpdateInstructions();

        Debug.Log("遊戲已重置");
    }

    public bool CanSelectTool()
    {
        return selectedTool == Tool.None;
    }

    public void SetSelectedTool(Tool tool)
    {
        selectedTool = tool;
    }
}