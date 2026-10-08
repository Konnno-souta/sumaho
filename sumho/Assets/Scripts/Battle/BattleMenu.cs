using UnityEngine;
using UnityEngine.UI;

namespace PrincessCollection.Battle
{
    /// <summary>
    /// 画面下のボタン（オート・倍速・アイテム・メニュー）と、ポーズ画面。
    /// 押されたら BattleManager に伝えるだけ。
    /// </summary>
    public class BattleMenu : MonoBehaviour
    {
        [SerializeField] BattleManager battle;

        [Header("下のボタン")]
        [SerializeField] Button autoButton;
        [SerializeField] Text autoText;
        [SerializeField] Button speedButton;
        [SerializeField] Text speedText;
        [SerializeField] Button itemButton;
        [SerializeField] Text itemText;
        [SerializeField] Button menuButton;

        [Header("ポーズ画面")]
        [SerializeField] GameObject pausePanel;
        [SerializeField] Button resumeButton;
        [SerializeField] Button retryButton;

        static readonly Color On = new Color(1f, 0.75f, 0.3f);
        static readonly Color Off = new Color(0.25f, 0.2f, 0.3f);

        void Start()
        {
            autoButton.onClick.AddListener(() => { battle.ToggleAuto(); Refresh(); });
            speedButton.onClick.AddListener(() => { battle.ToggleSpeed(); Refresh(); });
            itemButton.onClick.AddListener(() => { battle.UseItem(); Refresh(); });
            menuButton.onClick.AddListener(() => SetPaused(true));
            resumeButton.onClick.AddListener(() => SetPaused(false));
            retryButton.onClick.AddListener(() => { SetPaused(false); battle.Restart(); });
            pausePanel.SetActive(false);
            Refresh();
        }

        void SetPaused(bool paused)
        {
            pausePanel.SetActive(paused);
            battle.SetPaused(paused);
        }

        public void Refresh()
        {
            autoText.text = battle.Auto ? "オート\nON" : "オート";
            autoButton.image.color = battle.Auto ? On : Off;
            speedText.text = battle.Speed > 1f ? "×2" : "×1";
            speedButton.image.color = battle.Speed > 1f ? On : Off;
            itemText.text = $"回復薬\n×{battle.ItemCount}";
            itemButton.interactable = battle.ItemCount > 0;
        }
    }
}
