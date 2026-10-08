using System.Collections;
using System.Text;
using UnityEngine;

namespace PrincessCollection.Puzzle
{
    /// <summary>
    /// ターン結果をコンソールに出す確認用コンポーネント。
    /// バトル側をつなぐときは、ここと同じように board.TurnFinished を購読して使う。
    /// </summary>
    public class PuzzleDebugLog : MonoBehaviour
    {
        [SerializeField] Board board;

        static readonly string[] Names = { "火", "水", "木", "光", "闇" };

        void OnEnable()
        {
            if (board == null) return;
            board.TurnFinished += OnTurnFinished;
            board.Shuffled += OnShuffled;
        }

        void OnDisable()
        {
            if (board == null) return;
            board.TurnFinished -= OnTurnFinished;
            board.Shuffled -= OnShuffled;
        }

        IEnumerator Start()
        {
            // 盤面ができるのを1フレーム待ってから、石の画像の内訳を出す
            yield return null;
            if (board != null) Debug.Log("[Puzzle] 開始時の盤面 " + board.DescribePieces());
        }

        void OnTurnFinished(TurnResult r)
        {
            var sb = new StringBuilder();
            sb.Append($"[Puzzle] {r.Combo}コンボ ×{r.Multiplier:0.0#}  連鎖{r.Chain}段  消去{r.TotalCleared}個 / ");
            for (int i = 0; i < r.ClearedByType.Length; i++)
            {
                if (r.ClearedByType[i] == 0) continue;
                string n = i < Names.Length ? Names[i] : i.ToString();
                sb.Append($"{n}:{r.ClearedByType[i]}個({r.GroupsByType[i]}) ");
            }
            Debug.Log(sb.ToString());
            Debug.Log("[Puzzle] " + board.DescribePieces());
        }

        void OnShuffled() => Debug.Log("[Puzzle] 手詰まりのため盤面を並べ替えました");
    }
}
