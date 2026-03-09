using System.Security.Claims;
using BottleGame.Domain.Constants;
using BottleGame.Application.DTOs.Requests;
using BottleGame.Application.DTOs.Responses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using BottleGame.Application.Interfaces;

namespace BottleGame.API.Controllers
{
    /// <summary>
    /// Контроллер игровых раундов
    /// </summary>
    /// <remarks>
    /// Управление игровыми раундами, завершение игр и получение статистики
    /// </remarks>
    /// <remarks>
    /// Инициализирует новый экземпляр <see cref="GameRoundController"/>.
    /// </remarks>
    /// <param name="gameRoundService">Сервис для работы с игровыми раундами.</param>
    [ApiController]
    [Route("api/[controller]")]
    [Tags("Игровые раунды")]
    public class GameRoundController(IGameRoundService gameRoundService) : BaseApiController
    {
        #region Fields

        /// <summary>
        /// Сервис для работы с игровыми раундами.
        /// </summary>
        private readonly IGameRoundService _gameRoundService = gameRoundService;

        #endregion Fields

        #region Action Methods

        /// <summary>
        /// Завершить раунд
        /// </summary>
        /// <param name="playerId">ID игрока</param>
        /// <param name="request">Запрос для завершения раунда</param>
        /// <response code="200">Успешное завершение раунда</response>
        /// <response code="400">Некорректный запрос</response>
        /// <response code="403">Игрок в бане</response>
        /// <response code="404">Игрок не найден</response>
        /// <response code="500">Внутренняя ошибка сервера</response>
        [Authorize(Roles = RoleNames.AdminOrPlayer)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [EnableRateLimiting("game")]
        [HttpPost("finish-game-round/{playerId:int}")]
        public async Task<IActionResult> FinishGameRound([FromRoute] int playerId, [FromBody] FinishRoundRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var requestingPlayerId = GetCurrentPlayerId();
            var isAdmin = User.IsInRole(RoleNames.Admin);

            var result = await _gameRoundService.FinishGameRound(playerId, request, requestingPlayerId, isAdmin);
            return ToActionResult(result);
        }

        /// <summary>
        /// Получить сыгранные раунды игрока
        /// </summary>
        /// <param name="playerId">ID игрока</param>
        /// <response code="200">Список раундов</response>
        /// <response code="404">Игрок не найден</response>
        /// <response code="403">Игрок в бане</response>
        /// <response code="500">Внутренняя ошибка сервера</response>
        [Authorize(Roles = RoleNames.Admin)]
        [ProducesResponseType(typeof(GetPlayerPlayedRoundsResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [HttpGet("get-player-played-rounds/{playerId:int}")]
        public async Task<ActionResult<GetPlayerPlayedRoundsResponse>> GetPlayerPlayedRounds([FromRoute] int playerId)
        {
            var result = await _gameRoundService.GetPlayerPlayedRounds(playerId);
            return ToActionResult(result);
        }

        /// <summary>
        /// Получить прибыли за период
        /// </summary>
        /// <response code="200">Прибыль</response>
        /// <response code="404">Раунды не найдены</response>
        /// <response code="500">Внутренняя ошибка сервера</response>
        [Authorize(Roles = RoleNames.Admin)]
        [ProducesResponseType(typeof(GetProfitResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [HttpGet("get-profit")]
        public async Task<ActionResult<GetProfitResponse>> GetProfit([FromBody] GetProfitRequest request)
        {
            var result = await _gameRoundService.GetProfit(request);
            return ToActionResult(result);
        }

        #endregion Action Methods

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



