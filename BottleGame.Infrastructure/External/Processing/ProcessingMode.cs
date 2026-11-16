namespace BottleGame.Infrastructure.External.Processing
{
    /// <summary>
    /// Режим работы с платёжным процессингом.
    /// </summary>
    public enum ProcessingMode
    {
        /// <summary>
        /// Пополнения и выводы завершаются локально без обращения к процессингу и без реальных денег.
        /// Для разработки, тестов и демонстраций.
        /// </summary>
        Simulated,

        /// <summary>
        /// Платежи и выплаты проходят через платёжный процессинг, статусы приходят вебхуками.
        /// </summary>
        Live
    }
}
