namespace BottleGame.Application.DTOs.Processing
{
    /// <summary>
    /// Информация о валюте.
    /// </summary>
    public class CurrencyInfo
    {
        /// <summary>
        /// Код валюты (например, "rub").
        /// </summary>
        public string Code { get; set; } = string.Empty;

        /// <summary>
        /// Название валюты.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Активна ли валюта.
        /// </summary>
        public bool IsActive { get; set; }

        /// <summary>
        /// Инициализирует новый экземпляр <see cref="CurrencyInfo"/>.
        /// </summary>
        public CurrencyInfo()
        {
        }

        /// <summary>
        /// Инициализирует новый экземпляр <see cref="CurrencyInfo"/>.
        /// </summary>
        /// <param name="code">Код валюты.</param>
        /// <param name="name">Название валюты.</param>
        /// <param name="isActive">Активна ли валюта.</param>
        public CurrencyInfo(string code, string name, bool isActive)
        {
            Code = code;
            Name = name;
            IsActive = isActive;
        }
    }
}
