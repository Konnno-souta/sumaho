using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace PrincessCollection.Battle
{
    /// <summary>HP バー／スキルゲージ共通。fill は Image Type = Filled（横）にしておく。</summary>
    public class HpBarView : MonoBehaviour
    {
        [SerializeField] Image fill;
        [Tooltip("「15620 / 15620」のような数字（空でもよい）")]
        [SerializeField] Text label;
        [SerializeField] float animDuration = 0.3f;

        Coroutine _co;

        public void Set(int current, int max, bool animate = true)
        {
            float target = max > 0 ? Mathf.Clamp01((float)current / max) : 0f;
            if (label != null) label.text = $"{Mathf.Max(0, current)} / {max}";
            if (fill == null) return;
            if (_co != null) StopCoroutine(_co);
            if (!animate || !isActiveAndEnabled) { fill.fillAmount = target; return; }
            _co = StartCoroutine(Animate(target));
        }

        IEnumerator Animate(float target)
        {
            float start = fill.fillAmount;
            for (float t = 0; t < animDuration; t += Time.deltaTime)
            {
                fill.fillAmount = Mathf.Lerp(start, target, 1f - Mathf.Pow(1f - t / animDuration, 3f));
                yield return null;
            }
            fill.fillAmount = target;
        }
    }
}
