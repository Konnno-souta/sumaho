using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace PrincessCollection.Battle
{
    /// <summary>「WAVE 1/3」「BOSS」などの大きな文字を一瞬出す。</summary>
    [RequireComponent(typeof(CanvasGroup))]
    public class BannerView : MonoBehaviour
    {
        [SerializeField] Text text;

        CanvasGroup _group;

        void Awake()
        {
            _group = GetComponent<CanvasGroup>();
            _group.alpha = 0f;
            _group.blocksRaycasts = false;
        }

        public IEnumerator Show(string message, Color color, float hold = 0.7f)
        {
            text.text = message;
            text.color = color;
            var t = transform;
            for (float e = 0; e < 0.2f; e += Time.deltaTime)
            {
                float k = e / 0.2f;
                _group.alpha = k;
                t.localScale = Vector3.one * Mathf.Lerp(1.6f, 1f, k);
                yield return null;
            }
            _group.alpha = 1f;
            t.localScale = Vector3.one;
            yield return new WaitForSeconds(hold);
            for (float e = 0; e < 0.2f; e += Time.deltaTime)
            {
                _group.alpha = 1f - e / 0.2f;
                yield return null;
            }
            _group.alpha = 0f;
        }
    }
}
