using System.Collections.Generic;
using PrincessCollection.Puzzle;
using UnityEngine;

namespace PrincessCollection.Battle
{
    /// <summary>姫1人分のデータ。Project で右クリック → Create > Princess Collection > Princess。</summary>
    [CreateAssetMenu(menuName = "Princess Collection/Princess", fileName = "Princess")]
    public class PrincessData : ScriptableObject
    {
        public string displayName = "姫";
        public Element element;
        [Tooltip("パーティ欄のアイコン")] public Sprite icon;
        [Tooltip("スキル発動時のカットイン")] public Sprite portrait;

        [Header("ステータス")]
        public int hp = 1200;
        [Tooltip("自分の属性の石を3個消したときの攻撃力")] public int attack = 300;

        [Header("スキル")]
        public string skillName = "スキル";
        public SkillType skillType = SkillType.AttackAll;
        [Tooltip("攻撃なら攻撃力の何倍か。回復なら最大HPの何割か")] public float skillPower = 3f;
        [Tooltip("ゲージが満タンになるまでに消す、自分の属性の石の数")] public int skillGaugeMax = 12;
    }
}
