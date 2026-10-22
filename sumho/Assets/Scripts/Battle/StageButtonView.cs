using System;
using PrincessCollection.Battle;
using UnityEngine;
using UnityEngine.UI;

namespace PrincessCollection.Flow
{
    /// <summary>ステージ選択のボタン1つ分。</summary>
    [RequireComponent(typeof(Button))]
    public class StageButtonView : MonoBehaviour
    {
        [SerializeField] Text nameText;
        [Tooltip("右側の文字（★クリア／NEW／未解放）")]
        [SerializeField] Text statusText;
        [Tooltip("未解放のとき表示するもの（鍵のマークなど）。なくてもよい")]
        [SerializeField] GameObject lockMark;

        static readonly Color Normal = new Color(0.32f, 0.24f, 0.42f);
        static readonly Color Cleared = new Color(0.24f, 0.36f, 0.5f);
        static readonly Color Locked = new Color(0.2f, 0.2f, 0.22f);
        static readonly Color NewColor = new Color(0.72f, 0.36f, 0.5f);

        public void Setup(StageData stage, bool unlocked, bool cleared, int bestTurns, bool isNew, Action onClick)
        {
            var button = GetComponent<Button>();
            nameText.text = unlocked ? stage.stageName : "？？？";
            nameText.color = unlocked ? Color.white : new Color(1f, 1f, 1f, 0.4f);

            if (!unlocked) statusText.text = "";
            else if (cleared) statusText.text = $"★ クリア\n{bestTurns} ターン";
            else statusText.text = "NEW";
            statusText.color = cleared ? new Color(1f, 0.85f, 0.3f) : new Color(1f, 0.6f, 0.75f);

            if (lockMark != null) lockMark.SetActive(!unlocked);

            button.image.color = !unlocked ? Locked : isNew ? NewColor : cleared ? Cleared : Normal;
            button.interactable = unlocked;
            button.onClick.RemoveAllListeners();
            if (unlocked) button.onClick.AddListener(() => onClick());
        }
    }
}
