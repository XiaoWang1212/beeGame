using UnityEngine;

namespace Game_2
{
    public class CursorManager : MonoBehaviour
    {
        public static CursorManager Instance { get; private set; }

        [Header("Cursor Textures")]
        public Texture2D cursorDefault;
        public Texture2D cursorHover;
        public Texture2D cursorGrab;
        public Texture2D cursorHand;

        [Header("Cursor Settings")]
        public Vector2 defaultHotspot = Vector2.zero;
        public Vector2 hoverHotspot = new Vector2(5, 0);
        public Vector2 grabHotspot = new Vector2(16, 16);
        public Vector2 handHotspot = new Vector2(16, 16);

        void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }

        void Start()
        {
            SetDefaultCursor();
        }

        public void SetDefaultCursor()
        {
            if (cursorDefault != null)
            {
                Cursor.SetCursor(cursorDefault, defaultHotspot, CursorMode.Auto);
            }
            else
            {
                Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
            }
        }

        public void SetHoverCursor()
        {
            if (cursorHover != null)
            {
                Cursor.SetCursor(cursorHover, hoverHotspot, CursorMode.Auto);
            }
        }

        public void SetGrabCursor()
        {
            if (cursorGrab != null)
            {
                Cursor.SetCursor(cursorGrab, grabHotspot, CursorMode.Auto);
            }
        }

        public void SetHandCursor()
        {
            {
                if (cursorHand != null)
                {
                    Cursor.SetCursor(cursorHand, handHotspot, CursorMode.Auto);
                    Debug.Log("設置 Hand Cursor");
                }
            }
        }


        void OnDestroy()
        {
            SetDefaultCursor();
        }

        void OnApplicationFocus(bool hasFocus)
        {
            if (!hasFocus)
            {
                SetDefaultCursor();
            }
        }
    }
}