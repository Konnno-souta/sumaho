using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace PrincessCollection.Battle
{
    /// <summary>
    /// パーティ欄の姫1人分（アイコン＝スキルボタン）。
    /// ゲージが満タンになると光り、タップでスキル発動。
    /// </summary>
    public class PrincessSlotView : MonoBehaviour
    {
        [SerializeField] Image icon;
        [SerializeField] Image glow;
        [SerializeField] HpBarView gauge;
        [SerializeField] Text readyText;
        [SerializeField] Button button;

        bool _ready;
        RectTransform _iconRect;
        Vector2 _iconHome;

        public RectTransform IconRect => _iconRect;

        public void Setup(PrincessUnit unit, Action onTap)
        {
            _iconRect = icon.rectTransform;
            _iconHome = _iconRect.anchoredPosition;
            icon.sprite = unit.data.icon;
            icon.preserveAspect = true;
            button.onClick.RemoveAllListeners();
            if (onTap != null) button.onClick.AddListener(() => onTap());
            SetGauge(unit, false);
        }

        public void SetGauge(PrincessUnit unit, bool animate = true)
        {
            gauge.Set(Mathf.Min(unit.gauge, unit.data.skillGaugeMax), unit.data.skillGaugeMax, animate);
            _ready = unit.Ready;
            if (glow != null) glow.enabled = _ready;
            if (readyText != null) readyText.enabled = _ready;
        }

        void Update()
        {
            if (!_ready) return;
            float k = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 6f);
            if (glow != null) glow.color = new Color(1f, 0.9f, 0.3f, 0.45f + 0.5f * k);
            if (readyText != null) readyText.transform.localScale = Vector3.one * (1f + 0.08f * k);
        }

        /// <summary>攻撃（ぴょんと跳ねる）。</summary>
        public IEnumerator PlayAttack()
        {
            const float dur = 0.22f;
            for (float t = 0; t < dur; t += Time.deltaTime)
            {
                _iconRect.anchoredPosition = _iconHome + Vector2.up * Mathf.Sin(t / dur * Mathf.PI) * 36f;
                yield return null;
            }
            _iconRect.anchoredPosition = _iconHome;
        }

        /// <summary>ダメージを受けた（横に揺れる）。</summary>
        public IEnumerator PlayDamaged()
        {
            const float dur = 0.25f;
            for (float t = 0; t < dur; t += Time.deltaTime)
            {
                _iconRect.anchoredPosition = _iconHome + Vector2.right * Mathf.Sin(t * 80f) * 10f * (1f - t / dur);
                yield return null;
            }
            _iconRect.anchoredPosition = _iconHome;
        }
    }
}
