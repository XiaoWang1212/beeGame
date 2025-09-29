using UnityEngine;
using System.Collections;

namespace Game_4
{
    public class HoneyDropEffect : MonoBehaviour
    {
        [Header("Honey Drop Settings")]
        public Transform[] honeyDrops; // 蜂蜜滴的陣列
        public float dropDuration = 1f; // 掉落動畫持續時間
        public float fadeSpeed = 2f; // 淡入淡出速度
        
        [Header("Arc Movement Settings")]
        public float initialVelocityX = 2f; // 初始水平速度
        public float initialVelocityY = 1f; // 初始垂直速度
        public float gravity = 9.8f; // 重力加速度
        
        [Header("Individual Drop Directions")]
        public HoneyDropSettings[] dropSettings; // 每個蜂蜜滴的個別設定
        
        [Header("Fallback Direction (if dropSettings not set)")]
        public Vector3 defaultLaunchDirection = new Vector3(1f, 0.5f, 0); // 預設發射方向
        
        [Header("Random Settings")]
        public float randomHorizontalRange = 0.3f; // 水平隨機範圍
        public float randomVerticalRange = 0.2f; // 垂直隨機範圍
        public float randomVelocityRange = 0.5f; // 速度隨機範圍
        public float delayBetweenDrops = 0.1f; // 蜂蜜滴之間的延遲
        
        [Header("Visual Settings")]
        public AnimationCurve scaleCurve = AnimationCurve.EaseInOut(0, 1, 1, 0.5f); // 大小變化曲線
        public bool useGravity = true; // 是否使用重力
        
        private SpriteRenderer[] dropRenderers;
        private Vector3[] originalPositions;
        private bool isPlaying = false;

        void Start()
        {
            InitializeDrops();
            ValidateDropSettings();
        }

        void InitializeDrops()
        {
            if (honeyDrops == null || honeyDrops.Length == 0)
            {
                Debug.LogWarning("Honey drops 陣列為空！");
                return;
            }

            dropRenderers = new SpriteRenderer[honeyDrops.Length];
            originalPositions = new Vector3[honeyDrops.Length];

            for (int i = 0; i < honeyDrops.Length; i++)
            {
                if (honeyDrops[i] != null)
                {
                    dropRenderers[i] = honeyDrops[i].GetComponent<SpriteRenderer>();
                    originalPositions[i] = honeyDrops[i].localPosition;

                    // 初始時隱藏
                    if (dropRenderers[i] != null)
                    {
                        Color color = dropRenderers[i].color;
                        color.a = 0f;
                        dropRenderers[i].color = color;
                    }

                    // 設置初始縮放
                    honeyDrops[i].localScale = Vector3.one;
                }
            }
        }

        void ValidateDropSettings()
        {
            // 如果 dropSettings 陣列大小不匹配 honeyDrops，自動調整
            if (dropSettings == null || dropSettings.Length != honeyDrops.Length)
            {
                System.Array.Resize(ref dropSettings, honeyDrops.Length);
                
                // 為新的設定填入預設值
                for (int i = 0; i < dropSettings.Length; i++)
                {
                    if (dropSettings[i] == null)
                    {
                        dropSettings[i] = new HoneyDropSettings();
                        
                        // 根據索引設定不同的預設方向
                        switch (i)
                        {
                            case 0:
                                dropSettings[i].launchDirection = new Vector3(1f, 0.5f, 0); // 右上
                                break;
                            case 1:
                                dropSettings[i].launchDirection = new Vector3(-1f, 0.5f, 0); // 左上
                                break;
                            case 2:
                                dropSettings[i].launchDirection = new Vector3(0.5f, 1f, 0); // 偏右上
                                break;
                            case 3:
                                dropSettings[i].launchDirection = new Vector3(-0.5f, 1f, 0); // 偏左上
                                break;
                            default:
                                // 其他的隨機分配左右
                                float direction = (i % 2 == 0) ? 1f : -1f;
                                dropSettings[i].launchDirection = new Vector3(direction, 0.5f, 0);
                                break;
                        }
                        
                        dropSettings[i].velocityMultiplier = 1f;
                        dropSettings[i].useCustomDirection = true;
                    }
                }
                
                Debug.Log($"自動生成 {dropSettings.Length} 個蜂蜜滴設定");
            }
        }

        public void PlayDropEffect()
        {
            if (isPlaying) return;

            StartCoroutine(DropSequence());
        }

        IEnumerator DropSequence()
        {
            isPlaying = true;

            // 重置所有蜂蜜滴到起始位置
            ResetDrops();

            // 逐個播放蜂蜜滴動畫
            for (int i = 0; i < honeyDrops.Length; i++)
            {
                if (honeyDrops[i] != null)
                {
                    StartCoroutine(AnimateDropWithArc(i));
                    yield return new WaitForSeconds(delayBetweenDrops);
                }
            }

            // 等待所有動畫完成
            yield return new WaitForSeconds(dropDuration);

            isPlaying = false;
        }

        IEnumerator AnimateDropWithArc(int dropIndex)
        {
            if (dropIndex >= honeyDrops.Length || honeyDrops[dropIndex] == null)
                yield break;

            Transform drop = honeyDrops[dropIndex];
            SpriteRenderer renderer = dropRenderers[dropIndex];

            if (renderer == null) yield break;

            // 獲取這個蜂蜜滴的專屬設定
            Vector3 launchDirection = GetLaunchDirection(dropIndex);
            float velocityMultiplier = GetVelocityMultiplier(dropIndex);

            // 計算隨機的發射參數
            Vector3 randomizedDirection = CalculateRandomDirection(launchDirection);
            float randomizedVelocityX = (initialVelocityX * velocityMultiplier) + Random.Range(-randomVelocityRange, randomVelocityRange);
            float randomizedVelocityY = (initialVelocityY * velocityMultiplier) + Random.Range(-randomVelocityRange * 0.5f, randomVelocityRange * 0.5f);

            // 設置起始位置（加入隨機偏移）
            Vector3 startPos = originalPositions[dropIndex];
            Vector3 randomOffset = new Vector3(
                Random.Range(-randomHorizontalRange, randomHorizontalRange),
                Random.Range(-randomVerticalRange, randomVerticalRange),
                0f
            );
            startPos += randomOffset;
            drop.localPosition = startPos;

            // 初始速度向量
            Vector3 velocity = new Vector3(
                randomizedDirection.x * randomizedVelocityX,
                randomizedDirection.y * randomizedVelocityY,
                0f
            );

            float timer = 0f;
            Vector3 currentPos = startPos;

            while (timer < dropDuration)
            {
                float progress = timer / dropDuration;
                float deltaTime = Time.deltaTime;

                // 弧線運動計算
                if (useGravity)
                {
                    // 使用重力的拋物線運動
                    currentPos.x += velocity.x * deltaTime;
                    currentPos.y += velocity.y * deltaTime;
                    velocity.y -= gravity * deltaTime; // 重力影響垂直速度
                }
                else
                {
                    // 簡單的弧線插值
                    float arcProgress = progress;
                    currentPos = startPos + Vector3.Lerp(
                        Vector3.zero,
                        randomizedDirection * (randomizedVelocityX + randomizedVelocityY),
                        arcProgress
                    );
                    
                    // 添加拋物線效果
                    float arcHeight = Mathf.Sin(arcProgress * Mathf.PI) * randomizedVelocityY;
                    currentPos.y += arcHeight;
                }

                drop.localPosition = currentPos;

                // 透明度動畫（淡入然後淡出）
                float alpha = CalculateAlpha(progress);
                Color color = renderer.color;
                color.a = alpha;
                renderer.color = color;

                // 大小變化動畫
                float scale = scaleCurve.Evaluate(progress);
                drop.localScale = Vector3.one * scale;

                timer += deltaTime;
                yield return null;
            }

            // 確保最後完全隱藏
            Color finalColor = renderer.color;
            finalColor.a = 0f;
            renderer.color = finalColor;
            drop.localScale = Vector3.one;
        }

        Vector3 GetLaunchDirection(int dropIndex)
        {
            if (dropSettings != null && dropIndex < dropSettings.Length && 
                dropSettings[dropIndex] != null && dropSettings[dropIndex].useCustomDirection)
            {
                return dropSettings[dropIndex].launchDirection.normalized;
            }
            
            return defaultLaunchDirection.normalized;
        }

        float GetVelocityMultiplier(int dropIndex)
        {
            if (dropSettings != null && dropIndex < dropSettings.Length && 
                dropSettings[dropIndex] != null)
            {
                return dropSettings[dropIndex].velocityMultiplier;
            }
            
            return 1f;
        }

        Vector3 CalculateRandomDirection(Vector3 baseDirection)
        {
            // 基於設定的發射方向添加隨機變化
            Vector3 direction = baseDirection.normalized;
            
            // 添加隨機角度偏移
            float randomAngle = Random.Range(-30f, 30f) * Mathf.Deg2Rad;
            float cos = Mathf.Cos(randomAngle);
            float sin = Mathf.Sin(randomAngle);
            
            Vector3 randomizedDirection = new Vector3(
                direction.x * cos - direction.y * sin,
                direction.x * sin + direction.y * cos,
                0f
            );

            return randomizedDirection.normalized;
        }

        float CalculateAlpha(float progress)
        {
            if (progress < 0.2f)
            {
                // 前20%時間快速淡入
                return progress / 0.2f;
            }
            else if (progress > 0.8f)
            {
                // 後20%時間淡出
                return 1f - ((progress - 0.8f) / 0.2f);
            }
            else
            {
                // 中間保持完全顯示
                return 1f;
            }
        }

        void ResetDrops()
        {
            for (int i = 0; i < honeyDrops.Length; i++)
            {
                if (honeyDrops[i] != null && dropRenderers[i] != null)
                {
                    // 重置位置
                    honeyDrops[i].localPosition = originalPositions[i];

                    // 重置透明度
                    Color color = dropRenderers[i].color;
                    color.a = 0f;
                    dropRenderers[i].color = color;

                    // 重置縮放
                    honeyDrops[i].localScale = Vector3.one;
                }
            }
        }

        // 公開方法用於外部觸發
        public void TriggerDropEffect()
        {
            PlayDropEffect();
        }

        // 設置弧線參數
        public void SetArcSettings(float velocityX, float velocityY, float gravityForce)
        {
            initialVelocityX = velocityX;
            initialVelocityY = velocityY;
            gravity = gravityForce;
        }

        // 設置特定蜂蜜滴的方向
        public void SetDropDirection(int dropIndex, Vector3 direction, float velocityMult = 1f)
        {
            ValidateDropSettings();
            
            if (dropIndex >= 0 && dropIndex < dropSettings.Length)
            {
                dropSettings[dropIndex].launchDirection = direction.normalized;
                dropSettings[dropIndex].velocityMultiplier = velocityMult;
                dropSettings[dropIndex].useCustomDirection = true;
            }
        }

        // 停止所有效果
        public void StopAllEffects()
        {
            StopAllCoroutines();
            ResetDrops();
            isPlaying = false;
        }

        // 在編輯器中預覽弧線軌跡
        void OnDrawGizmos()
        {
            if (honeyDrops != null && honeyDrops.Length > 0)
            {
                // 繪製預覽軌跡
                for (int i = 0; i < honeyDrops.Length; i++)
                {
                    if (honeyDrops[i] != null)
                    {
                        DrawTrajectory(honeyDrops[i].position, i);
                    }
                }
            }
        }

        void DrawTrajectory(Vector3 startPos, int dropIndex)
        {
            // 根據索引設定不同顏色
            Color[] colors = { Color.red, Color.blue, Color.green, Color.yellow, Color.magenta, Color.cyan };
            Gizmos.color = colors[dropIndex % colors.Length];
            
            Vector3 launchDirection = GetLaunchDirection(dropIndex);
            float velocityMultiplier = GetVelocityMultiplier(dropIndex);
            
            Vector3 currentPos = startPos;
            Vector3 velocity = new Vector3(
                launchDirection.x * initialVelocityX * velocityMultiplier,
                launchDirection.y * initialVelocityY * velocityMultiplier,
                0f
            );

            float timeStep = 0.05f;
            Vector3 lastPos = currentPos;

            for (float t = 0; t < dropDuration; t += timeStep)
            {
                if (useGravity)
                {
                    currentPos.x += velocity.x * timeStep;
                    currentPos.y += velocity.y * timeStep;
                    velocity.y -= gravity * timeStep;
                }
                else
                {
                    float progress = t / dropDuration;
                    currentPos = startPos + launchDirection * (initialVelocityX + initialVelocityY) * velocityMultiplier * progress;
                    currentPos.y += Mathf.Sin(progress * Mathf.PI) * initialVelocityY * velocityMultiplier;
                }

                Gizmos.DrawLine(lastPos, currentPos);
                lastPos = currentPos;
            }

            // 繪製起始點
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(startPos, 0.05f);

            // 繪製發射方向箭頭
            Gizmos.color = colors[dropIndex % colors.Length];
            Gizmos.DrawRay(startPos, launchDirection * 0.5f);
        }

        void OnValidate()
        {
            // 在編輯器中預覽設置
            if (Application.isPlaying && !isPlaying)
            {
                InitializeDrops();
            }
            
            ValidateDropSettings();
        }
    }

    // 個別蜂蜜滴的設定類別
    [System.Serializable]
    public class HoneyDropSettings
    {
        [Header("Direction Settings")]
        public bool useCustomDirection = true;
        public Vector3 launchDirection = new Vector3(1f, 0.5f, 0f);
        
        [Header("Velocity Settings")]
        [Range(0.1f, 3f)]
        public float velocityMultiplier = 1f;
        
        [Header("Visual")]
        public string description = ""; // 用於在 Inspector 中標記用途
    }
}