using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PrincessCollection.Puzzle
{
    /// <summary>
    /// パズル盤面（6×6）。BoardLogic の結果をピースの見た目とアニメーションに反映する。
    /// 入れ替え → 消去（1グループ＝1コンボ）→ 落下・補充 → 連鎖 … をコルーチンで順に処理する。
    /// </summary>
    public class Board : MonoBehaviour
    {
        [Header("盤面")]
        [SerializeField] int width = 6;
        [SerializeField] int height = 6;
        [Tooltip("1マスの大きさ（ワールド単位）。PPU=50 なら 50px の石がちょうど 1。")]
        [SerializeField] float cellSize = 1f;
        [Tooltip("マスに対する石の大きさ。少し隙間を空けたいときは 0.95 など。")]
        [SerializeField] float pieceScale = 1f;
        [Tooltip("0 ならランダム。同じ盤面で確認したいときに数値を入れる。")]
        [SerializeField] int randomSeed = 0;

        [Header("素材（火・水・木・光・闇 の順）")]
        [SerializeField] Sprite[] pieceSprites = new Sprite[5];
        [SerializeField] int sortingOrder = 10;
        [Tooltip("盤面の外（上から落ちてくる途中）の石を隠す")]
        [SerializeField] bool maskOutsideBoard = true;

        [Header("演出（秒）")]
        [SerializeField] float swapDuration = 0.14f;
        [SerializeField] float clearDuration = 0.22f;
        [Tooltip("コンボ（グループ）ごとに消えるまでの間隔")]
        [SerializeField] float comboInterval = 0.16f;
        [Tooltip("1マス落ちるのにかかる時間の目安")]
        [SerializeField] float fallTimePerCell = 0.13f;
        [SerializeField] float chainPause = 0.06f;

        [Header("コンボ倍率： 1 + (コンボ数 - 開始数) × 増加量")]
        [SerializeField] int comboBonusStart = 2;
        [SerializeField] float comboBonusStep = 0.5f;

        // ------------------------------------------------------------------
        // 外部向け
        // ------------------------------------------------------------------

        /// <summary>1グループ消えるたびに呼ばれる（コンボ数, 消えたグループ）。</summary>
        public event Action<int, MatchGroup> ComboStep;

        /// <summary>入れ替え1回分の処理（連鎖含む）が全部終わったとき。</summary>
        public event Action<TurnResult> TurnFinished;

        /// <summary>揃わなかったので元に戻したとき。</summary>
        public event Action SwapRejected;

        /// <summary>手詰まりで並べ替えたとき。</summary>
        public event Action Shuffled;

        public BoardLogic Logic { get; private set; }
        public int Width => width;
        public int Height => height;
        public float CellSize => cellSize;

        /// <summary>アニメーション中は true。</summary>
        public bool IsBusy { get; private set; }

        /// <summary>敵のターン中などに操作を止めたいときにバトル側から設定する。</summary>
        public bool InputLocked { get; set; }

        public bool CanAcceptInput => !IsBusy && !InputLocked && Logic != null;

        Piece[,] _pieces;
        readonly Stack<Piece> _pool = new Stack<Piece>();
        Transform _pieceRoot;

        // ------------------------------------------------------------------

        void Start()
        {
            Build();
        }

        /// <summary>盤面を新しく作り直す。</summary>
        public void Build()
        {
            StopAllCoroutines();
            IsBusy = false;

            if (pieceSprites == null || pieceSprites.Length < 3)
            {
                Debug.LogError("[Board] pieceSprites に石の画像を3つ以上設定してください。", this);
                return;
            }

            if (_pieceRoot == null)
            {
                _pieceRoot = new GameObject("Pieces").transform;
                _pieceRoot.SetParent(transform, false);
            }
            if (maskOutsideBoard) EnsureMask();

            if (_pieces != null)
                foreach (var p in _pieces) if (p != null) Release(p);

            Logic = new BoardLogic(width, height, pieceSprites.Length, randomSeed != 0 ? randomSeed : (int?)null);
            Logic.FillInitial();

            _pieces = new Piece[width, height];
            for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
            {
                var p = Acquire(Logic[x, y]);
                p.transform.localPosition = CellToLocal(x, y);
                _pieces[x, y] = p;
            }

            StartCoroutine(RefreshSpritesNextFrame());
        }

        /// <summary>
        /// 環境によって（DX12 など）最初の1フレームだけ全部の石が同じ画像で描かれることがあるため、
        /// 1フレーム後に画像を入れ直して描き直させる。
        /// </summary>
        IEnumerator RefreshSpritesNextFrame()
        {
            yield return null;
            if (_pieces == null) yield break;
            foreach (var p in _pieces)
            {
                if (p == null) continue;
                var sr = p.Renderer;
                var s = pieceSprites[(int)p.Type];
                sr.sprite = null;
                sr.sprite = s;
            }
        }

        // ------------------------------------------------------------------
        // 座標変換
        // ------------------------------------------------------------------

        public Vector3 CellToLocal(int x, float y)
        {
            return new Vector3((x - (width - 1) * 0.5f) * cellSize, (y - (height - 1) * 0.5f) * cellSize, 0f);
        }

        public Vector3 CellToWorld(GridPos p) => transform.TransformPoint(CellToLocal(p.x, p.y));

        /// <summary>ワールド座標 → マス。盤面外なら false。</summary>
        public bool WorldToCell(Vector3 world, out GridPos cell)
        {
            Vector3 local = transform.InverseTransformPoint(world);
            int x = Mathf.FloorToInt(local.x / cellSize + width * 0.5f);
            int y = Mathf.FloorToInt(local.y / cellSize + height * 0.5f);
            cell = new GridPos(x, y);
            return x >= 0 && x < width && y >= 0 && y < height;
        }

        public Piece GetPiece(GridPos p) => _pieces != null && Logic.InBounds(p) ? _pieces[p.x, p.y] : null;

        public float ComboMultiplier(int combo)
        {
            return 1f + Mathf.Max(0, combo - comboBonusStart) * comboBonusStep;
        }

        // ------------------------------------------------------------------
        // 入れ替え
        // ------------------------------------------------------------------

        /// <summary>隣り合う2マスを入れ替える。受け付けたら true。</summary>
        public bool TrySwap(GridPos a, GridPos b)
        {
            if (!CanAcceptInput) return false;
            if (!Logic.InBounds(a) || !Logic.InBounds(b) || !a.IsAdjacent(b)) return false;
            StartCoroutine(SwapRoutine(a, b));
            return true;
        }

        IEnumerator SwapRoutine(GridPos a, GridPos b)
        {
            IsBusy = true;

            SwapViews(a, b);
            Logic.Swap(a, b);
            yield return WaitAll(
                _pieces[a.x, a.y].MoveTo(CellToLocal(a.x, a.y), swapDuration),
                _pieces[b.x, b.y].MoveTo(CellToLocal(b.x, b.y), swapDuration));

            var groups = Logic.FindMatches();
            if (groups.Count == 0)
            {
                // 揃わなかったので戻す
                SwapViews(a, b);
                Logic.Swap(a, b);
                yield return WaitAll(
                    _pieces[a.x, a.y].MoveTo(CellToLocal(a.x, a.y), swapDuration),
                    _pieces[b.x, b.y].MoveTo(CellToLocal(b.x, b.y), swapDuration));
                IsBusy = false;
                SwapRejected?.Invoke();
                yield break;
            }

            var result = new TurnResult(Logic.TypeCount);
            while (groups.Count > 0)
            {
                result.Chain++;
                yield return ClearGroups(groups, result);
                yield return DropAndRefill();
                if (chainPause > 0f) yield return new WaitForSeconds(chainPause);
                groups = Logic.FindMatches();
            }

            result.Multiplier = ComboMultiplier(result.Combo);
            TurnFinished?.Invoke(result);

            if (!Logic.HasPossibleMove())
                yield return ShuffleRoutine();

            IsBusy = false;
        }

        void SwapViews(GridPos a, GridPos b)
        {
            var t = _pieces[a.x, a.y];
            _pieces[a.x, a.y] = _pieces[b.x, b.y];
            _pieces[b.x, b.y] = t;
        }

        // ------------------------------------------------------------------
        // 消去・落下・補充
        // ------------------------------------------------------------------

        IEnumerator ClearGroups(List<MatchGroup> groups, TurnResult result)
        {
            var running = new List<Coroutine>();
            for (int i = 0; i < groups.Count; i++)
            {
                var g = groups[i];
                result.Add(g);
                ComboStep?.Invoke(result.Combo, g);

                foreach (var c in g.cells)
                {
                    var piece = _pieces[c.x, c.y];
                    if (piece != null) running.Add(StartCoroutine(piece.PlayClear(clearDuration)));
                }
                Logic.Clear(g);

                if (i < groups.Count - 1 && comboInterval > 0f)
                    yield return new WaitForSeconds(comboInterval);
            }
            foreach (var co in running) yield return co;

            // 消えたピースをプールへ
            foreach (var g in groups)
            foreach (var c in g.cells)
            {
                if (_pieces[c.x, c.y] != null)
                {
                    Release(_pieces[c.x, c.y]);
                    _pieces[c.x, c.y] = null;
                }
            }
        }

        IEnumerator DropAndRefill()
        {
            var anims = new List<IEnumerator>();

            foreach (var m in Logic.ApplyGravity())
            {
                var piece = _pieces[m.from.x, m.from.y];
                _pieces[m.to.x, m.to.y] = piece;
                _pieces[m.from.x, m.from.y] = null;
                anims.Add(piece.FallTo(CellToLocal(m.to.x, m.to.y), FallTime(m.from.y - m.to.y), 0f));
            }

            foreach (var s in Logic.Refill())
            {
                var piece = Acquire(s.type);
                float startY = height + s.spawnRow;
                piece.transform.localPosition = CellToLocal(s.to.x, startY);
                _pieces[s.to.x, s.to.y] = piece;
                anims.Add(piece.FallTo(CellToLocal(s.to.x, s.to.y), FallTime(startY - s.to.y), 0f));
            }

            yield return WaitAll(anims.ToArray());
        }

        float FallTime(float cells) => fallTimePerCell * Mathf.Sqrt(Mathf.Max(1f, cells));

        IEnumerator ShuffleRoutine()
        {
            var outs = new List<IEnumerator>();
            foreach (var p in _pieces) outs.Add(p.PlayClear(0.25f));
            yield return WaitAll(outs.ToArray());

            Logic.Shuffle();
            var ins = new List<IEnumerator>();
            for (int y = 0; y < height; y++)
            for (int x = 0; x < width; x++)
            {
                Release(_pieces[x, y]);
                var p = Acquire(Logic[x, y]);
                p.transform.localPosition = CellToLocal(x, y);
                _pieces[x, y] = p;
                ins.Add(p.PlayAppear(0.25f, (x + y) * 0.02f));
            }
            yield return WaitAll(ins.ToArray());
            Shuffled?.Invoke();
        }

        // ------------------------------------------------------------------
        // ユーティリティ
        // ------------------------------------------------------------------

        /// <summary>複数のコルーチンを同時に走らせて、全部終わるまで待つ。</summary>
        IEnumerator WaitAll(params IEnumerator[] routines)
        {
            var started = new List<Coroutine>(routines.Length);
            foreach (var r in routines) started.Add(StartCoroutine(r));
            foreach (var c in started) yield return c;
        }

        Piece Acquire(PieceType type)
        {
            Piece p;
            if (_pool.Count > 0)
            {
                p = _pool.Pop();
                p.gameObject.SetActive(true);
            }
            else
            {
                var go = new GameObject("Piece", typeof(SpriteRenderer), typeof(Piece));
                go.transform.SetParent(_pieceRoot, false);
                p = go.GetComponent<Piece>();
            }
            p.Setup(type, pieceSprites[(int)type], pieceScale, sortingOrder);
            if (!maskOutsideBoard) p.Renderer.maskInteraction = SpriteMaskInteraction.None;
            return p;
        }

        void Release(Piece p)
        {
            if (p == null) return;
            p.SetSelected(false);
            p.gameObject.SetActive(false);
            _pool.Push(p);
        }

        void EnsureMask()
        {
            if (transform.Find("PieceMask") != null) return;
            var tex = Texture2D.whiteTexture;
            var sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), tex.width);
            var go = new GameObject("PieceMask", typeof(SpriteMask));
            go.transform.SetParent(transform, false);
            go.transform.localScale = new Vector3(width * cellSize, height * cellSize, 1f);
            go.GetComponent<SpriteMask>().sprite = sprite;
        }

        /// <summary>確認用：盤面の石が実際にどの画像で表示されているかを数える。</summary>
        public string DescribePieces()
        {
            if (_pieces == null) return "(盤面なし)";
            var counts = new SortedDictionary<string, int>();
            var types = new int[Logic.TypeCount];
            foreach (var p in _pieces)
            {
                if (p == null) continue;
                string n = p.Renderer.sprite != null ? p.Renderer.sprite.name : "(なし)";
                counts.TryGetValue(n, out int c);
                counts[n] = c + 1;
                types[(int)p.Type]++;
            }
            var sb = new System.Text.StringBuilder("表示中の画像: ");
            foreach (var kv in counts) sb.Append($"{kv.Key}×{kv.Value} ");
            sb.Append(" / 属性: ");
            for (int i = 0; i < types.Length; i++) sb.Append($"{(PieceType)i}×{types[i]} ");
            return sb.ToString();
        }

#if UNITY_EDITOR
        void OnDrawGizmos()
        {
            Gizmos.color = new Color(1f, 0.8f, 0.3f, 0.6f);
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawWireCube(Vector3.zero, new Vector3(width * cellSize, height * cellSize, 0f));
        }
#endif
    }
}
