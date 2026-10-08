using System.Collections.Generic;
using PrincessCollection.Puzzle;
using UnityEngine;

namespace PrincessCollection.Battle
{
    /// <summary>ステージ1つ分（背景と WAVE 構成）。</summary>
    [CreateAssetMenu(menuName = "Princess Collection/Stage", fileName = "Stage")]
    public class StageData : ScriptableObject
    {
        public string stageName = "1-1 はじまりの森";
        public Sprite titleSprite;
        public Sprite background;
        [Tooltip("最後の WAVE を「BOSS」と表示する")] public bool lastWaveIsBoss = true;
        public List<Wave> waves = new List<Wave>();
    }
}
