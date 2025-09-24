using System.Security.Claims;
using BottleGameServer.Data;
using BottleGameServer.Models.DTOs.Responses;
using BottleGameServer.Models.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BottleGameServer.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ReferalController : Controller
    {
        #region Fields

        private readonly ApplicationDbContext _context;
        private readonly ILogger<PlayerController> _logger;
        private readonly string _appName;

        const string prefix = "[Referal Controller]";

        #endregion Fields

        #region Constructor

        public ReferalController(ApplicationDbContext context, ILogger<PlayerController> logger, IConfiguration configuration)
        {
            _context = context;
            _logger = logger;
            _appName = configuration["AppName"]!;
        }

        #endregion Constructor

        #region ActionMethods

        /// <summary>
        /// Получить реферальную ссылку для пользователя
        /// </summary>
        /// <param name="playerId">ID игрока</param>
        /// <response code="200">Реферальная ссылка</response>
        /// <response code="403">Игрок в бане</response>
        /// <response code="404">Игрок не найден</response>
        /// <response code="500">Внутренняя ошибка сервера</response>
        [Authorize(Roles = "Player,Admin")]
        [ProducesResponseType(typeof(GetReferalLinkResponse), 200)]
        [ProducesResponseType(403)]
        [ProducesResponseType(404)]
        [ProducesResponseType(500)]
        [HttpGet("get-referal-link/{playerId:int}")]
        public async Task<ActionResult<GetReferalLinkResponse>> GetReferalLink([FromRoute] int playerId)
        {
            try
            {
                if(User.IsInRole("Player") && playerId.ToString() != User.Claims.First(c => c.Type == ClaimTypes.NameIdentifier).Value)
                {
                    return Unauthorized();
                }

                // Поиск игрока
                var player = await _context.Players.Include(p => p.Ban).Include(p => p.ReferalLink).FirstOrDefaultAsync(p => p.Id == playerId);

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

                var referalLink = player.ReferalLink;

                // Создание реферальной ссылки
                if (referalLink is null)
                {
                    referalLink = new ReferalLink
                    {
                        PlayerId = playerId,
                        Link = $"https://t.me/{_appName}?startapp=ref_{playerId}"
                    };

                    _context.ReferalLinks.Add(referalLink);

                    await _context.SaveChangesAsync();
                }

                // Выдаем ссылку
                _logger.LogInformation("{Prefix} Выдана ссылка для пользователя {PlayerId}", prefix, playerId);

                return Ok(new GetReferalLinkResponse { ReferalLink = referalLink.Link });
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
