using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace PrincessCollection.Puzzle
{
    /// <summary>
    /// 盤面の操作。
    ///  ・ピースを押したまま隣へスライド → 入れ替え
    ///  ・ピースをタップで選択 → 隣のピースをタップ → 入れ替え
    /// マウス／タッチ両対応。新旧どちらの Input 設定でも動く。
    /// </summary>
    public class BoardInput : MonoBehaviour
    {
        [SerializeField] Board board;
        [SerializeField] Camera cam;
        [Tooltip("何マス分ドラッグしたら入れ替えとみなすか")]
        [Range(0.1f, 1f)]
        [SerializeField] float dragThreshold = 0.35f;

        GridPos? _selected;   // タップで選択中
        GridPos? _dragFrom;   // 押し始めたマス
        Vector3 _pressWorld;
        bool _pressedOnSelected;

        void Reset()
        {
            board = GetComponent<Board>();
            cam = Camera.main;
        }

        void Awake()
        {
            if (board == null) board = GetComponent<Board>();
            if (cam == null) cam = Camera.main;
        }

        void Update()
        {
            if (board == null || cam == null) return;
            if (!ReadPointer(out Vector2 screen, out bool down, out bool held, out bool up)) return;

            if (!board.CanAcceptInput)
            {
                _dragFrom = null;
                return;
            }

            Vector3 world = cam.ScreenToWorldPoint(new Vector3(screen.x, screen.y, -cam.transform.position.z));

            if (down) OnDown(world);
            else if (held && _dragFrom.HasValue) OnDrag(world);
            if (up) OnUp(world);
        }

        void OnDown(Vector3 world)
        {
            if (!board.WorldToCell(world, out GridPos cell))
            {
                Select(null);
                _dragFrom = null;
                return;
            }

            // 選択中のピースの隣をタップ → 入れ替え
            if (_selected.HasValue && _selected.Value.IsAdjacent(cell))
            {
                var from = _selected.Value;
                Select(null);
                board.TrySwap(from, cell);
                _dragFrom = null;
                return;
            }

            _pressedOnSelected = _selected.HasValue && _selected.Value == cell;
            Select(cell);
            _dragFrom = cell;
            _pressWorld = world;
        }

        void OnDrag(Vector3 world)
        {
            Vector3 delta = world - _pressWorld;
            float threshold = board.CellSize * dragThreshold * board.transform.lossyScale.x;
            if (delta.magnitude < threshold) return;

            var from = _dragFrom.Value;
            GridPos to = Mathf.Abs(delta.x) > Mathf.Abs(delta.y)
                ? new GridPos(from.x + (delta.x > 0 ? 1 : -1), from.y)
                : new GridPos(from.x, from.y + (delta.y > 0 ? 1 : -1));

            _dragFrom = null;
            Select(null);
            if (board.Logic.InBounds(to)) board.TrySwap(from, to);
        }

        void OnUp(Vector3 world)
        {
            // 選択中のピースをもう一度タップ（ドラッグせずに離した）→ 選択解除
            if (_dragFrom.HasValue && _pressedOnSelected) Select(null);
            _dragFrom = null;
            _pressedOnSelected = false;
        }

        void Select(GridPos? cell)
        {
            if (_selected.HasValue) board.GetPiece(_selected.Value)?.SetSelected(false);
            _selected = cell;
            if (_selected.HasValue) board.GetPiece(_selected.Value)?.SetSelected(true);
        }

        // ------------------------------------------------------------------
        // 入力の読み取り（Input System / 旧 Input Manager 両対応）
        // ------------------------------------------------------------------

        static bool ReadPointer(out Vector2 pos, out bool down, out bool held, out bool up)
        {
#if ENABLE_INPUT_SYSTEM
            var p = Pointer.current;
            if (p == null) { pos = default; down = held = up = false; return false; }
            pos = p.position.ReadValue();
            down = p.press.wasPressedThisFrame;
            held = p.press.isPressed;
            up = p.press.wasReleasedThisFrame;
            return true;
#else
            if (Input.touchCount > 0)
            {
                var t = Input.GetTouch(0);
                pos = t.position;
                down = t.phase == TouchPhase.Began;
                up = t.phase == TouchPhase.Ended || t.phase == TouchPhase.Canceled;
                held = !up;
                return true;
            }
            pos = (Vector2)Input.mousePosition;
            down = Input.GetMouseButtonDown(0);
            held = Input.GetMouseButton(0);
            up = Input.GetMouseButtonUp(0);
            return true;
#endif
        }
    }
}
