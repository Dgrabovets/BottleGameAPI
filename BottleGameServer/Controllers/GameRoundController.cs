using System.Security.Claims;
using BottleGameServer.Data;
using BottleGameServer.Models.DTOs.Requests;
using BottleGameServer.Models.DTOs.Responses;
using BottleGameServer.Models.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BottleGameServer.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class GameRoundController : Controller
    {
        #region Fields

        private readonly ApplicationDbContext _context;
        private readonly ILogger<PlayerController> _logger;

        const string prefix = "[GameRound Controller]";

        #endregion Fields

        #region Constructor

        public GameRoundController(ApplicationDbContext context, ILogger<PlayerController> logger)
        {
            _context = context;
            _logger = logger;
        }

        #endregion Constructor

        #region ActionMethods

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
        [Authorize(Roles = "Player,Admin")]
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(403)]
        [ProducesResponseType(404)]
        [ProducesResponseType(500)]
        [HttpPost("finish-game-round/{playerId:int}")]
        public async Task<ActionResult> FinishGameRound([FromRoute] int playerId, [FromBody] FinishRoundRequest request)
        {
            try
            {
                // Проверка входных данных
                if (!ModelState.IsValid)
                {
                    _logger.LogWarning("{Prefix} Некорректные данные запроса для игрока {PlayerId}", prefix, playerId);
                    return BadRequest(ModelState);
                }

                if (User.IsInRole("Player") && playerId.ToString() != User.Claims.First(c => c.Type == ClaimTypes.NameIdentifier).Value)
                {
                    return Unauthorized();
                }

                // Поиск игрока
                var player = await _context.Players
                    .Include(p => p.GameRounds)
                    .Include(p => p.Balance)
                    .Include(p => p.Ban)
                    .FirstOrDefaultAsync(p => p.Id == playerId);

                if (player is null)
                {
                    _logger.LogWarning("{Prefix} Игрок с ID {PlayerId} не найден", prefix, playerId);
                    return NotFound($"Игрок с ID {playerId} не найден");
                }

                if (User.IsInRole("Player") && player.Ban is not null && player.Ban.IsActive)
                {
                    _logger.LogWarning("{Prefix} Пользователь забанен", prefix);
                    return Forbid("Пользователь в бане");
                }

                if (player.Balance.Amount < request.Bet && !request.IsWin)
                {
                    _logger.LogWarning("{Prefix} Недостаточно средств на балансе для снятия у игрока {PlayerId}", prefix, playerId);
                    return BadRequest("Недостаточно средств!");
                }

                var roundsPlayedStat = await _context.PlayersPlayedRoundsStatisticForWithdrawals
                                            .FirstOrDefaultAsync(s => s.PlayerId == playerId);

                if (roundsPlayedStat is null)
                {
                    _logger.LogWarning("{Prefix} Не найдены данные статистики сыгранных раундов для игрока {PlayerId}", prefix, playerId);
                    return BadRequest("Не найдены данные статистики сыгранных раундов игрока");
                }

                roundsPlayedStat.PlayedRoundsCount++;

                // Создание записи о раунде
                var round = new GameRound
                {
                    Player = player,
                    PlayerId = playerId,
                    Bet = request.Bet,
                    IsWin = request.IsWin,
                    FinishedAt = DateTime.UtcNow
                };

                _context.GameRounds.Add(round);

                // Обновление баланса игрока
                player.Balance.Amount += request.IsWin ? request.Bet : -request.Bet;

                // Сохранение изменений
                await _context.SaveChangesAsync();

                _logger.LogInformation(
                    "{Prefix} Раунд завершен для игрока {PlayerId}. Статус: {Status}, Сумма: {Bet}",
                    prefix,
                    playerId,
                    request.IsWin ? "Победа" : "Проигрыш",
                    request.Bet);

                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "{Prefix} Ошибка при завершении раунда для игрока {PlayerId}", prefix, playerId);
                return StatusCode(500, "Внутренняя ошибка сервера");
            }
        }

        /// <summary>
        /// Получить сыгранные раунды игрока
        /// </summary>
        /// <param name="playerId">ID игрока</param>
        /// <response code="200">Список раундов</response>
        /// <response code="404">Игрок не найден</response>
        /// <response code="403">Игрок в бане</response>
        /// <response code="500">Внутренняя ошибка сервера</response>
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(typeof(GetPlayerPlayedRoundsResponse), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(403)]
        [ProducesResponseType(404)]
        [ProducesResponseType(500)]
        [HttpGet("get-player-played-rounds/{playerId:int}")]
        public async Task<ActionResult<GetPlayerPlayedRoundsResponse>> GetPlayerPlayedRounds([FromRoute] int playerId)
        {
            try
            {
                // Поиск игрока
                var player = await _context.Players
                    .Include(p => p.GameRounds)
                    .Include(p => p.Ban)
                    .FirstOrDefaultAsync(p => p.Id == playerId);

                if (player is null)
                {
                    _logger.LogWarning("{Prefix} Игрок с ID {PlayerId} не найден", prefix, playerId);
                    return NotFound($"Игрок с ID {playerId} не найден");
                }

                // Вывод результата
                _logger.LogInformation("{Prefix} Получен список сыгранных раундов игрока {PlayerId}", prefix, playerId);

                var response = new GetPlayerPlayedRoundsResponse
                {
                    Player = new PlayerDTO
                    {
                        Id = playerId,
                        TelegramId = player!.TelegramId,
                        Name = player.Name,
                        AvatarUrl = player.AvatarUrl,
                        RegisteredInAppAt = player.RegisteredInAppAt,
                        WinRate = player.WinRate,
                    },
                    Rounds = player.GameRounds.Select(t => new RoundDTO
                    {
                        Id = t.Id,
                        Bet = t.Bet,
                        IsWin = t.IsWin,
                        FinishedAt = t.FinishedAt
                    }).ToList()
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "{Prefix} Ошибка при получении раундов для игрока {PlayerId}", prefix, playerId);
                return StatusCode(500, "Внутренняя ошибка сервера");
            }
        }

        /// <summary>
        /// Получить прибыли за период
        /// </summary>
        /// <response code="200">Прибыль</response>
        /// <response code="404">Раунды не найдены</response>
        /// <response code="500">Внутренняя ошибка сервера</response>
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(typeof(GetProfitResponse), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(404)]
        [ProducesResponseType(500)]
        [HttpGet("get-profit")]
        public async Task<ActionResult<GetProfitResponse>> GetProfit([FromBody] GetProfitRequest request)
        {
            try
            {
                // Поиск информации о раундах
                var query = _context.GameRounds.AsNoTracking();

                if (request.DateFrom.HasValue)
                {
                    query = query.Where(r => r.FinishedAt > request.DateFrom.Value);
                }

                if (request.DateTill.HasValue)
                {
                    query = query.Where(r => r.FinishedAt < request.DateTill.Value);
                }

                var rounds = await query.ToListAsync();

                if (rounds is null || rounds.Any())
                {
                    _logger.LogWarning("{Prefix} Раунды игроков не найдены", prefix);
                    return NotFound($"Раунды игроков не найдены, нет возможности посчитать прибыль");
                }

                // Вывод результата
                _logger.LogInformation("{Prefix} Получен список всех раундов игроков, высчитывается прибыль", prefix);

                var response = new GetProfitResponse
                {
                    Profit = rounds.Where(r => r.IsWin is false).Sum(r => r.Bet),
                    DateFrom = rounds.Min(r => r.FinishedAt),
                    DateTill = rounds.Max(r => r.FinishedAt)
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "{Prefix} Ошибка при получении прибыли", prefix);
                return StatusCode(500, "Внутренняя ошибка сервера");
            }
        }

        #endregion ActionMethods
    }
}
