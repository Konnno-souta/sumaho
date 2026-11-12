using System;
using PrincessCollection.Battle;
using UnityEngine;
using UnityEngine.UI;

namespace PrincessCollection.Flow
{
    /// <summary>
    /// ステージ選択のボタン1つ分（飾り枠の版）。
    /// 左の丸いバッジに「1-1」、真ん中にステージ名、右に CLEAR／NEW!／鍵 を出す。
    /// ステージ名は StageData の Stage Name「1-1 はじまりの森」を、最初の空白で2つに分けて使う。
    /// </summary>
    [RequireComponent(typeof(Button))]
    public class StageButtonView : MonoBehaviour
    {
        [Header("文字")]
        [Tooltip("バッジの中の「1-1」")]
        [SerializeField] Text numberText;
        [Tooltip("ステージ名")]
        [SerializeField] Text nameText;
        [Tooltip("最少ターン（クリアしたときだけ出る）")]
        [SerializeField] Text statusText;

        [Header("画像")]
        [Tooltip("左の丸いバッジ")]
        [SerializeField] Image badge;
        [SerializeField] GameObject clearMark;
        [SerializeField] GameObject newMark;
        [SerializeField] GameObject lockMark;

        static readonly Color LockedTint = new Color(0.62f, 0.62f, 0.68f);
        static readonly Color NameColor = new Color(0.35f, 0.2f, 0.09f);
        static readonly Color LockedNameColor = new Color(0.43f, 0.41f, 0.47f);

        bool _pulse;     // 解放されたばかり → NEW! をぴょこぴょこ
        float _shake;    // 鍵つきを押したとき → 横にぶるぶる
        float _t;
        Vector3 _newScale = Vector3.one;
        RectTransform _shakeTarget;
        Vector2 _shakeHome;

        public void Setup(StageData stage, bool unlocked, bool cleared, int bestTurns, bool isNew, Action onClick)
        {
            var button = GetComponent<Button>();

            // 「1-1 はじまりの森」→「1-1」と「はじまりの森」
            string full = stage.stageName ?? "";
            int sp = full.IndexOf(' ');
            if (sp < 0) sp = full.IndexOf('　'); // 全角スペースでもよい
            string number = sp > 0 ? full.Substring(0, sp) : "";
            string title = sp > 0 ? full.Substring(sp + 1).Trim() : full;

            if (numberText != null) numberText.text = number;
            nameText.text = unlocked ? title : "？？？";
            nameText.color = unlocked ? NameColor : LockedNameColor;
            if (statusText != null) statusText.text = cleared ? $"最少 {bestTurns} ターン" : "";

            if (clearMark != null) clearMark.SetActive(cleared);
            if (newMark != null) newMark.SetActive(unlocked && !cleared);
            if (lockMark != null) lockMark.SetActive(!unlocked);

            // 鍵つきは灰色に
            var tint = unlocked ? Color.white : LockedTint;
            button.image.color = tint;
            if (badge != null) badge.color = tint;

            // 押せる見た目のまま、鍵つきは押すとぶるぶるするだけ
            button.interactable = true;
            button.onClick.RemoveAllListeners();
            if (unlocked) button.onClick.AddListener(() => onClick());
            else button.onClick.AddListener(() => _shake = 0.35f);

            _pulse = isNew && newMark != null;
            if (newMark != null) _newScale = newMark.transform.localScale;
            _shakeTarget = lockMark != null ? (RectTransform)lockMark.transform : null;
            if (_shakeTarget != null) _shakeHome = _shakeTarget.anchoredPosition;
        }

        void Update()
        {
            _t += Time.unscaledDeltaTime;

            if (_pulse)
            {
                float s = 1f + 0.12f * Mathf.Abs(Mathf.Sin(_t * 4f));
                newMark.transform.localScale = _newScale * s;
            }

            if (_shakeTarget != null)
            {
                if (_shake > 0f)
                {
                    _shake -= Time.unscaledDeltaTime;
                    _shakeTarget.anchoredPosition = _shakeHome + Vector2.right * Mathf.Sin(_t * 60f) * 10f * (_shake / 0.35f);
                }
                else _shakeTarget.anchoredPosition = _shakeHome;
            }
        }
    }
}
