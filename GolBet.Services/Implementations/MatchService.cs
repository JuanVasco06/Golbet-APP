using AutoMapper;
using GolBet.Entities;
using GolBet.Entities.Enums;
using GolBet.Repositories.Interfaces;
using GolBet.Services.DTOs;
using GolBet.Services.Exceptions;
using GolBet.Services.Helpers;
using GolBet.Services.Interfaces;

namespace GolBet.Services.Implementations;

public class MatchService(IMatchRepository matches, IGenericRepository<Team> teams, IMapper mapper) : IMatchService
{
    public async Task<IEnumerable<MatchDto>> GetBoardAsync(MatchStatus? status = null, int? teamId = null)
        => mapper.Map<IEnumerable<MatchDto>>(await matches.GetAllWithTeamsAsync(status, teamId));

    public async Task<MatchDetailDto?> GetDetailAsync(int id)
        => mapper.Map<MatchDetailDto?>(await matches.GetByIdWithDetailsAsync(id));

    public async Task<MatchFormDto?> GetForEditAsync(int id)
    {
        var match = await matches.GetByIdAsync(id);
        if (match is null || !match.IsActive) return null;
        EnsureEditable(match);
        var dto = mapper.Map<MatchFormDto>(match);
        dto.Date = dto.Date.ToColombiaTime();
        return dto;
    }

    public async Task CreateAsync(MatchFormDto dto)
    {
        dto.Id = 0;
        await ValidateAsync(dto);
        var match = mapper.Map<Match>(dto);
        match.Date = dto.Date.ToUtcFromColombia();
        await matches.AddAsync(match);
    }

    public async Task UpdateAsync(MatchFormDto dto)
    {
        var match = await matches.GetByIdAsync(dto.Id);
        if (match is null || !match.IsActive) throw new KeyNotFoundException();
        EnsureEditable(match);
        await ValidateAsync(dto);
        var details = await matches.GetByIdWithDetailsAsync(dto.Id);
        if (details!.Bets.Count > 0 && (match.HomeTeamId != dto.HomeTeamId || match.AwayTeamId != dto.AwayTeamId || match.Date != dto.Date.ToUtcFromColombia()))
            throw new BusinessRuleException("Un partido con apuestas solo permite actualizar las cuotas.");
        mapper.Map(dto, match);
        match.Date = dto.Date.ToUtcFromColombia();
        await matches.UpdateAsync(match);
    }

    public async Task DeactivateAsync(int id)
    {
        var match = await matches.GetByIdWithDetailsAsync(id) ?? throw new KeyNotFoundException();
        if (match.Bets.Count > 0)
            throw new BusinessRuleException("No se puede desactivar un partido con apuestas.");
        EnsureEditable(match);
        await matches.DeactivateAsync(id);
    }

    private static void EnsureEditable(Match match)
    {
        if (match.Status != MatchStatus.Scheduled || match.Date <= DateTime.UtcNow)
            throw new BusinessRuleException("Solo se pueden modificar o desactivar partidos programados que aún no han comenzado.");
    }

    private async Task ValidateAsync(MatchFormDto dto)
    {
        if (dto.HomeTeamId == dto.AwayTeamId)
            throw new BusinessRuleException("El equipo local y el visitante no pueden ser el mismo.");
        foreach (var id in new[] { dto.HomeTeamId, dto.AwayTeamId })
        {
            var team = await teams.GetByIdAsync(id);
            if (team is null || !team.IsActive) throw new BusinessRuleException("Seleccione dos equipos activos y existentes.");
        }
        if (dto.Date.ToUtcFromColombia() <= DateTime.UtcNow)
            throw new BusinessRuleException("La fecha del partido debe ser futura.");
        foreach (var odds in new[] { dto.HomeOdds, dto.DrawOdds, dto.AwayOdds })
            if (odds < 1.01m || odds > 999.99m || decimal.Round(odds, 2) != odds)
                throw new BusinessRuleException("Las cuotas deben estar entre 1,01 y 999,99, con máximo dos decimales.");
    }
}
