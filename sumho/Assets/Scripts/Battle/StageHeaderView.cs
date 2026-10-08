using UnityEngine;
using UnityEngine.UI;

namespace PrincessCollection.Battle
{
    /// <summary>画面上部のステージ名と WAVE 表示。</summary>
    public class StageHeaderView : MonoBehaviour
    {
        [SerializeField] Image titleImage;
        [SerializeField] Text stageText;
        [SerializeField] Text waveText;

        public void SetStage(StageData stage)
        {
            if (titleImage != null)
            {
                titleImage.sprite = stage.titleSprite;
                titleImage.enabled = stage.titleSprite != null;
                titleImage.preserveAspect = true;
            }
            if (stageText != null) stageText.text = stage.stageName;
        }

        public void SetWave(int current, int total)
        {
            if (waveText != null) waveText.text = $"WAVE {current}/{total}";
        }
    }
}
