using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Game_5
{
    public class TutorialManager : MonoBehaviour
    {
        public static TutorialManager Instance { get; private set; }

        [Header("Tutorial UI")]
        public GameObject tutorialPanel;
        public TextMeshProUGUI tutorialTitle;
        public TextMeshProUGUI tutorialContent;
        public TextMeshProUGUI startInstruction;

        [Header("Visual Settings")]
        public Color titleColor = Color.yellow;
        public Color contentColor = Color.white;
        public Color instructionColor = Color.green;

        private bool isTutorialActive = false;
        private ShooterGameManager gameManager;
        private Color originalInstructionColor; // 儲存原始顏色

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
            gameManager = FindObjectOfType<ShooterGameManager>();
            
            // 隱藏教學面板
            if (tutorialPanel != null)
            {
                tutorialPanel.SetActive(false);
            }

            // 自動開始教學
            Invoke(nameof(StartTutorial), 0.5f);
        }

        void Update()
        {
            // 檢測空白鍵
            if (isTutorialActive && Input.GetKeyDown(KeyCode.Space))
            {
                StartGame();
            }
        }

        public void StartTutorial()
        {
            if (tutorialPanel == null)
            {
                Debug.LogError("Tutorial Panel 未設置！");
                return;
            }

            isTutorialActive = true;

            // 暫停遊戲
            if (gameManager != null)
            {
                gameManager.enabled = false; // 暫停遊戲邏輯
            }

            // 設置教學內容
            SetupTutorialContent();

            tutorialPanel.SetActive(true);

            Debug.Log("射擊遊戲教學開始");
        }

        void SetupTutorialContent()
        {
            // 設置標題
            if (tutorialTitle != null)
            {
                tutorialTitle.color = titleColor;
            }

            // 設置內容
            if (tutorialContent != null)
            {   
                tutorialContent.color = contentColor;
            }

            // 設置開始指示
            if (startInstruction != null)
            {
                startInstruction.text = "スペースキーを押してゲーム開始！"; // 改回空白鍵
                startInstruction.color = instructionColor;
                
                // 儲存原始顏色
                originalInstructionColor = instructionColor;
                
                // 確保文字對齊和溢出設置正確
                startInstruction.alignment = TextAlignmentOptions.Center;
                startInstruction.overflowMode = TextOverflowModes.Overflow;
                startInstruction.textWrappingMode = TextWrappingModes.NoWrap;
                
                // 添加閃爍效果
                StartCoroutine(BlinkText());
            }
        }

        System.Collections.IEnumerator BlinkText()
        {
            while (isTutorialActive && startInstruction != null)
            {
                // 使用固定的 alpha 值，避免重新計算佈局
                float fullAlpha = 1f;
                float dimAlpha = 0.4f;
                
                // 淡出
                for (float t = 0; t < 0.8f; t += Time.deltaTime)
                {
                    if (!isTutorialActive || startInstruction == null) yield break;
                    
                    float alpha = Mathf.Lerp(fullAlpha, dimAlpha, t / 0.8f);
                    startInstruction.color = new Color(
                        originalInstructionColor.r, 
                        originalInstructionColor.g, 
                        originalInstructionColor.b, 
                        alpha
                    );
                    yield return null;
                }
                
                // 淡入
                for (float t = 0; t < 0.8f; t += Time.deltaTime)
                {
                    if (!isTutorialActive || startInstruction == null) yield break;
                    
                    float alpha = Mathf.Lerp(dimAlpha, fullAlpha, t / 0.8f);
                    startInstruction.color = new Color(
                        originalInstructionColor.r, 
                        originalInstructionColor.g, 
                        originalInstructionColor.b, 
                        alpha
                    );
                    yield return null;
                }
            }
        }

        public void StartGame()
        {
            if (!isTutorialActive) return;

            Debug.Log("教學結束，準備開始遊戲");
            isTutorialActive = false;

            // 停止閃爍協程
            StopAllCoroutines();
            
            // 恢復原始顏色
            if (startInstruction != null)
            {
                startInstruction.color = originalInstructionColor;
            }

            if (tutorialPanel != null)
            {
                tutorialPanel.SetActive(false);
            }

            // 啟用遊戲管理器並明確開始遊戲
            if (gameManager != null)
            {
                gameManager.enabled = true;
                gameManager.StartGame(); // 明確呼叫開始遊戲
                Debug.Log("射擊遊戲教學結束，遊戲開始");
            }

            Debug.Log("射擊遊戲教學系統結束");
        }

        // 公開屬性
        public bool IsTutorialActive => isTutorialActive;

        // 用於遊戲管理器檢查是否在教學中
        public bool ShouldBlockGameplay()
        {
            return isTutorialActive;
        }
    }
}