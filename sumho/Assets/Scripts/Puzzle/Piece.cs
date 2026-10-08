using System.Collections;
using UnityEngine;

namespace PrincessCollection.Puzzle
{
    /// <summary>盤面上の1ピースの見た目とアニメーション。</summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public class Piece : MonoBehaviour
    {
        public PieceType Type { get; private set; }

        SpriteRenderer _sr;
        bool _selected;
        float _baseScale = 1f;

        public SpriteRenderer Renderer => _sr != null ? _sr : (_sr = GetComponent<SpriteRenderer>());

        public void Setup(PieceType type, Sprite sprite, float scale, int sortingOrder)
        {
            Type = type;
            _baseScale = scale;
            Renderer.sprite = sprite;
            Renderer.color = Color.white;
            Renderer.sortingOrder = sortingOrder;
            Renderer.maskInteraction = SpriteMaskInteraction.VisibleInsideMask;
            transform.localScale = Vector3.one * scale;
            transform.localRotation = Quaternion.identity;
            _selected = false;
            gameObject.name = $"Piece_{type}";
        }

        // ------------------------------------------------------------------
        // 選択中の強調（ふわふわ拡大）
        // ------------------------------------------------------------------

        public void SetSelected(bool selected)
        {
            _selected = selected;
            if (!selected) transform.localScale = Vector3.one * _baseScale;
        }

        void Update()
        {
            if (!_selected) return;
            float s = _baseScale * (1.12f + Mathf.Sin(Time.time * 10f) * 0.04f);
            transform.localScale = new Vector3(s, s, 1f);
        }

        // ------------------------------------------------------------------
        // アニメーション（コルーチン）
        // ------------------------------------------------------------------

        /// <summary>なめらかに移動（入れ替え用）。</summary>
        public IEnumerator MoveTo(Vector3 target, float duration)
        {
            Vector3 start = transform.localPosition;
            for (float t = 0; t < duration; t += Time.deltaTime)
            {
                float k = Ease.OutCubic(t / duration);
                transform.localPosition = Vector3.LerpUnclamped(start, target, k);
                yield return null;
            }
            transform.localPosition = target;
        }

        /// <summary>落下（加速して着地後に少し弾む）。</summary>
        public IEnumerator FallTo(Vector3 target, float duration, float delay)
        {
            if (delay > 0f) yield return new WaitForSeconds(delay);
            Vector3 start = transform.localPosition;
            for (float t = 0; t < duration; t += Time.deltaTime)
            {
                float k = Ease.InQuad(t / duration);
                transform.localPosition = Vector3.LerpUnclamped(start, target, k);
                yield return null;
            }
            transform.localPosition = target;

            // 着地の弾み（つぶれて戻る）
            const float squash = 0.12f;
            for (float t = 0; t < squash; t += Time.deltaTime)
            {
                float k = Mathf.Sin(t / squash * Mathf.PI) * 0.12f;
                transform.localScale = new Vector3(_baseScale * (1f + k), _baseScale * (1f - k), 1f);
                yield return null;
            }
            transform.localScale = Vector3.one * _baseScale;
        }

        /// <summary>消去（ふくらんで消える）。</summary>
        public IEnumerator PlayClear(float duration)
        {
            Color c = Color.white;
            for (float t = 0; t < duration; t += Time.deltaTime)
            {
                float k = t / duration;
                float s = _baseScale * (1f + Ease.OutCubic(k) * 0.45f);
                transform.localScale = new Vector3(s, s, 1f);
                c.a = 1f - Ease.InQuad(k);
                Renderer.color = c;
                yield return null;
            }
            c.a = 0f;
            Renderer.color = c;
        }

        /// <summary>出現（シャッフル時など）。</summary>
        public IEnumerator PlayAppear(float duration, float delay)
        {
            transform.localScale = Vector3.zero;
            if (delay > 0f) yield return new WaitForSeconds(delay);
            for (float t = 0; t < duration; t += Time.deltaTime)
            {
                float s = _baseScale * Ease.OutBack(t / duration);
                transform.localScale = new Vector3(s, s, 1f);
                yield return null;
            }
            transform.localScale = Vector3.one * _baseScale;
        }
    }

    /// <summary>簡易イージング関数。</summary>
    public static class Ease
    {
        public static float OutCubic(float t) { t = Mathf.Clamp01(t); return 1f - Mathf.Pow(1f - t, 3f); }
        public static float InQuad(float t) { t = Mathf.Clamp01(t); return t * t; }
        public static float OutBack(float t)
        {
            t = Mathf.Clamp01(t);
            const float c1 = 1.70158f, c3 = c1 + 1f;
            return 1f + c3 * Mathf.Pow(t - 1f, 3f) + c1 * Mathf.Pow(t - 1f, 2f);
        }
    }
}
