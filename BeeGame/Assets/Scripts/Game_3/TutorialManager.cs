using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Game_3
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
        private ShakeAndBrushGameManager gameManager;

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
            gameManager = ShakeAndBrushGameManager.Instance;
            
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

            Debug.Log("教學開始");
        }

        void SetupTutorialContent()
        {
            // 設置標題
            if (tutorialTitle != null)
            {
                tutorialTitle.text = "蜂の巣掃除ゲーム";
                tutorialTitle.color = titleColor;
            }

            // 設置內容
            if (tutorialContent != null)
            {
                tutorialContent.text = 
                    "遊び方:\n\n" +
                    "1. 蜂の巣を<color=#FFD700>上下にドラッグ</color>して振り、" +
                    "蜂を落としてください\n\n" +
                    "2. 進捗バーが100%になったら、" +
                    "<color=#87CEEB>ブラシをドラッグ</color>して\n" +
                    "残った蜂をすべて取り除いてください\n\n" +
                    "制限時間内にできるだけ多くの\n" +
                    "蜂の巣を処理しましょう！";
                
                tutorialContent.color = contentColor;
            }

            // 設置開始指示
            if (startInstruction != null)
            {
                startInstruction.text = "スペースキーを押してゲーム開始！";
                startInstruction.color = instructionColor;
                
                // 添加閃爍效果
                StartCoroutine(BlinkText());
            }
        }

        System.Collections.IEnumerator BlinkText()
        {
            while (isTutorialActive)
            {
                if (startInstruction != null)
                {
                    startInstruction.color = new Color(instructionColor.r, instructionColor.g, instructionColor.b, 1f);
                    yield return new WaitForSeconds(0.8f);
                    
                    if (isTutorialActive) // 再次檢查避免錯誤
                    {
                        startInstruction.color = new Color(instructionColor.r, instructionColor.g, instructionColor.b, 0.3f);
                        yield return new WaitForSeconds(0.8f);
                    }
                }
                else
                {
                    break;
                }
            }
        }

        public void StartGame()
        {
            if (!isTutorialActive) return;

            isTutorialActive = false;

            if (tutorialPanel != null)
            {
                tutorialPanel.SetActive(false);
            }

            // 恢復遊戲
            if (gameManager != null)
            {
                gameManager.enabled = true;
                Debug.Log("教學結束，遊戲開始");
            }

            Debug.Log("教學系統結束");
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