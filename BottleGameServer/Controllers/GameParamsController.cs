using BottleGameServer.Data;
using BottleGameServer.Models.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BottleGameServer.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class GameParamsController : Controller
    {
        #region Fields

        private readonly ApplicationDbContext _context;
        private readonly ILogger<PlayerController> _logger;

        const string prefix = "[GameParams Controller]";

        #endregion Fields

        #region Constructor

        public GameParamsController(ApplicationDbContext context, ILogger<PlayerController> logger)
        {
            _context = context;
            _logger = logger;
        }

        #endregion Constructor

        #region ActionMethods

        /// <summary>
        /// Получить игровые параметры
        /// </summary>
        /// <response code="200">Параметры игры</response>
        /// <response code="404">Параметры не найдены</response>
        /// <response code="500">Внутренняя ошибка сервера</response>
        [Authorize(Roles = "Admin,Player")]
        [ProducesResponseType(200)]
        [ProducesResponseType(404)]
        [ProducesResponseType(500)]
        [HttpGet("get-game-params")]
        public async Task<ActionResult<GameParams>> GetGameParams()
        {
            try
            {
                // Поиск параметров
                var gameParams = await _context.GameParams.FirstOrDefaultAsync();

                if (gameParams is null)
                {
                    _logger.LogWarning("{Prefix} Игровые параметры не найдены", prefix);
                    return NotFound($"Игровые параметры не найдены");
                }

                _logger.LogInformation("{Prefix} Найдены игровые параметры", prefix);

                return Ok(gameParams);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "{Prefix} Ошибка при получении параметров игры", prefix);
                return StatusCode(500, "Внутренняя ошибка сервера");
            }
        }

        /// <summary>
        /// Обновить игровые параметры
        /// </summary>
        /// <param name="inputGameParams">Игровые параметры</param>
        /// <response code="200">Умпешное изменение параметров</response>
        /// <response code="404">Параметры не найдены</response>
        /// <response code="500">Внутренняя ошибка сервера</response>
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(200)]
        [ProducesResponseType(404)]
        [ProducesResponseType(500)]
        [HttpPost("update-game-params")]
        public async Task<ActionResult> UpdateGameParams([FromBody] GameParams inputGameParams)
        {
            try
            {
                // Проверка входных данных
                if (!ModelState.IsValid)
                {
                    _logger.LogWarning("{Prefix} Некорректные данные для изменения игровых параметров", prefix);
                    return BadRequest(ModelState);
                }

                // Поиск параметров
                var gameParams = await _context.GameParams.FirstOrDefaultAsync();

                if (gameParams is null)
                {
                    _logger.LogWarning("{Prefix} Игровые параметры не найдены", prefix);
                    return NotFound($"Игровые параметры не найдены");
                }

                _context.Entry(gameParams).CurrentValues.SetValues(inputGameParams);

                await _context.SaveChangesAsync();

                _logger.LogInformation("{Prefix} Игровые параметры обновлены", prefix);

                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "{Prefix} Ошибка при обновлении параметров игры", prefix);
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
        [ProducesResponseType(typeof(IEnumerable<string>), 200)]
        [ProducesResponseType(500)]
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
        [ProducesResponseType(typeof(IEnumerable<string>), 200)]
        [ProducesResponseType(500)]
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
        [ProducesResponseType(typeof(IEnumerable<string>), 200)]
        [ProducesResponseType(500)]
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
