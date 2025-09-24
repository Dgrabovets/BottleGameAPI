using System.Security.Claims;
using BottleGameServer.Authorization;
using BottleGameServer.Authorization.Models;
using BottleGameServer.Data;
using BottleGameServer.Models.DTOs.Requests;
using BottleGameServer.Models.DTOs.Responses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BottleGameServer.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AdminController : Controller
    {
        #region Fields

        private readonly ApplicationDbContext _context;
        private readonly ILogger<AuthController> _logger;
        private readonly IPasswordHasher _passwordHasher;

        const string prefix = "[Admin Controller]";

        #endregion Fields

        #region Constructor

        public AdminController(ApplicationDbContext context, ILogger<AuthController> logger, IPasswordHasher passwordHasher)
        {
            _context = context;
            _logger = logger;
            _passwordHasher = passwordHasher;
        }

        #endregion Constructor

        #region ActionMethods

        /// <summary>
        /// Получение текущего баланса приложения
        /// </summary>
        [Authorize(Roles = "Admin")]
        [HttpGet("get-app-balance")]
        [ProducesResponseType(typeof(AppBalanceDTO), 200)]
        [ProducesResponseType(404)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> GetAppBalance()
        {
            try
            {
                var balance = await _context.AppBalance.FirstOrDefaultAsync();
                if (balance is null)
                {
                    _logger.LogWarning("{Prefix} Запись баланса приложения не найдена", prefix);
                    return NotFound("Баланс приложения не инициализирован");
                }

                // Получаем всех модераторов заранее
                var admins = await _context.AdminCreds
                    .ToDictionaryAsync(m => m.Id, m => m.Login);

                // Безопасное получение логинов
                string GetAdminLogin(int? adminId) =>
                    adminId.HasValue && admins.TryGetValue(adminId.Value, out var login)
                        ? login
                        : null!;

                var result = new AppBalanceDTO
                {
                    Amount = balance.Amount,
                    BalanceAmountUpdatedAt = balance.BalanceAmountUpdatedAt,
                    BalanceAmountAdminIdUpdatedBy = balance.BalanceAmountAdminIdUpdatedBy,
                    BalanceAmountAdminLoginUpdatedBy = GetAdminLogin(balance.BalanceAmountAdminIdUpdatedBy),
                    LowerThreshold = balance.LowerThreshold,
                    LowerThresholdUpdatedAt = balance.LowerThresholdUpdatedAt,
                    LowerThresholdAdminIdUpdatedBy = balance.LowerThresholdAdminIdUpdatedBy,
                    LowerThresholdAdminLoginUpdatedBy = GetAdminLogin(balance.LowerThresholdAdminIdUpdatedBy)
                };

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "{Prefix} Ошибка при получении баланса приложения", prefix);
                return StatusCode(500, "Внутренняя ошибка сервера");
            }
        }

        /// <summary>
        /// Обновление нижнего порога баланса приложения
        /// </summary>
        /// <response code="200">Нижний порог успешно обновлен</response>
        /// <response code="400">Не найдены данные для обновления, скорее всего нарушена целостность данных или некорректные данные запроса</response>
        /// <response code="500">Внутренняя ошибка сервера</response>
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(500)]
        [HttpPost("update-threshold")]
        public async Task<IActionResult> UpdateAppBalanceThreshold([FromBody] UpdateAppBalanceThresholdRequest request)
        {
            try
            {
                // Проверка входных данных
                if (!ModelState.IsValid)
                {
                    _logger.LogWarning("{Prefix} Некорректные данные для обновления нижнего порога баланса приложения", prefix);
                    return BadRequest(ModelState);
                }

                var appBalance = await _context.AppBalance.FirstOrDefaultAsync();

                if (appBalance is null)
                {
                    _logger.LogWarning("{Prefix} Не найдены данные для обновления, скорее всего нарушена целостность данных", prefix);
                    return BadRequest($"Не найдены данные для обновления, скорее всего нарушена целостность данных");
                }

                appBalance.LowerThreshold = request.LowerThreshold;
                appBalance.LowerThresholdUpdatedAt = DateTime.UtcNow;
                appBalance.LowerThresholdAdminIdUpdatedBy = Convert.ToInt32(User.Claims.First(c => c.Type == ClaimTypes.NameIdentifier).Value);

                await _context.SaveChangesAsync();

                _logger.LogInformation("{Prefix} Обновлен нижний порог для баланса приложения: {LowerThreshold}", prefix, request.LowerThreshold);

                return Ok(new { Message = "Нижний порог успешно обновлен" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "{Prefix} Ошибка при обновлении нижнего порога баланса приложения", prefix);
                return StatusCode(500, "Внутренняя ошибка сервера");
            }
        }

        /// <summary>
        /// Пополнение баланса приложения
        /// </summary>
        /// <response code="200">Баланс успешно пополнен</response>
        /// <response code="400">Не найдены данные для обновления или некорректные данные запроса</response>
        /// <response code="500">Внутренняя ошибка сервера</response>
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(500)]
        [HttpPost("topup-balance")]
        public async Task<IActionResult> TopUpAppBalance([FromBody] TopUpAppBalanceRequest request)
        {
            try
            {
                // Проверка входных данных
                if (!ModelState.IsValid)
                {
                    _logger.LogWarning("{Prefix} Некорректные данные для пополнения баланса", prefix);
                    return BadRequest(ModelState);
                }

                // Проверка что сумма пополнения положительная
                if (request.Amount <= 0)
                {
                    _logger.LogWarning("{Prefix} Сумма пополнения должна быть положительной: {Amount}", prefix, request.Amount);
                    return BadRequest("Сумма пополнения должна быть положительной");
                }

                var appBalance = await _context.AppBalance.FirstOrDefaultAsync();

                if (appBalance is null)
                {
                    _logger.LogWarning("{Prefix} Не найдены данные баланса приложения", prefix);
                    return BadRequest("Не найдены данные баланса приложения");
                }

                // Увеличиваем баланс
                appBalance.Amount += request.Amount;

                // Обновляем метаданные
                appBalance.BalanceAmountUpdatedAt = DateTime.UtcNow;
                appBalance.BalanceAmountAdminIdUpdatedBy = Convert.ToInt32(User.Claims.First(c => c.Type == ClaimTypes.NameIdentifier).Value);

                await _context.SaveChangesAsync();

                _logger.LogInformation("{Prefix} Баланс приложения пополнен на {Amount}. Новый баланс: {NewBalance}",
                    prefix, request.Amount, appBalance.Amount);

                return Ok(new
                {
                    Message = "Баланс успешно пополнен",
                    NewBalance = appBalance.Amount
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "{Prefix} Ошибка при пополнении баланса приложения", prefix);
                return StatusCode(500, "Внутренняя ошибка сервера");
            }
        }

        /// <summary>
        /// Получение админской статистики
        /// </summary>
        /// <response code="200">Статистика</response>
        /// <response code="400">Нужные данные не найдены, скорее всего нарушена целостность данных</response>
        /// <response code="500">Внутренняя ошибка сервера</response>
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(typeof(GetAdminStatisticsResponse), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(500)]
        [HttpGet("get-statistics")]
        public async Task<ActionResult<GetAdminStatisticsResponse>> GetStatistics()
        {
            try
            {
                // Поиск пользователей со всей необходмой информацией
                var players = await _context.Players.AsNoTracking().ToListAsync();  
                var balances = await _context.PlayersBalances.AsNoTracking().ToListAsync();

                // Проверка учетных данных
                if (!players.Any() || !balances.Any())
                {
                    _logger.LogWarning("{Prefix} Нужные данные не найдены, скорее всего нарушена целостность данных", prefix);
                    return BadRequest("Нужные данные не найдены, скорее всего нарушена целостность данных");
                }

                var transactions = await _context.MoneyTransactions.AsNoTracking().ToListAsync();

                _logger.LogInformation("{Prefix} Успешно получена админская статистика", prefix);

                var response = new GetAdminStatisticsResponse
                {
                    UsersTotal = players.Count,
                    DepositsTotal = transactions.Where(t => t.TransactionTypeId == 1).Sum(t => t.Amount),
                    BalancesTotal = balances.Sum(b => b.Amount),
                    PendingWithdrawalsTotal = transactions.Where(t => t.TransactionTypeId == 2 && t.TransactionStatusId == 1).Sum(t => t.Amount)
                };

                // Возвращаем статистику
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "{Prefix} Ошибка при получении админской статистики", prefix);
                return StatusCode(500, "Внутренняя ошибка сервера");
            }
        }

        /// <summary>
        /// Регистрация нового модератора
        /// </summary>
        /// <param name="authRequest">Данные для регистрации (логин и пароль)</param>
        /// <response code="200">Модератор успешно зарегистрирован</response>
        /// <response code="400">Некорректные данные запроса</response>
        /// <response code="409">Модератор уже есть</response>
        /// <response code="500">Внутренняя ошибка сервера</response>
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(409)]
        [ProducesResponseType(500)]
        [HttpPost("moderator-register")]
        public async Task<IActionResult> RegisterModerator([FromBody] AuthAdminRequest authRequest)
        {
            try
            {
                // Проверка входных данных
                if (!ModelState.IsValid)
                {
                    _logger.LogWarning("{Prefix} Некорректные данные для регистрации модератора", prefix);
                    return BadRequest(ModelState);
                }

                // Проверка наличия модератора
                if (await _context.AdminCreds.AnyAsync(a => a.Login == authRequest.Login))
                {
                    _logger.LogWarning("{Prefix} Модератор уже есть", prefix);
                    return Conflict($"Модератор уже существует");
                }

                // Хеширование пароля
                var (hash, salt) = _passwordHasher.HashWithSalt(authRequest.Password);

                // Создание нового модератора
                var moderator = new Admin
                {
                    Login = authRequest.Login,
                    PasswordHash = hash,
                    Salt = salt,
                    CreatedAt = DateTime.UtcNow,
                    Role = 2
                };

                // Сохранение в базу данных
                _context.AdminCreds.Add(moderator);
                await _context.SaveChangesAsync();

                _logger.LogInformation("{Prefix} Зарегистрирован новый модератор: {Login}", prefix, authRequest.Login);

                return Ok(new { Message = "Модератор успешно зарегистрирован" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "{Prefix} Ошибка при регистрации модератора", prefix);
                return StatusCode(500, "Внутренняя ошибка сервера");
            }
        }

        /// <summary>
        /// Получение списка модераторов или конкретного модератора по ID
        /// </summary>
        /// <param name="moderatorId">ID модератора (опционально)</param>
        /// <response code="200">Успешный запрос</response>
        /// <response code="404">Модератор не найден (при запросе по ID)</response>
        /// <response code="500">Внутренняя ошибка сервера</response>
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(typeof(IEnumerable<ModeratorDTO>), 200)]
        [ProducesResponseType(typeof(ModeratorDTO), 200)]
        [ProducesResponseType(404)]
        [ProducesResponseType(500)]
        [HttpGet("get-moderators")]
        public async Task<IActionResult> GetModerators([FromQuery] int? moderatorId = null)
        {
            try
            {
                // Если указан ID - возвращаем одного модератора
                if (moderatorId.HasValue)
                {
                    var moderator = await _context.AdminCreds
                        .Where(a => a.Role == 2 && a.Id == moderatorId.Value)
                        .Select(a => new ModeratorDTO
                        {
                            Id = a.Id,
                            Login = a.Login,
                            CreatedAt = a.CreatedAt
                        })
                        .FirstOrDefaultAsync();

                    if (moderator == null)
                    {
                        _logger.LogWarning("{Prefix} Модератор с ID {Id} не найден", prefix, moderatorId.Value);
                        return NotFound();
                    }

                    return Ok(moderator);
                }

                // Если ID не указан - возвращаем всех модераторов
                var moderators = await _context.AdminCreds
                    .Where(a => a.Role == 2)
                    .Select(a => new ModeratorDTO
                    {
                        Id = a.Id,
                        Login = a.Login,
                        CreatedAt = a.CreatedAt
                    })
                    .ToListAsync();

                return Ok(moderators);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "{Prefix} Ошибка при получении списка модераторов", prefix);
                return StatusCode(500, "Внутренняя ошибка сервера");
            }
        }

        /// <summary>
        /// Удаление модератора по ID
        /// </summary>
        /// <param name="moderatorId">ID модератора</param>
        /// <response code="200">Модератор успешно удален</response>
        /// <response code="404">Модератор не найден</response>
        /// <response code="500">Внутренняя ошибка сервера</response>
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(200)]
        [ProducesResponseType(404)]
        [ProducesResponseType(500)]
        [HttpDelete("delete-moderator/{moderatorId}")]
        public async Task<IActionResult> DeleteModerator([FromRoute] int moderatorId)
        {
            try
            {
                // Ищем модератора по ID и роли (Role = 2)
                
                var moderator = await _context.AdminCreds
                    .FirstOrDefaultAsync(a => a.Id == moderatorId && a.Role == 2);

                if (moderator == null)
                {
                    _logger.LogWarning("{Prefix} Модератор с ID {Id} не найден", prefix, moderatorId);
                    return NotFound();
                }

                // Удаление модератора
                _context.AdminCreds.Remove(moderator);
                await _context.SaveChangesAsync();

                _logger.LogInformation("{Prefix} Модератор с ID {Id} удален", prefix, moderatorId);
                return Ok(new { Message = "Модератор успешно удален" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "{Prefix} Ошибка при удалении модератора с ID {Id}", prefix, moderatorId);
                return StatusCode(500, "Внутренняя ошибка сервера");
            }
        }

        #endregion ActionMethods
    }
}
