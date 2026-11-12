using System.Collections;
using System.Collections.Generic;
using PrincessCollection.Battle;
using UnityEngine;
using UnityEngine.UI;

namespace PrincessCollection.Flow
{
    /// <summary>
    /// ステージ選択画面。StageCatalog の中身から、エリア見出しとステージボタンを並べる。
    /// 並べる場所（content）には Vertical Layout Group を付けておくと、上から自動で積まれる。
    /// ボタンは上から順にふわっと出てくる。左下の姫（案内役）が吹き出しでひとこと話す。
    /// </summary>
    public class StageSelectScreen : MonoBehaviour
    {
        [SerializeField] StageCatalog catalog;

        [Header("並べる場所")]
        [SerializeField] ScrollRect scrollRect;
        [Tooltip("Scroll View の中の Content")]
        [SerializeField] RectTransform content;

        [Header("ひな形（非表示で置いておく）")]
        [Tooltip("エリア見出し。Image（エリアの絵）と、子に Text（絵がないとき用）")]
        [SerializeField] GameObject areaTemplate;
        [SerializeField] StageButtonView stageTemplate;

        [Header("ボタン")]
        [SerializeField] Button backButton;

        [Header("案内役の姫（なくてもよい）")]
        [SerializeField] RectTransform guideChara;
        [Tooltip("吹き出し（Image）")]
        [SerializeField] RectTransform bubble;
        [Tooltip("吹き出しの中の Text")]
        [SerializeField] Text bubbleText;

        static readonly string[] Chatter =
        {
            "どのステージに\n行く？",
            "今日もいっしょに\nがんばろうね！",
            "強い敵には\n属性の相性が大事！",
            "コンボをつなげると\nダメージアップ！",
        };

        readonly List<RectTransform> _items = new List<RectTransform>();
        float _t;
        float _shownAt = -1f;   // 並べ終わった時間（ここから出てくる演出を始める）
        Vector2 _guideHome;
        Vector3 _bubbleScale = Vector3.one;

        void Start()
        {
            Time.timeScale = 1f;
            backButton.onClick.AddListener(GameSession.GoTitle);
            areaTemplate.SetActive(false);
            stageTemplate.gameObject.SetActive(false);

            if (guideChara != null) _guideHome = guideChara.anchoredPosition;
            if (bubble != null)
            {
                _bubbleScale = bubble.localScale;
                bubble.localScale = Vector3.zero;
            }

            if (catalog == null)
            {
                Debug.LogError("[StageSelect] StageCatalog が設定されていません。", this);
                return;
            }
            GameSession.Catalog = catalog;
            if (bubbleText != null) bubbleText.text = PickLine();
            StartCoroutine(Build());
        }

        string PickLine()
        {
            var all = catalog.AllStages();
            int cleared = 0;
            foreach (var s in all) if (GameProgress.IsCleared(s)) cleared++;
            if (GameSession.NewlyUnlocked != null) return "新しいステージが\n開いたよ！";
            if (all.Count > 0 && cleared == all.Count) return "ぜんぶクリア！\nすごいね！";
            if (cleared == 0) return "まずは最初の\nステージから！";
            return Chatter[Random.Range(0, Chatter.Length)];
        }

        IEnumerator Build()
        {
            RectTransform focus = null;
            RectTransform lastUnlockedItem = null;

            foreach (var area in catalog.areas)
            {
                // ---- エリア見出し ----
                var header = Instantiate(areaTemplate, content);
                header.SetActive(true);
                var image = header.GetComponent<Image>();
                var label = header.GetComponentInChildren<Text>(true);
                if (image != null)
                {
                    image.sprite = area.titleSprite;
                    image.enabled = area.titleSprite != null;
                    image.preserveAspect = true;
                }
                if (label != null)
                {
                    label.text = area.areaName;
                    label.gameObject.SetActive(area.titleSprite == null);
                }
                _items.Add((RectTransform)header.transform);

                // ---- ステージボタン ----
                foreach (var stage in area.stages)
                {
                    if (stage == null) continue;
                    var view = Instantiate(stageTemplate, content);
                    view.gameObject.SetActive(true);
                    bool unlocked = catalog.IsUnlocked(stage);
                    bool cleared = GameProgress.IsCleared(stage);
                    bool isNew = stage == GameSession.NewlyUnlocked;
                    var s = stage; // ラムダ用にコピー
                    view.Setup(stage, unlocked, cleared, GameProgress.BestTurns(stage), isNew,
                        () => GameSession.PlayStage(s));

                    var rt = (RectTransform)view.transform;
                    _items.Add(rt);
                    if (isNew) focus = rt;
                    if (unlocked) lastUnlockedItem = rt;
                }
            }
            GameSession.NewlyUnlocked = null;

            // 出てくる前は見えないようにしておく
            foreach (var it in _items) Fade(it, 0f, 0f);

            // 並び終わるのを1フレーム待ってから、新しいステージ（なければ最後に遊べるステージ）が見える位置へ
            yield return null;
            Canvas.ForceUpdateCanvases();
            ScrollTo(focus != null ? focus : lastUnlockedItem);
            _shownAt = _t;
        }

        void Update()
        {
            _t += Time.unscaledDeltaTime;

            // ボタンが上から順にふわっと出てくる（大きさと透明度だけ。位置は Layout Group が決める）
            if (_shownAt >= 0f)
            {
                for (int i = 0; i < _items.Count; i++)
                {
                    float p = Mathf.Clamp01((_t - _shownAt - i * 0.06f) / 0.3f);
                    Fade(_items[i], p, EaseOutBack(p));
                }
            }

            // 案内役の姫：ふわふわ
            if (guideChara != null)
                guideChara.anchoredPosition = _guideHome + Vector2.up * Mathf.Sin(_t * 1.8f) * 10f;

            // 吹き出し：少し遅れてぽんと出る
            if (bubble != null)
            {
                float b = Mathf.Clamp01((_t - 0.5f) / 0.3f);
                bubble.localScale = _bubbleScale * EaseOutBack(b);
            }
        }

        static void Fade(RectTransform rt, float alpha, float scaleT)
        {
            var g = rt.GetComponent<CanvasGroup>();
            if (g == null) g = rt.gameObject.AddComponent<CanvasGroup>();
            g.alpha = alpha;
            rt.localScale = Vector3.one * Mathf.LerpUnclamped(0.88f, 1f, scaleT);
        }

        static float EaseOutBack(float x)
        {
            const float c1 = 1.70158f, c3 = c1 + 1f;
            float p = x - 1f;
            return 1f + c3 * p * p * p + c1 * p * p;
        }

        void ScrollTo(RectTransform item)
        {
            if (item == null || scrollRect == null) return;
            var viewport = scrollRect.viewport != null ? scrollRect.viewport : (RectTransform)scrollRect.transform;
            float contentH = content.rect.height;
            float viewH = viewport.rect.height;
            if (contentH <= viewH) return;

            // item の中心が、content の上から何 px のところにあるか
            Vector3 local = content.InverseTransformPoint(item.TransformPoint(item.rect.center));
            float fromTop = contentH * (1f - content.pivot.y) - local.y;
            float target = fromTop - viewH * 0.4f; // 画面の少し上寄りに来るように（下は姫がいるため）
            float n = 1f - Mathf.Clamp01(target / (contentH - viewH));
            scrollRect.verticalNormalizedPosition = n;
        }
    }
}
