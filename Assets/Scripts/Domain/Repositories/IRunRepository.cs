namespace OneDayGame.Domain.Repositories
{
    public interface IRunRepository
    {
        void SaveHighScore(int highScore);

        int LoadHighScore();

        void SaveControlLayoutJson(string json);

        string LoadControlLayoutJson();
    }
}
