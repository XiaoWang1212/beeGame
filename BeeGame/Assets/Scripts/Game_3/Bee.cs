using UnityEngine;

namespace Game_3
{
    public class Bee : MonoBehaviour
    {
        [Header("蜜蜂設置")]
        public Sprite beeSprite;
        public int hitsToRemove = 1;
        public float fixedBeeScale = 0.3f; // 固定的蜜蜂大小

        private int beeID;
        private int currentHits = 0;
        private Beehive parentHive;
        private SpriteRenderer spriteRenderer;
        private bool isBeingRemoved = false; // 標記是否正在被移除

        // 添加屬性供外部查詢（兩種方式都支援）
        public Beehive ParentHive => parentHive;
        public Beehive belongsToHive => parentHive; // 新增這個屬性供 Brush.cs 使用

        public void Initialize(int id, Beehive hive)
        {
            beeID = id;
            parentHive = hive;

            // 隨機設定需要幾下才能刷掉
            hitsToRemove = Random.Range(1, 4); // 1-3下

            SetupVisuals();
            SetupCollider();
            SetFixedScale(); // 設置固定大小

            Debug.Log($"蜜蜂 {beeID} 初始化完成，需要 {hitsToRemove} 下才能移除，歸屬蜂巢: {(parentHive != null ? parentHive.name : "無")}");
        }

        void SetupVisuals()
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
            if (spriteRenderer == null)
                spriteRenderer = gameObject.AddComponent<SpriteRenderer>();

            spriteRenderer.sprite = beeSprite;
        }

        void SetupCollider()
        {
            // 確保蜜蜂有 Collider2D 用於刷子檢測
            Collider2D beeCollider = GetComponent<Collider2D>();
            if (beeCollider == null)
            {
                beeCollider = gameObject.AddComponent<CircleCollider2D>();
            }
        }

        void SetFixedScale()
        {
            // 計算相對於父物件縮放的補償值
            if (transform.parent != null)
            {
                Vector3 parentScale = transform.parent.localScale;
                float compensationX = fixedBeeScale / parentScale.x;
                float compensationY = fixedBeeScale / parentScale.y;

                transform.localScale = new Vector3(compensationX, compensationY, 1f);

                Debug.Log($"蜜蜂 {beeID} 設置大小：父物件縮放 {parentScale}，蜜蜂縮放 {transform.localScale}");
            }
        }

        void Update()
        {
            // 只有在沒有被移除時才持續監控並修正縮放
            if (!isBeingRemoved && transform.parent != null)
            {
                Vector3 parentScale = transform.parent.localScale;
                float targetX = fixedBeeScale / parentScale.x;
                float targetY = fixedBeeScale / parentScale.y;

                if (Mathf.Abs(transform.localScale.x - targetX) > 0.01f ||
                    Mathf.Abs(transform.localScale.y - targetY) > 0.01f)
                {
                    transform.localScale = new Vector3(targetX, targetY, 1f);
                }
            }
        }

        public void OnBrushed()
        {
            // 如果正在被移除，忽略刷子互動
            if (isBeingRemoved) return;

            // 檢查是否只能刷當前蜂巢的蜜蜂
            if (!CanBeBrushed())
            {
                Debug.Log($"蜜蜂 {beeID} 不能被刷 - 不屬於當前蜂巢");
                return;
            }

            Debug.Log($"蜜蜂 {beeID} 被刷了！當前次數: {currentHits + 1}/{hitsToRemove}");

            currentHits++;

            // 檢查是否達到移除條件
            if (currentHits >= hitsToRemove)
            {
                RemoveBee();
            }
        }

        // 檢查這隻蜜蜂是否可以被刷
        private bool CanBeBrushed()
        {
            // 檢查遊戲管理器是否允許使用刷子
            if (!ShakeAndBrushGameManager.Instance.CanUseBrush())
            {
                return false;
            }

            // 檢查這隻蜜蜂是否屬於當前的蜂巢
            var currentHive = ShakeAndBrushGameManager.Instance.GetCurrentHive();
            if (currentHive == null || parentHive != currentHive)
            {
                return false;
            }

            return true;
        }

        void RemoveBee()
        {
            // 防止重複移除
            if (isBeingRemoved) return;

            isBeingRemoved = true; // 標記開始移除
            Debug.Log($"蜜蜂 {beeID} 被移除了！歸屬蜂巢: {(parentHive != null ? parentHive.name : "無")}");

            // 檢查 parentHive 是否存在
            if (parentHive != null)
            {
                parentHive.RemoveBee(this);
            }
            else
            {
                Debug.LogError($"蜜蜂 {beeID} 的 parentHive 是 null！直接銷毀物件");
                Destroy(gameObject);
            }
        }

        // 開始移除動畫時調用
        public void StartRemovalAnimation()
        {
            isBeingRemoved = true; // 停止自動大小調整
            Debug.Log($"蜜蜂 {beeID} 開始移除動畫");
        }

        // 公開屬性用於調試
        public int CurrentHits => currentHits;
        public int HitsToRemove => hitsToRemove;
        public bool IsReadyToRemove => currentHits >= hitsToRemove;
        public bool IsBeingRemoved => isBeingRemoved;
    }
}