using System.Collections.Generic;

namespace PrincessCollection.Puzzle
{
    /// <summary>
    /// 1回の入れ替え（＝1ターン）で起きたことのまとめ。
    /// バトル側はこれを受け取って、属性ごとの攻撃やダメージ計算を行う。
    /// </summary>
    public class TurnResult
    {
        /// <summary>消えたグループの数（＝コンボ数）。連鎖分も含む。</summary>
        public int Combo;

        /// <summary>コンボ倍率。</summary>
        public float Multiplier = 1f;

        /// <summary>属性ごとの消去数。インデックスは (int)PieceType。</summary>
        public readonly int[] ClearedByType;

        /// <summary>属性ごとのコンボ数（その属性で何グループ消したか）。</summary>
        public readonly int[] GroupsByType;

        /// <summary>消えた順のグループ一覧。</summary>
        public readonly List<MatchGroup> Groups = new List<MatchGroup>();

        /// <summary>何段連鎖したか（入れ替え直後の消去を1とする）。</summary>
        public int Chain;

        public TurnResult(int typeCount)
        {
            ClearedByType = new int[typeCount];
            GroupsByType = new int[typeCount];
        }

        public int TotalCleared
        {
            get
            {
                int n = 0;
                foreach (var c in ClearedByType) n += c;
                return n;
            }
        }

        internal void Add(MatchGroup g)
        {
            Groups.Add(g);
            ClearedByType[(int)g.type] += g.Count;
            GroupsByType[(int)g.type]++;
            Combo++;
        }
    }
}
