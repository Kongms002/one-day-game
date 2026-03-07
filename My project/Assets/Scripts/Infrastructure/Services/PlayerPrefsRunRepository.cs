using OneDayGame.Domain.Repositories;
using UnityEngine;

namespace OneDayGame.Infrastructure.Services
{
    public sealed class PlayerPrefsRunRepository : IRunRepository
    {
        private const string HighScoreKey = "OneDayGame.HighScore";
        private const string ControlLayoutKey = "OneDayGame.ControlLayoutJson";

        public void SaveHighScore(int highScore)
        {
            PlayerPrefs.SetInt(HighScoreKey, highScore);
            PlayerPrefs.Save();
        }

        public int LoadHighScore()
        {
            return PlayerPrefs.GetInt(HighScoreKey, 0);
        }

        public void SaveControlLayoutJson(string json)
        {
            PlayerPrefs.SetString(ControlLayoutKey, json ?? string.Empty);
            PlayerPrefs.Save();
        }

        public string LoadControlLayoutJson()
        {
            return PlayerPrefs.GetString(ControlLayoutKey, string.Empty);
        }
    }
}
