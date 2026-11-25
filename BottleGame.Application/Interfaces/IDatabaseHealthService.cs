namespace BottleGame.Application.Interfaces
{
    /// <summary>
    /// Service that checks the state of the database
    /// </summary>
    public interface IDatabaseHealthService
    {
        /// <summary>
        /// Checks the connection and counts the players
        /// </summary>
        /// <returns>Object with the status for the health response</returns>
        Task<object> CheckAsync();
    }
}
