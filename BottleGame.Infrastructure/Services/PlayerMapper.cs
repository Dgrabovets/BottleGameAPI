using BottleGame.Application.DTOs.Responses;
using BottleGame.Domain.Entities;

namespace BottleGame.Infrastructure.Services
{
    /// <summary>
    /// Conversion of player entities into response DTOs
    /// </summary>
    internal static class PlayerMapper
    {
        /// <summary>
        /// Mapping of Player to PlayerInfoResponse
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
        /// Mapping of Player to PlayerDTO
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
        /// Mapping of GameRound to RoundDTO
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
