using UnityEngine;
using System.Collections.Generic;
using System.Collections;

namespace Game_3
{
    public class Beehive : MonoBehaviour
    {
        [Header("蜂巢設置")]
        public Sprite beehiveSprite;
        public int totalBees = 50; // 增加到50隻讓蜂巢看起來密密麻麻
        public float shakeThreshold = 100f;

        [Header("蜜蜂隨機生成設置 - 長方形範圍")]
        public GameObject beePrefab;
        public Vector2 beehiveSize = new Vector2(4f, 2f);
        public float minDistanceBetweenBees = 0.15f; // 減小距離讓蜜蜂更密集
        public int maxGenerationAttempts = 100; // 增加嘗試次數

        [Header("抖動移除設置")]
        public int finalBeeCount = 6; // 最終剩餘的蜜蜂數量 (5-8隻的中間值)
        public float shakeRemovalInterval = 2f; // 每2%移除一些蜜蜂

        public List<Bee> activeBees = new List<Bee>();
        public List<Bee> beesToBrush = new List<Bee>(); // 需要用刷子清除的蜜蜂
        
        private float currentShakeProgress = 0f;
        private bool isShakeComplete = false;
        private bool isComplete = false;
        private Vector3 originalPosition;
        private float lastRemovalProgress = 0f;

        public bool IsShakeComplete => isShakeComplete;
        public bool IsComplete => isComplete;
        public float ShakeProgress => currentShakeProgress / shakeThreshold;

        void Start()
        {
            originalPosition = transform.position;
            InitializeBees();
        }

        void InitializeBees()
        {
            // 清除現有蜜蜂
            foreach (var bee in activeBees)
            {
                if (bee != null)
                    Destroy(bee.gameObject);
            }
            activeBees.Clear();
            beesToBrush.Clear();

            if (beePrefab == null)
            {
                Debug.LogError("beePrefab 未設置！請在 Inspector 中設置蜜蜂預製體");
                return;
            }

            Debug.Log($"開始生成 {totalBees} 隻蜜蜂");

            // 生成密密麻麻的蜜蜂
            int successCount = 0;
            for (int i = 0; i < totalBees; i++)
            {
                Vector3 beePosition = GenerateRandomBeePositionInRectangle();
                
                if (beePosition != Vector3.zero)
                {
                    GameObject beeObj = Instantiate(beePrefab, beePosition, Quaternion.identity);
                    beeObj.transform.SetParent(transform);

                    Bee bee = beeObj.GetComponent<Bee>();
                    if (bee != null)
                    {
                        bee.Initialize(i, this);
                        activeBees.Add(bee);
                        successCount++;
                    }
                    else
                    {
                        Debug.LogError("beePrefab 沒有 Bee 腳本！");
                        Destroy(beeObj);
                    }
                }
                else if (successCount < finalBeeCount)
                {
                    // 如果連最少數量都生成不了，嘗試備用位置
                    Vector3 fallbackPosition = GenerateFallbackPosition();
                    if (fallbackPosition != Vector3.zero)
                    {
                        GameObject beeObj = Instantiate(beePrefab, fallbackPosition, Quaternion.identity);
                        beeObj.transform.SetParent(transform);

                        Bee bee = beeObj.GetComponent<Bee>();
                        if (bee != null)
                        {
                            bee.Initialize(i, this);
                            activeBees.Add(bee);
                            successCount++;
                        }
                    }
                }
            }
            
            Debug.Log($"成功生成 {successCount} 隻蜜蜂");
        }

        Vector3 GenerateRandomBeePositionInRectangle()
        {
            for (int attempt = 0; attempt < maxGenerationAttempts; attempt++)
            {
                // 根據蜂巢的實際縮放調整生成範圍
                Vector3 currentScale = transform.localScale;
                Vector2 scaledBeehiveSize = new Vector2(
                    beehiveSize.x * currentScale.x,
                    beehiveSize.y * currentScale.y
                );
                
                float randomX = Random.Range(-scaledBeehiveSize.x / 2f, scaledBeehiveSize.x / 2f);
                float randomY = Random.Range(-scaledBeehiveSize.y / 2f, scaledBeehiveSize.y / 2f);
                
                Vector3 candidatePosition = transform.position + new Vector3(randomX, randomY, 0);

                bool positionIsValid = true;
                foreach (var existingBee in activeBees)
                {
                    if (existingBee != null)
                    {
                        float distance = Vector3.Distance(candidatePosition, existingBee.transform.position);
                        if (distance < minDistanceBetweenBees)
                        {
                            positionIsValid = false;
                            break;
                        }
                    }
                }

                if (positionIsValid)
                {
                    return candidatePosition;
                }
            }

            return Vector3.zero;
        }

        Vector3 GenerateFallbackPosition()
        {
            // 根據蜂巢的實際縮放調整生成範圍
            Vector3 currentScale = transform.localScale;
            Vector2 scaledBeehiveSize = new Vector2(
                beehiveSize.x * currentScale.x,
                beehiveSize.y * currentScale.y
            );
            
            float randomX = Random.Range(-scaledBeehiveSize.x / 2f, scaledBeehiveSize.x / 2f);
            float randomY = Random.Range(-scaledBeehiveSize.y / 2f, scaledBeehiveSize.y / 2f);
            
            Vector3 candidatePosition = transform.position + new Vector3(randomX, randomY, 0);
            return candidatePosition;
        }

        public void AddShakeProgress(float amount)
        {
            if (!isShakeComplete)
            {
                currentShakeProgress += amount;
                float progressPercent = (currentShakeProgress / shakeThreshold) * 100f;
                
                // 檢查是否需要移除蜜蜂（每2%檢查一次）
                if (progressPercent - lastRemovalProgress >= shakeRemovalInterval)
                {
                    RemoveBeesFromShaking(progressPercent);
                    lastRemovalProgress = progressPercent;
                }
                
                if (currentShakeProgress >= shakeThreshold)
                {
                    currentShakeProgress = shakeThreshold;
                    CompleteShaking();
                }
            }
        }

        void RemoveBeesFromShaking(float progressPercent)
        {
            if (activeBees.Count <= finalBeeCount) return; // 已經達到最少數量
            
            // 簡化計算：直接基於進度百分比計算目標數量
            int totalToRemove = Mathf.Max(0, activeBees.Count + beesToBrush.Count - finalBeeCount);
            float targetRemainingRatio = 1f - (progressPercent / 100f); // 100%時為0，0%時為1
            int targetRemaining = Mathf.RoundToInt(totalToRemove * targetRemainingRatio) + finalBeeCount;
            
            // 計算當前應該剩餘多少隻蜜蜂
            int currentTotal = activeBees.Count + beesToBrush.Count;
            int needToRemove = currentTotal - targetRemaining;
            
            // 限制每次最多移除的數量，避免一次移除太多
            int maxRemovalPerStep = Mathf.Max(1, totalToRemove / 20); // 分20步移除，每步最多移除總數的1/20
            needToRemove = Mathf.Min(needToRemove, maxRemovalPerStep);
            
            // 確保不會移除超過應該移除的數量
            needToRemove = Mathf.Min(needToRemove, activeBees.Count - finalBeeCount);
            
            Debug.Log($"進度 {progressPercent:F1}%，目標剩餘 {targetRemaining}，當前總數 {currentTotal}，需要移除 {needToRemove} 隻");
            
            // 隨機移除蜜蜂
            for (int i = 0; i < needToRemove && activeBees.Count > finalBeeCount; i++)
            {
                int randomIndex = Random.Range(0, activeBees.Count);
                Bee beeToRemove = activeBees[randomIndex];
                
                if (beeToRemove != null)
                {
                    activeBees.RemoveAt(randomIndex);
                    StartCoroutine(AnimateBeeRemoval(beeToRemove));
                }
            }
            
            Debug.Log($"移除完成，剩餘 {activeBees.Count} 隻蜜蜂");
        }

        IEnumerator AnimateBeeRemoval(Bee bee)
        {
            Debug.Log($"AnimateBeeRemoval 開始執行，蜜蜂: {bee?.name}");
            
            if (bee == null || bee.gameObject == null) 
            {
                Debug.LogError("AnimateBeeRemoval: bee 或 bee.gameObject 是 null");
                yield break;
            }
            
            // **不要設置 bee.enabled = false，這會影響 SpriteRenderer**
            // 改用 Bee 腳本中的標記來停止更新
            if (bee.GetComponent<Bee>() != null)
            {
                bee.GetComponent<Bee>().StartRemovalAnimation();
            }

            // 蜜蜂飛走的動畫
            Vector3 startPos = bee.transform.position;
            Vector3 endPos = startPos + new Vector3(Random.insideUnitCircle.normalized.x, Random.insideUnitCircle.normalized.y, 0) * 5f;
            float duration = 0.5f;
            float elapsed = 0f;
            
            SpriteRenderer renderer = bee.GetComponent<SpriteRenderer>();
            if (renderer == null)
            {
                Debug.LogError($"蜜蜂 {bee.name} 沒有 SpriteRenderer 組件！");
                Destroy(bee.gameObject);
                yield break;
            }
            
            Color startColor = renderer.color;
            Vector3 startScale = bee.transform.localScale;
            Vector3 endScale = Vector3.zero;

            Debug.Log($"動畫參數 - 起始位置: {startPos}, 目標位置: {endPos}, 起始顏色: {startColor}, 起始大小: {startScale}");

            while (elapsed < duration && bee != null && bee.gameObject != null)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                
                // 移動
                bee.transform.position = Vector3.Lerp(startPos, endPos, t);
                
                // 淡出
                if (renderer != null)
                {
                    Color newColor = Color.Lerp(startColor, new Color(startColor.r, startColor.g, startColor.b, 0), t);
                    renderer.color = newColor;
                    Debug.Log($"動畫進度: {t:F2}, 當前顏色: {newColor}"); // 調試顏色變化
                }
                
                // 縮小
                bee.transform.localScale = Vector3.Lerp(startScale, endScale, t);
                
                yield return null;
            }
            
            Debug.Log($"動畫完成，銷毀蜜蜂: {bee?.name}");
            if (bee != null && bee.gameObject != null)
            {
                Destroy(bee.gameObject);
            }
        }

        void CompleteShaking()
        {
            isShakeComplete = true;
            
            // 確保最終數量在5-8之間
            int targetFinalCount = Random.Range(5, 9);
            
            Debug.Log($"抖動完成！目標最終數量: {targetFinalCount}，當前數量: {activeBees.Count}");
            
            // 如果還有太多蜜蜂，移除多餘的
            List<Bee> beesToRemoveAtEnd = new List<Bee>();
            while (activeBees.Count > targetFinalCount)
            {
                int randomIndex = Random.Range(0, activeBees.Count);
                Bee beeToRemove = activeBees[randomIndex];
                beesToRemoveAtEnd.Add(beeToRemove);
                activeBees.RemoveAt(randomIndex);
            }
            
            // 分批移除剩餘蜜蜂，避免同時飛走太多
            StartCoroutine(RemoveBeesGradually(beesToRemoveAtEnd));
            
            // 將最終剩餘的蜜蜂設為需要刷子清除
            beesToBrush.Clear();
            beesToBrush.AddRange(activeBees);
            
            Debug.Log($"最終剩餘 {activeBees.Count} 隻蜜蜂需要用刷子清除");
        }

        IEnumerator RemoveBeesGradually(List<Bee> beesToRemove)
        {
            // 分批移除，每批間隔一點時間
            int batchSize = Mathf.Max(1, beesToRemove.Count / 5); // 分5批
            
            for (int i = 0; i < beesToRemove.Count; i += batchSize)
            {
                // 移除這一批的蜜蜂
                for (int j = i; j < Mathf.Min(i + batchSize, beesToRemove.Count); j++)
                {
                    if (beesToRemove[j] != null)
                    {
                        StartCoroutine(AnimateBeeRemoval(beesToRemove[j]));
                    }
                }
                
                // 等待一小段時間再移除下一批
                yield return new WaitForSeconds(0.2f);
            }
        }

        public void RemoveBee(Bee bee)
        {
            if (bee == null) 
            {
                Debug.LogError("RemoveBee: bee 是 null");
                return;
            }
            
            Debug.Log($"RemoveBee 被調用，蜜蜂 ID: {bee.name}");
            
            if (activeBees.Contains(bee))
            {
                activeBees.Remove(bee);
                beesToBrush.Remove(bee);
                Debug.Log($"用刷子移除蜜蜂，剩餘 {activeBees.Count} 隻");
                
                // 檢查蜜蜂是否還存在
                if (bee != null && bee.gameObject != null)
                {
                    Debug.Log($"開始播放蜜蜂 {bee.name} 的移除動畫");
                    StartCoroutine(AnimateBeeRemoval(bee));
                }
                else
                {
                    Debug.LogError("蜜蜂物件已經被銷毀，無法播放動畫");
                }

                if (activeBees.Count == 0)
                {
                    isComplete = true;
                    Debug.Log("蜂巢完成！所有蜜蜂都被清除了");
                }
            }
            else
            {
                Debug.LogWarning($"嘗試移除不在 activeBees 列表中的蜜蜂: {bee.name}");
                // 即使不在列表中，也嘗試播放動畫
                if (bee != null && bee.gameObject != null)
                {
                    StartCoroutine(AnimateBeeRemoval(bee));
                }
            }
        }

        public void ResetBeehive()
        {
            foreach (var bee in activeBees)
            {
                if (bee != null)
                    Destroy(bee.gameObject);
            }

            activeBees.Clear();
            beesToBrush.Clear();
            currentShakeProgress = 0f;
            isShakeComplete = false;
            isComplete = false;
            lastRemovalProgress = 0f;
            transform.position = originalPosition;

            InitializeBees();
        }

        void OnDrawGizmosSelected()
        {
            // 畫長方形邊框
            Gizmos.color = Color.green;
            Vector3 center = transform.position;
            
            Vector3 topLeft = center + new Vector3(-beehiveSize.x / 2f, beehiveSize.y / 2f, 0);
            Vector3 topRight = center + new Vector3(beehiveSize.x / 2f, beehiveSize.y / 2f, 0);
            Vector3 bottomLeft = center + new Vector3(-beehiveSize.x / 2f, -beehiveSize.y / 2f, 0);
            Vector3 bottomRight = center + new Vector3(beehiveSize.x / 2f, -beehiveSize.y / 2f, 0);
            
            Gizmos.DrawLine(topLeft, topRight);
            Gizmos.DrawLine(topRight, bottomRight);
            Gizmos.DrawLine(bottomRight, bottomLeft);
            Gizmos.DrawLine(bottomLeft, topLeft);
            
            Gizmos.color = new Color(0, 1, 0, 0.2f);
            Gizmos.DrawCube(center, new Vector3(beehiveSize.x, beehiveSize.y, 0.1f));
        }
    }
}