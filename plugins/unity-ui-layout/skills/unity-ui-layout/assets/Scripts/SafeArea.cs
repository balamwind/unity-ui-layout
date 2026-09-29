// UI Layout Gen - 노치·홈 인디케이터 영역을 피해 RectTransform을 Screen.safeArea에 맞춘다.
// 부모는 화면 전체를 덮는 Canvas여야 한다.
using UnityEngine;

namespace UILayoutGen
{
    [RequireComponent(typeof(RectTransform))]
    public class SafeArea : MonoBehaviour
    {
        RectTransform _rt;
        Rect _lastSafe;
        Vector2Int _lastScreen;
        ScreenOrientation _lastOrientation;

        void OnEnable()
        {
            _rt = GetComponent<RectTransform>();
            Apply();
        }

        void Update()
        {
            if (Screen.safeArea != _lastSafe
                || Screen.width != _lastScreen.x || Screen.height != _lastScreen.y
                || Screen.orientation != _lastOrientation)
                Apply();
        }

        void Apply()
        {
            _lastSafe = Screen.safeArea;
            _lastScreen = new Vector2Int(Screen.width, Screen.height);
            _lastOrientation = Screen.orientation;
            if (Screen.width <= 0 || Screen.height <= 0) return;

            Vector2 min = _lastSafe.position;
            Vector2 max = _lastSafe.position + _lastSafe.size;
            min.x /= Screen.width;  min.y /= Screen.height;
            max.x /= Screen.width;  max.y /= Screen.height;

            _rt.anchorMin = min;
            _rt.anchorMax = max;
            _rt.offsetMin = Vector2.zero;
            _rt.offsetMax = Vector2.zero;
        }
    }
}
