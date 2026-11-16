using System.Security.Claims;
using BottleGame.Domain.Constants;
using BottleGame.Application.DTOs.Requests;
using BottleGame.Application.DTOs.Responses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using BottleGame.Application.Interfaces;
using System.ComponentModel;

namespace BottleGame.API.Controllers
{
    /// <summary>
    /// Контроллер администрирования
    /// </summary>
    /// <remarks>
    /// Управление балансом приложения, статистикой и модераторами
    /// </remarks>
    /// <remarks>
    /// Инициализирует новый экземпляр <see cref="AdministrationController"/>.
    /// </remarks>
    /// <param name="appBalanceService">Сервис баланса приложения.</param>
    /// <param name="statisticsService">Сервис статистики.</param>
    /// <param name="moderatorService">Сервис управления модераторами.</param>
    [ApiController]
    [Route("api/admin")]
    [Tags("Администрирование")]
    public class AdministrationController(
        IAppBalanceService appBalanceService,
        IStatisticsService statisticsService,
        IModeratorService moderatorService) : BaseApiController
    {
        #region Fields

        /// <summary>
        /// Сервис баланса приложения.
        /// </summary>
        private readonly IAppBalanceService _appBalanceService = appBalanceService;

        /// <summary>
        /// Сервис статистики.
        /// </summary>
        private readonly IStatisticsService _statisticsService = statisticsService;

        /// <summary>
        /// Сервис управления модераторами.
        /// </summary>
        private readonly IModeratorService _moderatorService = moderatorService;

        #endregion Fields

        #region App Balance Methods

        /// <summary>
        /// Получение текущего баланса приложения
        /// </summary>
        [Authorize(Roles = RoleNames.Admin)]
        [HttpGet("get-app-balance")]
        [ProducesResponseType(typeof(AppBalanceDTO), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<AppBalanceDTO>> GetAppBalance()
        {
            var result = await _appBalanceService.GetAppBalance();
            return ToActionResult(result);
        }

        /// <summary>
        /// Обновление нижнего порога баланса приложения
        /// </summary>
        /// <response code="200">Нижний порог успешно обновлен</response>
        /// <response code="400">Не найдены данные для обновления, скорее всего нарушена целостность данных или некорректные данные запроса</response>
        /// <response code="500">Внутренняя ошибка сервера</response>
        [Authorize(Roles = RoleNames.Admin)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [HttpPost("update-threshold")]
        public async Task<IActionResult> UpdateAppBalanceThreshold(
            [Description("Запрос на обновление нижнего порога баланса приложения")] 
            [FromBody] UpdateAppBalanceThresholdRequest request
        )
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var adminId = GetCurrentAdminId();
            if (!adminId.HasValue)
            {
                return Unauthorized();
            }

            var result = await _appBalanceService.UpdateAppBalanceThreshold(request, adminId.Value);

            if (result.IsSuccess)
            {
                return Ok(new { Message = "Нижний порог успешно обновлен" });
            }

            return ToActionResult(result);
        }

        /// <summary>
        /// Пополнение баланса приложения
        /// </summary>
        /// <response code="200">Баланс успешно пополнен</response>
        /// <response code="400">Не найдены данные для обновления или некорректные данные запроса</response>
        /// <response code="500">Внутренняя ошибка сервера</response>
        [Authorize(Roles = RoleNames.Admin)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [HttpPost("topup-balance")]
        public async Task<IActionResult> TopUpAppBalance(
            [Description("Запрос на пополнение баланса приложения")]
            [FromBody] TopUpAppBalanceRequest request
        )
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var adminId = GetCurrentAdminId();
            if (!adminId.HasValue)
            {
                return Unauthorized();
            }

            var result = await _appBalanceService.TopUpAppBalance(request, adminId.Value);

            if (result.IsSuccess)
            {
                return Ok(new
                {
                    Message = "Баланс успешно пополнен",
                    NewBalance = result.Value
                });
            }

            return ToActionResultObject(result);
        }

        #endregion App Balance Methods

        #region Statistics Methods

        /// <summary>
        /// Получение админской статистики
        /// </summary>
        /// <response code="200">Статистика</response>
        /// <response code="400">Нужные данные не найдены, скорее всего нарушена целостность данных</response>
        /// <response code="500">Внутренняя ошибка сервера</response>
        [Authorize(Roles = RoleNames.Admin)]
        [ProducesResponseType(typeof(GetAppGeneralStatisticsResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [HttpGet("get-statistics")]
        public async Task<ActionResult<GetAppGeneralStatisticsResponse>> GetStatistics([FromQuery] DateTime? dateFrom,
                                                                                       [FromQuery] DateTime? dateTill)
        {
            var request = new GetAppGeneralStatisticsRequest
            {
                DateFrom = dateFrom,
                DateTill = dateTill
            };

            var result = await _statisticsService.GetStatistics(request);
            return ToActionResult(result);
        }

        /// <summary>
        /// Получение временной шкалы статистики для графиков
        /// </summary>
        [Authorize(Roles = RoleNames.Admin)]
        [ProducesResponseType(typeof(GetAppStatisticsTimelineResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [HttpGet("get-statistics-timeline")]
        public async Task<ActionResult<GetAppStatisticsTimelineResponse>> GetStatisticsTimeline(
            [FromQuery] DateTime dateFrom,
            [FromQuery] DateTime dateTill)
        {
            var request = new GetAppStatisticsTimelineRequest
            {
                DateFrom = dateFrom,
                DateTill = dateTill
            };

            var result = await _statisticsService.GetStatisticsTimeline(request);
            return ToActionResult(result);
        }

        #endregion Statistics Methods

        #region Moderator Methods

        /// <summary>
        /// Регистрация нового модератора
        /// </summary>
        /// <param name="authRequest">Данные для регистрации (логин и пароль)</param>
        /// <response code="200">Модератор успешно зарегистрирован</response>
        /// <response code="400">Некорректные данные запроса</response>
        /// <response code="409">Модератор уже есть</response>
        /// <response code="500">Внутренняя ошибка сервера</response>
        [Authorize(Roles = RoleNames.Admin)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [HttpPost("moderator-register")]
        public async Task<IActionResult> RegisterModerator(
            [Description("Модель учетных данных для административных ролей")]
            [FromBody] AdministrationRoleUserCredentials authRequest
        )
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var result = await _moderatorService.RegisterModerator(authRequest);

            if (result.IsSuccess)
            {
                return Ok(new { Message = "Модератор успешно зарегистрирован" });
            }

            return ToActionResult(result);
        }

        /// <summary>
        /// Получение списка модераторов или конкретного модератора по ID
        /// </summary>
        /// <param name="moderatorId">ID модератора (опционально)</param>
        /// <response code="200">Успешный запрос</response>
        /// <response code="404">Модератор не найден (при запросе по ID)</response>
        /// <response code="500">Внутренняя ошибка сервера</response>
        [Authorize(Roles = RoleNames.Admin)]
        [ProducesResponseType(typeof(IEnumerable<ModeratorDTO>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ModeratorDTO), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [HttpGet("get-moderators")]
        public async Task<IActionResult> GetModerators(
            [Description("Идентификатор модератора")]
            [FromQuery] int? moderatorId = null
        )
        {
            var result = await _moderatorService.GetModerators(moderatorId);
            return ToActionResultObject(result);
        }

        /// <summary>
        /// Удаление модератора по ID
        /// </summary>
        /// <param name="moderatorId">ID модератора</param>
        /// <response code="200">Модератор успешно удален</response>
        /// <response code="404">Модератор не найден</response>
        /// <response code="500">Внутренняя ошибка сервера</response>
        [Authorize(Roles = RoleNames.Admin)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [HttpDelete("delete-moderator/{moderatorId}")]
        public async Task<IActionResult> DeleteModerator(
            [Description("Идентификатор модератора")]
            [FromRoute] int moderatorId
        )
        {
            var result = await _moderatorService.DeleteModerator(moderatorId);

            if (result.IsSuccess)
            {
                return Ok(new { Message = "Модератор успешно удален" });
            }

            return ToActionResult(result);
        }

        #endregion Moderator Methods

        #region Private Methods

        /// <summary>
        /// Получает ID текущего администратора из claims
        /// </summary>
        private int? GetCurrentAdminId()
        {
            var claim = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier);
            if (claim is not null && int.TryParse(claim.Value, out var adminId))
            {
                return adminId;
            }
            return null;
        }

        #endregion Private Methods
    }
}



