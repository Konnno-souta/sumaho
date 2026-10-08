using System;
using System.Collections.Generic;
using System.IO;
using PrincessCollection.Battle;
using PrincessCollection.Puzzle;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace PrincessCollection.EditorTools
{
    /// <summary>
    /// メニュー「Tools > Princess Collection > バトルシーンを作成」で、
    /// パズル＋バトル（敵・姫・HP・スキル・WAVE・勝敗・ボタン）を全部配置したシーンを作る。
    /// 姫・敵・ステージのデータは Assets/PrincessCollection/Data に作る（既にあれば上書きしない）。
    ///
    /// 画面の割り振り（UI 基準 1080×1920、下から）
    ///   0〜130     ボタン（オート・倍速・回復薬・メニュー）
    ///   130〜250   コンボ・ダメージ表示
    ///   250〜1290  パズル盤面
    ///   1298〜1340 プレイヤーHP
    ///   1350〜1510 姫3人（スキルボタン）
    ///   1514〜上端-80 敵エリア（16:9 で約330。縦長の端末ほど広い）
    ///   上端 80    ステージ名・WAVE
    /// </summary>
    public static class BattleSceneBuilder
    {
        const string Root = "Assets/PrincessCollection";
        const string SpriteRoot = Root + "/Sprites";
        const string DataRoot = Root + "/Data";
        const string ScenePath = Root + "/Scenes/Battle.unity";

        const float ButtonH = 130f, ComboH = 120f, BoardSide = 20f;
        const float BoardBottom = ButtonH + ComboH;                  // 250
        const float BoardTop = BoardBottom + 1080f - BoardSide * 2f; // 1290
        const float HpY = BoardTop + 8f, HpH = 42f;                  // 1298〜1340
        const float PartyY = HpY + HpH + 10f, PartyH = 160f;         // 1350〜1510
        const float EnemyY = PartyY + PartyH + 4f;                   // 1514〜
        const float HeaderH = 80f;

        static Font _font;
        static Sprite _uiSprite;

        [MenuItem("Tools/Princess Collection/バトルシーンを作成")]
        public static void Build()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            ImportAllSprites();
            _font = LoadBuiltinFont();
            _uiSprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");

            var stones = new Sprite[5];
            for (int i = 0; i < 5; i++)
            {
                stones[i] = S($"Stone/0{i + 1}.png");
                if (stones[i] == null)
                {
                    EditorUtility.DisplayDialog("Princess Collection",
                        $"素材が見つかりません: {SpriteRoot}/Stone/0{i + 1}.png\n\nzip の Assets/PrincessCollection をそのまま入れてください。", "OK");
                    return;
                }
            }

            var stage = CreateData(out var party);
            PuzzleSceneBuilder.ApplyMobilePlayerSettings();

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

            CreateEventSystem();

            // ---- UI ----
            var canvas = CreateCanvas(cam);
            var safe = Rect("SafeArea", canvas.transform);
            Stretch(safe);
            safe.gameObject.AddComponent<SafeArea>();

            var bg = BuildBackground(safe, stage.background);
            var header = BuildHeader(safe);
            var enemyArea = Rect("EnemyArea", safe);
            Stretch(enemyArea, new Vector2(0, EnemyY), new Vector2(0, -HeaderH));
            var enemyTemplate = BuildEnemyTemplate(enemyArea);
            var slots = BuildParty(safe);
            var hpBar = BuildPlayerHp(safe);
            var boardSlot = BuildBoardSlot(safe);
            var combo = BuildComboArea(safe);
            var buttons = BuildButtons(safe);

            var banner = BuildBanner(safe);
            var cutin = BuildCutin(safe);
            var fx = Rect("FxLayer", safe);
            Stretch(fx);
            var popup = BuildPopupTemplate(fx);
            var pause = BuildPausePanel(canvas.transform, out var resumeBtn, out var retryBtn);
            var result = BuildResult(canvas.transform);

            // ---- 盤面 ----
            var board = BuildBoard(stones, boardSlot, cam);
            SetRef(combo.view, "board", board);

            // ---- バトル進行 ----
            var bmGo = new GameObject("BattleManager");
            var bm = bmGo.AddComponent<BattleManager>();
            var so = new SerializedObject(bm);
            so.FindProperty("stage").objectReferenceValue = stage;
            SetArray(so.FindProperty("party"), party);
            SetArray(so.FindProperty("elementIcons"), new Object[] { S("UI/gameicon_1.png"), S("UI/icon_Attribute_2.png"), S("UI/icon_Attribute_3.png") });
            so.FindProperty("board").objectReferenceValue = board;
            so.FindProperty("comboView").objectReferenceValue = combo.view;
            so.FindProperty("background").objectReferenceValue = bg;
            so.FindProperty("header").objectReferenceValue = header;
            so.FindProperty("enemyArea").objectReferenceValue = enemyArea;
            so.FindProperty("enemyTemplate").objectReferenceValue = enemyTemplate;
            SetArray(so.FindProperty("slots"), slots);
            so.FindProperty("playerHpBar").objectReferenceValue = hpBar;
            so.FindProperty("fxLayer").objectReferenceValue = fx;
            so.FindProperty("popupTemplate").objectReferenceValue = popup;
            so.FindProperty("banner").objectReferenceValue = banner;
            so.FindProperty("cutin").objectReferenceValue = cutin;
            so.FindProperty("result").objectReferenceValue = result;
            so.FindProperty("menu").objectReferenceValue = buttons.menu;
            so.ApplyModifiedPropertiesWithoutUndo();

            var mso = new SerializedObject(buttons.menu);
            mso.FindProperty("battle").objectReferenceValue = bm;
            mso.FindProperty("pausePanel").objectReferenceValue = pause;
            mso.FindProperty("resumeButton").objectReferenceValue = resumeBtn;
            mso.FindProperty("retryButton").objectReferenceValue = retryBtn;
            mso.ApplyModifiedPropertiesWithoutUndo();

            Directory.CreateDirectory(Path.GetDirectoryName(ScenePath));
            EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene(), ScenePath);
            AddToBuildSettings(ScenePath);
            Selection.activeGameObject = bmGo;
            Debug.Log($"[Princess Collection] バトルシーンを作成しました: {ScenePath}\n" +
                      $"姫・敵・ステージの数値は {DataRoot} のアセットを選ぶと Inspector で変えられます。");
        }

        // ==================================================================
        // データ
        // ==================================================================

        static StageData CreateData(out Object[] party)
        {
            Directory.CreateDirectory(DataRoot);

            var fire = Asset<PrincessData>("Princess_Fire", d =>
            {
                d.displayName = "ルビー姫"; d.element = Element.Fire;
                d.icon = S("Chara/icon_chara_b_10.png"); d.portrait = S("Chara/chara_player_10.png");
                d.hp = 1200; d.attack = 320;
                d.skillName = "フレイムバースト"; d.skillType = SkillType.AttackAll; d.skillPower = 2.5f; d.skillGaugeMax = 12;
            });
            var water = Asset<PrincessData>("Princess_Water", d =>
            {
                d.displayName = "サファイア姫"; d.element = Element.Water;
                d.icon = S("Chara/icon_chara_b_11.png"); d.portrait = S("Chara/chara_player_11.png");
                d.hp = 1400; d.attack = 280;
                d.skillName = "アクアヒール"; d.skillType = SkillType.Heal; d.skillPower = 0.35f; d.skillGaugeMax = 12;
            });
            var wood = Asset<PrincessData>("Princess_Wood", d =>
            {
                d.displayName = "エメラルド姫"; d.element = Element.Wood;
                d.icon = S("Chara/icon_chara_b_12.png"); d.portrait = S("Chara/chara_player_12.png");
                d.hp = 1300; d.attack = 300;
                d.skillName = "リーフスピア"; d.skillType = SkillType.AttackSingle; d.skillPower = 6f; d.skillGaugeMax = 15;
            });
            party = new Object[] { fire, water, wood };

            string[] names = { "ヒノコ", "ミズノコ", "クサノコ", "ファイアウルフ", "アクアウルフ", "リーフウルフ", "レッドドラゴン", "ブルードラゴン", "グリーンドラゴン" };
            var enemies = new EnemyData[9];
            for (int i = 0; i < 9; i++)
            {
                int idx = i;
                enemies[i] = Asset<EnemyData>($"Enemy_{i + 1}", d =>
                {
                    d.displayName = names[idx];
                    d.element = (Element)(idx % 3);
                    d.sprite = S($"Enemy/chara_enemy_{idx + 1}.png");
                    int tier = idx / 3;
                    d.hp = new[] { 500, 1500, 5000 }[tier];
                    d.attack = new[] { 250, 450, 1100 }[tier];
                    d.turnInterval = new[] { 2, 3, 2 }[tier];
                    // 素材は 200px の同じ枠の中に、小・中・大の大きさで描かれているので、表示枠は全員同じにする
                    d.displaySize = 420f;
                });
            }

            return Asset<StageData>("Stage_1-1", d =>
            {
                d.stageName = "1-1 はじまりの森";
                d.titleSprite = S("UI/gametitle_1.png");
                d.background = S("BattleBG/forest.png");
                d.lastWaveIsBoss = true;
                d.waves = new List<Wave>
                {
                    new Wave { enemies = new List<EnemyData> { enemies[0], enemies[1], enemies[2] } },
                    new Wave { enemies = new List<EnemyData> { enemies[3], enemies[2], enemies[5] } },
                    new Wave { enemies = new List<EnemyData> { enemies[8] } }, // 木のドラゴン（火が有利）
                };
            });
        }

        static T Asset<T>(string name, Action<T> init) where T : ScriptableObject
        {
            string path = $"{DataRoot}/{name}.asset";
            var existing = AssetDatabase.LoadAssetAtPath<T>(path);
            if (existing != null) return existing; // 調整済みの数値を消さない
            var a = ScriptableObject.CreateInstance<T>();
            init(a);
            AssetDatabase.CreateAsset(a, path);
            AssetDatabase.SaveAssets();
            return a;
        }

        // ==================================================================
        // UI の各部
        // ==================================================================

        static Canvas CreateCanvas(Camera cam)
        {
            var go = new GameObject("Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = cam;
            canvas.planeDistance = 5f;
            canvas.sortingOrder = 100;
            var scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0f;
            return canvas;
        }

        static Image BuildBackground(RectTransform safe, Sprite sprite)
        {
            // 盤面の上端から画面の上（ノッチの裏）まで。はみ出した分は RectMask2D で切る
            var area = Rect("BattleBG", safe);
            Stretch(area, new Vector2(0, BoardTop), new Vector2(0, 400));
            area.gameObject.AddComponent<RectMask2D>();
            var img = Img("Image", area, sprite, Color.white);
            Stretch(img.rectTransform);
            var fit = img.gameObject.AddComponent<AspectRatioFitter>();
            fit.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
            fit.aspectRatio = sprite != null ? sprite.rect.width / sprite.rect.height : 800f / 246f;
            img.raycastTarget = false;
            return img;
        }

        static StageHeaderView BuildHeader(RectTransform safe)
        {
            var h = Rect("Header", safe);
            h.anchorMin = new Vector2(0, 1); h.anchorMax = new Vector2(1, 1); h.pivot = new Vector2(0.5f, 1);
            h.sizeDelta = new Vector2(0, HeaderH); h.anchoredPosition = Vector2.zero;
            var back = Img("Back", h, _uiSprite, new Color(0, 0, 0, 0.45f));
            Stretch(back.rectTransform);

            var title = Img("Title", h, S("UI/gametitle_1.png"), Color.white);
            Place(title.rectTransform, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(20, 0), new Vector2(366, 52));
            var stage = Txt("StageText", h, "1-1", 34, TextAnchor.MiddleLeft, Color.white);
            Place(stage.rectTransform, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(400, 0), new Vector2(380, 60));
            var wave = Txt("WaveText", h, "WAVE 1/3", 42, TextAnchor.MiddleRight, new Color(1f, 0.9f, 0.5f));
            Place(wave.rectTransform, new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(-20, 0), new Vector2(300, 60));

            var view = h.gameObject.AddComponent<StageHeaderView>();
            SetRef(view, "titleImage", title);
            SetRef(view, "stageText", stage);
            SetRef(view, "waveText", wave);
            return view;
        }

        static EnemyView BuildEnemyTemplate(RectTransform area)
        {
            var root = Rect("EnemyTemplate", area);
            root.anchorMin = root.anchorMax = new Vector2(0.5f, 0);
            root.pivot = new Vector2(0.5f, 0);
            root.sizeDelta = new Vector2(220, 0);

            var body = Img("Body", root, null, Color.white);
            Place(body.rectTransform, new Vector2(0.5f, 0), new Vector2(0.5f, 0), Vector2.zero, new Vector2(180, 180));
            var btn = body.gameObject.AddComponent<Button>();
            btn.targetGraphic = body;
            btn.transition = Selectable.Transition.None;

            var marker = Txt("TargetMarker", body.rectTransform, "▼", 44, TextAnchor.MiddleCenter, new Color(1f, 0.85f, 0.2f));
            Place(marker.rectTransform, new Vector2(0.5f, 1), new Vector2(0.5f, 0.5f), new Vector2(0, 24), new Vector2(80, 60));

            var hp = BuildSmallBar("HpBar", root, S("UI/hpbar_naka_2.png"), new Vector2(0, -22), new Vector2(190, 22));
            var icon = Img("Attribute", root, null, Color.white);
            Place(icon.rectTransform, new Vector2(0.5f, 0), new Vector2(0.5f, 0.5f), new Vector2(-118, -22), new Vector2(38, 38));
            var countdown = Txt("Countdown", root, "あと 2", 30, TextAnchor.MiddleCenter, Color.white);
            Place(countdown.rectTransform, new Vector2(0.5f, 0), new Vector2(0.5f, 0.5f), new Vector2(0, -54), new Vector2(200, 40));

            var view = root.gameObject.AddComponent<EnemyView>();
            SetRef(view, "body", body);
            SetRef(view, "hpBar", hp);
            SetRef(view, "attributeIcon", icon);
            SetRef(view, "countdownText", countdown);
            SetRef(view, "targetMarker", marker.gameObject);
            SetRef(view, "button", btn);
            root.gameObject.SetActive(false);
            return view;
        }

        static PrincessSlotView[] BuildParty(RectTransform safe)
        {
            var row = Rect("Party", safe);
            row.anchorMin = new Vector2(0, 0); row.anchorMax = new Vector2(1, 0); row.pivot = new Vector2(0.5f, 0);
            row.sizeDelta = new Vector2(0, PartyH); row.anchoredPosition = new Vector2(0, PartyY);

            var slots = new PrincessSlotView[3];
            for (int i = 0; i < 3; i++)
            {
                var slot = Rect($"Slot{i}", row);
                float x = (i * 2 + 1) / 6f;
                slot.anchorMin = slot.anchorMax = new Vector2(x, 0);
                slot.pivot = new Vector2(0.5f, 0);
                slot.sizeDelta = new Vector2(300, PartyH);
                slot.anchoredPosition = Vector2.zero;

                var glow = Img("Glow", slot, _uiSprite, new Color(1f, 0.9f, 0.3f, 0.8f));
                Place(glow.rectTransform, new Vector2(0.5f, 0), new Vector2(0.5f, 0.5f), new Vector2(0, 92), new Vector2(148, 148));
                var icon = Img("Icon", slot, null, Color.white);
                Place(icon.rectTransform, new Vector2(0.5f, 0), new Vector2(0.5f, 0.5f), new Vector2(0, 92), new Vector2(130, 130));
                var ready = Txt("Ready", slot, "SKILL!", 34, TextAnchor.MiddleCenter, new Color(1f, 0.95f, 0.4f));
                Place(ready.rectTransform, new Vector2(0.5f, 0), new Vector2(0.5f, 0.5f), new Vector2(0, 140), new Vector2(200, 50));
                var gauge = BuildSmallBar("Gauge", slot, S("UI/spbar_naka.png"), new Vector2(0, 12), new Vector2(170, 20));

                var btn = slot.gameObject.AddComponent<Button>();
                btn.targetGraphic = icon;
                var colors = btn.colors; colors.pressedColor = new Color(0.8f, 0.8f, 0.8f); btn.colors = colors;

                var view = slot.gameObject.AddComponent<PrincessSlotView>();
                SetRef(view, "icon", icon);
                SetRef(view, "glow", glow);
                SetRef(view, "gauge", gauge);
                SetRef(view, "readyText", ready);
                SetRef(view, "button", btn);
                slots[i] = view;
            }
            return slots;
        }

        /// <summary>sphpbar_base を使った細いバー（敵HP・スキルゲージ）。</summary>
        static HpBarView BuildSmallBar(string name, RectTransform parent, Sprite fillSprite, Vector2 pos, Vector2 size)
        {
            var root = Rect(name, parent);
            Place(root, new Vector2(0.5f, 0), new Vector2(0.5f, 0.5f), pos, size);
            var b = Img("Base", root, S("UI/sphpbar_base.png"), Color.white);
            Stretch(b.rectTransform);
            var fill = Img("Fill", root, fillSprite, Color.white);
            FillImage(fill);
            fill.rectTransform.anchorMin = new Vector2(0.04f, 0.2f);
            fill.rectTransform.anchorMax = new Vector2(0.96f, 0.8f);
            fill.rectTransform.offsetMin = fill.rectTransform.offsetMax = Vector2.zero;
            var view = root.gameObject.AddComponent<HpBarView>();
            SetRef(view, "fill", fill);
            return view;
        }

        static HpBarView BuildPlayerHp(RectTransform safe)
        {
            var root = Rect("PlayerHp", safe);
            root.anchorMin = new Vector2(0, 0); root.anchorMax = new Vector2(1, 0); root.pivot = new Vector2(0.5f, 0);
            root.offsetMin = new Vector2(24, 0); root.offsetMax = new Vector2(-24, 0);
            root.sizeDelta = new Vector2(root.sizeDelta.x, HpH);
            root.anchoredPosition = new Vector2(0, HpY);

            var b = Img("Base", root, S("UI/hpbar_base.png"), Color.white);
            Stretch(b.rectTransform);
            var fill = Img("Fill", root, S("UI/hpbar_naka.png"), Color.white);
            FillImage(fill);
            // hpbar_base / hpbar_freme（786×34px）の中の、バーが入る範囲
            fill.rectTransform.anchorMin = new Vector2(56f / 786f, 10f / 34f);
            fill.rectTransform.anchorMax = new Vector2(780f / 786f, 24f / 34f);
            fill.rectTransform.offsetMin = fill.rectTransform.offsetMax = Vector2.zero;
            var frame = Img("Frame", root, S("UI/hpbar_freme.png"), Color.white);
            Stretch(frame.rectTransform);
            var label = Txt("Label", root, "0 / 0", 30, TextAnchor.MiddleRight, Color.white);
            Stretch(label.rectTransform, new Vector2(0, 0), new Vector2(-24, 0));

            var view = root.gameObject.AddComponent<HpBarView>();
            SetRef(view, "fill", fill);
            SetRef(view, "label", label);
            return view;
        }

        static RectTransform BuildBoardSlot(RectTransform safe)
        {
            var slot = Rect("BoardSlot", safe);
            slot.anchorMin = new Vector2(0, 0); slot.anchorMax = new Vector2(1, 0); slot.pivot = new Vector2(0.5f, 0);
            slot.offsetMin = new Vector2(BoardSide, 0); slot.offsetMax = new Vector2(-BoardSide, 0);
            slot.anchoredPosition = new Vector2(0, BoardBottom);
            var arf = slot.gameObject.AddComponent<AspectRatioFitter>();
            arf.aspectMode = AspectRatioFitter.AspectMode.WidthControlsHeight;
            arf.aspectRatio = 1f;
            return slot;
        }

        struct ComboParts { public ComboView view; }

        static ComboParts BuildComboArea(RectTransform safe)
        {
            var root = Rect("ComboView", safe);
            Place(root, new Vector2(0.5f, 0), new Vector2(0.5f, 0.5f), new Vector2(0, ButtonH + ComboH * 0.5f), new Vector2(1040, ComboH));

            var combo = Txt("ComboText", root, "", 72, TextAnchor.MiddleCenter, new Color(1f, 0.92f, 0.5f));
            Place(combo.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-260, 20), new Vector2(520, 90));
            var mult = Txt("MultiplierText", root, "", 46, TextAnchor.MiddleCenter, Color.white);
            Place(mult.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-260, -36), new Vector2(520, 60));
            var dLabel = Txt("DamageLabel", root, "TOTAL DAMAGE", 30, TextAnchor.MiddleCenter, new Color(1f, 0.8f, 0.6f));
            Place(dLabel.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(260, 30), new Vector2(520, 44));
            var dmg = Txt("DamageText", root, "", 64, TextAnchor.MiddleCenter, new Color(1f, 0.95f, 0.6f));
            Place(dmg.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(260, -20), new Vector2(520, 80));

            var view = root.gameObject.AddComponent<ComboView>();
            SetRef(view, "comboText", combo);
            SetRef(view, "multiplierText", mult);
            SetRef(view, "damageLabel", dLabel);
            SetRef(view, "damageText", dmg);
            return new ComboParts { view = view };
        }

        struct ButtonParts { public BattleMenu menu; }

        static ButtonParts BuildButtons(RectTransform safe)
        {
            var area = Rect("ButtonArea", safe);
            area.anchorMin = new Vector2(0, 0); area.anchorMax = new Vector2(1, 0); area.pivot = new Vector2(0.5f, 0);
            area.sizeDelta = new Vector2(0, ButtonH); area.anchoredPosition = Vector2.zero;

            var auto = Btn("Auto", area, "オート", new Vector2(-390, 0), new Vector2(240, 100), out var autoText);
            var speed = Btn("Speed", area, "×1", new Vector2(-130, 0), new Vector2(240, 100), out var speedText);
            var item = Btn("Item", area, "回復薬", new Vector2(130, 0), new Vector2(240, 100), out var itemText);
            var menuBtn = Btn("Menu", area, "メニュー", new Vector2(390, 0), new Vector2(240, 100), out _);
            foreach (var b in new[] { auto, speed, item, menuBtn })
            {
                var rt = (RectTransform)b.transform;
                rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            }

            var menu = area.gameObject.AddComponent<BattleMenu>();
            SetRef(menu, "autoButton", auto);
            SetRef(menu, "autoText", autoText);
            SetRef(menu, "speedButton", speed);
            SetRef(menu, "speedText", speedText);
            SetRef(menu, "itemButton", item);
            SetRef(menu, "itemText", itemText);
            SetRef(menu, "menuButton", menuBtn);
            return new ButtonParts { menu = menu };
        }

        static BannerView BuildBanner(RectTransform safe)
        {
            var root = Rect("Banner", safe);
            Stretch(root, new Vector2(0, EnemyY), new Vector2(0, -HeaderH));
            root.gameObject.AddComponent<CanvasGroup>();
            var t = Txt("Text", root, "WAVE 1/3", 120, TextAnchor.MiddleCenter, Color.white);
            Stretch(t.rectTransform);
            t.GetComponent<Outline>().effectDistance = new Vector2(6, -6);
            var view = root.gameObject.AddComponent<BannerView>();
            SetRef(view, "text", t);
            return view;
        }

        static SkillCutinView BuildCutin(RectTransform safe)
        {
            var root = Rect("SkillCutin", safe);
            Stretch(root, new Vector2(0, PartyY), new Vector2(0, -HeaderH));
            root.gameObject.AddComponent<CanvasGroup>();

            var band = Img("Band", root, _uiSprite, new Color(0.4f, 0.1f, 0.1f, 0.85f));
            var bandRt = band.rectTransform;
            bandRt.anchorMin = new Vector2(0, 0.5f); bandRt.anchorMax = new Vector2(1, 0.5f); bandRt.pivot = new Vector2(0.5f, 0.5f);
            bandRt.sizeDelta = new Vector2(0, 280); bandRt.anchoredPosition = Vector2.zero;

            var face = Img("Portrait", bandRt, null, Color.white);
            Place(face.rectTransform, new Vector2(0, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(200, 20), new Vector2(320, 320));
            var name = Txt("SkillName", bandRt, "スキル", 76, TextAnchor.MiddleCenter, Color.white);
            Place(name.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(150, 0), new Vector2(680, 120));

            var view = root.gameObject.AddComponent<SkillCutinView>();
            SetRef(view, "band", bandRt);
            SetRef(view, "bandImage", band);
            SetRef(view, "portrait", face);
            SetRef(view, "skillName", name);
            return view;
        }

        static DamagePopup BuildPopupTemplate(RectTransform fx)
        {
            var t = Txt("PopupTemplate", fx, "1234", 68, TextAnchor.MiddleCenter, Color.white);
            Place(t.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(600, 120));
            t.GetComponent<Outline>().effectDistance = new Vector2(4, -4);
            var p = t.gameObject.AddComponent<DamagePopup>();
            t.gameObject.SetActive(false);
            return p;
        }

        static GameObject BuildPausePanel(Transform canvas, out Button resume, out Button retry)
        {
            var root = Img("PausePanel", canvas, _uiSprite, new Color(0, 0, 0, 0.65f));
            Stretch(root.rectTransform);
            var title = Txt("Title", root.rectTransform, "ポーズ", 90, TextAnchor.MiddleCenter, Color.white);
            Place(title.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 260), new Vector2(800, 140));
            resume = Btn("Resume", root.rectTransform, "つづける", new Vector2(0, 60), new Vector2(520, 130), out _);
            retry = Btn("Retry", root.rectTransform, "最初からやり直す", new Vector2(0, -110), new Vector2(520, 130), out _);
            foreach (var b in new[] { resume, retry })
            {
                var rt = (RectTransform)b.transform;
                rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            }
            root.gameObject.SetActive(false);
            return root.gameObject;
        }

        static ResultView BuildResult(Transform canvas)
        {
            var root = Img("Result", canvas, _uiSprite, new Color(0, 0, 0, 0.7f));
            Stretch(root.rectTransform);
            root.gameObject.AddComponent<CanvasGroup>();
            var title = Txt("Title", root.rectTransform, "WIN!", 170, TextAnchor.MiddleCenter, Color.white);
            Place(title.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 260), new Vector2(1000, 240));
            title.GetComponent<Outline>().effectDistance = new Vector2(7, -7);
            var msg = Txt("Message", root.rectTransform, "", 46, TextAnchor.MiddleCenter, Color.white);
            Place(msg.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 60), new Vector2(1000, 160));
            var btn = Btn("Button", root.rectTransform, "もう一度", new Vector2(0, -160), new Vector2(560, 140), out var btnText);
            var brt = (RectTransform)btn.transform;
            brt.anchorMin = brt.anchorMax = new Vector2(0.5f, 0.5f);

            var view = root.gameObject.AddComponent<ResultView>();
            SetRef(view, "title", title);
            SetRef(view, "message", msg);
            SetRef(view, "button", btn);
            SetRef(view, "buttonText", btnText);
            return view;
        }

        static Board BuildBoard(Sprite[] stones, RectTransform slot, Camera cam)
        {
            var boardGo = new GameObject("Board");
            var frame = S("Battle/board_frame.png");
            if (frame != null)
            {
                var f = new GameObject("Frame");
                f.transform.SetParent(boardGo.transform, false);
                var sr = f.AddComponent<SpriteRenderer>();
                sr.sprite = frame;
                sr.sortingOrder = 0;
            }

            var board = boardGo.AddComponent<Board>();
            var so = new SerializedObject(board);
            SetArray(so.FindProperty("pieceSprites"), stones);
            // バトル画面では盤面の上を UI（背景・姫）が覆うので、マスクは使わない
            so.FindProperty("maskOutsideBoard").boolValue = false;
            so.ApplyModifiedPropertiesWithoutUndo();

            var fitter = boardGo.AddComponent<BoardFitter>();
            SetRef(fitter, "slot", slot);
            var input = boardGo.AddComponent<BoardInput>();
            SetRef(input, "board", board);
            SetRef(input, "cam", cam);
            var log = boardGo.AddComponent<PuzzleDebugLog>();
            SetRef(log, "board", board);
            return board;
        }

        static void CreateEventSystem()
        {
            var go = new GameObject("EventSystem", typeof(EventSystem));
            // Input System を使っている場合はそれ用の部品、そうでなければ従来の部品を付ける
            var t = Type.GetType("UnityEngine.InputSystem.UI.InputSystemUIInputModule, Unity.InputSystem");
#if ENABLE_INPUT_SYSTEM
            if (t != null) { go.AddComponent(t); return; }
#endif
            go.AddComponent<StandaloneInputModule>();
        }

        // ==================================================================
        // 小さな道具
        // ==================================================================

        static Sprite S(string path) => AssetDatabase.LoadAssetAtPath<Sprite>($"{SpriteRoot}/{path}");

        static void ImportAllSprites()
        {
            foreach (var guid in AssetDatabase.FindAssets("t:Texture2D", new[] { SpriteRoot }))
            {
                string p = AssetDatabase.GUIDToAssetPath(guid);
                if (AssetImporter.GetAtPath(p) is TextureImporter ti && PrincessSpriteImporter.Apply(ti))
                    ti.SaveAndReimport();
            }
        }

        static RectTransform Rect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            return rt;
        }

        static void Stretch(RectTransform rt) => Stretch(rt, Vector2.zero, Vector2.zero);

        static void Stretch(RectTransform rt, Vector2 offsetMin, Vector2 offsetMax)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.offsetMin = offsetMin;
            rt.offsetMax = offsetMax;
        }

        static void Place(RectTransform rt, Vector2 anchor, Vector2 pivot, Vector2 pos, Vector2 size)
        {
            rt.anchorMin = rt.anchorMax = anchor;
            rt.pivot = pivot;
            rt.sizeDelta = size;
            rt.anchoredPosition = pos;
        }

        static Image Img(string name, Transform parent, Sprite sprite, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var img = go.GetComponent<Image>();
            img.sprite = sprite;
            img.color = color;
            if (sprite == _uiSprite && sprite != null) img.type = Image.Type.Sliced;
            return img;
        }

        static void FillImage(Image img)
        {
            img.type = Image.Type.Filled;
            img.fillMethod = Image.FillMethod.Horizontal;
            img.fillOrigin = (int)Image.OriginHorizontal.Left;
            img.fillAmount = 1f;
            img.raycastTarget = false;
        }

        static Text Txt(string name, Transform parent, string text, int size, TextAnchor align, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Text), typeof(Outline));
            go.transform.SetParent(parent, false);
            var t = go.GetComponent<Text>();
            t.font = _font;
            t.text = text;
            t.fontSize = size;
            t.fontStyle = FontStyle.Bold;
            t.alignment = align;
            t.color = color;
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            t.raycastTarget = false;
            var o = go.GetComponent<Outline>();
            o.effectColor = new Color(0.2f, 0.08f, 0.05f, 1f);
            o.effectDistance = new Vector2(3, -3);
            return t;
        }

        static Button Btn(string name, Transform parent, string label, Vector2 pos, Vector2 size, out Text text)
        {
            var img = Img(name, parent, _uiSprite, new Color(0.25f, 0.2f, 0.3f));
            Place(img.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), pos, size);
            var btn = img.gameObject.AddComponent<Button>();
            btn.targetGraphic = img;
            text = Txt("Text", img.rectTransform, label, 38, TextAnchor.MiddleCenter, Color.white);
            Stretch(text.rectTransform);
            text.lineSpacing = 0.9f;
            return btn;
        }

        static Font LoadBuiltinFont()
        {
            Font f = null;
            try { f = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); } catch { }
            if (f == null) { try { f = Resources.GetBuiltinResource<Font>("Arial.ttf"); } catch { } }
            return f;
        }

        static void SetRef(Object target, string field, Object value)
        {
            var so = new SerializedObject(target);
            var p = so.FindProperty(field);
            if (p == null) { Debug.LogError($"{target.GetType().Name} に {field} がありません"); return; }
            p.objectReferenceValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        static void SetArray(SerializedProperty prop, IList<Object> values)
        {
            prop.arraySize = values.Count;
            for (int i = 0; i < values.Count; i++) prop.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
        }

        static void AddToBuildSettings(string path)
        {
            var list = new List<EditorBuildSettingsScene>();
            list.Add(new EditorBuildSettingsScene(path, true)); // 最初に起動するシーンにする
            foreach (var s in EditorBuildSettings.scenes) if (s.path != path) list.Add(s);
            EditorBuildSettings.scenes = list.ToArray();
        }
    }
}
