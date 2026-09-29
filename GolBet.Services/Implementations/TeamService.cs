using System.ComponentModel.DataAnnotations;
using System.Globalization;
using AutoMapper;
using GolBet.Entities;
using GolBet.Repositories.Exceptions;
using GolBet.Repositories.Interfaces;
using GolBet.Services.DTOs;
using GolBet.Services.Exceptions;
using GolBet.Services.Interfaces;

namespace GolBet.Services.Implementations;

public class TeamService(IGenericRepository<Team> teams, IMatchRepository matches, IMapper mapper) : ITeamService
{
    public async Task<IEnumerable<TeamDto>> GetAllAsync()
        => mapper.Map<IEnumerable<TeamDto>>((await teams.GetAllAsync()).OrderBy(t => t.Id));

    public async Task<TeamFormDto?> GetForEditAsync(int id)
    {
        var team = await teams.GetByIdAsync(id);
        return team is null || !team.IsActive ? null : mapper.Map<TeamFormDto>(team);
    }

    public async Task CreateAsync(TeamFormDto dto)
    {
        dto.Id = 0;
        await ValidateAsync(dto);
        try { await teams.AddAsync(mapper.Map<Team>(dto)); }
        catch (DuplicateRecordException) { throw new BusinessRuleException("Ya existe un equipo con ese nombre, incluso si está inactivo."); }
    }

    public async Task UpdateAsync(TeamFormDto dto)
    {
        var team = await teams.GetByIdAsync(dto.Id);
        if (team is null || !team.IsActive) throw new KeyNotFoundException();
        await ValidateAsync(dto);
        mapper.Map(dto, team);
        try { await teams.UpdateAsync(team); }
        catch (DuplicateRecordException) { throw new BusinessRuleException("Ya existe un equipo con ese nombre, incluso si está inactivo."); }
    }

    public async Task DeactivateAsync(int id)
    {
        var team = await teams.GetByIdAsync(id);
        if (team is null || !team.IsActive) throw new KeyNotFoundException();
        if (await matches.HasTeamMatchesAsync(id))
            throw new BusinessRuleException("No se puede desactivar un equipo con partidos asociados. Se conserva su historial.");
        await teams.DeactivateAsync(id);
    }

    private async Task ValidateAsync(TeamFormDto dto)
    {
        dto.Name = dto.Name?.Trim() ?? "";
        dto.City = dto.City?.Trim() ?? "";
        dto.CrestUrl = string.IsNullOrWhiteSpace(dto.CrestUrl) ? null : dto.CrestUrl.Trim();
        var errors = new List<ValidationResult>();
        if (!Validator.TryValidateObject(dto, new ValidationContext(dto), errors, true))
            throw new BusinessRuleException(errors[0].ErrorMessage!);
        if (dto.CrestUrl is not null && !System.Text.RegularExpressions.Regex.IsMatch(dto.CrestUrl, @"^/images/crests/[a-z]{2,3}\.svg$") && (!Uri.TryCreate(dto.CrestUrl, UriKind.Absolute, out var uri) || uri.Scheme != "https"))
            throw new BusinessRuleException("El escudo debe tener una URL HTTPS válida.");
        var comparison = CultureInfo.GetCultureInfo("es-CO").CompareInfo;
        if ((await teams.GetAllAsync(includeInactive: true)).Any(t => t.Id != dto.Id &&
            comparison.Compare(t.Name, dto.Name, CompareOptions.IgnoreCase | CompareOptions.IgnoreNonSpace) == 0))
            throw new BusinessRuleException("Ya existe un equipo con ese nombre, incluso si está inactivo.");
    }
}
