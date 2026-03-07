using OneDayGame.Domain;
using OneDayGame.Domain.Gameplay;
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
        private Text _timeText;

        [SerializeField]
        private Text _highScoreText;

        [SerializeField]
        private Text _statusText;

        private IRunState _runState;

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
        }

        private void OnDestroy()
        {
            Bind(null);
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
                _hpText.text = $"HP: {(int) snapshot.Hp}/{(int) snapshot.MaxHp}";
            }

            if (_ultimateText != null)
            {
                _ultimateText.text = $"Ultimate: {(int) snapshot.Ultimate}";
            }

            if (_timeText != null)
            {
                _timeText.text = $"Time: {snapshot.ElapsedTime:F1}";
            }

            if (_highScoreText != null && _runState != null)
            {
                _highScoreText.text = $"HighScore: {_runState.HighScore}";
            }
        }

        private void OnRunEnded(RunSnapshot snapshot)
        {
            if (_statusText != null)
            {
                _statusText.text = "DEAD - tap anywhere to restart";
            }
        }

        private void OnRunRestarted(RunSnapshot snapshot)
        {
            if (_statusText != null)
            {
                _statusText.text = "";
            }

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
    }
}
