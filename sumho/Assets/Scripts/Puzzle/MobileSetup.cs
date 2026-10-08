using UnityEngine;

namespace PrincessCollection.Puzzle
{
    /// <summary>
    /// 起動時にスマホ向けの設定を行う（シーンに置く必要はない）。
    /// </summary>
    public static class MobileSetup
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Init()
        {
            // スマホの既定は 30fps なので、演出がなめらかに見えるよう 60fps にする
            Application.targetFrameRate = 60;

            // 遊んでいる最中に画面が暗くならないようにする
            Screen.sleepTimeout = SleepTimeout.NeverSleep;

            // 縦持ち固定
            Screen.orientation = ScreenOrientation.Portrait;

#if ENABLE_LEGACY_INPUT_MANAGER
            // 2本目の指で誤動作しないよう、1本指だけ受け付ける
            // （Input System 使用時は最初に触れた指だけを追う仕組みなので設定不要）
            Input.multiTouchEnabled = false;
#endif
        }
    }
}
