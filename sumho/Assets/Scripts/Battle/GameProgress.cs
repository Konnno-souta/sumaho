using PrincessCollection.Battle;
using UnityEngine;

namespace PrincessCollection.Flow
{
    /// <summary>
    /// セーブデータ（どのステージをクリアしたか、最少ターン数）。
    /// PlayerPrefs に「clear_ステージのファイル名」= 最少ターン数 で保存する。
    /// スマホではアプリを消すまで残る。
    /// </summary>
    public static class GameProgress
    {
        static string Key(StageData stage) => "clear_" + stage.name;

        public static bool IsCleared(StageData stage) =>
            stage != null && PlayerPrefs.HasKey(Key(stage));

        /// <summary>クリアしたときの最少ターン数。未クリアなら 0。</summary>
        public static int BestTurns(StageData stage) =>
            stage != null ? PlayerPrefs.GetInt(Key(stage), 0) : 0;

        /// <summary>クリアを記録する。最少ターンを更新したら true。</summary>
        public static bool RecordClear(StageData stage, int turns)
        {
            if (stage == null) return false;
            int best = BestTurns(stage);
            bool isBest = best == 0 || turns < best;
            if (isBest)
            {
                PlayerPrefs.SetInt(Key(stage), turns);
                PlayerPrefs.Save();
            }
            return isBest;
        }

        /// <summary>セーブデータを全部消す（確認用）。</summary>
        public static void ResetAll()
        {
            PlayerPrefs.DeleteAll();
            PlayerPrefs.Save();
        }
    }
}
