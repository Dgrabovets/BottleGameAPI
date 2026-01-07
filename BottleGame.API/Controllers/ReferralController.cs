using System.Security.Claims;
using BottleGame.Infrastructure.Data;
using BottleGame.Application.DTOs.Responses;
using BottleGame.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace BottleGame.API.Controllers
{
    /// <summary>
    /// Контроллер реферальной системы
    /// </summary>
    /// <remarks>
    /// Управление реферальными ссылками и приглашениями
    /// </remarks>
    [ApiController]
    [Route("api/[controller]")]
    [Tags("Реферальная система")]
    public class ReferralController : Controller
    {
        #region Fields

        private readonly ApplicationDbContext _context;
        private readonly ILogger<ReferralController> _logger;
        private readonly string _appName;

        const string prefix = "[Referral Controller]";

        #endregion Fields

        #region Constructor

        /// <summary>
        /// Инициализирует новый экземпляр <see cref="ReferralController"/>.
        /// </summary>
        /// <param name="context">Контекст базы данных.</param>
        /// <param name="logger">Логгер.</param>
        /// <param name="configuration">Конфигурация приложения.</param>
        public ReferralController(ApplicationDbContext context, ILogger<ReferralController> logger, IConfiguration configuration)
        {
            _context = context;
            _logger = logger;
            _appName = configuration["AppName"]!;
        }

        #endregion Constructor

        #region ActionMethods

        /// <summary>
        /// Получить реферальную ссылку для игрока
        /// </summary>
        /// <param name="playerId">ID игрока</param>
        /// <response code="200">Реферальная ссылка</response>
        /// <response code="403">Игрок в бане</response>
        /// <response code="404">Игрок не найден</response>
        /// <response code="500">Внутренняя ошибка сервера</response>
        [Authorize(Roles = "Player,Admin")]
        [ProducesResponseType(typeof(GetReferralLinkResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [HttpGet("get-referral-link/{playerId:int}")]
        public async Task<ActionResult<GetReferralLinkResponse>> GetReferralLink([FromRoute] int playerId)
        {
            try
            {
                if (User.IsInRole("Player") && playerId.ToString() != User.Claims.First(c => c.Type == ClaimTypes.NameIdentifier).Value)
                {
                    return Unauthorized();
                }

                // Поиск игрока
                var player = await _context.Players.Include(p => p.Ban).Include(p => p.ReferralLink).FirstOrDefaultAsync(p => p.Id == playerId);

                if (player is null)
                {
                    _logger.LogWarning("{Prefix} Игрок с ID {PlayerId} не найден", prefix, playerId);
                    return NotFound($"Игрок с ID {playerId} не найден");
                }

                if (User.IsInRole("Player") && player.Ban is not null && player.Ban.IsActive)
                {
                    _logger.LogWarning("{Prefix} Игрок забанен", prefix);
                    return Forbid("Игрок находится в бане");
                }

                var referralLink = player.ReferralLink;

                // Создание реферальной ссылки
                if (referralLink is null)
                {
                    referralLink = new ReferralLink
                    {
                        PlayerId = playerId,
                        Link = $"https://t.me/{_appName}?startapp=ref_{playerId}"
                    };

                    _context.ReferralLinks.Add(referralLink);

                    await _context.SaveChangesAsync();
                }

                // Возврат ссылки
                _logger.LogInformation("{Prefix} Получена ссылка для игрока {PlayerId}", prefix, playerId);

                return Ok(new GetReferralLinkResponse { ReferralLink = referralLink.Link });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "{Prefix} Ошибка при получении реф. ссылки для игрока {PlayerId}", prefix, playerId);
                return StatusCode(500, "Внутренняя ошибка сервера");
            }
        }

        #endregion ActionMethods
    }
}
