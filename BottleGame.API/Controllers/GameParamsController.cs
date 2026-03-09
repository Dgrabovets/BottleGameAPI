using BottleGame.Infrastructure.Data;
using BottleGame.Domain.Entities;
using BottleGame.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BottleGame.Application.Interfaces;

namespace BottleGame.API.Controllers
{
    /// <summary>
    /// Контроллер игровых параметров
    /// </summary>
    /// <remarks>
    /// Управление настройками игры, методами оплаты и валютами
    /// </remarks>
    /// <remarks>
    /// Инициализирует новый экземпляр <see cref="GameParamsController"/>
    /// </remarks>
    /// <param name="context">Контекст базы данных</param>
    /// <param name="logger">Логгер</param>
    /// <param name="cacheService">Сервис кэширования</param>
    [ApiController]
    [Route("api/[controller]")]
    [Tags("Игровые параметры")]
    public class GameParamsController(ApplicationDbContext context, ILogger<GameParamsController> logger, ICacheService cacheService) : Controller
    {
        #region Fields

        /// <summary>
        /// Контекст базы данных
        /// </summary>
        private readonly ApplicationDbContext _context = context;

        /// <summary>
        /// Логгер
        /// </summary>
        private readonly ILogger<GameParamsController> _logger = logger;

        /// <summary>
        /// Сервис кэширования
        /// </summary>
        private readonly ICacheService _cacheService = cacheService;

        /// <summary>
        /// Префикс для логов
        /// </summary>
        private const string Prefix = "[GameParams Controller]";

        #endregion Fields

        #region ActionMethods

        /// <summary>
        /// Получить игровые параметры
        /// </summary>
        /// <response code="200">Параметры игры</response>
        /// <response code="404">Параметры не найдены</response>
        /// <response code="500">Внутренняя ошибка сервера</response>
        [Authorize(Roles = "Admin,Player")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [HttpGet("get-game-params")]
        public async Task<ActionResult<GameParams>> GetGameParams()
        {
            try
            {
                // Получение параметров из кэша
                var gameParams = await _cacheService.GetGameParamsAsync();

                if (gameParams is null)
                {
                    _logger.LogWarning("{Prefix} Игровые параметры не найдены", Prefix);
                    return NotFound($"Игровые параметры не найдены");
                }

                _logger.LogInformation("{Prefix} Найдены игровые параметры (из кэша)", Prefix);

                return Ok(gameParams);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "{Prefix} Ошибка при получении параметров игры", Prefix);
                return StatusCode(500, "Внутренняя ошибка сервера");
            }
        }

        /// <summary>
        /// Обновить игровые параметры
        /// </summary>
        /// <param name="inputGameParams">Игровые параметры</param>
        /// <response code="200">Успешное изменение параметров</response>
        /// <response code="404">Параметры не найдены</response>
        /// <response code="500">Внутренняя ошибка сервера</response>
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [HttpPost("update-game-params")]
        public async Task<ActionResult> UpdateGameParams([FromBody] GameParams inputGameParams)
        {
            try
            {
                // Проверка входных данных
                if (!ModelState.IsValid)
                {
                    _logger.LogWarning("{Prefix} Некорректные данные для изменения игровых параметров", Prefix);
                    return BadRequest(ModelState);
                }

                // Поиск параметров
                var gameParams = await _context.GameParams.FirstOrDefaultAsync();

                if (gameParams is null)
                {
                    _logger.LogWarning("{Prefix} Игровые параметры не найдены", Prefix);
                    return NotFound($"Игровые параметры не найдены");
                }

                _context.Entry(gameParams).CurrentValues.SetValues(inputGameParams);

                await _context.SaveChangesAsync();

                // Инвалидация кэша после обновления
                _cacheService.InvalidateGameParams();

                _logger.LogInformation("{Prefix} Игровые параметры обновлены, кэш инвалидирован", Prefix);

                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "{Prefix} Ошибка при обновлении параметров игры", Prefix);
                return StatusCode(500, "Внутренняя ошибка сервера");
            }
        }

        /// <summary>
        /// Получить все доступные методы выплат
        /// </summary>
        /// <response code="200">Список всех методов выплат</response>
        /// <response code="500">Внутренняя ошибка сервера</response>
        [Authorize(Roles = "Player,Admin")]
        [HttpGet("get-all-payout-methods")]
        [ProducesResponseType(typeof(IEnumerable<string>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public ActionResult<IEnumerable<string>> GetAllPayoutMethods()
        {
            try
            {
                var methods = Enum.GetNames(typeof(PayoutMethodType));
                return Ok(methods);
            }
            catch (Exception)
            {
                return StatusCode(500, "Внутренняя ошибка сервера");
            }
        }

        /// <summary>
        /// Получить все доступные методы пополнения
        /// </summary>
        /// <response code="200">Список всех методов пополнения</response>
        /// <response code="500">Внутренняя ошибка сервера</response>
        [Authorize(Roles = "Player,Admin")]
        [HttpGet("get-all-payment-methods")]
        [ProducesResponseType(typeof(IEnumerable<string>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public ActionResult<IEnumerable<string>> GetAllPaymentMethods()
        {
            try
            {
                var methods = Enum.GetNames(typeof(PaymentMethodType));
                return Ok(methods);
            }
            catch (Exception)
            {
                return StatusCode(500, "Внутренняя ошибка сервера");
            }
        }

        /// <summary>
        /// Получить все доступные валюты
        /// </summary>
        /// <response code="200">Список всех валют</response>
        /// <response code="500">Внутренняя ошибка сервера</response>
        [Authorize(Roles = "Player,Admin")]
        [HttpGet("get-all-currencies")]
        [ProducesResponseType(typeof(IEnumerable<string>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public ActionResult<IEnumerable<string>> GetAllCurrencies()
        {
            try
            {
                var currencies = Enum.GetNames(typeof(CurrencyType));
                return Ok(currencies);
            }
            catch (Exception)
            {
                return StatusCode(500, "Внутренняя ошибка сервера");
            }
        }

        #endregion ActionMethods
    }
}



