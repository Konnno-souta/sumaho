using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace PrincessCollection.Battle
{
    /// <summary>勝利／敗北の画面。ボタンは2つ（2つ目はなくても動く）。</summary>
    [RequireComponent(typeof(CanvasGroup))]
    public class ResultView : MonoBehaviour
    {
        [SerializeField] Text title;
        [SerializeField] Text message;
        [SerializeField] Button button;
        [SerializeField] Text buttonText;
        [Tooltip("2つ目のボタン（ステージ選択へ など）。なくてもよい")]
        [SerializeField] Button subButton;
        [SerializeField] Text subButtonText;

        CanvasGroup _group;

        void Awake()
        {
            _group = GetComponent<CanvasGroup>();
            Hide();
        }

        void Hide()
        {
            _group.alpha = 0f;
            _group.blocksRaycasts = false;
            _group.interactable = false;
        }

        /// <summary>前のバージョンと同じ呼び方（ボタン1つ）。</summary>
        public IEnumerator Show(bool win, string detail, Action onButton) =>
            Show(win, detail, win ? "もう一度あそぶ" : "リトライ", onButton, null, null);

        public IEnumerator Show(bool win, string detail, string mainLabel, Action onMain, string subLabel, Action onSub)
        {
            title.text = win ? "WIN!" : "全滅…";
            title.color = win ? new Color(1f, 0.85f, 0.25f) : new Color(0.7f, 0.75f, 0.9f);
            message.text = detail;

            buttonText.text = mainLabel;
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => onMain());

            if (subButton != null)
            {
                bool hasSub = onSub != null;
                subButton.gameObject.SetActive(hasSub);
                subButton.onClick.RemoveAllListeners();
                if (hasSub)
                {
                    if (subButtonText != null) subButtonText.text = subLabel;
                    subButton.onClick.AddListener(() => onSub());
                }
            }

            _group.blocksRaycasts = true;
            _group.interactable = true;
            var t = title.transform;
            for (float e = 0; e < 0.4f; e += Time.unscaledDeltaTime)
            {
                float k = e / 0.4f;
                _group.alpha = k;
                t.localScale = Vector3.one * Mathf.Lerp(2f, 1f, 1f - Mathf.Pow(1f - k, 3f));
                yield return null;
            }
            _group.alpha = 1f;
            t.localScale = Vector3.one;
        }
    }
}
