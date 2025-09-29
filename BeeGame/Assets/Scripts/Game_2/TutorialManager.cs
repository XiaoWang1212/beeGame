using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

namespace Game_2
{
    public class TutorialManager : MonoBehaviour
    {
        public static TutorialManager Instance { get; private set; }

        [Header("UI References")]
        public GameObject tutorialPanel;
        public TextMeshProUGUI titleText;
        public TextMeshProUGUI contentText;
        public Button nextButton;
        public Button skipButton;
        // 移除箭頭相關 - public GameObject arrow;

        [Header("Button Visual Settings")]
        public Color disabledButtonColor = Color.gray;
        public Color enabledButtonColor = Color.green;

        [Header("Tutorial Steps")]
        public TutorialStep[] tutorialSteps;

        [Header("Game References")]
        public QueenRearingGameManager gameManager;

        [Header("教學專用工具引用 (直接拖入教學工具)")]
        public GooseFeather tutorialGooseFeather;      // 教學專用鵝毛
        public Tweezers tutorialTweezers;              // 教學專用鑷子
        public HoneyJar tutorialHoneyJar;              // 教學專用蜂蜜罐
        public SpecialCup[] tutorialSpecialCups;       // 教學專用杯子陣列
        public Larva tutorialLarva;                    // 教學專用幼蟲

        private int currentStepIndex = 0;
        private bool isTutorialActive = false;
        private bool stepCompleted = false;

        // 追蹤教學進度的變數
        private bool featherInteracted = false;
        private bool tweezersInteracted = false;
        private bool honeyDipped = false;
        private bool jellyApplied = false;
        private int larvaeMovedInTutorial = 0;

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
            if (tutorialPanel != null)
            {
                tutorialPanel.SetActive(false);
            }

            // 設置按鈕事件
            if (nextButton != null)
            {
                nextButton.onClick.RemoveAllListeners();
                nextButton.onClick.AddListener(OnNextButtonClick);
                Debug.Log("Next 按鈕事件已設置");
            }

            if (skipButton != null)
            {
                skipButton.onClick.RemoveAllListeners();
                skipButton.onClick.AddListener(SkipTutorial);
                Debug.Log("Skip 按鈕事件已設置");
            }

            SetupTutorialSteps();
        }

        private void SetupTutorialSteps()
        {
            // 確保有4個步驟
            if (tutorialSteps.Length != 4)
            {
                System.Array.Resize(ref tutorialSteps, 4);
            }

            // 初始化步驟
            for (int i = 0; i < tutorialSteps.Length; i++)
            {
                if (tutorialSteps[i] == null)
                {
                    tutorialSteps[i] = new TutorialStep();
                }
            }

            // 設置步驟內容
            tutorialSteps[0].title = "道具の使い方を学ぼう";
            tutorialSteps[0].content = "まず、ガチョウの羽とピンセットの両方をクリックして、\n使い方を覚えましょう！";
            tutorialSteps[0].stepType = TutorialStepType.InteractWithTools;
            tutorialSteps[0].requiresCompletion = true;

            tutorialSteps[1].title = "ローヤルゼリーを作ろう";
            tutorialSteps[1].content = "ガチョウの羽でハチミツを取り、\nカップに塗ってローヤルゼリーを作ってください。";
            tutorialSteps[1].stepType = TutorialStepType.CreateRoyalJelly;
            tutorialSteps[1].requiresCompletion = true;

            tutorialSteps[2].title = "卵を移動させよう"; 
            tutorialSteps[2].content = "ピンセットで卵を掴み、\nローヤルゼリーの入ったカップに入れてください。";  // 改為「卵」
            tutorialSteps[2].stepType = TutorialStepType.MoveLarva;
            tutorialSteps[2].requiresCompletion = true;

            tutorialSteps[3].title = "チュートリアル完了！";
            tutorialSteps[3].content = "素晴らしい！基本操作をマスターしました！\n\n本番では5個の卵をすべて女王蜂カップに移動させてください。\n時間内にすべての卵を育てて、最高の女王蜂を作りましょう！\n\n頑張って！"; 
            tutorialSteps[3].stepType = TutorialStepType.Information;
            tutorialSteps[3].requiresCompletion = false;
        }

        public void StartTutorial()
        {
            Debug.Log("教學開始");
            isTutorialActive = true;
            currentStepIndex = 0;
            stepCompleted = false;

            // 重置教學進度追蹤
            ResetTutorialProgress();

            // 設置教學模式
            if (gameManager != null)
            {
                gameManager.SetTutorialMode(true);
            }

            if (tutorialPanel != null)
            {
                tutorialPanel.SetActive(true);
            }

            ShowCurrentStep();
        }

        private void ResetTutorialProgress()
        {
            featherInteracted = false;
            tweezersInteracted = false;
            honeyDipped = false;
            jellyApplied = false;
            larvaeMovedInTutorial = 0;
        }

        private void ShowCurrentStep()
        {
            if (currentStepIndex >= tutorialSteps.Length)
            {
                EndTutorial();
                return;
            }

            var currentStep = tutorialSteps[currentStepIndex];
            stepCompleted = false;

            Debug.Log($"顯示步驟 {currentStepIndex + 1}: {currentStep.title}");

            // 根據步驟顯示/隱藏對應的工具
            ShowToolsForCurrentStep();

            // 設置文字
            if (titleText != null)
            {
                titleText.text = currentStep.title;
            }

            if (contentText != null)
            {
                contentText.text = currentStep.content;
            }

            // 設置按鈕狀態
            SetupButtons();

            // 執行步驟特殊邏輯
            ExecuteStepLogic(currentStep.stepType);
        }

        private void ShowToolsForCurrentStep()
        {
            // 預設隱藏所有教學工具
            if (tutorialGooseFeather != null) tutorialGooseFeather.gameObject.SetActive(false);
            if (tutorialTweezers != null) tutorialTweezers.gameObject.SetActive(false);
            if (tutorialHoneyJar != null) tutorialHoneyJar.gameObject.SetActive(false);

            if (tutorialSpecialCups != null)
            {
                foreach (var cup in tutorialSpecialCups)
                {
                    if (cup != null) cup.gameObject.SetActive(false);
                }
            }

            // 隱藏教學幼蟲
            if (tutorialLarva != null) tutorialLarva.gameObject.SetActive(false);

            // 根據當前步驟顯示需要的工具
            switch (currentStepIndex)
            {
                case 0: // 步驟1：學習使用工具
                    if (tutorialGooseFeather != null)
                    {
                        tutorialGooseFeather.gameObject.SetActive(true);
                        tutorialGooseFeather.Initialize();
                    }
                    if (tutorialTweezers != null)
                    {
                        tutorialTweezers.gameObject.SetActive(true);
                        tutorialTweezers.Initialize();
                    }
                    Debug.Log("步驟1：顯示鵝毛和鑷子");
                    break;

                case 1: // 步驟2：製作蜂王乳
                    if (tutorialGooseFeather != null)
                    {
                        tutorialGooseFeather.gameObject.SetActive(true);
                        tutorialGooseFeather.Initialize();
                    }
                    if (tutorialHoneyJar != null)
                    {
                        tutorialHoneyJar.gameObject.SetActive(true);
                    }
                    if (tutorialSpecialCups != null && tutorialSpecialCups.Length > 0)
                    {
                        // 只顯示第一個杯子
                        tutorialSpecialCups[0].gameObject.SetActive(true);
                        tutorialSpecialCups[0].Initialize(0);
                    }
                    Debug.Log("步驟2：顯示鵝毛、蜂蜜罐和杯子");
                    break;

                case 2: // 步驟3：移動幼蟲
                    if (tutorialTweezers != null)
                    {
                        tutorialTweezers.gameObject.SetActive(true);
                        tutorialTweezers.Initialize();
                    }
                    if (tutorialSpecialCups != null)
                    {
                        foreach (var cup in tutorialSpecialCups)
                        {
                            if (cup != null)
                            {
                                cup.gameObject.SetActive(true);
                                cup.Initialize(0);
                            }
                        }
                    }
                    // 顯示教學幼蟲（只有一隻）並初始化
                    if (tutorialLarva != null)
                    {
                        tutorialLarva.gameObject.SetActive(true);
                        tutorialLarva.Initialize(0);
                        Debug.Log("步驟3：顯示並初始化教學幼蟲");
                    }
                    Debug.Log("步驟3：顯示鑷子、杯子和幼蟲");
                    break;

                case 3: // 步驟4：教學完成，隱藏所有道具
                    Debug.Log("步驟4：隱藏所有教學道具，準備開始遊戲");
                    // 所有道具都已在上面被隱藏，不需要額外操作
                    break;
            }
        }

        private void SetupButtons()
        {
            var currentStep = tutorialSteps[currentStepIndex];

            if (nextButton != null)
            {
                // 設置按鈕可互動性和顏色
                bool canProceed = !currentStep.requiresCompletion || stepCompleted;
                nextButton.interactable = canProceed;

                // 改變按鈕顏色
                var buttonImage = nextButton.GetComponent<Image>();
                if (buttonImage != null)
                {
                    buttonImage.color = canProceed ? enabledButtonColor : disabledButtonColor;
                }

                // 設置按鈕文字
                var buttonText = nextButton.GetComponentInChildren<TextMeshProUGUI>();
                if (buttonText != null)
                {
                    if (currentStepIndex == tutorialSteps.Length - 1)
                    {
                        buttonText.text = "ゲーム開始！";
                    }
                    else
                    {
                        buttonText.text = stepCompleted ? "次へ" : "完了待ち...";
                    }
                }

                Debug.Log($"按鈕設置 - 可互動: {canProceed}, 步驟: {currentStepIndex}, 完成: {stepCompleted}");
            }

            if (skipButton != null)
            {
                // 最後一步隱藏 Skip 按鈕
                skipButton.gameObject.SetActive(currentStepIndex < tutorialSteps.Length - 1);
            }
        }

        private void ExecuteStepLogic(TutorialStepType stepType)
        {
            switch (stepType)
            {
                case TutorialStepType.InteractWithTools:
                    StartCoroutine(WaitForToolInteraction());
                    break;

                case TutorialStepType.CreateRoyalJelly:
                    StartCoroutine(WaitForRoyalJellyCreation());
                    break;

                case TutorialStepType.MoveLarva:
                    StartCoroutine(WaitForFirstLarvaMove());
                    break;

                case TutorialStepType.Information:
                    // 純說明步驟，立即標記為完成
                    OnStepCompleted();
                    break;

                case TutorialStepType.CompleteAll:
                    OnStepCompleted();
                    break;
            }
        }

        #region 條件完成檢查協程

        private System.Collections.IEnumerator WaitForToolInteraction()
        {
            while (!featherInteracted || !tweezersInteracted)
            {
                // 檢查教學專用工具
                if (tutorialGooseFeather != null && tutorialGooseFeather.IsSelected && !featherInteracted)
                {
                    featherInteracted = true;
                    Debug.Log("教學鵝毛互動完成！");
                }

                if (tutorialTweezers != null && tutorialTweezers.IsSelected && !tweezersInteracted)
                {
                    tweezersInteracted = true;
                    Debug.Log("教學鑷子互動完成！");
                }

                yield return null;
            }

            Debug.Log("工具互動完成！");
            OnStepCompleted();
        }

        private System.Collections.IEnumerator WaitForRoyalJellyCreation()
        {
            while (!honeyDipped || !jellyApplied)
            {
                if (tutorialGooseFeather != null && tutorialGooseFeather.HasHoney && !honeyDipped)
                {
                    honeyDipped = true;
                    Debug.Log("教學蜂蜜沾取完成！");
                }

                if (honeyDipped && !jellyApplied && tutorialSpecialCups != null)
                {
                    foreach (var cup in tutorialSpecialCups)
                    {
                        if (cup != null && cup.HasRoyalJelly)
                        {
                            jellyApplied = true;
                            Debug.Log("教學蜂王乳塗抹完成！");
                            break;
                        }
                    }
                }

                yield return null;
            }

            Debug.Log("蜂王乳製作完成！");
            OnStepCompleted();
        }

        private System.Collections.IEnumerator WaitForFirstLarvaMove()
        {
            int initialMoved = larvaeMovedInTutorial;

            while (larvaeMovedInTutorial <= initialMoved)
            {
                // 檢查教學幼蟲是否被移動到杯子中
                bool larvaMoved = false;

                // 檢查教學幼蟲是否在杯子中
                if (tutorialSpecialCups != null)
                {
                    foreach (var cup in tutorialSpecialCups)
                    {
                        if (cup != null && cup.HasLarva)
                        {
                            larvaMoved = true;
                            break;
                        }
                    }
                }

                // 檢查 GameManager 的收集數量
                if (!larvaMoved && gameManager != null && gameManager.CollectedLarvae > initialMoved)
                {
                    larvaMoved = true;
                }

                if (larvaMoved)
                {
                    larvaeMovedInTutorial++;
                    break;
                }

                yield return null;
            }

            Debug.Log("第一隻幼蟲移動完成！");
            OnStepCompleted();
        }

        private void OnStepCompleted()
        {
            stepCompleted = true;
            SetupButtons(); // 重新設置按鈕狀態和顏色

            Debug.Log($"步驟 {currentStepIndex + 1} 完成！");

            if (currentStepIndex < tutorialSteps.Length - 1)
            {
                StartCoroutine(AutoProgressToNextStep());
            }
            else
            {
                Debug.Log("教學已完成，等待玩家點擊開始遊戲");
            }
        }

        private System.Collections.IEnumerator AutoProgressToNextStep()
        {
            // 等待1秒讓玩家看到完成狀態
            yield return new WaitForSeconds(1f);

            // 自動進入下一步
            if (currentStepIndex < tutorialSteps.Length - 1)
            {
                Debug.Log("自動進入下一步");
                NextStep();
            }
            else
            {
                Debug.Log("教學已完成，等待玩家點擊開始遊戲");
            }
        }

        public void NextStep()
        {
            if (stepCompleted || !tutorialSteps[currentStepIndex].requiresCompletion)
            {
                currentStepIndex++;
                ShowCurrentStep();
            }
        }

        public void SkipTutorial()
        {
            EndTutorial();
        }

        private void EndTutorial()
        {
            Debug.Log("EndTutorial 被調用");

            isTutorialActive = false;

            if (tutorialPanel != null)
            {
                tutorialPanel.SetActive(false);
                Debug.Log("教學面板已隱藏");
            }

            // 移除箭頭相關代碼
            // if (arrow != null)
            // {
            //     arrow.SetActive(false);
            //     Debug.Log("箭頭已隱藏");
            // }

            // 隱藏所有教學工具
            if (tutorialGooseFeather != null)
            {
                tutorialGooseFeather.gameObject.SetActive(false);
                Debug.Log("教學鵝毛已隱藏");
            }
            if (tutorialTweezers != null)
            {
                tutorialTweezers.gameObject.SetActive(false);
                Debug.Log("教學鑷子已隱藏");
            }
            if (tutorialHoneyJar != null)
            {
                tutorialHoneyJar.gameObject.SetActive(false);
                Debug.Log("教學蜂蜜罐已隱藏");
            }

            if (tutorialSpecialCups != null)
            {
                foreach (var cup in tutorialSpecialCups)
                {
                    if (cup != null) cup.gameObject.SetActive(false);
                }
                Debug.Log("教學杯子已隱藏");
            }

            if (tutorialLarva != null)
            {
                tutorialLarva.gameObject.SetActive(false);
                Debug.Log("教學幼蟲已隱藏");
            }

            Debug.Log("教學結束，開始遊戲");

            // 通知 GameManager 開始遊戲
            if (gameManager != null)
            {
                Debug.Log("通知 GameManager 開始遊戲");
                gameManager.StartGameAfterTutorial();
            }
            else
            {
                Debug.LogError("gameManager 為 null！無法開始遊戲");
            }
        }

        public bool IsTutorialActive => isTutorialActive;

        // 公開方法供其他腳本調用來更新教學進度
        public void NotifyFeatherInteraction()
        {
            if (isTutorialActive && currentStepIndex == 0)
            {
                featherInteracted = true;
            }
        }

        public void NotifyTweezersInteraction()
        {
            if (isTutorialActive && currentStepIndex == 0)
            {
                tweezersInteracted = true;
            }
        }

        public void NotifyHoneyDipped()
        {
            if (isTutorialActive && currentStepIndex == 1)
            {
                honeyDipped = true;
            }
        }

        public void NotifyJellyApplied()
        {
            if (isTutorialActive && currentStepIndex == 1)
            {
                jellyApplied = true;
            }
        }

        public void NotifyLarvaMoved()
        {
            if (isTutorialActive)
            {
                larvaeMovedInTutorial++;
            }
        }

        public void OnNextButtonClick()
        {
            Debug.Log($"Next 按鈕被點擊，當前步驟: {currentStepIndex}, 步驟完成: {stepCompleted}");

            if (currentStepIndex == tutorialSteps.Length - 1)
            {
                // 最後一步，結束教學並開始遊戲
                Debug.Log("最後一步，開始遊戲");
                EndTutorial();
            }
            else if (stepCompleted || !tutorialSteps[currentStepIndex].requiresCompletion)
            {
                // 進入下一步
                Debug.Log("進入下一步");
                NextStep();
            }
            else
            {
                Debug.Log("步驟未完成，無法繼續");
            }
        }

        #endregion
    }

    [System.Serializable]
    public class TutorialStep
    {
        [Header("Display Info")]
        public string title;
        [TextArea(3, 6)]
        public string content;

        [Header("Interaction")]
        public TutorialStepType stepType;
        // 移除箭頭相關 - public GameObject targetObject; // 箭頭指向的目標
        public bool requiresCompletion; // 是否需要完成條件才能繼續
    }

    public enum TutorialStepType
    {
        Information,         // 純說明，不需要操作
        InteractWithTools,   // 與工具互動
        CreateRoyalJelly,    // 創造蜂王乳
        MoveLarva,          // 移動幼蟲
        CompleteAll         // 完成所有
    }
}