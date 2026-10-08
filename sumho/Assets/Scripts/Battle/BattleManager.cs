using System.Collections;
using System.Collections.Generic;
using PrincessCollection.Puzzle;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace PrincessCollection.Battle
{
    /// <summary>
    /// バトル全体の進行。
    ///   自分のターン（パズル）→ 回復・ゲージ・攻撃 → 敵全滅なら次の WAVE ／ 最後なら勝利
    ///   → 敵のターン（「あと N」が 0 の敵が攻撃）→ HP 0 なら敗北 → 自分のターン …
    /// パズルとは board.TurnFinished（結果）と board.InputLocked（操作の禁止）だけでつながる。
    /// </summary>
    public class BattleManager : MonoBehaviour
    {
        [Header("データ")]
        [SerializeField] StageData stage;
        [SerializeField] PrincessData[] party = new PrincessData[3];
        [Tooltip("火・水・木の属性アイコン")]
        [SerializeField] Sprite[] elementIcons = new Sprite[3];

        [Header("ルール")]
        [Tooltip("闇の石3個で、姫全員が攻撃力の何倍で攻撃するか")]
        [SerializeField] float darkAttackRate = 0.5f;
        [Tooltip("光の石3個で、最大HPの何割を回復するか")]
        [SerializeField] float lightHealRate = 0.1f;
        [Tooltip("闇の石も姫のスキルゲージをためるか（2個で1）")]
        [SerializeField] bool darkChargesGauge = true;
        [SerializeField] float advantageRate = 1.5f;
        [SerializeField] float disadvantageRate = 0.5f;
        [SerializeField] int itemCount = 3;
        [Tooltip("回復薬で最大HPの何割を回復するか")]
        [SerializeField] float itemHealRate = 0.3f;
        [Tooltip("オート時、次の操作までの待ち時間")]
        [SerializeField] float autoDelay = 0.5f;

        [Header("パズル")]
        [SerializeField] Board board;
        [SerializeField] ComboView comboView;

        [Header("画面")]
        [SerializeField] UnityEngine.UI.Image background;
        [SerializeField] StageHeaderView header;
        [SerializeField] RectTransform enemyArea;
        [Tooltip("敵1体分のひな形（非表示で置いておく）")]
        [SerializeField] EnemyView enemyTemplate;
        [SerializeField] PrincessSlotView[] slots = new PrincessSlotView[3];
        [SerializeField] HpBarView playerHpBar;
        [SerializeField] RectTransform fxLayer;
        [SerializeField] DamagePopup popupTemplate;
        [SerializeField] BannerView banner;
        [SerializeField] SkillCutinView cutin;
        [SerializeField] ResultView result;
        [SerializeField] BattleMenu menu;

        // ---- 状態 ----
        readonly List<PrincessUnit> _party = new List<PrincessUnit>();
        readonly List<PrincessSlotView> _slotViews = new List<PrincessSlotView>();
        readonly List<EnemyUnit> _enemies = new List<EnemyUnit>();
        readonly List<EnemyView> _enemyViews = new List<EnemyView>();
        int _hp, _maxHp, _wave, _target = -1, _turns;
        bool _resolving, _ended, _paused, _auto;
        float _speed = 1f, _autoTimer;

        public bool Auto => _auto;
        public float Speed => _speed;
        public int ItemCount => itemCount;

        // ------------------------------------------------------------------
        // 開始
        // ------------------------------------------------------------------

        void OnEnable() { if (board != null) board.TurnFinished += OnTurnFinished; }
        void OnDisable() { if (board != null) board.TurnFinished -= OnTurnFinished; }

        void Start()
        {
            Time.timeScale = 1f;
            if (stage == null || stage.waves.Count == 0)
            {
                Debug.LogError("[Battle] StageData が設定されていないか、WAVE が空です。", this);
                enabled = false;
                return;
            }

            if (background != null && stage.background != null) background.sprite = stage.background;
            header.SetStage(stage);

            _maxHp = 0;
            for (int i = 0; i < party.Length && i < slots.Length; i++)
            {
                if (party[i] == null) { slots[i].gameObject.SetActive(false); continue; }
                var unit = new PrincessUnit(party[i]);
                _party.Add(unit);
                _slotViews.Add(slots[i]);
                _maxHp += unit.data.hp;
                int index = _party.Count - 1;
                slots[i].Setup(unit, () => TryUseSkill(index));
            }
            _hp = _maxHp;
            playerHpBar.Set(_hp, _maxHp, false);

            StartCoroutine(BeginWave(0));
        }

        // ------------------------------------------------------------------
        // WAVE
        // ------------------------------------------------------------------

        IEnumerator BeginWave(int wave)
        {
            _resolving = true;
            UpdateLock();
            _wave = wave;
            header.SetWave(wave + 1, stage.waves.Count);

            foreach (var v in _enemyViews) Destroy(v.gameObject);
            _enemyViews.Clear();
            _enemies.Clear();
            _target = -1;

            Canvas.ForceUpdateCanvases();
            var list = stage.waves[wave].enemies;
            Rect area = enemyArea.rect;
            int n = Mathf.Max(1, list.Count);
            float slotWidth = area.width / n;
            const float footer = 70f; // HP バーと「あと N」の分
            for (int i = 0; i < list.Count; i++)
            {
                if (list[i] == null) continue;
                var unit = new EnemyUnit(list[i]);
                var view = Instantiate(enemyTemplate, enemyArea);
                view.gameObject.SetActive(true);
                var rt = (RectTransform)view.transform;
                rt.anchoredPosition = new Vector2((i + 0.5f) * slotWidth - area.width * 0.5f, footer);
                float size = Mathf.Min(unit.data.displaySize, area.height - footer - 10f, slotWidth - 16f);
                int index = _enemies.Count;
                view.Setup(unit, ElementIcon(unit.Element), Mathf.Max(60f, size), () => SetTarget(index));
                _enemies.Add(unit);
                _enemyViews.Add(view);
                StartCoroutine(view.PlayAppear());
            }
            RefreshTarget();

            bool boss = stage.lastWaveIsBoss && wave == stage.waves.Count - 1;
            yield return banner.Show(boss ? "BOSS" : $"WAVE {wave + 1}/{stage.waves.Count}",
                boss ? new Color(1f, 0.35f, 0.3f) : Color.white);

            _resolving = false;
            UpdateLock();
        }

        bool AllEnemiesDead
        {
            get
            {
                foreach (var e in _enemies) if (!e.Dead) return false;
                return true;
            }
        }

        IEnumerator WaveCleared()
        {
            yield return new WaitForSeconds(0.5f);
            if (_wave + 1 < stage.waves.Count)
            {
                yield return BeginWave(_wave + 1);
            }
            else
            {
                _ended = true;
                UpdateLock();
                yield return result.Show(true, $"{stage.stageName} クリア！\n{_turns} ターン", Restart);
            }
        }

        // ------------------------------------------------------------------
        // 自分のターン
        // ------------------------------------------------------------------

        void OnTurnFinished(TurnResult r)
        {
            if (_ended) return;
            _turns++;
            StartCoroutine(ResolveTurn(r));
        }

        int Cleared(TurnResult r, PieceType t) => (int)t < r.ClearedByType.Length ? r.ClearedByType[(int)t] : 0;

        IEnumerator ResolveTurn(TurnResult r)
        {
            _resolving = true;
            UpdateLock();

            // 光：回復
            int light = Cleared(r, PieceType.Light);
            if (light > 0)
                yield return Heal(Mathf.RoundToInt(_maxHp * lightHealRate * light / 3f * r.Multiplier));

            // スキルゲージ
            int dark = Cleared(r, PieceType.Dark);
            for (int i = 0; i < _party.Count; i++)
            {
                var p = _party[i];
                int add = Cleared(r, p.Element.ToPiece()) + (darkChargesGauge ? dark / 2 : 0);
                p.gauge = Mathf.Min(p.data.skillGaugeMax, p.gauge + add);
                _slotViews[i].SetGauge(p);
            }

            // 攻撃：自分の属性の石 ＋ 闇の石
            int total = 0;
            for (int i = 0; i < _party.Count; i++)
            {
                var p = _party[i];
                int own = Cleared(r, p.Element.ToPiece());
                float power = p.data.attack * (own / 3f) + p.data.attack * darkAttackRate * (dark / 3f);
                if (power <= 0f) continue;

                int t = CurrentTarget();
                if (t < 0) break;
                var enemy = _enemies[t];
                float aff = ElementUtil.Affinity(p.Element, enemy.Element, advantageRate, disadvantageRate);
                int dmg = Mathf.Max(1, Mathf.RoundToInt(power * r.Multiplier * aff));

                yield return _slotViews[i].PlayAttack();
                total += dmg;
                yield return DealDamage(t, dmg, p.Element, aff);
            }
            if (total > 0 && comboView != null) comboView.ShowTotalDamage(total);

            if (AllEnemiesDead)
            {
                yield return WaveCleared();
                yield break;
            }

            yield return new WaitForSeconds(0.2f);
            yield return EnemyPhase();

            if (!_ended)
            {
                _resolving = false;
                UpdateLock();
            }
        }

        IEnumerator DealDamage(int index, int dmg, Element element, float affinity)
        {
            var enemy = _enemies[index];
            var view = _enemyViews[index];
            enemy.hp = Mathf.Max(0, enemy.hp - dmg);

            Color c = element.ToColor();
            float scale = affinity > 1f ? 1.3f : affinity < 1f ? 0.85f : 1f;
            Popup(view.BodyRect, dmg.ToString("N0"), c, scale);
            if (affinity > 1f) Popup(view.BodyRect, "WEAK!", new Color(1f, 0.9f, 0.3f), 0.7f, 60f);

            view.Refresh(enemy);
            yield return view.PlayHit();

            if (enemy.Dead)
            {
                StartCoroutine(view.PlayDie());
                if (_target == index) _target = -1;
                RefreshTarget();
            }
        }

        // ------------------------------------------------------------------
        // 敵のターン
        // ------------------------------------------------------------------

        IEnumerator EnemyPhase()
        {
            for (int i = 0; i < _enemies.Count; i++)
            {
                var e = _enemies[i];
                if (e.Dead) continue;
                e.countdown--;
                _enemyViews[i].Refresh(e);
            }

            for (int i = 0; i < _enemies.Count; i++)
            {
                var e = _enemies[i];
                if (e.Dead || e.countdown > 0) continue;

                yield return _enemyViews[i].PlayAttack();
                int dmg = e.data.attack;
                _hp = Mathf.Max(0, _hp - dmg);
                playerHpBar.Set(_hp, _maxHp);
                Popup((RectTransform)playerHpBar.transform, $"-{dmg:N0}", new Color(1f, 0.35f, 0.3f), 0.9f);
                foreach (var sv in _slotViews) StartCoroutine(sv.PlayDamaged());

                e.countdown = Mathf.Max(1, e.data.turnInterval);
                _enemyViews[i].Refresh(e);
                yield return new WaitForSeconds(0.3f);

                if (_hp <= 0)
                {
                    _ended = true;
                    UpdateLock();
                    yield return result.Show(false, "姫たちは力尽きた…", Restart);
                    yield break;
                }
            }
        }

        // ------------------------------------------------------------------
        // スキル・アイテム
        // ------------------------------------------------------------------

        bool CanAct => !_resolving && !_ended && !_paused && board.CanAcceptInput;

        public void TryUseSkill(int index)
        {
            if (!CanAct || index < 0 || index >= _party.Count || !_party[index].Ready) return;
            StartCoroutine(SkillRoutine(index));
        }

        IEnumerator SkillRoutine(int index)
        {
            _resolving = true;
            UpdateLock();

            var p = _party[index];
            p.gauge = 0;
            _slotViews[index].SetGauge(p);
            yield return cutin.Show(p.data.portrait != null ? p.data.portrait : p.data.icon, p.data.skillName, p.Element.ToColor());

            switch (p.data.skillType)
            {
                case SkillType.AttackAll:
                    for (int i = 0; i < _enemies.Count; i++)
                    {
                        if (_enemies[i].Dead) continue;
                        float aff = ElementUtil.Affinity(p.Element, _enemies[i].Element, advantageRate, disadvantageRate);
                        StartCoroutine(DealDamage(i, Mathf.RoundToInt(p.data.attack * p.data.skillPower * aff), p.Element, aff));
                    }
                    yield return new WaitForSeconds(0.4f);
                    break;

                case SkillType.AttackSingle:
                    int t = CurrentTarget();
                    if (t >= 0)
                    {
                        float aff = ElementUtil.Affinity(p.Element, _enemies[t].Element, advantageRate, disadvantageRate);
                        yield return DealDamage(t, Mathf.RoundToInt(p.data.attack * p.data.skillPower * aff), p.Element, aff);
                    }
                    break;

                case SkillType.Heal:
                    yield return Heal(Mathf.RoundToInt(_maxHp * p.data.skillPower));
                    break;
            }

            if (AllEnemiesDead)
            {
                yield return WaveCleared();
                yield break;
            }
            // スキルはターンを消費しない
            _resolving = false;
            UpdateLock();
        }

        public void UseItem()
        {
            if (!CanAct || itemCount <= 0 || _hp >= _maxHp) return;
            itemCount--;
            StartCoroutine(ItemRoutine());
        }

        IEnumerator ItemRoutine()
        {
            _resolving = true;
            UpdateLock();
            yield return Heal(Mathf.RoundToInt(_maxHp * itemHealRate));
            _resolving = false;
            UpdateLock();
            if (menu != null) menu.Refresh();
        }

        IEnumerator Heal(int amount)
        {
            if (amount <= 0) yield break;
            _hp = Mathf.Min(_maxHp, _hp + amount);
            playerHpBar.Set(_hp, _maxHp);
            Popup((RectTransform)playerHpBar.transform, $"+{amount:N0}", new Color(0.5f, 1f, 0.6f), 0.9f);
            yield return new WaitForSeconds(0.35f);
        }

        // ------------------------------------------------------------------
        // 狙い
        // ------------------------------------------------------------------

        void SetTarget(int index)
        {
            if (index < 0 || index >= _enemies.Count || _enemies[index].Dead) return;
            _target = index;
            RefreshTarget();
        }

        int CurrentTarget()
        {
            if (_target >= 0 && _target < _enemies.Count && !_enemies[_target].Dead) return _target;
            for (int i = 0; i < _enemies.Count; i++) if (!_enemies[i].Dead) return i;
            return -1;
        }

        void RefreshTarget()
        {
            int t = CurrentTarget();
            for (int i = 0; i < _enemyViews.Count; i++) _enemyViews[i].SetTargeted(i == t && _enemies.Count > 1);
        }

        // ------------------------------------------------------------------
        // ボタンから
        // ------------------------------------------------------------------

        public void ToggleAuto() { _auto = !_auto; _autoTimer = 0f; }

        public void ToggleSpeed()
        {
            _speed = _speed > 1f ? 1f : 2f;
            if (!_paused) Time.timeScale = _speed;
        }

        public void SetPaused(bool paused)
        {
            _paused = paused;
            Time.timeScale = paused ? 0f : _speed;
            UpdateLock();
        }

        public void Restart()
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }

        void Update()
        {
            if (!_auto || !CanAct) { _autoTimer = 0f; return; }
            _autoTimer += Time.deltaTime;
            if (_autoTimer < autoDelay) return;
            _autoTimer = 0f;

            for (int i = 0; i < _party.Count; i++)
                if (_party[i].Ready) { TryUseSkill(i); return; }

            var hint = board.Logic.FindHint();
            if (hint.HasValue) board.TrySwap(hint.Value.a, hint.Value.b);
        }

        // ------------------------------------------------------------------

        void UpdateLock()
        {
            if (board != null) board.InputLocked = _resolving || _ended || _paused;
        }

        Sprite ElementIcon(Element e)
        {
            int i = (int)e;
            return elementIcons != null && i < elementIcons.Length ? elementIcons[i] : null;
        }

        void Popup(RectTransform anchor, string text, Color color, float scale, float yOffset = 0f)
        {
            if (popupTemplate == null || fxLayer == null) return;
            Vector3 pos = anchor.TransformPoint(anchor.rect.center + Vector2.up * yOffset);
            popupTemplate.Spawn(fxLayer, pos, text, color, scale);
        }
    }
}
