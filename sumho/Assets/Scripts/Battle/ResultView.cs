using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace PrincessCollection.Battle
{
    /// <summary>勝利／敗北の画面。</summary>
    [RequireComponent(typeof(CanvasGroup))]
    public class ResultView : MonoBehaviour
    {
        [SerializeField] Text title;
        [SerializeField] Text message;
        [SerializeField] Button button;
        [SerializeField] Text buttonText;

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

        public IEnumerator Show(bool win, string detail, Action onButton)
        {
            title.text = win ? "WIN!" : "全滅…";
            title.color = win ? new Color(1f, 0.85f, 0.25f) : new Color(0.7f, 0.75f, 0.9f);
            message.text = detail;
            buttonText.text = win ? "もう一度あそぶ" : "リトライ";
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => onButton());

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
