using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace PrincessCollection.Flow
{
    /// <summary>
    /// タイトル画面（キャラクターが主役の版）。
    /// 再生すると：左右の姫がすべり込む → 真ん中の姫が下から登場 → 白く光ってロゴがドンと出る
    /// → 「画面をタップしてスタート」が点滅。そのあとは光の筋が回り、姫がふわふわ、星がきらきら。
    /// 登場中にタップすると演出を飛ばし、もう一度タップするとステージ選択へ。
    /// </summary>
    public class TitleScreen : MonoBehaviour
    {
        [Header("キャラクター")]
        [SerializeField] RectTransform mainChara;
        [SerializeField] RectTransform leftChara;
        [SerializeField] RectTransform rightChara;

        [Header("演出")]
        [SerializeField] RectTransform logo;
        [Tooltip("ゆっくり回す光の筋")]
        [SerializeField] RectTransform rays;
        [Tooltip("姫の後ろの光")]
        [SerializeField] RectTransform glow;
        [Tooltip("きらきら光る星（いくつでも）")]
        [SerializeField] RectTransform[] sparkles = new RectTransform[0];
        [Tooltip("点滅させるもの（画像でも文字でもよい）")]
        [SerializeField] Graphic tapText;
        [Tooltip("画面全体の白い Image（最初は透明）")]
        [SerializeField] Image flash;

        [Header("ボタン")]
        [Tooltip("画面全体をおおう透明なボタン")]
        [SerializeField] Button startButton;
        [Header("セーブデータ消去（なくてもよい）")]
        [SerializeField] Button resetButton;
        [SerializeField] Text resetText;

        // 登場演出の長さ（秒）
        const float IntroEnd = 1.6f;

        struct Home
        {
            public Vector2 pos;
            public Vector3 scale;
            public Graphic graphic;
            public Color color;
        }

        Home _main, _left, _right, _logo, _rays, _glow;
        float _t;           // 再生してからの時間
        bool _starting;
        int _resetTaps;

        void Start()
        {
            Time.timeScale = 1f;
            startButton.onClick.AddListener(OnTap);
            if (resetButton != null) resetButton.onClick.AddListener(OnReset);

            _main = Remember(mainChara);
            _left = Remember(leftChara);
            _right = Remember(rightChara);
            _logo = Remember(logo);
            _rays = Remember(rays);
            _glow = Remember(glow);
            SetAlpha(flash, 0f);
            if (flash != null) flash.raycastTarget = false;
            Apply(); // 1フレーム目から登場前の状態にしておく
        }

        static Home Remember(RectTransform rt)
        {
            var h = new Home();
            if (rt == null) return h;
            h.pos = rt.anchoredPosition;
            h.scale = rt.localScale;
            h.graphic = rt.GetComponent<Graphic>();
            if (h.graphic != null) h.color = h.graphic.color;
            return h;
        }

        void Update()
        {
            _t += Time.unscaledDeltaTime;
            Apply();
        }

        // ------------------------------------------------------------
        // 見た目を時間 _t に合わせて決める
        // ------------------------------------------------------------
        void Apply()
        {
            float t = _t;

            // 左右の姫：横からすべり込む
            Slide(leftChara, _left, Phase(t, 0.0f, 0.6f), new Vector2(-520f, 0f), 0.0f);
            Slide(rightChara, _right, Phase(t, 0.1f, 0.7f), new Vector2(520f, 0f), 1.4f);

            // 真ん中の姫：下から大きくなりながら
            float m = Phase(t, 0.3f, 0.9f);
            if (mainChara != null)
            {
                float k = EaseOutBack(m);
                mainChara.anchoredPosition = _main.pos + new Vector2(0f, -320f * (1f - k)) + Bob(t, 0.7f, 12f);
                mainChara.localScale = _main.scale * Mathf.LerpUnclamped(0.8f, 1f, k);
                SetAlpha(_main, Mathf.Clamp01(m * 3f));
            }

            // 光の筋：ゆっくり回り続ける
            if (rays != null)
            {
                rays.localEulerAngles = new Vector3(0f, 0f, -t * 6f);
                SetAlpha(_rays, Phase(t, 0.2f, 1.2f));
            }

            // 姫の後ろの光：ふくらんだりしぼんだり
            if (glow != null)
            {
                glow.localScale = _glow.scale * (1f + 0.05f * Mathf.Sin(t * 2.2f));
                SetAlpha(_glow, Phase(t, 0.3f, 1.0f));
            }

            // 白い光：ロゴが出る瞬間に一瞬だけ
            if (!_starting && flash != null)
            {
                float f = t < 0.9f ? 0f : 1f - Phase(t, 0.9f, 1.3f);
                SetAlpha(flash, 0.75f * f);
            }

            // ロゴ：大きい状態からドンと縮んで止まる → そのあとゆっくり呼吸
            float l = Phase(t, 0.9f, 1.35f);
            if (logo != null)
            {
                float k = EaseOutBack(l);
                float breathe = t > IntroEnd ? 1f + 0.015f * Mathf.Sin((t - IntroEnd) * 2.5f) : 1f;
                logo.localScale = _logo.scale * Mathf.LerpUnclamped(1.8f, 1f, k) * breathe;
                logo.anchoredPosition = _logo.pos;
                SetAlpha(_logo, Mathf.Clamp01(l * 2.5f));
            }

            // 星：1つずつずらして、ぱっと光って消える
            for (int i = 0; i < sparkles.Length; i++)
            {
                var s = sparkles[i];
                if (s == null) continue;
                float w = Mathf.Sin(t * (1.6f + 0.37f * i) + i * 1.9f);
                float on = t > 1.0f ? Mathf.Max(0f, w) : 0f;
                s.localScale = Vector3.one * (on * on);
                s.localEulerAngles = new Vector3(0f, 0f, t * 40f + i * 30f);
            }

            // 「画面をタップしてスタート」：登場が終わってから点滅
            if (tapText != null)
            {
                float a;
                if (_starting) a = Mathf.Repeat(t * 8f, 1f) < 0.5f ? 1f : 0.2f;
                else if (t < IntroEnd) a = 0f;
                else a = 0.35f + 0.65f * (0.5f + 0.5f * Mathf.Cos((t - IntroEnd) * Mathf.PI * 2f));
                SetAlpha(tapText, a);
            }
        }

        void Slide(RectTransform rt, Home h, float p, Vector2 from, float bobPhase)
        {
            if (rt == null) return;
            float k = EaseOutBack(p);
            rt.anchoredPosition = h.pos + from * (1f - k) + Bob(_t, bobPhase, 10f);
            SetAlpha(h, Mathf.Clamp01(p * 2f));
        }

        // 登場が終わったあとのふわふわ（登場中は動かさない）
        static Vector2 Bob(float t, float phase, float height)
        {
            float on = Mathf.Clamp01((t - IntroEnd) * 2f);
            return Vector2.up * Mathf.Sin((t - IntroEnd) * 1.8f + phase) * height * on;
        }

        // start〜end の間で 0→1 に進む
        static float Phase(float t, float start, float end) => Mathf.Clamp01((t - start) / (end - start));

        // 少し行き過ぎてから戻る動き
        static float EaseOutBack(float x)
        {
            const float c1 = 1.70158f, c3 = c1 + 1f;
            float p = x - 1f;
            return 1f + c3 * p * p * p + c1 * p * p;
        }

        static void SetAlpha(Home h, float a)
        {
            if (h.graphic == null) return;
            var c = h.color;
            c.a = h.color.a * a;
            h.graphic.color = c;
        }

        static void SetAlpha(Graphic g, float a)
        {
            if (g == null) return;
            var c = g.color;
            c.a = a;
            g.color = c;
        }

        // ------------------------------------------------------------
        // タップ
        // ------------------------------------------------------------
        void OnTap()
        {
            if (_starting) return;
            if (_t < IntroEnd)
            {
                _t = IntroEnd; // 登場中なら演出を飛ばす
                return;
            }
            _starting = true;
            StartCoroutine(StartRoutine());
        }

        IEnumerator StartRoutine()
        {
            // 白くなりながらステージ選択へ
            for (float e = 0f; e < 0.45f; e += Time.unscaledDeltaTime)
            {
                SetAlpha(flash, e / 0.45f);
                yield return null;
            }
            SetAlpha(flash, 1f);
            GameSession.GoStageSelect();
        }

        void OnReset()
        {
            _resetTaps++;
            if (_resetTaps == 1)
            {
                if (resetText != null) resetText.text = "もう一度押すと消去";
                return;
            }
            GameProgress.ResetAll();
            _resetTaps = 0;
            if (resetText != null) resetText.text = "消去しました";
        }
    }
}
