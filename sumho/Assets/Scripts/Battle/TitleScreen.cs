using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace PrincessCollection.Flow
{
    /// <summary>
    /// タイトル画面。画面のどこかをタップするとステージ選択へ。
    /// 「TAP TO START」の点滅と、姫たちのふわふわ動きもここで行う。
    /// </summary>
    public class TitleScreen : MonoBehaviour
    {
        [Tooltip("画面全体をおおう透明なボタン")]
        [SerializeField] Button startButton;
        [Tooltip("点滅させる文字")]
        [SerializeField] Text tapText;
        [Tooltip("ふわふわ上下させる画像（姫など）。空でもよい")]
        [SerializeField] RectTransform[] floaters = new RectTransform[0];

        [Header("セーブデータ消去（なくてもよい）")]
        [SerializeField] Button resetButton;
        [SerializeField] Text resetText;

        Vector2[] _home;
        bool _starting;
        int _resetTaps;

        void Start()
        {
            Time.timeScale = 1f;
            startButton.onClick.AddListener(OnStart);
            if (resetButton != null) resetButton.onClick.AddListener(OnReset);

            _home = new Vector2[floaters.Length];
            for (int i = 0; i < floaters.Length; i++)
                if (floaters[i] != null) _home[i] = floaters[i].anchoredPosition;
        }

        void Update()
        {
            float t = Time.time;
            // 点滅：1秒周期でうすく → こく
            if (tapText != null)
            {
                var c = tapText.color;
                c.a = _starting ? (Mathf.Repeat(t * 8f, 1f) < 0.5f ? 1f : 0.2f) : 0.35f + 0.65f * (0.5f + 0.5f * Mathf.Sin(t * Mathf.PI * 2f));
                tapText.color = c;
            }
            // ふわふわ：1人ずつ少しずらして上下
            for (int i = 0; i < floaters.Length; i++)
            {
                if (floaters[i] == null) continue;
                floaters[i].anchoredPosition = _home[i] + Vector2.up * Mathf.Sin(t * 2f + i * 1.3f) * 14f;
            }
        }

        void OnStart()
        {
            if (_starting) return;
            _starting = true;
            StartCoroutine(StartRoutine());
        }

        IEnumerator StartRoutine()
        {
            yield return new WaitForSeconds(0.5f); // 文字が速く点滅してから切り替え
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
