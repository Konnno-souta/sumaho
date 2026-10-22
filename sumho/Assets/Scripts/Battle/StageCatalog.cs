using System.Collections.Generic;
using PrincessCollection.Battle;
using UnityEngine;

namespace PrincessCollection.Flow
{
    /// <summary>
    /// ステージの一覧。エリア（エメラルドフォレスト など）ごとに、ステージを遊ぶ順に並べる。
    /// 上から順番に解放される（前のステージをクリアすると次が遊べる）。
    /// </summary>
    [CreateAssetMenu(menuName = "Princess Collection/Stage Catalog", fileName = "StageCatalog")]
    public class StageCatalog : ScriptableObject
    {
        [System.Serializable]
        public class Area
        {
            public string areaName = "エメラルドフォレスト";
            [Tooltip("エリア名の画像（gametitle_1 など）。空なら areaName を文字で出す")]
            public Sprite titleSprite;
            public List<StageData> stages = new List<StageData>();
        }

        public List<Area> areas = new List<Area>();

        /// <summary>全ステージを遊ぶ順に並べたもの。</summary>
        public List<StageData> AllStages()
        {
            var list = new List<StageData>();
            foreach (var a in areas)
                foreach (var s in a.stages)
                    if (s != null) list.Add(s);
            return list;
        }

        /// <summary>このステージの次のステージ。最後なら null。</summary>
        public StageData Next(StageData stage)
        {
            var all = AllStages();
            int i = all.IndexOf(stage);
            return i >= 0 && i + 1 < all.Count ? all[i + 1] : null;
        }

        /// <summary>遊べるか（最初のステージ、または1つ前をクリア済み）。</summary>
        public bool IsUnlocked(StageData stage)
        {
            var all = AllStages();
            int i = all.IndexOf(stage);
            if (i <= 0) return true;
            return GameProgress.IsCleared(all[i - 1]);
        }
    }
}
