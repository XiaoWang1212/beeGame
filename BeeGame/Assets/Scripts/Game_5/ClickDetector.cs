using UnityEngine;

public class ClickDetector : MonoBehaviour
{
    void Update()
    {
        // 偵測滑鼠左鍵點擊 (0 代表滑鼠左鍵)
        if (Input.GetMouseButtonDown(0))
        {
            // 將滑鼠的螢幕座標轉換為世界座標
            Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);

            // 創建一個 2D 射線並發射
            // 我們使用 mouseWorldPos 作為起點，Vector2.zero 作為方向，表示只檢查點擊位置
            RaycastHit2D hit = Physics2D.Raycast(mouseWorldPos, Vector2.zero);

            // 檢查射線是否擊中了任何 2D Collider
            if (hit.collider != null)
            {
                // 檢查擊中物件的 Tag
                if (hit.collider.CompareTag("Hornet"))
                {
                    // 擊中虎頭蜂
                    ShooterGameManager.instance.AddScore(ShooterGameManager.instance.scorePerHornet);
                    Destroy(hit.collider.gameObject); // 銷毀虎頭蜂
                    // TODO: 播放擊中虎頭蜂音效/特效
                    Debug.Log("擊中虎頭蜂！");
                }
                else if (hit.collider.CompareTag("Bee"))
                {
                    // 誤傷西洋蜜蜂
                    ShooterGameManager.instance.AddScore(ShooterGameManager.instance.penaltyPerBee);
                    // 誤傷後銷毀蜜蜂 (或者可以讓它短暫消失再重新生成，更符合「受驚飛走」)
                    Destroy(hit.collider.gameObject);
                    // TODO: 播放誤傷蜜蜂音效/特效
                    Debug.Log("誤傷蜜蜂！");
                }
            }
        }
    }
}