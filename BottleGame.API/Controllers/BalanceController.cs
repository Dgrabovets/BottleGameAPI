using System.Security.Claims;
using BottleGame.Domain.Constants;
using BottleGame.Application.DTOs.Requests;
using BottleGame.Application.DTOs.Responses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using BottleGame.Application.Interfaces;

namespace BottleGame.API.Controllers
{
    /// <summary>
    /// Контроллер управления балансом и транзакциями
    /// </summary>
    /// <remarks>
    /// Управление балансами игроков, депозитами, выводами и статусами транзакций
    /// </remarks>
    /// <remarks>
    /// Инициализирует новый экземпляр <see cref="BalanceController"/>.
    /// </remarks>
    /// <param name="transactionService">Сервис для работы с транзакциями.</param>
    [ApiController]
    [Route("api/[controller]")]
    [Tags("Баланс и транзакции")]
    public class BalanceController(ITransactionService transactionService) : BaseApiController
    {
        #region Fields

        /// <summary>
        /// Сервис для работы с транзакциями и балансами.
        /// </summary>
        private readonly ITransactionService _transactionService = transactionService;

        #endregion Fields

        #region Balance Methods

        /// <summary>
        /// Получение баланса игрока
        /// </summary>
        /// <param name="playerId">ID игрока</param>
        /// <response code="200">Возвращает текущий баланс игрока</response>
        /// <response code="404">Игрок не найден</response>
        /// <response code="403">Игрок в бане</response>
        /// <response code="500">Внутренняя ошибка сервера</response>
        [Authorize(Roles = RoleNames.AdminOrPlayer)]
        [HttpGet("get-player-balance/{playerId:int}")]
        [ProducesResponseType(typeof(GetPlayerBalanceResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<GetPlayerBalanceResponse>> GetPlayerBalance([FromRoute] int playerId)
        {
            var requestingPlayerId = GetCurrentPlayerId();
            var isAdmin = User.IsInRole(RoleNames.Admin);

            var result = await _transactionService.GetPlayerBalance(playerId, requestingPlayerId, isAdmin);
            return ToActionResult(result);
        }

        #endregion Balance Methods

        #region Transaction Methods

        /// <summary>
        /// Создать транзакцию
        /// </summary>
        /// <param name="playerId">ID игрока</param>
        /// <param name="request">Запрос для создания транзакции</param>
        /// <response code="200">Успешное создание транзакции</response>
        /// <response code="400">Некорректный запрос/Недостаточно средств</response>
        /// <response code="403">Игрок в бане</response>
        /// <response code="404">Игрок не найден</response>
        /// <response code="500">Внутренняя ошибка сервера</response>
        [Authorize(Roles = RoleNames.AdminOrPlayer)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [HttpPost("create-transaction/{playerId:int}")]
        public async Task<IActionResult> CreateTransaction([FromRoute] int playerId, [FromBody] MoneyTransactionRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var requestingPlayerId = GetCurrentPlayerId();
            var isAdmin = User.IsInRole(RoleNames.Admin);

            var result = await _transactionService.CreateTransaction(playerId, request, requestingPlayerId, isAdmin);
            return ToActionResultObject(result);
        }

        /// <summary>
        /// Изменить статус транзакции (пополнение или вывод).
        /// </summary>
        /// <param name="transactionId">ID транзакции</param>
        /// <param name="request">Модель запроса: Новый статус (2 - Одобрено, 3 - Отклонено)</param>
        /// <response code="200">Статус транзакции успешно изменен</response>
        /// <response code="400">Некорректный запрос/Неверный статус</response>
        /// <response code="404">Транзакция не найдена</response>
        /// <response code="500">Внутренняя ошибка сервера</response>
        [Authorize(Roles = RoleNames.AdminOrModerator)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [HttpPost("update-transaction-status/{transactionId:int}")]
        public async Task<IActionResult> UpdateTransactionStatus([FromRoute] int transactionId, [FromBody] ChangeTransactionStatusRequest request)
        {
            var result = await _transactionService.UpdateTransactionStatus(transactionId, request);
            return ToActionResult(result);
        }

        /// <summary>
        /// Получить все транзакции пользователя
        /// </summary>
        /// <param name="playerId">ID игрока</param>
        /// <response code="200">Список транзакций пользователя</response>
        /// <response code="400">Некорректные данные запроса</response>
        /// <response code="404">Игрок не найден</response>
        /// <response code="403">Игрок в бане</response>
        /// <response code="500">Внутренняя ошибка сервера</response>
        [Authorize(Roles = RoleNames.AdminOrPlayer)]
        [HttpGet("get-player-transactions/{playerId:int}")]
        [ProducesResponseType(typeof(GetPlayerTransactionsResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<GetPlayerTransactionsResponse>> GetPlayerTransactions([FromRoute] int playerId)
        {
            var requestingPlayerId = GetCurrentPlayerId();
            var isAdmin = User.IsInRole(RoleNames.Admin);

            var result = await _transactionService.GetPlayerTransactions(playerId, requestingPlayerId, isAdmin);
            return ToActionResult(result);
        }

        /// <summary>
        /// Получить все выводы
        /// </summary>
        /// <param name="typeId">Тип транзакции</param>
        /// <response code="200">Список всех транзакций по указанному типу</response>
        /// <response code="400">Некорректные данные запроса</response>
        /// <response code="500">Внутренняя ошибка сервера</response>
        [Authorize(Roles = RoleNames.Admin)]
        [HttpGet("get-all-transactions")]
        [ProducesResponseType(typeof(IEnumerable<GetPlayerTransactionsResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<IEnumerable<GetPlayerTransactionsResponse>>> GetAllTransactions([FromQuery] int? typeId)
        {
            var result = await _transactionService.GetAllTransactions(typeId);
            return ToActionResult(result);
        }

        #endregion Transaction Methods

        #region Reference Data Methods

        /// <summary>
        /// Получить все доступные статусы транзакций
        /// </summary>
        /// <response code="200">Список всех статусов транзакций</response>
        /// <response code="500">Внутренняя ошибка сервера</response>
        [Authorize(Roles = RoleNames.Admin)]
        [HttpGet("get-all-transaction-statuses")]
        [ProducesResponseType(typeof(IEnumerable<TransactionStatusOrTypeDTO>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<IEnumerable<TransactionStatusOrTypeDTO>>> GetAllTransactionStatuses()
        {
            var result = await _transactionService.GetAllTransactionStatuses();
            return ToActionResult(result);
        }

        /// <summary>
        /// Получить все возможные типы транзакций
        /// </summary>
        /// <response code="200">Список всех типов транзакций</response>
        /// <response code="500">Внутренняя ошибка сервера</response>
        [Authorize(Roles = RoleNames.Admin)]
        [HttpGet("get-all-transaction-types")]
        [ProducesResponseType(typeof(IEnumerable<TransactionStatusOrTypeDTO>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<IEnumerable<TransactionStatusOrTypeDTO>>> GetAllTransactionTypes()
        {
            var result = await _transactionService.GetAllTransactionTypes();
            return ToActionResult(result);
        }

        #endregion Reference Data Methods

        #region Private Methods

        /// <summary>
        /// Получает ID текущего игрока из claims
        /// </summary>
        private int? GetCurrentPlayerId()
        {
            var claim = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier);
            if (claim is not null && int.TryParse(claim.Value, out var playerId))
            {
                return playerId;
            }
            return null;
        }

        #endregion Private Methods
    }
}



