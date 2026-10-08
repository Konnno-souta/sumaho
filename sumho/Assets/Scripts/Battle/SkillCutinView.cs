using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace PrincessCollection.Battle
{
    /// <summary>スキル発動時のカットイン（帯が横から入って、姫とスキル名を出す）。</summary>
    [RequireComponent(typeof(CanvasGroup))]
    public class SkillCutinView : MonoBehaviour
    {
        [SerializeField] RectTransform band;
        [SerializeField] Image bandImage;
        [SerializeField] Image portrait;
        [SerializeField] Text skillName;

        CanvasGroup _group;

        void Awake()
        {
            _group = GetComponent<CanvasGroup>();
            _group.alpha = 0f;
            _group.blocksRaycasts = false;
        }

        public IEnumerator Show(Sprite face, string name, Color color)
        {
            portrait.sprite = face;
            portrait.preserveAspect = true;
            skillName.text = name;
            bandImage.color = new Color(color.r * 0.5f, color.g * 0.5f, color.b * 0.5f, 0.85f);

            float width = ((RectTransform)transform).rect.width;
            Vector2 home = band.anchoredPosition;
            _group.alpha = 1f;

            for (float e = 0; e < 0.2f; e += Time.deltaTime)
            {
                band.anchoredPosition = home + Vector2.left * width * (1f - e / 0.2f);
                yield return null;
            }
            band.anchoredPosition = home;
            yield return new WaitForSeconds(0.6f);
            for (float e = 0; e < 0.2f; e += Time.deltaTime)
            {
                band.anchoredPosition = home + Vector2.right * width * (e / 0.2f);
                _group.alpha = 1f - e / 0.2f;
                yield return null;
            }
            band.anchoredPosition = home;
            _group.alpha = 0f;
        }
    }
}
