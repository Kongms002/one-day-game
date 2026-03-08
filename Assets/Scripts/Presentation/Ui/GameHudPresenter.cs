using System;
using System.Collections.Generic;
using OneDayGame.Domain;
using OneDayGame.Domain.Gameplay;
using OneDayGame.Domain.Policies;
using OneDayGame.Domain.Weapons;
using OneDayGame.Presentation.Gameplay;
using UnityEngine;
using UnityEngine.UI;

namespace OneDayGame.Presentation.Ui
{
    public sealed class GameHudPresenter : MonoBehaviour
    {
        [SerializeField]
        private Text _scoreText;

        [SerializeField]
        private Text _stageText;

        [SerializeField]
        private Text _hpText;

        [SerializeField]
        private Text _ultimateText;

        [SerializeField]
        private Text _expText;

        [SerializeField]
        private Text _levelText;

        [SerializeField]
        private Image _expBarFill;

        [SerializeField]
        private Text _timeText;

        [SerializeField]
        private Text _highScoreText;

        [SerializeField]
        private Text _enemiesSpawnedText;

        [SerializeField]
        private Text _statusText;

        [SerializeField]
        private Text _weaponText;

        [SerializeField]
        private Image _weaponIcon;

        [SerializeField]
        private Button _weaponButton;

        [SerializeField]
        private GameObject _weaponDetailPanel;

        [SerializeField]
        private Text _weaponDetailTitle;

        [SerializeField]
        private Text _weaponDetailStats;

        [SerializeField]
        private Text _weaponDetailDescription;

        [SerializeField]
        private Button _weaponDetailConfirmButton;

        [SerializeField]
        private GameObject _levelUpPanel;

        [SerializeField]
        private Text _levelUpTitleText;

        [SerializeField]
        private Button _upgradeButtonA;

        [SerializeField]
        private Button _upgradeButtonB;

        [SerializeField]
        private Button _upgradeButtonC;

        [SerializeField]
        private Text _upgradeButtonAText;

        [SerializeField]
        private Text _upgradeButtonBText;

        [SerializeField]
        private Text _upgradeButtonCText;

        [SerializeField]
        private GameObject _resultPanel;

        [SerializeField]
        private Text _resultDamageText;

        [SerializeField]
        private Text _resultTimeText;

        [SerializeField]
        private Text _resultKillsText;

        [SerializeField]
        private Button _restartButton;

        private IRunState _runState;
        private IWeaponPolicy _weaponPolicy;
        private IWeaponLoadoutReadModel _weaponLoadout;
        private bool _weaponDetailVisible;
        private Action<int> _onUpgradeSelected;
        private RectTransform _weaponListRoot;
        private readonly List<Button> _weaponSlotButtons = new List<Button>();
        private bool _isDestroying;

        public event Action RestartRequested;

        public void Bind(IRunState runState)
        {
            if (_runState != null)
            {
                _runState.SnapshotChanged -= OnSnapshotChanged;
                _runState.RunEnded -= OnRunEnded;
                _runState.Restarted -= OnRunRestarted;
                _runState.DeadStateChanged -= OnDeadStateChanged;
            }

            _runState = runState;

            if (_runState != null)
            {
                _runState.SnapshotChanged += OnSnapshotChanged;
                _runState.RunEnded += OnRunEnded;
                _runState.Restarted += OnRunRestarted;
                _runState.DeadStateChanged += OnDeadStateChanged;
                OnSnapshotChanged(_runState.Snapshot);
            }

            if (_statusText != null)
            {
                _statusText.text = "";
            }

            EnsureRuntimeLayout();
            SetupWeaponButton();
            SetupWeaponDetailConfirmButton();
            SetupRestartButton();
            RefreshWeaponUi();
            HideLevelUpChoices();
            HideRunResult();
        }

        public void BindWeapon(IWeaponPolicy weaponPolicy)
        {
            _weaponPolicy = weaponPolicy;
            SetupWeaponDetailConfirmButton();
            RefreshWeaponUi();
        }

        public void BindWeaponLoadout(IWeaponLoadoutReadModel weaponLoadout)
        {
            if (_weaponLoadout != null)
            {
                _weaponLoadout.Changed -= OnWeaponLoadoutChanged;
            }

            _weaponLoadout = weaponLoadout;
            if (_weaponLoadout != null)
            {
                _weaponLoadout.Changed += OnWeaponLoadoutChanged;
            }

            EnsureWeaponListRoot();
            RebuildWeaponSlotButtons();
            RefreshWeaponUi();
        }

        private void OnDestroy()
        {
            _isDestroying = true;

            if (_weaponButton != null)
            {
                _weaponButton.onClick.RemoveListener(ToggleWeaponDetail);
            }

            if (_weaponDetailConfirmButton != null)
            {
                _weaponDetailConfirmButton.onClick.RemoveListener(CloseWeaponDetail);
            }

            if (_restartButton != null)
            {
                _restartButton.onClick.RemoveListener(OnRestartClicked);
            }

            if (_weaponLoadout != null)
            {
                _weaponLoadout.Changed -= OnWeaponLoadoutChanged;
            }

            if (_runState != null)
            {
                _runState.SnapshotChanged -= OnSnapshotChanged;
                _runState.RunEnded -= OnRunEnded;
                _runState.Restarted -= OnRunRestarted;
                _runState.DeadStateChanged -= OnDeadStateChanged;
                _runState = null;
            }

            UnregisterUpgradeButtons();
        }

        public bool IsDead => _runState != null && _runState.IsDead;

        public bool AnyActionNeededToRestart => true;

        private void OnSnapshotChanged(RunSnapshot snapshot)
        {
            if (_scoreText != null)
            {
                _scoreText.text = $"Score: {snapshot.Score}";
            }

            if (_stageText != null)
            {
                _stageText.text = $"Stage: {snapshot.Stage}";
            }

            if (_hpText != null)
            {
                _hpText.text = $"HP {(int) snapshot.Hp}/{(int) snapshot.MaxHp}";
            }

            if (_ultimateText != null)
            {
                _ultimateText.text = $"Ultimate: {(int) snapshot.Ultimate}";
            }

            if (_expText != null)
            {
                _expText.text = $"EXP {snapshot.ExpInLevel}/{snapshot.ExpToNextLevel}";
            }

            if (_levelText != null)
            {
                _levelText.text = $"Lv.{snapshot.Level}";
            }

            if (_expBarFill != null)
            {
                float denom = Mathf.Max(1f, snapshot.ExpToNextLevel);
                _expBarFill.fillAmount = Mathf.Clamp01(snapshot.ExpInLevel / denom);
            }

            if (_timeText != null)
            {
                _timeText.text = $"Time: {snapshot.ElapsedTime:F1}";
            }

            if (_highScoreText != null && _runState != null)
            {
                _highScoreText.text = $"HighScore: {_runState.HighScore}";
            }

            if (_enemiesSpawnedText != null)
            {
                _enemiesSpawnedText.text = $"Enemies: {snapshot.EnemiesSpawned}";
            }

            RefreshWeaponUi();
        }

        private void OnRunEnded(RunSnapshot snapshot)
        {
            if (_statusText != null)
            {
                _statusText.text = "";
            }

            ShowRunResult(snapshot);
        }

        private void OnRunRestarted(RunSnapshot snapshot)
        {
            if (_statusText != null)
            {
                _statusText.text = "";
            }

            HideRunResult();
            OnSnapshotChanged(snapshot);
        }

        private void OnDeadStateChanged(bool isDead)
        {
            if (_statusText == null)
            {
                return;
            }

            if (!isDead)
            {
                _statusText.text = "";
            }
        }

        private void RefreshWeaponUi()
        {
            SetupWeaponButton();
            EnsureWeaponListRoot();

            int stage = _runState != null ? _runState.Stage : 1;
            var selectedWeapon = _weaponLoadout != null ? _weaponLoadout.GetSelectedWeapon() : null;
            var selectedStats = _weaponLoadout != null ? _weaponLoadout.GetSelectedStats(stage) : default;
            if (_weaponText != null)
            {
                _weaponText.text = "Weapons";
            }

            if (_weaponIcon != null)
            {
                var selectedWeaponIcon = selectedWeapon != null ? WeaponSpriteLibrary.GetWeaponIcon(selectedWeapon) : null;
                _weaponIcon.sprite = selectedWeaponIcon ?? RuntimeSpriteLibrary.GetDiamond();
                _weaponIcon.color = selectedWeaponIcon != null ? Color.white : new Color(1f, 0.9f, 0.25f, 1f);
                _weaponIcon.preserveAspect = true;
            }

            if (_weaponDetailTitle != null)
            {
                _weaponDetailTitle.text = selectedWeapon != null
                    ? selectedWeapon.DisplayName
                    : (_weaponPolicy != null ? _weaponPolicy.GetWeaponDisplayName(stage) : "Unknown Weapon");
            }

            if (_weaponDetailStats != null)
            {
                if (selectedWeapon != null)
                {
                    _weaponDetailStats.text =
                        $"Damage {selectedStats.Damage:F1} | Delay {selectedStats.Cooldown:F2}s\n" +
                        $"Projectiles {selectedStats.ProjectileCount} | Range {selectedStats.Range:F2}\n" +
                        $"DOT {selectedStats.DotPerSecond:F1}/s | Type {selectedWeapon.Type}";
                }
                else if (_weaponPolicy == null)
                {
                    _weaponDetailStats.text = "No weapon data";
                }
                else
                {
                    float damage = _weaponPolicy.GetPlayerAttackDamage(stage);
                    float delay = _weaponPolicy.GetPlayerAttackCooldown(stage);
                    int projectileCount = _weaponPolicy.GetProjectileCount(stage);
                    float ultCost = _weaponPolicy.GetUltimateCost(stage);
                    float ultMultiplier = _weaponPolicy.GetUltimateMultiplier(stage);
                    float dot = _weaponPolicy.GetDamageOverTimePerSecond(stage);
                    string dotLabel = _weaponPolicy.HasDamageOverTime(stage) ? $"DOT {dot:F1}/s" : "DOT 없음";
                    _weaponDetailStats.text = $"Damage {damage:F1} | Delay {delay:F2}s\nProjectiles {projectileCount} | Ult Cost {ultCost:F0}\nUlt x{ultMultiplier:F2} | {dotLabel}";
                }
            }

            if (_weaponDetailDescription != null)
            {
                if (selectedWeapon != null)
                {
                    _weaponDetailDescription.text = selectedWeapon.Description;
                }
                else if (_weaponPolicy == null)
                {
                    _weaponDetailDescription.text = "No description";
                }
                else
                {
                    _weaponDetailDescription.text = _weaponPolicy.GetWeaponDescription(stage);
                }
            }

            ApplyWeaponDetailVisibility();
        }

        private void OnWeaponLoadoutChanged()
        {
            RebuildWeaponSlotButtons();
            RefreshWeaponUi();
        }

        private void SetupWeaponButton()
        {
            if (_weaponButton == null && _weaponIcon != null)
            {
                _weaponButton = _weaponIcon.GetComponent<Button>();
                if (_weaponButton == null)
                {
                    _weaponButton = _weaponIcon.gameObject.AddComponent<Button>();
                }
            }

            if (_weaponButton == null)
            {
                return;
            }

            _weaponButton.onClick.RemoveListener(ToggleWeaponDetail);
            _weaponButton.onClick.AddListener(ToggleWeaponDetail);
        }

        private void SetupWeaponDetailConfirmButton()
        {
            if (_weaponDetailConfirmButton == null && _weaponDetailPanel != null)
            {
                var existing = _weaponDetailPanel.transform.Find("WeaponDetailConfirm");
                if (existing != null)
                {
                    _weaponDetailConfirmButton = existing.GetComponent<Button>();
                }
            }

            if (_weaponDetailConfirmButton == null)
            {
                return;
            }

            _weaponDetailConfirmButton.onClick.RemoveListener(CloseWeaponDetail);
            _weaponDetailConfirmButton.onClick.AddListener(CloseWeaponDetail);
        }

        private void SetupRestartButton()
        {
            if (_restartButton == null)
            {
                return;
            }

            _restartButton.onClick.RemoveListener(OnRestartClicked);
            _restartButton.onClick.AddListener(OnRestartClicked);
        }

        private void OnRestartClicked()
        {
            RestartRequested?.Invoke();
        }

        private void ToggleWeaponDetail()
        {
            _weaponDetailVisible = !_weaponDetailVisible;
            ApplyWeaponDetailVisibility();
        }

        private void CloseWeaponDetail()
        {
            _weaponDetailVisible = false;
            ApplyWeaponDetailVisibility();
        }

        private void ApplyWeaponDetailVisibility()
        {
            if (_weaponDetailPanel != null)
            {
                _weaponDetailPanel.SetActive(_weaponDetailVisible);
            }
        }

        private void ShowRunResult(RunSnapshot snapshot)
        {
            if (_resultPanel == null || _runState == null)
            {
                return;
            }

            if (_resultDamageText != null)
            {
                _resultDamageText.text = $"Damage Taken: {_runState.TotalDamageTaken:F0}";
            }

            if (_resultTimeText != null)
            {
                _resultTimeText.text = $"Survival Time: {snapshot.ElapsedTime:F1}s";
            }

            if (_resultKillsText != null)
            {
                _resultKillsText.text = $"Kills: {_runState.TotalKills}";
            }

            _resultPanel.SetActive(true);
        }

        public void HideRunResult()
        {
            if (_resultPanel != null)
            {
                _resultPanel.SetActive(false);
            }
        }

        public bool ShowLevelUpChoices(int level, string optionA, string optionB, string optionC, Action<int> onUpgradeSelected)
        {
            if (_levelUpPanel == null || (_upgradeButtonA == null && _upgradeButtonB == null && _upgradeButtonC == null))
            {
                return false;
            }

            _onUpgradeSelected = onUpgradeSelected;

            if (_levelUpTitleText != null)
            {
                _levelUpTitleText.text = $"LEVEL UP! (Lv.{level})";
            }

            if (_upgradeButtonAText != null)
            {
                _upgradeButtonAText.text = optionA;
            }

            if (_upgradeButtonBText != null)
            {
                _upgradeButtonBText.text = optionB;
            }

            if (_upgradeButtonCText != null)
            {
                _upgradeButtonCText.text = optionC;
            }

            RegisterUpgradeButtons();
            _levelUpPanel.SetActive(true);
            return true;
        }

        public void HideLevelUpChoices()
        {
            UnregisterUpgradeButtons();
            _onUpgradeSelected = null;
            if (_levelUpPanel != null)
            {
                _levelUpPanel.SetActive(false);
            }
        }

        private void RegisterUpgradeButtons()
        {
            if (_upgradeButtonA != null)
            {
                _upgradeButtonA.onClick.RemoveListener(OnUpgradeA);
                _upgradeButtonA.onClick.AddListener(OnUpgradeA);
            }

            if (_upgradeButtonB != null)
            {
                _upgradeButtonB.onClick.RemoveListener(OnUpgradeB);
                _upgradeButtonB.onClick.AddListener(OnUpgradeB);
            }

            if (_upgradeButtonC != null)
            {
                _upgradeButtonC.onClick.RemoveListener(OnUpgradeC);
                _upgradeButtonC.onClick.AddListener(OnUpgradeC);
            }
        }

        private void UnregisterUpgradeButtons()
        {
            if (_upgradeButtonA != null)
            {
                _upgradeButtonA.onClick.RemoveListener(OnUpgradeA);
            }

            if (_upgradeButtonB != null)
            {
                _upgradeButtonB.onClick.RemoveListener(OnUpgradeB);
            }

            if (_upgradeButtonC != null)
            {
                _upgradeButtonC.onClick.RemoveListener(OnUpgradeC);
            }
        }

        private void OnUpgradeA() => SelectUpgrade(0);

        private void OnUpgradeB() => SelectUpgrade(1);

        private void OnUpgradeC() => SelectUpgrade(2);

        private void SelectUpgrade(int index)
        {
            var handler = _onUpgradeSelected;
            HideLevelUpChoices();
            handler?.Invoke(index);
        }

        private void EnsureWeaponListRoot()
        {
            if (_isDestroying)
            {
                return;
            }

            if (_weaponListRoot != null)
            {
                return;
            }

            Transform parent = _weaponText != null ? _weaponText.transform.parent : transform;
            if (parent == null || !parent.gameObject.scene.IsValid())
            {
                return;
            }

            var existing = parent.Find("WeaponSlotList");
            if (existing == null)
            {
                var go = new GameObject("WeaponSlotList", typeof(RectTransform));
                go.transform.SetParent(parent, false);
                existing = go.transform;
            }

            _weaponListRoot = existing as RectTransform;
            if (_weaponListRoot == null)
            {
                _weaponListRoot = existing.gameObject.AddComponent<RectTransform>();
            }

            _weaponListRoot.anchorMin = new Vector2(0f, 1f);
            _weaponListRoot.anchorMax = new Vector2(0f, 1f);
            _weaponListRoot.pivot = new Vector2(0f, 1f);
            _weaponListRoot.anchoredPosition = new Vector2(16f, -284f);
            _weaponListRoot.sizeDelta = new Vector2(360f, 220f);
        }

        private void RebuildWeaponSlotButtons()
        {
            if (_weaponListRoot == null)
            {
                return;
            }

            for (int i = _weaponListRoot.childCount - 1; i >= 0; i--)
            {
                var child = _weaponListRoot.GetChild(i);
                if (child != null)
                {
                    Destroy(child.gameObject);
                }
            }

            _weaponSlotButtons.Clear();

            if (_weaponLoadout == null || _weaponLoadout.Slots == null)
            {
                return;
            }

            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            for (int i = 0; i < _weaponLoadout.Slots.Count; i++)
            {
                var slot = _weaponLoadout.Slots[i];
                if (slot == null || slot.IsLocked || slot.IsEmpty)
                {
                    continue;
                }

                var item = new GameObject($"WeaponSlot_{i}", typeof(RectTransform), typeof(Image), typeof(Button));
                item.transform.SetParent(_weaponListRoot, false);

                var itemRect = item.GetComponent<RectTransform>();
                itemRect.anchorMin = new Vector2(0f, 1f);
                itemRect.anchorMax = new Vector2(0f, 1f);
                itemRect.pivot = new Vector2(0f, 1f);
                itemRect.anchoredPosition = new Vector2(0f, -i * 52f);
                itemRect.sizeDelta = new Vector2(320f, 44f);

                var image = item.GetComponent<Image>();
                bool selected = _weaponLoadout.SelectedSlot != null
                    && _weaponLoadout.SelectedSlot.Definition != null
                    && _weaponLoadout.SelectedSlot.Definition.Id == slot.Definition.Id;
                image.color = selected ? new Color(0.22f, 0.33f, 0.5f, 0.9f) : new Color(0.12f, 0.14f, 0.2f, 0.75f);

                var icon = item.transform.Find("WeaponSlotIcon");
                if (icon == null)
                {
                    var iconGo = new GameObject("WeaponSlotIcon", typeof(RectTransform), typeof(Image));
                    iconGo.transform.SetParent(item.transform, false);
                    icon = iconGo.transform;
                }

                var iconImage = icon.GetComponent<Image>();
                var iconRect = iconImage.rectTransform;
                iconRect.anchorMin = new Vector2(0f, 0.5f);
                iconRect.anchorMax = new Vector2(0f, 0.5f);
                iconRect.sizeDelta = new Vector2(30f, 30f);
                iconRect.anchoredPosition = new Vector2(17f, 0f);
                iconImage.color = Color.white;
                iconImage.preserveAspect = true;
                iconImage.sprite = WeaponSpriteLibrary.GetWeaponIcon(slot.Definition) ?? RuntimeSpriteLibrary.GetDiamond();

                var button = item.GetComponent<Button>();
                button.targetGraphic = image;
                WeaponId weaponId = slot.Definition.Id;
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(() => OnWeaponSlotClicked(weaponId));

                var labelGo = new GameObject("Label", typeof(RectTransform), typeof(Text));
                labelGo.transform.SetParent(item.transform, false);
                var labelRect = labelGo.GetComponent<RectTransform>();
                labelRect.anchorMin = Vector2.zero;
                labelRect.anchorMax = Vector2.one;
                labelRect.offsetMin = new Vector2(44f, 6f);
                labelRect.offsetMax = new Vector2(-10f, -6f);

                var label = labelGo.GetComponent<Text>();
                label.font = font;
                label.fontSize = 20;
                label.alignment = TextAnchor.MiddleLeft;
                label.color = Color.white;
                label.text = $"{slot.Definition.DisplayName}  Lv.{slot.Level}";

                _weaponSlotButtons.Add(button);
            }
        }

        private void OnWeaponSlotClicked(WeaponId weaponId)
        {
            if (_weaponLoadout == null)
            {
                return;
            }

            if (_weaponLoadout.TrySelectWeapon(weaponId))
            {
                _weaponDetailVisible = true;
                ApplyWeaponDetailVisibility();
            }
        }

        private void EnsureRuntimeLayout()
        {
            ConfigureTopLeft(_hpText, new Vector2(100f, -58f), new Vector2(280f, 36f));
            ConfigureTopLeft(_expText, new Vector2(100f, -92f), new Vector2(280f, 36f));
            ConfigureTopLeft(_levelText, new Vector2(392f, -92f), new Vector2(120f, 36f));

            ConfigureTopLeft(_scoreText, new Vector2(470f, -22f), new Vector2(400f, 36f));
            ConfigureTopLeft(_stageText, new Vector2(470f, -56f), new Vector2(400f, 36f));
            ConfigureTopLeft(_timeText, new Vector2(470f, -90f), new Vector2(400f, 36f));
            ConfigureTopLeft(_highScoreText, new Vector2(470f, -124f), new Vector2(420f, 36f));
            ConfigureTopLeft(_enemiesSpawnedText, new Vector2(470f, -158f), new Vector2(420f, 36f));

            ConfigureTopLeft(_weaponText, new Vector2(72f, -196f), new Vector2(420f, 36f));
            ConfigureTopLeft(_ultimateText, new Vector2(72f, -236f), new Vector2(420f, 36f));
            ConfigureTopLeft(_statusText, new Vector2(0f, -56f), new Vector2(720f, 40f), true);

            if (_weaponIcon != null)
            {
                var rect = _weaponIcon.rectTransform;
                rect.anchorMin = new Vector2(0f, 1f);
                rect.anchorMax = new Vector2(0f, 1f);
                rect.pivot = new Vector2(0f, 1f);
                rect.anchoredPosition = new Vector2(20f, -194f);
                rect.sizeDelta = new Vector2(40f, 40f);
            }

            if (_hpText != null)
            {
                _hpText.alignment = TextAnchor.MiddleLeft;
            }

            if (_expText != null)
            {
                _expText.alignment = TextAnchor.MiddleLeft;
            }

            if (_levelText != null)
            {
                _levelText.alignment = TextAnchor.MiddleRight;
            }

            if (_expBarFill != null)
            {
                var expBarRect = _expBarFill.rectTransform;
                expBarRect.anchorMin = new Vector2(0f, 1f);
                expBarRect.anchorMax = new Vector2(0f, 1f);
                expBarRect.pivot = new Vector2(0f, 1f);
                expBarRect.anchoredPosition = new Vector2(102f, -156f);
                expBarRect.sizeDelta = new Vector2(336f, 14f);
            }

            if (_weaponDetailPanel != null)
            {
                var panelRect = _weaponDetailPanel.GetComponent<RectTransform>();
                if (panelRect != null)
                {
                    panelRect.anchorMin = new Vector2(0.5f, 0.5f);
                    panelRect.anchorMax = new Vector2(0.5f, 0.5f);
                    panelRect.pivot = new Vector2(0.5f, 0.5f);
                    panelRect.anchoredPosition = new Vector2(-120f, -30f);
                    panelRect.sizeDelta = new Vector2(460f, 240f);
                }
            }

            if (_levelUpPanel != null)
            {
                var panelRect = _levelUpPanel.GetComponent<RectTransform>();
                if (panelRect != null)
                {
                    panelRect.anchorMin = new Vector2(0.5f, 0.5f);
                    panelRect.anchorMax = new Vector2(0.5f, 0.5f);
                    panelRect.pivot = new Vector2(0.5f, 0.5f);
                    panelRect.anchoredPosition = Vector2.zero;
                    panelRect.sizeDelta = new Vector2(560f, 380f);
                }
            }
        }

        private static void ConfigureTopLeft(Text text, Vector2 anchoredPosition, Vector2 sizeDelta, bool centered = false)
        {
            if (text == null)
            {
                return;
            }

            var rect = text.rectTransform;
            if (centered)
            {
                rect.anchorMin = new Vector2(0.5f, 1f);
                rect.anchorMax = new Vector2(0.5f, 1f);
                rect.pivot = new Vector2(0.5f, 1f);
                text.alignment = TextAnchor.MiddleCenter;
            }
            else
            {
                rect.anchorMin = new Vector2(0f, 1f);
                rect.anchorMax = new Vector2(0f, 1f);
                rect.pivot = new Vector2(0f, 1f);
            }

            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = sizeDelta;
        }
    }
}
