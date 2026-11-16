namespace BottleGame.Application.DTOs.Processing
{
    /// <summary>
    /// Currency information
    /// </summary>
    public class CurrencyInfo
    {
        /// <summary>
        /// Currency code (for example, "rub")
        /// </summary>
        public string Code { get; set; } = string.Empty;

        /// <summary>
        /// Currency name
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Whether the currency is active
        /// </summary>
        public bool IsActive { get; set; }

        /// <summary>
        /// Initializes a new instance of <see cref="CurrencyInfo"/>
        /// </summary>
        public CurrencyInfo()
        {
        }

        /// <summary>
        /// Initializes a new instance of <see cref="CurrencyInfo"/>
        /// </summary>
        /// <param name="code">Currency code</param>
        /// <param name="name">Currency name</param>
        /// <param name="isActive">Whether the currency is active</param>
        public CurrencyInfo(string code, string name, bool isActive)
        {
            Code = code;
            Name = name;
            IsActive = isActive;
        }
    }
}
