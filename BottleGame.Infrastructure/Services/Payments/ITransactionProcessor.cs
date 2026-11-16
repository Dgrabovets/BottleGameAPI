using BottleGame.Application.Common;
using BottleGame.Domain.Entities;

namespace BottleGame.Infrastructure.Services.Payments
{
    /// <summary>
    /// Обработка пополнений и выводов. Реализация выбирается по режиму процессинга:
    /// Simulated завершает операции локально, Live работает через платёжного провайдера
    /// </summary>
    public interface ITransactionProcessor
    {
        /// <summary>
        /// Проверяет условия, при которых игрок может создать пополнение или вывод
        /// </summary>
        /// <param name="player">Игрок</param>
        /// <param name="isDeposit">true для пополнения, false для вывода</param>
        /// <param name="gameParams">Игровые параметры</param>
        /// <returns>Успех или причина отказа</returns>
        Task<Result> CheckRequestAsync(Player player, bool isDeposit, GameParams gameParams);

        /// <summary>
        /// Запускает пополнение для уже созданной транзакции
        /// </summary>
        /// <param name="transaction">Транзакция пополнения</param>
        /// <param name="player">Игрок с загруженными настройками и балансом</param>
        /// <returns>Данные для ответа клиенту (ссылка на оплату и идентификатор ордера) или ошибка</returns>
        Task<Result<object?>> StartDepositAsync(MoneyTransaction transaction, Player player);

        /// <summary>
        /// Одобряет вывод: проверяет условия и отправляет выплату
        /// </summary>
        /// <param name="transaction">Транзакция вывода с игроком, его балансом и настройками</param>
        /// <param name="gameParams">Игровые параметры</param>
        /// <param name="appBalance">Баланс приложения</param>
        /// <returns>Успех или причина отказа</returns>
        Task<Result> ApproveWithdrawalAsync(MoneyTransaction transaction, GameParams gameParams, AppBalance appBalance);
    }
}
