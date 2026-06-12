using System.ComponentModel;
using System.Reflection;
using System.Security.Claims;
using BottleGame.Domain.Constants;
using BottleGame.Application.Common;
using BottleGame.Application.DTOs.Requests;
using BottleGame.Application.DTOs.Responses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using BottleGame.Application.Interfaces;

namespace BottleGame.API.Controllers
{
    /// <summary>
    /// Контроллер управления игроками
    /// </summary>
    /// <remarks>
    /// Управление данными игроков, банами, статистикой и профилями
    /// </remarks>
    /// <remarks>
    /// Инициализирует новый экземпляр <see cref="PlayerController"/>.
    /// </remarks>
    /// <param name="playerService">Сервис для работы с игроками.</param>
    /// <param name="banService">Сервис для работы с банами.</param>
    [ApiController]
    [Route("api/[controller]")]
    [Tags("Управление игроками")]
    public class PlayerController(IPlayerService playerService, IBanService banService) : BaseApiController
    {
        #region Fields

        /// <summary>
        /// Сервис для работы с игроками.
        /// </summary>
        private readonly IPlayerService _playerService = playerService;

        /// <summary>
        /// Сервис для работы с банами игроков.
        /// </summary>
        private readonly IBanService _banService = banService;

        #endregion Fields

        #region Ban Methods

        /// <summary>
        /// Проверить, забанен ли пользователь
        /// </summary>
        /// <param name="playerId">ID игрока</param>
        /// <response code="200">Информация о бане</response>
        /// <response code="404">Игрок не найден</response>
        /// <response code="500">Внутренняя ошибка сервера</response>
        [Authorize(Roles = RoleNames.All)]
        [HttpGet("is-banned/{playerId:int}")]
        [ProducesResponseType(typeof(IsBannedResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<IsBannedResponse>> IsBanned([FromRoute] int playerId)
        {
            if (!HasAccessToPlayer(playerId))
            {
                return Unauthorized();
            }

            var result = await _playerService.IsBanned(playerId);
            return ToActionResult(result);
        }

        /// <summary>
        /// Забанить пользователя
        /// </summary>
        /// <response code="200">Пользователь успешно забанен</response>
        /// <response code="404">Игрок не найден</response>
        /// <response code="409">Пользователь уже забанен</response>
        /// <response code="500">Внутренняя ошибка сервера</response>
        [Authorize(Roles = RoleNames.Admin)]
        [HttpPost("ban-player")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> BanPlayer([FromBody] BanPlayerRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var result = await _banService.BanPlayer(request);

            if (result.IsSuccess)
            {
                return Ok($"Пользователь #{request.PlayerId} успешно забанен!");
            }

            return ToActionResult(result);
        }

        /// <summary>
        /// Разбанить пользователя
        /// </summary>
        /// <param name="playerId">ID игрока</param>
        /// <response code="200">Пользователь успешно разбанен</response>
        /// <response code="404">Игрок не найден или не имеет активного бана</response>
        /// <response code="500">Внутренняя ошибка сервера</response>
        [Authorize(Roles = RoleNames.Admin)]
        [HttpPost("unban-player/{playerId}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> UnbanPlayer(int playerId)
        {
            var result = await _banService.UnbanPlayer(playerId);

            if (result.IsSuccess)
            {
                return Ok($"Пользователь #{playerId} успешно разбанен!");
            }

            return ToActionResult(result);
        }

        #endregion Ban Methods

        #region Player Data Methods

        /// <summary>
        /// Получение списка всех игроков
        /// </summary>
        /// <response code="200">Список пользователей</response>
        /// <response code="404">Игроки не найдены</response>
        /// <response code="500">Внутренняя ошибка сервера</response>
        [Authorize(Roles = RoleNames.Admin)]
        [HttpGet("get-all-players")]
        [ProducesResponseType(typeof(IEnumerable<PlayerInfoResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<IEnumerable<PlayerInfoResponse>>> GetAllPlayers([FromQuery] long? telegramId = null,
                                                                                       [FromQuery] string? name = null,
                                                                                       [FromQuery] string? q = null)
        {
            var search = PlayerSearchQuery.Resolve(telegramId, name, q);
            var result = await _playerService.GetAllPlayers(search.TelegramId, search.Name);
            return ToActionResult(result);
        }

        /// <summary>
        /// Получение данных об игроке
        /// </summary>
        /// <response code="200">Пользователь</response>
        /// <response code="404">Игрок не найден</response>
        /// <response code="403">Игрок в бане</response>
        /// <response code="500">Внутренняя ошибка сервера</response>
        [Authorize(Roles = RoleNames.AdminOrPlayer)]
        [HttpGet("get-player/{playerId:int}")]
        [ProducesResponseType(typeof(PlayerInfoByIdResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<PlayerInfoByIdResponse>> GetPlayer([FromRoute] int playerId)
        {
            if (!HasAccessToPlayer(playerId))
            {
                return Unauthorized();
            }

            var requestingPlayerId = GetCurrentPlayerId();
            var isAdmin = User.IsInRole(RoleNames.Admin);

            var result = await _playerService.GetPlayerById(playerId, requestingPlayerId, isAdmin);
            return ToActionResult(result);
        }

        /// <summary>
        /// Изменить пользователя
        /// </summary>
        /// <param name="playerId">ID игрока</param>
        /// <param name="request">Модель запроса на изменение игрока</param>
        /// <response code="200">Пользователь успешно изменен</response>
        /// <response code="400">Неккоретные данные запроса</response>
        /// <response code="404">Игрок не найден</response>
        /// <response code="500">Внутренняя ошибка сервера</response>
        [Authorize(Roles = RoleNames.AdminOrPlayer)]
        [HttpPost("edit-player/{playerId:int}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> EditPlayer([FromRoute] int playerId, [FromBody] EditPlayerRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            if (!HasAccessToPlayer(playerId))
            {
                return Unauthorized();
            }

            var isAdmin = User.IsInRole(RoleNames.Admin);
            var result = await _playerService.EditPlayer(playerId, request, isAdmin);
            return ToActionResult(result);
        }

        /// <summary>
        /// Получение топ 100 списка всех игроков
        /// </summary>
        /// <response code="200">Список пользователей</response>
        /// <response code="404">Игроки не найдены</response>
        /// <response code="500">Внутренняя ошибка сервера</response>
        [Authorize(Roles = RoleNames.Admin)]
        [HttpGet("get-top100-players")]
        [ProducesResponseType(typeof(PlayerInfoResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<IEnumerable<PlayerInfoResponse>>> GetTop100Players()
        {
            var result = await _playerService.GetTop100Players();
            return ToActionResult(result);
        }

        #endregion Player Data Methods

        #region Statistics Methods

        /// <summary>
        /// Получить статистику по всем игрокам
        /// </summary>
        /// <response code="200">Статистика по всем игрокам</response>
        /// <response code="500">Внутренняя ошибка сервера</response>
        [Authorize(Roles = RoleNames.Admin)]
        [HttpGet("get-all-players-statistics")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<IEnumerable<PlayerStatisticsResponse>>> GetAllPlayersStatistics()
        {
            var result = await _playerService.GetAllPlayersStatistics();
            return ToActionResult(result);
        }

        /// <summary>
        /// Получить статистику по игроку
        /// </summary>
        /// <response code="200">Статистика по игроку</response>
        /// <response code="400">Игрок не найден</response>
        /// <response code="403">Игрок в бане</response>
        /// <response code="500">Внутренняя ошибка сервера</response>
        [Authorize(Roles = RoleNames.Admin)]
        [HttpGet("get-player-statistics/{playerId:int}")]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<PlayerStatisticsResponse>> GetPlayerStatistics([FromRoute] int playerId)
        {
            var requestingPlayerId = GetCurrentPlayerId();
            var isAdmin = User.IsInRole(RoleNames.Admin);

            var result = await _playerService.GetPlayerStatistics(playerId, requestingPlayerId, isAdmin);
            return ToActionResult(result);
        }

        /// <summary>
        /// Получить статистику сыгранных раундов для вывода по игроку
        /// </summary>
        /// <param name="playerId">ID игрока</param>
        /// <response code="200">Статистика сыгранных раундов для вывода</response>
        /// <response code="404">Игрок или статистика не найдены</response>
        /// <response code="403">Игрок в бане</response>
        /// <response code="500">Внутренняя ошибка сервера</response>
        [Authorize(Roles = RoleNames.AdminOrPlayer)]
        [HttpGet("get-player-rounds-statistics-for-withdrawal/{playerId:int}")]
        [ProducesResponseType(typeof(PlayerRoundsStatisticsForWithdrawalDTO), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<PlayerRoundsStatisticsForWithdrawalDTO>> GetPlayerRoundsStatisticsForWithdrawal([FromRoute] int playerId)
        {
            if (!HasAccessToPlayer(playerId))
            {
                return Unauthorized();
            }

            var requestingPlayerId = GetCurrentPlayerId();
            var isAdmin = User.IsInRole(RoleNames.Admin);

            var result = await _playerService.GetPlayerRoundsStatisticsForWithdrawal(playerId, requestingPlayerId, isAdmin);
            return ToActionResult(result);
        }

        /// <summary>
        /// Получить статистику сыгранных раундов для вывода по всем игрокам
        /// </summary>
        /// <response code="200">Список статистики сыгранных раундов для вывода</response>
        /// <response code="404">Статистика не найдена</response>
        /// <response code="500">Внутренняя ошибка сервера</response>
        [Authorize(Roles = RoleNames.AdminOrModerator)]
        [HttpGet("get-all-players-rounds-statistics-for-withdrawal")]
        [ProducesResponseType(typeof(IEnumerable<PlayerRoundsStatisticsForWithdrawalDTO>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<IEnumerable<PlayerRoundsStatisticsForWithdrawalDTO>>> GetAllPlayersRoundsStatisticsForWithdrawal()
        {
            var result = await _playerService.GetAllPlayersRoundsStatisticsForWithdrawal();
            return ToActionResult(result);
        }

        #endregion Statistics Methods

        #region Private Methods

        /// <summary>
        /// Проверяет, имеет ли текущий пользователь доступ к данным игрока
        /// </summary>
        private bool HasAccessToPlayer(int playerId)
        {
            if (User.IsInRole(RoleNames.Admin) || User.IsInRole(RoleNames.Moderator))
            {
                return true;
            }

            var currentPlayerId = GetCurrentPlayerId();
            return currentPlayerId == playerId;
        }

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

        /// <summary>
        /// Получает значение атрибута категории свойства
        /// </summary>
        public static string GetCategoryAttributeValue(Enum enumValue)
        {
            var field = enumValue.GetType().GetField(enumValue.ToString());
            var attribute = field?.GetCustomAttribute<CategoryAttribute>();
            return attribute?.Category!;
        }

        #endregion Private Methods
    }
}



