using UnityEngine;
using TMPro;
using System.Collections; 

public class ShooterGameManager : MonoBehaviour
{
    // 單例模式：方便其他腳本存取 GameManager 的實例
    public static ShooterGameManager instance;

    // --- UI 相關引用 ---
    public TextMeshProUGUI scoreText; // 拖曳您的分數 TextMeshProUGUI 物件到這裡
    public TextMeshProUGUI timerText; // 拖曳您的時間 TextMeshProUGUI 物件到這裡
    public GameObject hornetPrefab; // 拖曳您的虎頭蜂預製體到這裡
    public GameObject beePrefab;    // 拖曳您的西洋蜜蜂預製體到這裡

    // --- 遊戲參數 ---
    [Header("遊戲設定")]
    public float gameDuration = 60f; // 遊戲總時間 (秒)
    public float spawnInterval = 0.75f; // 每隔多久嘗試生成一次物件 (秒)
    public int maxBeesOnScreen = 15; // 螢幕上最多同時存在的西洋蜜蜂數量
    public int maxHornetsOnScreen = 10; // 螢幕上最多同時存在的虎頭蜂數量
    public float beeSpeed = 1f; // 西洋蜜蜂的移動速度倍數
    public float hornetSpeed = 1.5f; // 虎頭蜂的移動速度倍數 (會比蜜蜂快)

    [Header("分數設定")]
    public int scorePerHornet = 100; // 清除虎頭蜂得分
    public int penaltyPerBee = -50; // 誤傷西洋蜜蜂扣分

    // --- 私有變數 ---
    private float currentTimer;
    private int currentScore;
    private bool isGameRunning = false;

    // Awake 在 Start 之前被呼叫，用於初始化單例
    void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else
        {
            // 如果場景中已經有 GameManager 實例，銷毀這個新的
            Destroy(gameObject);
        }
    }

    // Start 在第一個幀更新前被呼叫
    void Start()
    {
        StartGame(); // 遊戲開始時啟動遊戲
    }

    // 啟動遊戲
    public void StartGame()
    {
        currentTimer = gameDuration;
        currentScore = 0;
        isGameRunning = true;

        UpdateScoreUI();
        UpdateTimerUI();

        // 啟動協程來處理時間倒數和物件生成
        StartCoroutine(GameCountdownRoutine());
        StartCoroutine(SpawnRoutine());
    }

    // 遊戲倒數協程
    IEnumerator GameCountdownRoutine()
    {
        while (currentTimer > 0 && isGameRunning)
        {
            yield return new WaitForSeconds(1f); // 每秒更新一次
            currentTimer--;
            UpdateTimerUI();
        }

        if (isGameRunning) // 確保不是手動停止遊戲
        {
            EndGame();
        }
    }

    // 物件生成協程
    IEnumerator SpawnRoutine()
    {
        // 獲取主相機的世界座標邊界
        Vector2 screenBounds = Camera.main.ScreenToWorldPoint(new Vector3(Screen.width, Screen.height, Camera.main.transform.position.z));
        
        while (isGameRunning)
        {
            yield return new WaitForSeconds(spawnInterval);

            // 嘗試生成虎頭蜂
            if (GameObject.FindGameObjectsWithTag("Hornet").Length < maxHornetsOnScreen)
            {
                if (Random.Range(0f, 100f) < 60f) // 60% 機率生成
                {
                    SpawnObject(hornetPrefab, "Hornet", hornetSpeed, screenBounds);
                }
            }

            // 嘗試生成西洋蜜蜂
            if (GameObject.FindGameObjectsWithTag("Bee").Length < maxBeesOnScreen)
            {
                if (Random.Range(0f, 100f) < 80f) // 80% 機率生成
                {
                    SpawnObject(beePrefab, "Bee", beeSpeed, screenBounds);
                }
            }
        }
    }

    // 生成物件的輔助函數
    void SpawnObject(GameObject prefab, string tag, float speedMultiplier, Vector2 screenBounds)
    {
        Vector3 spawnPos = Vector3.zero;
        Vector2 targetPos = Vector2.zero; // 物件的目標移動點，使其向中心移動

        // 隨機從螢幕四個邊緣生成
        int side = Random.Range(0, 4); // 0:上, 1:下, 2:左, 3:右
        float buffer = 0.5f; // 讓物件稍微從螢幕外生成

        switch (side)
        {
            case 0: // 上邊
                spawnPos = new Vector3(Random.Range(-screenBounds.x, screenBounds.x), screenBounds.y + buffer, 0);
                targetPos = new Vector2(Random.Range(-screenBounds.x, screenBounds.x), -screenBounds.y - buffer);
                break;
            case 1: // 下邊
                spawnPos = new Vector3(Random.Range(-screenBounds.x, screenBounds.x), -screenBounds.y - buffer, 0);
                targetPos = new Vector2(Random.Range(-screenBounds.x, screenBounds.x), screenBounds.y + buffer);
                break;
            case 2: // 左邊
                spawnPos = new Vector3(-screenBounds.x - buffer, Random.Range(-screenBounds.y, screenBounds.y), 0);
                targetPos = new Vector2(screenBounds.x + buffer, Random.Range(-screenBounds.y, screenBounds.y));
                break;
            case 3: // 右邊
                spawnPos = new Vector3(screenBounds.x + buffer, Random.Range(-screenBounds.y, screenBounds.y), 0);
                targetPos = new Vector2(-screenBounds.x - buffer, Random.Range(-screenBounds.y, screenBounds.y));
                break;
        }

        GameObject obj = Instantiate(prefab, spawnPos, Quaternion.identity);
        obj.tag = tag; // 設定 Tag (非常重要，用於點擊檢測)

        // 設定移動方向和速度
        EntityMovement movement = obj.GetComponent<EntityMovement>();
        if (movement == null)
        {
            movement = obj.AddComponent<EntityMovement>(); // 如果沒有 EntityMovement，則添加一個
        }
        movement.SetTargetAndSpeed(targetPos, speedMultiplier);
    }

    // 更新分數 UI
    public void AddScore(int amount)
    {
        currentScore += amount;
        UpdateScoreUI();
    }

    void UpdateScoreUI()
    {
        if (scoreText != null)
        {
            scoreText.text = "Score: " + currentScore;
        }
    }

    // 更新時間 UI
    void UpdateTimerUI()
    {
        if (timerText != null)
        {
            timerText.text = "Timer: " + Mathf.RoundToInt(currentTimer) + "s";
        }
    }

    // 遊戲結束
    public void EndGame()
    {
        isGameRunning = false;
        StopAllCoroutines(); // 停止所有正在運行的協程

        // 銷毀所有場景中的蜜蜂和虎頭蜂
        GameObject[] hornets = GameObject.FindGameObjectsWithTag("Hornet");
        foreach (GameObject hornet in hornets) Destroy(hornet);

        GameObject[] bees = GameObject.FindGameObjectsWithTag("Bee");
        foreach (GameObject bee in bees) Destroy(bee);

        Debug.Log("遊戲結束！最終分數: " + currentScore);
        // 在這裡可以加載遊戲結束畫面，顯示最終分數等
        // 例如：
        // SceneManager.LoadScene("GameOverScene");
    }
}