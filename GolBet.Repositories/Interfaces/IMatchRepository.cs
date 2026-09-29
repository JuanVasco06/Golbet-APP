// GolBet.Repositories/Interfaces/IMatchRepository.cs
using GolBet.Entities;
using GolBet.Entities.Enums;

namespace GolBet.Repositories.Interfaces;

public interface IMatchRepository : IGenericRepository<Match>
{
    Task<IEnumerable<Match>> GetAllWithTeamsAsync(MatchStatus? status = null, int? teamId = null);
    Task<Match?> GetByIdWithDetailsAsync(int id);
    Task<bool> HasTeamMatchesAsync(int teamId);
}
