using System.Collections.Generic;
using PrincessCollection.Puzzle;
using UnityEngine;

namespace PrincessCollection.Battle
{
    /// <summary>敵1体分のデータ。</summary>
    [CreateAssetMenu(menuName = "Princess Collection/Enemy", fileName = "Enemy")]
    public class EnemyData : ScriptableObject
    {
        public string displayName = "敵";
        public Element element;
        public Sprite sprite;
        [Tooltip("画像の枠を画面に出す大きさ（UI 基準 px）。場所が足りなければ自動で小さくなる")] public float displaySize = 420f;

        [Header("ステータス")]
        public int hp = 800;
        public int attack = 150;
        [Tooltip("何ターンごとに攻撃してくるか（「あと N」の N の初期値）")] public int turnInterval = 2;
    }
}
