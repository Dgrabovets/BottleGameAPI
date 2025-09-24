using BottleGameServer.Models.Entities;

namespace BottleGameServer.Models.DTOs.Responses
{
    public class GetPlayerPlayedRoundsResponse
    {
        public PlayerDTO Player { get; set; } = null!;

        public IEnumerable<RoundDTO> Rounds { get; set; } = null!;
    }
}
