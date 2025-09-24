namespace BottleGameServer.Authorization
{
    /// <summary>
    /// Служит для хэширования паролей
    /// </summary>
    public interface IPasswordHasher
    {
        /// <summary>
        /// Создает "соль" и хэширует пароль с ней
        /// </summary>
        (string Hash, string Salt) HashWithSalt(string password);

        /// <summary>
        /// Хуширует пароль
        /// </summary>
        string Hash(string input);

        /// <summary>
        /// Верифицирует пароль
        /// </summary>
        bool Verify(string hashedPassword, string providedPassword, string salt);
    }
}
