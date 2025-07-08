using UnityEngine;

public class EntityMovement : MonoBehaviour
{
    private Vector2 targetPosition;
    private float moveSpeed;
    private bool hasTarget = false;
    private bool isOutOfBounds = false;

    public void SetTargetAndSpeed(Vector2 target, float speed)
    {
        targetPosition = target;
        moveSpeed = speed;
        hasTarget = true;
        isOutOfBounds = false;
    }

    void Update()
    {
        if (hasTarget && !isOutOfBounds)
        {
            // 向目標移動
            transform.position = Vector2.MoveTowards(transform.position, targetPosition, moveSpeed * Time.deltaTime);

            // 檢查是否到達目標或超出螢幕邊界
            if (Vector2.Distance(transform.position, targetPosition) < 0.1f || IsOutOfScreen())
            {
                DestroyEntity();
            }
        }
    }

    private bool IsOutOfScreen()
    {
        Vector2 screenBounds = Camera.main.ScreenToWorldPoint(new Vector3(Screen.width, Screen.height, Camera.main.transform.position.z));
        Vector3 pos = transform.position;
        
        return pos.x < -screenBounds.x - 1f || pos.x > screenBounds.x + 1f || 
               pos.y < -screenBounds.y - 1f || pos.y > screenBounds.y + 1f;
    }

    private void DestroyEntity()
    {
        if (!isOutOfBounds)
        {
            isOutOfBounds = true;
            // 延遲銷毀以避免 Editor 錯誤
            Invoke(nameof(SafeDestroy), 0.1f);
        }
    }

    private void SafeDestroy()
    {
        if (gameObject != null)
        {
            Destroy(gameObject);
        }
    }

    void OnBecameInvisible()
    {
        // 當物件離開相機視野時自動銷毀
        if (hasTarget)
        {
            DestroyEntity();
        }
    }
}