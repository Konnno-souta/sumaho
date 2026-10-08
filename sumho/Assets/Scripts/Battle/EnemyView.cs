using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace PrincessCollection.Battle
{
    /// <summary>敵1体の見た目（絵・HP バー・「あと N」・狙いマーク）。</summary>
    public class EnemyView : MonoBehaviour
    {
        [SerializeField] Image body;
        [SerializeField] HpBarView hpBar;
        [SerializeField] Image attributeIcon;
        [SerializeField] Text countdownText;
        [SerializeField] GameObject targetMarker;
        [SerializeField] Button button;

        CanvasGroup _group;
        RectTransform _bodyRect;
        Vector2 _bodyHome;

        public RectTransform BodyRect => _bodyRect;

        public void Setup(EnemyUnit unit, Sprite attrSprite, float size, Action onTap)
        {
            _group = GetComponent<CanvasGroup>();
            if (_group == null) _group = gameObject.AddComponent<CanvasGroup>();
            _group.alpha = 1f;

            _bodyRect = body.rectTransform;
            body.sprite = unit.data.sprite;
            body.color = Color.white;
            body.preserveAspect = true;
            _bodyRect.sizeDelta = new Vector2(size, size);
            _bodyHome = _bodyRect.anchoredPosition;

            if (attributeIcon != null) attributeIcon.sprite = attrSprite;
            hpBar.Set(unit.hp, unit.data.hp, false);
            Refresh(unit);
            SetTargeted(false);

            button.onClick.RemoveAllListeners();
            if (onTap != null) button.onClick.AddListener(() => onTap());
        }

        public void Refresh(EnemyUnit unit)
        {
            hpBar.Set(unit.hp, unit.data.hp);
            if (countdownText != null)
            {
                countdownText.text = unit.Dead ? "" : $"あと {unit.countdown}";
                countdownText.color = unit.countdown <= 1 ? new Color(1f, 0.35f, 0.3f) : Color.white;
            }
        }

        public void SetTargeted(bool on)
        {
            if (targetMarker != null) targetMarker.SetActive(on);
        }

        public IEnumerator PlayAppear()
        {
            _group.alpha = 0f;
            for (float t = 0; t < 0.3f; t += Time.deltaTime)
            {
                _group.alpha = t / 0.3f;
                _bodyRect.anchoredPosition = _bodyHome + Vector2.up * (1f - t / 0.3f) * 30f;
                yield return null;
            }
            _group.alpha = 1f;
            _bodyRect.anchoredPosition = _bodyHome;
        }

        /// <summary>攻撃を受けた（揺れて赤く光る）。</summary>
        public IEnumerator PlayHit()
        {
            const float dur = 0.25f;
            for (float t = 0; t < dur; t += Time.deltaTime)
            {
                float k = 1f - t / dur;
                _bodyRect.anchoredPosition = _bodyHome + new Vector2(Mathf.Sin(t * 90f) * 12f * k, 0f);
                body.color = Color.Lerp(Color.white, new Color(1f, 0.4f, 0.4f), k);
                yield return null;
            }
            _bodyRect.anchoredPosition = _bodyHome;
            body.color = Color.white;
        }

        /// <summary>攻撃する（下に飛び出して戻る）。</summary>
        public IEnumerator PlayAttack()
        {
            const float dur = 0.3f;
            for (float t = 0; t < dur; t += Time.deltaTime)
            {
                float k = Mathf.Sin(t / dur * Mathf.PI);
                _bodyRect.anchoredPosition = _bodyHome + Vector2.down * k * 40f;
                _bodyRect.localScale = Vector3.one * (1f + k * 0.15f);
                yield return null;
            }
            _bodyRect.anchoredPosition = _bodyHome;
            _bodyRect.localScale = Vector3.one;
        }

        public IEnumerator PlayDie()
        {
            SetTargeted(false);
            if (countdownText != null) countdownText.text = "";
            for (float t = 0; t < 0.4f; t += Time.deltaTime)
            {
                _group.alpha = 1f - t / 0.4f;
                body.color = Color.Lerp(Color.white, new Color(1f, 1f, 1f, 0f), t / 0.4f);
                yield return null;
            }
            _group.alpha = 0f;
            button.interactable = false;
        }
    }
}
