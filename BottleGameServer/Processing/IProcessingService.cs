using BottleGameServer.Processing.Models;

namespace BottleGameServer.Processing
{
    /// <summary>
    /// Сервис процессинга
    /// </summary>
    public interface IProcessingService
    {
        /// <summary>
        /// Создать платеж на процессинговой площадке
        /// </summary>
        /// <param name="request">Запрос на создание платежа</param>
        Task<CreatePaymentResult> CreatePaymentAsync(CreatePaymentRequest request);

        /// <summary>
        /// Создать вывод на процессинговой площадке
        /// </summary>
        /// <param name="request">Запрос на создание вывода</param>
        Task<CreatePayoutResult> CreatePayoutAsync(CreatePayoutRequest request);
    }
}
