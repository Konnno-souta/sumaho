using UnityEngine;
using UnityEngine.UI;

namespace PrincessCollection.Battle
{
    /// <summary>
    /// ダメージや回復の数字（浮き上がって消える）。
    /// シーンに非表示で置いたものをひな形にして、Spawn で複製する。
    /// </summary>
    [RequireComponent(typeof(Text))]
    public class DamagePopup : MonoBehaviour
    {
        [SerializeField] float lifetime = 0.9f;
        [SerializeField] float rise = 90f;

        Text _text;
        RectTransform _rt;
        Vector2 _start;
        float _t;
        float _baseScale = 1f;

        public DamagePopup Spawn(RectTransform layer, Vector3 worldPos, string text, Color color, float scale = 1f)
        {
            var p = Instantiate(this, layer);
            p.gameObject.SetActive(true);
            p.Init(worldPos, text, color, scale);
            return p;
        }

        void Init(Vector3 worldPos, string text, Color color, float scale)
        {
            _text = GetComponent<Text>();
            _rt = (RectTransform)transform;
            _text.text = text;
            _text.color = color;
            transform.position = worldPos;
            _rt.localPosition = new Vector3(_rt.localPosition.x, _rt.localPosition.y, 0f);
            _baseScale = scale;
            _rt.localScale = Vector3.one * scale;
            _start = _rt.anchoredPosition;
            _t = 0f;
        }

        void Update()
        {
            _t += Time.deltaTime;
            float k = _t / lifetime;
            // 最初にぽんと大きく、そのあと上へ
            float pop = k < 0.15f ? 1f + (1f - k / 0.15f) * 0.6f : 1f;
            _rt.anchoredPosition = _start + Vector2.up * rise * Mathf.Sqrt(Mathf.Clamp01(k));
            _rt.localScale = new Vector3(pop, pop, 1f) * _baseScale;
            var c = _text.color;
            c.a = k < 0.6f ? 1f : 1f - (k - 0.6f) / 0.4f;
            _text.color = c;
            if (_t >= lifetime) Destroy(gameObject);
        }
    }
}
