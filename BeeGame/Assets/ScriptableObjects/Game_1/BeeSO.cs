using UnityEngine;

[CreateAssetMenu(fileName = "New Bee Config", menuName = "Bee Game/Bee Config")]
public class BeeSO : ScriptableObject
{
    [Header("Basic Properties")]
    public bool isQueen = false;
    public Color beeColor = Color.yellow;
    public float baseScale = 1.0f;
    
    [Header("Movement Settings")]
    public float moveSpeed = 2f;
    public float wanderRadius = 1.5f;
    public Vector2 directionChangeTimeRange = new Vector2(1.5f, 3.5f);
    
    [Header("Queen Specific Settings")]
    [Tooltip("皇冠是否可見")]
    public bool hasCrown = false;
    
    [Tooltip("光暈效果強度 (0-1)")]
    [Range(0f, 1f)]
    public float auraIntensity = 0f;
    
    [Header("Difficulty Scaling")]
    [Tooltip("基於難度等級的顏色變化")]
    public Gradient difficultyColorGradient;
    
    [Tooltip("基於難度等級的大小變化")]
    public AnimationCurve difficultySizeCurve;
    
    [Tooltip("皇冠可見度閾值")]
    [Range(0f, 1f)]
    public float crownVisibilityThreshold = 0.3f;

    [Header("Interactive Effects")]
    [Tooltip("滑鼠懸停邊框顏色")]
    public Color hoverBorderColor = Color.white;

    [Tooltip("滑鼠懸停邊框厚度")]
    [Range(0.1f, 0.5f)]
    public float hoverBorderThickness = 0.2f;

    [Tooltip("點擊音效")]
    public AudioClip clickSound;

    // 根據難度等級獲取顏色
    public Color GetColorByDifficulty(float difficultyLevel)
    {
        if (difficultyColorGradient.colorKeys.Length == 0)
        {
            return beeColor;
        }
        
        // difficultyLevel 0 = 簡單, 1 = 困難
        // gradient 中 0 = 最明顯的差異, 1 = 最不明顯的差異
        return difficultyColorGradient.Evaluate(difficultyLevel);
    }
    
    // 根據難度等級獲取大小
    public float GetScaleByDifficulty(float difficultyLevel)
    {
        if (difficultySizeCurve.keys.Length == 0)
        {
            return baseScale;
        }
        
        return baseScale * difficultySizeCurve.Evaluate(difficultyLevel);
    }
    
    // 檢查皇冠是否應該可見
    public bool ShouldShowCrown(float difficultyLevel)
    {
        return hasCrown && (1f - difficultyLevel) > crownVisibilityThreshold;
    }
}