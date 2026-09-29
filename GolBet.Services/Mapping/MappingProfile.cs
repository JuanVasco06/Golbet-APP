// GolBet.Services/Mapping/MappingProfile.cs
using AutoMapper;
using GolBet.Entities;
using GolBet.Services.DTOs;

namespace GolBet.Services.Mapping;

public class MappingProfile : Profile
{
    public MappingProfile()
    {
        // Limit mapping depth; the course pins AutoMapper 13.0.1.
        // Flattening by convention:
        // MatchDto.HomeTeamName  <- Match.HomeTeam.Name
        // MatchDto.AwayTeamCrestUrl <- Match.AwayTeam.CrestUrl
        CreateMap<Match, MatchDto>().MaxDepth(4);

        CreateMap<Match, MatchDetailDto>()
            .ForMember(dto => dto.TotalBets,
                       options => options.MapFrom(
                           match => match.Bets.Count)).MaxDepth(4);

        CreateMap<Team, TeamDto>().MaxDepth(4);
        CreateMap<Team, TeamFormDto>().MaxDepth(4);
        CreateMap<TeamFormDto, Team>(MemberList.None).MaxDepth(4);

        CreateMap<Match, MatchFormDto>().MaxDepth(4);
        CreateMap<MatchFormDto, Match>(MemberList.None).MaxDepth(4);
    }
}
