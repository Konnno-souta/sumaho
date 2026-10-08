using UnityEngine;

namespace PrincessCollection.Puzzle
{
    /// <summary>
    /// ノッチやホームバーを避けるため、この RectTransform を Screen.safeArea に合わせる。
    /// Canvas の直下に置き、UI はすべてこの中に入れる。
    /// </summary>
    [ExecuteAlways]
    [RequireComponent(typeof(RectTransform))]
    public class SafeArea : MonoBehaviour
    {
        RectTransform _rt;
        Rect _lastArea;
        Vector2Int _lastScreen;

        void OnEnable() => Apply(true);
        void Update() => Apply(false);

        void Apply(bool force)
        {
            if (_rt == null) _rt = (RectTransform)transform;
            Rect area = Screen.safeArea;
            var screen = new Vector2Int(Screen.width, Screen.height);
            if (!force && area == _lastArea && screen == _lastScreen) return;
            if (screen.x <= 0 || screen.y <= 0) return;

            _lastArea = area;
            _lastScreen = screen;
            _rt.anchorMin = new Vector2(area.xMin / screen.x, area.yMin / screen.y);
            _rt.anchorMax = new Vector2(area.xMax / screen.x, area.yMax / screen.y);
            _rt.offsetMin = Vector2.zero;
            _rt.offsetMax = Vector2.zero;
        }
    }
}
