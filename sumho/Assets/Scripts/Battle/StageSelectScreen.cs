using System.Collections;
using PrincessCollection.Battle;
using UnityEngine;
using UnityEngine.UI;

namespace PrincessCollection.Flow
{
    /// <summary>
    /// ステージ選択画面。StageCatalog の中身から、エリア見出しとステージボタンを並べる。
    /// 並べる場所（content）には Vertical Layout Group を付けておくと、上から自動で積まれる。
    /// </summary>
    public class StageSelectScreen : MonoBehaviour
    {
        [SerializeField] StageCatalog catalog;

        [Header("並べる場所")]
        [SerializeField] ScrollRect scrollRect;
        [Tooltip("Scroll View の中の Content")]
        [SerializeField] RectTransform content;

        [Header("ひな形（非表示で置いておく）")]
        [Tooltip("エリア見出し。Image（エリア名の画像）と、子に Text（画像がないとき用）")]
        [SerializeField] GameObject areaTemplate;
        [SerializeField] StageButtonView stageTemplate;

        [Header("ボタン")]
        [SerializeField] Button backButton;

        void Start()
        {
            Time.timeScale = 1f;
            backButton.onClick.AddListener(GameSession.GoTitle);
            areaTemplate.SetActive(false);
            stageTemplate.gameObject.SetActive(false);

            if (catalog == null)
            {
                Debug.LogError("[StageSelect] StageCatalog が設定されていません。", this);
                return;
            }
            GameSession.Catalog = catalog;
            StartCoroutine(Build());
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

                    if (isNew) focus = (RectTransform)view.transform;
                    if (unlocked) lastUnlockedItem = (RectTransform)view.transform;
                }
            }
            GameSession.NewlyUnlocked = null;

            // 並び終わるのを1フレーム待ってから、新しいステージ（なければ最後に遊べるステージ）が見える位置へ
            yield return null;
            Canvas.ForceUpdateCanvases();
            ScrollTo(focus != null ? focus : lastUnlockedItem);
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
            float target = fromTop - viewH * 0.5f; // 画面の真ん中に来るように
            float n = 1f - Mathf.Clamp01(target / (contentH - viewH));
            scrollRect.verticalNormalizedPosition = n;
        }
    }
}
