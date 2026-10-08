using System.IO;
using PrincessCollection.Puzzle;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace PrincessCollection.EditorTools
{
    /// <summary>
    /// メニュー「Tools > Princess Collection > パズル確認シーンを作成」で、
    /// スマホ縦持ち用のレイアウト（盤面・コンボ表示・セーフエリア）を配置したシーンを自動生成する。
    ///
    /// レイアウトは UI（基準 1080×1920、横幅基準）で決め、盤面はその「BoardSlot」に追従する。
    ///   ┌──────────────┐
    ///   │ バトル画面エリア（今後）   │ ← 縦長の端末ほど広がる
    ///   ├──────────────┤
    ///   │ 盤面 6×6（画面幅いっぱい） │ BoardSlot
    ///   ├──────────────┤
    ///   │ ○ Combo! ×1.5             │ ComboView
    ///   ├──────────────┤
    ///   │ オート/倍速/アイテム/メニュー│ ButtonArea（今後）
    ///   └──────────────┘
    /// </summary>
    public static class PuzzleSceneBuilder
    {
        const string Root = "Assets/PrincessCollection";
        const string ScenePath = Root + "/Scenes/PuzzleTest.unity";
        const string FramePath = Root + "/Sprites/Battle/board_frame.png";

        static readonly string[] StonePaths =
        {
            Root + "/Sprites/Stone/01.png", // 火
            Root + "/Sprites/Stone/02.png", // 水
            Root + "/Sprites/Stone/03.png", // 木
            Root + "/Sprites/Stone/04.png", // 光
            Root + "/Sprites/Stone/05.png", // 闇
        };

        // UI レイアウト（基準解像度 1080×1920 での px）
        const float RefWidth = 1080f, RefHeight = 1920f;
        const float ButtonAreaHeight = 170f;
        const float ComboAreaHeight = 150f;
        const float BoardSideMargin = 20f;

        [MenuItem("Tools/Princess Collection/パズル確認シーンを作成")]
        public static void Build()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            foreach (var p in StonePaths) EnsureSprite(p);
            EnsureSprite(FramePath);

            var stones = new Sprite[StonePaths.Length];
            for (int i = 0; i < StonePaths.Length; i++)
            {
                stones[i] = AssetDatabase.LoadAssetAtPath<Sprite>(StonePaths[i]);
                if (stones[i] == null)
                {
                    EditorUtility.DisplayDialog("Princess Collection",
                        $"石の画像が見つかりません:\n{StonePaths[i]}\n\nSprites/Stone に 01〜05.png を置いてください。", "OK");
                    return;
                }
            }
            var frame = AssetDatabase.LoadAssetAtPath<Sprite>(FramePath);

            ApplyMobilePlayerSettings();
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // ---- カメラ ----
            var camGo = new GameObject("Main Camera") { tag = "MainCamera" };
            camGo.transform.position = new Vector3(0, 0, -10);
            var cam = camGo.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = 10f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.16f, 0.12f, 0.18f);
            camGo.AddComponent<AudioListener>();

            // ---- UI（レイアウトの基準）----
            var safe = BuildCanvas(cam);
            var boardSlot = BuildBoardSlot(safe);

            // ---- 盤面 ----
            var boardGo = new GameObject("Board");
            if (frame != null)
            {
                var frameGo = new GameObject("Frame");
                frameGo.transform.SetParent(boardGo.transform, false);
                var sr = frameGo.AddComponent<SpriteRenderer>();
                sr.sprite = frame;
                sr.sortingOrder = 0;
            }

            var board = boardGo.AddComponent<Board>();
            var so = new SerializedObject(board);
            var arr = so.FindProperty("pieceSprites");
            arr.arraySize = stones.Length;
            for (int i = 0; i < stones.Length; i++) arr.GetArrayElementAtIndex(i).objectReferenceValue = stones[i];
            so.ApplyModifiedPropertiesWithoutUndo();

            var fitter = boardGo.AddComponent<BoardFitter>();
            SetRef(fitter, "slot", boardSlot);

            var input = boardGo.AddComponent<BoardInput>();
            SetRef(input, "board", board);
            SetRef(input, "cam", cam);

            var log = boardGo.AddComponent<PuzzleDebugLog>();
            SetRef(log, "board", board);

            // ---- コンボ表示 ----
            BuildComboView(safe, board);

            // ---- 保存 ----
            Directory.CreateDirectory(Path.GetDirectoryName(ScenePath));
            EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene(), ScenePath);
            AddToBuildSettings(ScenePath);
            Selection.activeGameObject = boardGo;
            Debug.Log($"[Princess Collection] シーンを作成しました: {ScenePath}\n" +
                      "Game ビューの解像度を 1080x1920（または Device Simulator でスマホ機種）にして再生してください。");
        }

        // ------------------------------------------------------------------

        static RectTransform BuildCanvas(Camera cam)
        {
            var canvasGo = new GameObject("Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = cam;
            canvas.planeDistance = 5f;
            canvas.sortingOrder = 100;

            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(RefWidth, RefHeight);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0f; // 横幅基準（縦長の端末では上のバトルエリアが広がる）

            var safe = NewRect("SafeArea", canvasGo.transform);
            safe.anchorMin = Vector2.zero;
            safe.anchorMax = Vector2.one;
            safe.offsetMin = safe.offsetMax = Vector2.zero;
            safe.gameObject.AddComponent<SafeArea>();

            // 今後ボタン（オート・倍速・アイテム・メニュー）を置く場所
            var buttons = NewRect("ButtonArea", safe);
            buttons.anchorMin = new Vector2(0, 0);
            buttons.anchorMax = new Vector2(1, 0);
            buttons.pivot = new Vector2(0.5f, 0);
            buttons.sizeDelta = new Vector2(0, ButtonAreaHeight);
            buttons.anchoredPosition = Vector2.zero;

            // 今後バトル画面（敵・味方アイコン・HP）を置く場所の目印
            var top = NewRect("BattleArea(今後)", safe);
            top.anchorMin = new Vector2(0, 1);
            top.anchorMax = new Vector2(1, 1);
            top.pivot = new Vector2(0.5f, 1);
            top.sizeDelta = new Vector2(0, 120);
            top.anchoredPosition = new Vector2(0, -40);
            var label = CreateText("Label", top, LoadBuiltinFont(), 40, Vector2.zero, new Color(1, 1, 1, 0.35f));
            label.text = "バトル画面エリア（今後）";
            label.fontStyle = FontStyle.Normal;
            Object.DestroyImmediate(label.GetComponent<Outline>());

            return safe;
        }

        static RectTransform BuildBoardSlot(RectTransform safe)
        {
            var slot = NewRect("BoardSlot", safe);
            slot.anchorMin = new Vector2(0, 0);
            slot.anchorMax = new Vector2(1, 0);
            slot.pivot = new Vector2(0.5f, 0);
            slot.offsetMin = new Vector2(BoardSideMargin, 0);
            slot.offsetMax = new Vector2(-BoardSideMargin, 0);
            slot.anchoredPosition = new Vector2(0, ButtonAreaHeight + ComboAreaHeight);
            var arf = slot.gameObject.AddComponent<AspectRatioFitter>();
            arf.aspectMode = AspectRatioFitter.AspectMode.WidthControlsHeight;
            arf.aspectRatio = 1f;
            return slot;
        }

        static void BuildComboView(RectTransform safe, Board board)
        {
            var font = LoadBuiltinFont();
            var root = NewRect("ComboView", safe);
            root.anchorMin = new Vector2(0.5f, 0);
            root.anchorMax = new Vector2(0.5f, 0);
            root.pivot = new Vector2(0.5f, 0.5f);
            root.sizeDelta = new Vector2(900, ComboAreaHeight);
            root.anchoredPosition = new Vector2(0, ButtonAreaHeight + ComboAreaHeight * 0.5f);

            var combo = CreateText("ComboText", root, font, 92, new Vector2(-200, 0), new Color(1f, 0.92f, 0.5f));
            combo.alignment = TextAnchor.MiddleRight;   // 右端 x=+150
            var mult = CreateText("MultiplierText", root, font, 64, new Vector2(530, -10), Color.white);
            mult.alignment = TextAnchor.MiddleLeft;     // 左端 x=+180

            var view = root.gameObject.AddComponent<ComboView>();
            SetRef(view, "board", board);
            SetRef(view, "comboText", combo);
            SetRef(view, "multiplierText", mult);
        }

        // ------------------------------------------------------------------

        [MenuItem("Tools/Princess Collection/スマホ向けプレイヤー設定を適用")]
        public static void ApplyMobilePlayerSettings()
        {
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
            PlayerSettings.allowedAutorotateToPortrait = true;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            PlayerSettings.allowedAutorotateToLandscapeLeft = false;
            PlayerSettings.allowedAutorotateToLandscapeRight = false;
        }

        static RectTransform NewRect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            return rt;
        }

        static Text CreateText(string name, Transform parent, Font font, int size, Vector2 pos, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Text), typeof(Outline));
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            rt.sizeDelta = new Vector2(700, size + 30);
            rt.anchoredPosition = pos;
            var t = go.GetComponent<Text>();
            t.font = font;
            t.fontSize = size;
            t.fontStyle = FontStyle.BoldAndItalic;
            t.alignment = TextAnchor.MiddleCenter;
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            t.color = color;
            t.raycastTarget = false;
            var o = go.GetComponent<Outline>();
            o.effectColor = new Color(0.25f, 0.1f, 0.05f, 1f);
            o.effectDistance = new Vector2(4, -4);
            return t;
        }

        static Font LoadBuiltinFont()
        {
            Font f = null;
            try { f = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); } catch { }
            if (f == null) { try { f = Resources.GetBuiltinResource<Font>("Arial.ttf"); } catch { } }
            return f;
        }

        static void EnsureSprite(string path)
        {
            if (AssetImporter.GetAtPath(path) is TextureImporter ti && PrincessSpriteImporter.Apply(ti))
                ti.SaveAndReimport();
        }

        static void SetRef(Object target, string field, Object value)
        {
            var so = new SerializedObject(target);
            so.FindProperty(field).objectReferenceValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        static void AddToBuildSettings(string path)
        {
            var scenes = EditorBuildSettings.scenes;
            foreach (var s in scenes) if (s.path == path) return;
            var list = new System.Collections.Generic.List<EditorBuildSettingsScene>(scenes)
            {
                new EditorBuildSettingsScene(path, true)
            };
            EditorBuildSettings.scenes = list.ToArray();
        }
    }
}
