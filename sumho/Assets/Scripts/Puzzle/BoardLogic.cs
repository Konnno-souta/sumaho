using System;
using System.Collections.Generic;

namespace PrincessCollection.Puzzle
{
    /// <summary>ピースの属性。Stone/01〜05.png の並びに対応。</summary>
    public enum PieceType
    {
        None = -1,
        Fire = 0,   // 01 火
        Water = 1,  // 02 水
        Wood = 2,   // 03 木
        Light = 3,  // 04 光
        Dark = 4,   // 05 闇
    }

    /// <summary>盤面上の座標。x=列(左→右)、y=行(下→上)。</summary>
    [Serializable]
    public struct GridPos : IEquatable<GridPos>
    {
        public int x;
        public int y;

        public GridPos(int x, int y) { this.x = x; this.y = y; }

        public bool IsAdjacent(GridPos o) => Math.Abs(x - o.x) + Math.Abs(y - o.y) == 1;
        public bool Equals(GridPos o) => x == o.x && y == o.y;
        public override bool Equals(object obj) => obj is GridPos o && Equals(o);
        public override int GetHashCode() => (x * 397) ^ y;
        public override string ToString() => $"({x},{y})";
        public static bool operator ==(GridPos a, GridPos b) => a.Equals(b);
        public static bool operator !=(GridPos a, GridPos b) => !a.Equals(b);
    }

    /// <summary>同色でつながって消えるひとかたまり（＝1コンボ）。</summary>
    public class MatchGroup
    {
        public PieceType type;
        public List<GridPos> cells = new List<GridPos>();
        public int Count => cells.Count;
    }

    /// <summary>落下：from の位置にあったピースが to に移動した。</summary>
    public struct FallMove
    {
        public GridPos from;
        public GridPos to;
        public FallMove(GridPos from, GridPos to) { this.from = from; this.to = to; }
    }

    /// <summary>補充：to の位置に新しいピースが生成された。spawnRow は盤面上端から何段上に出現させるか(0始まり)。</summary>
    public struct Spawn
    {
        public GridPos to;
        public PieceType type;
        public int spawnRow;
    }

    /// <summary>
    /// パズル盤面の純粋なロジック（UnityEngine に依存しない）。
    /// 表示やアニメーションは Board.cs が担当する。
    /// </summary>
    public class BoardLogic
    {
        public const int MinMatch = 3;

        public int Width { get; }
        public int Height { get; }
        public int TypeCount { get; }

        readonly PieceType[,] _grid;
        readonly Random _rng;

        public BoardLogic(int width, int height, int typeCount, int? seed = null)
        {
            if (width < MinMatch || height < MinMatch) throw new ArgumentException("盤面が小さすぎます");
            if (typeCount < 3) throw new ArgumentException("属性は3種類以上必要です");
            Width = width;
            Height = height;
            TypeCount = typeCount;
            _grid = new PieceType[width, height];
            _rng = seed.HasValue ? new Random(seed.Value) : new Random();
        }

        public PieceType this[int x, int y]
        {
            get => _grid[x, y];
            set => _grid[x, y] = value;
        }

        public PieceType this[GridPos p]
        {
            get => _grid[p.x, p.y];
            set => _grid[p.x, p.y] = value;
        }

        public bool InBounds(GridPos p) => p.x >= 0 && p.x < Width && p.y >= 0 && p.y < Height;

        PieceType RandomType() => (PieceType)_rng.Next(TypeCount);

        // ------------------------------------------------------------------
        // 初期配置
        // ------------------------------------------------------------------

        /// <summary>最初から揃っている所がなく、かつ動かせる手が必ずある盤面を作る。</summary>
        public void FillInitial()
        {
            do
            {
                for (int y = 0; y < Height; y++)
                for (int x = 0; x < Width; x++)
                    _grid[x, y] = PickTypeWithoutMatch(x, y);
            } while (!HasPossibleMove());
        }

        /// <summary>左と下だけを見て、置いても3つ揃わない色を選ぶ。</summary>
        PieceType PickTypeWithoutMatch(int x, int y)
        {
            var candidates = new List<PieceType>(TypeCount);
            for (int i = 0; i < TypeCount; i++)
            {
                var t = (PieceType)i;
                bool h = x >= 2 && _grid[x - 1, y] == t && _grid[x - 2, y] == t;
                bool v = y >= 2 && _grid[x, y - 1] == t && _grid[x, y - 2] == t;
                if (!h && !v) candidates.Add(t);
            }
            return candidates[_rng.Next(candidates.Count)];
        }

        // ------------------------------------------------------------------
        // 入れ替え・判定
        // ------------------------------------------------------------------

        public void Swap(GridPos a, GridPos b)
        {
            var t = this[a];
            this[a] = this[b];
            this[b] = t;
        }

        /// <summary>
        /// 縦横3つ以上並んでいるマスを探し、同色で隣接しているものを1グループ（1コンボ）にまとめる。
        /// L字・T字・十字は1グループになる。
        /// </summary>
        public List<MatchGroup> FindMatches()
        {
            var marked = new bool[Width, Height];

            // 横
            for (int y = 0; y < Height; y++)
            {
                int run = 1;
                for (int x = 1; x <= Width; x++)
                {
                    bool same = x < Width && _grid[x, y] != PieceType.None && _grid[x, y] == _grid[x - 1, y];
                    if (same) { run++; continue; }
                    if (run >= MinMatch && _grid[x - 1, y] != PieceType.None)
                        for (int k = 1; k <= run; k++) marked[x - k, y] = true;
                    run = 1;
                }
            }

            // 縦
            for (int x = 0; x < Width; x++)
            {
                int run = 1;
                for (int y = 1; y <= Height; y++)
                {
                    bool same = y < Height && _grid[x, y] != PieceType.None && _grid[x, y] == _grid[x, y - 1];
                    if (same) { run++; continue; }
                    if (run >= MinMatch && _grid[x, y - 1] != PieceType.None)
                        for (int k = 1; k <= run; k++) marked[x, y - k] = true;
                    run = 1;
                }
            }

            // 印のついたマスを同色の4近傍でつなげてグループ化（下の行から順に＝消える順）
            var groups = new List<MatchGroup>();
            var visited = new bool[Width, Height];
            var stack = new Stack<GridPos>();
            for (int y = 0; y < Height; y++)
            for (int x = 0; x < Width; x++)
            {
                if (!marked[x, y] || visited[x, y]) continue;
                var g = new MatchGroup { type = _grid[x, y] };
                stack.Push(new GridPos(x, y));
                visited[x, y] = true;
                while (stack.Count > 0)
                {
                    var p = stack.Pop();
                    g.cells.Add(p);
                    TryVisit(p.x + 1, p.y, g.type, marked, visited, stack);
                    TryVisit(p.x - 1, p.y, g.type, marked, visited, stack);
                    TryVisit(p.x, p.y + 1, g.type, marked, visited, stack);
                    TryVisit(p.x, p.y - 1, g.type, marked, visited, stack);
                }
                groups.Add(g);
            }
            return groups;
        }

        void TryVisit(int nx, int ny, PieceType t, bool[,] marked, bool[,] visited, Stack<GridPos> stack)
        {
            if (nx < 0 || nx >= Width || ny < 0 || ny >= Height) return;
            if (visited[nx, ny] || !marked[nx, ny] || _grid[nx, ny] != t) return;
            visited[nx, ny] = true;
            stack.Push(new GridPos(nx, ny));
        }

        /// <summary>a と b を入れ替えたら揃うか（盤面は変更しない）。</summary>
        public bool WouldMatch(GridPos a, GridPos b)
        {
            Swap(a, b);
            bool ok = HasMatchAt(a) || HasMatchAt(b);
            Swap(a, b);
            return ok;
        }

        bool HasMatchAt(GridPos p)
        {
            var t = this[p];
            if (t == PieceType.None) return false;
            int h = 1, v = 1;
            for (int x = p.x - 1; x >= 0 && _grid[x, p.y] == t; x--) h++;
            for (int x = p.x + 1; x < Width && _grid[x, p.y] == t; x++) h++;
            for (int y = p.y - 1; y >= 0 && _grid[p.x, y] == t; y--) v++;
            for (int y = p.y + 1; y < Height && _grid[p.x, y] == t; y++) v++;
            return h >= MinMatch || v >= MinMatch;
        }

        /// <summary>1手で揃えられる入れ替えが存在するか。</summary>
        public bool HasPossibleMove() => FindHint().HasValue;

        /// <summary>揃えられる入れ替えを1つ返す（ヒント表示用）。無ければ null。</summary>
        public (GridPos a, GridPos b)? FindHint()
        {
            for (int y = 0; y < Height; y++)
            for (int x = 0; x < Width; x++)
            {
                var a = new GridPos(x, y);
                if (x + 1 < Width && WouldMatch(a, new GridPos(x + 1, y))) return (a, new GridPos(x + 1, y));
                if (y + 1 < Height && WouldMatch(a, new GridPos(x, y + 1))) return (a, new GridPos(x, y + 1));
            }
            return null;
        }

        // ------------------------------------------------------------------
        // 消去・落下・補充
        // ------------------------------------------------------------------

        public void Clear(MatchGroup g)
        {
            foreach (var c in g.cells) this[c] = PieceType.None;
        }

        /// <summary>空いたマスに上のピースを詰める。移動したピースの一覧を返す。</summary>
        public List<FallMove> ApplyGravity()
        {
            var moves = new List<FallMove>();
            for (int x = 0; x < Width; x++)
            {
                int write = 0;
                for (int y = 0; y < Height; y++)
                {
                    if (_grid[x, y] == PieceType.None) continue;
                    if (y != write)
                    {
                        _grid[x, write] = _grid[x, y];
                        _grid[x, y] = PieceType.None;
                        moves.Add(new FallMove(new GridPos(x, y), new GridPos(x, write)));
                    }
                    write++;
                }
            }
            return moves;
        }

        /// <summary>空きマスをランダムな色で埋める。生成したピースの一覧を返す。</summary>
        public List<Spawn> Refill()
        {
            var spawns = new List<Spawn>();
            for (int x = 0; x < Width; x++)
            {
                int n = 0;
                for (int y = 0; y < Height; y++)
                {
                    if (_grid[x, y] != PieceType.None) continue;
                    var t = RandomType();
                    _grid[x, y] = t;
                    spawns.Add(new Spawn { to = new GridPos(x, y), type = t, spawnRow = n++ });
                }
            }
            return spawns;
        }

        /// <summary>
        /// 動かせる手が無いときに盤面を並べ替える（色の数は保ったまま）。
        /// 揃っておらず、かつ手がある配置になるまで繰り返す。
        /// </summary>
        public void Shuffle()
        {
            var pool = new List<PieceType>(Width * Height);
            foreach (var t in _grid) pool.Add(t);

            for (int attempt = 0; attempt < 200; attempt++)
            {
                for (int i = pool.Count - 1; i > 0; i--)
                {
                    int j = _rng.Next(i + 1);
                    (pool[i], pool[j]) = (pool[j], pool[i]);
                }
                int k = 0;
                for (int y = 0; y < Height; y++)
                for (int x = 0; x < Width; x++)
                    _grid[x, y] = pool[k++];
                if (FindMatches().Count == 0 && HasPossibleMove()) return;
            }
            // 色の偏りで解けない場合は作り直す
            FillInitial();
        }
    }
}
