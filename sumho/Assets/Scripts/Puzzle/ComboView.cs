using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace PrincessCollection.Puzzle
{
    /// <summary>
    /// 「3 Combo! ×1.5」と「TOTAL DAMAGE」の表示。Board のイベントを受けて動く。
    /// ダメージはバトル側から ShowTotalDamage で渡す（無ければ表示しない）。
    /// </summary>
    public class ComboView : MonoBehaviour
    {
        [SerializeField] Board board;
        [SerializeField] Text comboText;
        [SerializeField] Text multiplierText;
        [Tooltip("「TOTAL DAMAGE」の文字（空でもよい）")]
        [SerializeField] Text damageLabel;
        [Tooltip("ダメージの数字（空でもよい）")]
        [SerializeField] Text damageText;
        [Tooltip("表示を消すまでの秒数")]
        [SerializeField] float hideDelay = 1.2f;

        static readonly Color[] TypeColors =
        {
            new Color(1.00f, 0.45f, 0.35f), // 火
            new Color(0.40f, 0.75f, 1.00f), // 水
            new Color(0.45f, 0.90f, 0.45f), // 木
            new Color(1.00f, 0.90f, 0.35f), // 光
            new Color(0.80f, 0.55f, 1.00f), // 闇
        };

        CanvasGroup _group;
        Coroutine _hide, _punch;
        Transform _punchTarget;

        void Awake()
        {
            _group = GetComponent<CanvasGroup>();
            if (_group == null) _group = gameObject.AddComponent<CanvasGroup>();
            _group.alpha = 0f;
            _group.blocksRaycasts = false;
            SetDamageVisible(false);
        }

        void OnEnable()
        {
            if (board == null) return;
            board.ComboStep += OnComboStep;
            board.TurnFinished += OnTurnFinished;
        }

        void OnDisable()
        {
            if (board == null) return;
            board.ComboStep -= OnComboStep;
            board.TurnFinished -= OnTurnFinished;
        }

        void OnComboStep(int combo, MatchGroup group)
        {
            StopHide();
            _group.alpha = 1f;
            if (combo == 1) SetDamageVisible(false);

            if (comboText != null)
            {
                comboText.text = $"{combo} Combo!";
                int t = (int)group.type;
                comboText.color = t >= 0 && t < TypeColors.Length ? TypeColors[t] : Color.white;
            }
            if (multiplierText != null)
                multiplierText.text = $"×{board.ComboMultiplier(combo):0.0#}";

            Punch(comboText != null ? comboText.transform : transform);
        }

        void OnTurnFinished(TurnResult r)
        {
            if (comboText != null) comboText.color = Color.white;
            StopHide();
            _hide = StartCoroutine(HideLater());
        }

        /// <summary>バトル側から、このターンの合計ダメージを渡す。</summary>
        public void ShowTotalDamage(int damage)
        {
            StopHide();
            _group.alpha = 1f;
            SetDamageVisible(true);
            if (damageText != null) damageText.text = damage.ToString("N0");
            Punch(damageText != null ? damageText.transform : transform);
            _hide = StartCoroutine(HideLater());
        }

        void SetDamageVisible(bool on)
        {
            if (damageLabel != null) damageLabel.enabled = on;
            if (damageText != null) damageText.enabled = on;
        }

        void StopHide()
        {
            if (_hide != null) { StopCoroutine(_hide); _hide = null; }
        }

        void Punch(Transform target)
        {
            if (_punch != null) StopCoroutine(_punch);
            if (_punchTarget != null) _punchTarget.localScale = Vector3.one;
            _punchTarget = target;
            _punch = StartCoroutine(PunchRoutine(target));
        }

        IEnumerator PunchRoutine(Transform t)
        {
            const float dur = 0.18f;
            for (float e = 0; e < dur; e += Time.deltaTime)
            {
                float s = 1f + Mathf.Sin(e / dur * Mathf.PI) * 0.25f;
                t.localScale = new Vector3(s, s, 1f);
                yield return null;
            }
            t.localScale = Vector3.one;
        }

        IEnumerator HideLater()
        {
            yield return new WaitForSeconds(hideDelay);
            for (float a = 1f; a > 0f; a -= Time.deltaTime * 4f)
            {
                _group.alpha = a;
                yield return null;
            }
            _group.alpha = 0f;
        }
    }
}
