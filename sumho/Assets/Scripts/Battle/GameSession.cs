using PrincessCollection.Battle;
using UnityEngine.SceneManagement;

namespace PrincessCollection.Flow
{
    /// <summary>
    /// シーンをまたいで渡す情報（どのステージを遊ぶか）と、シーンの名前。
    /// static なので、シーンを切り替えても消えない。
    /// </summary>
    public static class GameSession
    {
        // Build Profiles の Scene List に入れたシーンの名前と同じにする
        public const string TitleScene = "Title";
        public const string StageSelectScene = "StageSelect";
        public const string BattleScene = "Main";

        /// <summary>ステージ選択で選んだステージ。null ならバトルシーンに設定してあるステージを使う。</summary>
        public static StageData SelectedStage;

        /// <summary>ステージ選択画面で使っている一覧（次のステージを探すのに使う）。</summary>
        public static StageCatalog Catalog;

        /// <summary>直前にクリアして新しく解放されたステージ（ステージ選択で目立たせる）。</summary>
        public static StageData NewlyUnlocked;

        public static void GoTitle() => SceneManager.LoadScene(TitleScene);
        public static void GoStageSelect() => SceneManager.LoadScene(StageSelectScene);

        public static void PlayStage(StageData stage)
        {
            SelectedStage = stage;
            SceneManager.LoadScene(BattleScene);
        }
    }
}
