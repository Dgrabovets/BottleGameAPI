using BottleGame.Application.DTOs.Responses;
using BottleGame.Domain.Entities;

namespace BottleGame.Infrastructure.Services
{
    /// <summary>
    /// Преобразование сущностей игрока в DTO ответов
    /// </summary>
    internal static class PlayerMapper
    {
        /// <summary>
        /// Маппинг Player в PlayerInfoResponse
        /// </summary>
        internal static PlayerInfoResponse MapToPlayerInfoResponse(Player player) =>
            new()
            {
                Player = MapToPlayerDTO(player),
                Balance = player.Balance.Amount,
                Rounds = [.. player.GameRounds.Select(MapToRoundDTO)],
                BannedAt = player.Ban is not null && player.Ban.IsActive ? player.Ban.BannedAt : null,
                PlayersIdsInvited = [.. player.InvitedByMe.Select(pi => pi.PlayerIdInvited)],
            };

        /// <summary>
        /// Маппинг Player в PlayerDTO
        /// </summary>
        internal static PlayerDTO MapToPlayerDTO(Player player)
        {
            return new PlayerDTO
            {
                Id = player.Id,
                TelegramId = player.TelegramId,
                Name = player.Name,
                AvatarUrl = player.AvatarUrl,
                RegisteredInAppAt = player.RegisteredInAppAt,
                WinRate = player.WinRate
            };
        }

        /// <summary>
        /// Маппинг GameRound в RoundDTO
        /// </summary>
        internal static RoundDTO MapToRoundDTO(GameRound round)
        {
            return new RoundDTO
            {
                Id = round.Id,
                Bet = round.Bet,
                IsWin = round.IsWin,
                FinishedAt = round.FinishedAt
            };
        }
    }
}
