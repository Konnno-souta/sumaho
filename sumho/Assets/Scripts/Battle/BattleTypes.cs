using System.Collections.Generic;
using PrincessCollection.Puzzle;
using UnityEngine;

namespace PrincessCollection.Battle
{
    /// <summary>キャラクターの属性。値は PieceType の火・水・木と同じ。</summary>
    public enum Element
    {
        Fire = 0,
        Water = 1,
        Wood = 2,
    }

    public enum SkillType
    {
        AttackAll,     // 敵全体に攻撃
        AttackSingle,  // 狙っている敵1体に大ダメージ
        Heal,          // HP を回復
    }

    public static class ElementUtil
    {
        /// <summary>属性相性。火→木→水→火 の順に有利。</summary>
        public static float Affinity(Element attacker, Element defender, float strong = 1.5f, float weak = 0.5f)
        {
            if (Beats(attacker, defender)) return strong;
            if (Beats(defender, attacker)) return weak;
            return 1f;
        }

        public static bool Beats(Element a, Element b)
        {
            return (a == Element.Fire && b == Element.Wood)
                || (a == Element.Wood && b == Element.Water)
                || (a == Element.Water && b == Element.Fire);
        }

        public static PieceType ToPiece(this Element e) => (PieceType)(int)e;

        public static Color ToColor(this Element e)
        {
            switch (e)
            {
                case Element.Fire: return new Color(1f, 0.45f, 0.3f);
                case Element.Water: return new Color(0.35f, 0.7f, 1f);
                default: return new Color(0.4f, 0.9f, 0.4f);
            }
        }
    }

    [System.Serializable]
    public class Wave
    {
        public List<EnemyData> enemies = new List<EnemyData>();
    }

    // ----------------------------------------------------------------------
    // バトル中の状態
    // ----------------------------------------------------------------------

    public class PrincessUnit
    {
        public readonly PrincessData data;
        public int gauge;
        public PrincessUnit(PrincessData d) { data = d; }
        public Element Element => data.element;
        public bool Ready => gauge >= data.skillGaugeMax;
    }

    public class EnemyUnit
    {
        public readonly EnemyData data;
        public int hp;
        public int countdown;
        public EnemyUnit(EnemyData d)
        {
            data = d;
            hp = d.hp;
            countdown = Mathf.Max(1, d.turnInterval);
        }
        public bool Dead => hp <= 0;
        public Element Element => data.element;
    }
}
