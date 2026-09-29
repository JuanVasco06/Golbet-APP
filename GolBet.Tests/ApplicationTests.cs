using System.Net;
using System.Text.RegularExpressions;
using AutoMapper;
using GolBet.Entities;
using GolBet.Entities.Enums;
using GolBet.Repositories.Data;
using GolBet.Services.DTOs;
using GolBet.Services.Exceptions;
using GolBet.Services.Interfaces;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Xunit;

namespace GolBet.Tests;

public sealed class GolBetFactory : WebApplicationFactory<Program>
{
    private readonly string database = "GolBetTests_" + Guid.NewGuid().ToString("N");
    public string ConnectionString => new SqlConnectionStringBuilder
    {
        DataSource = Environment.GetEnvironmentVariable("GOLBET_TEST_SERVER") ?? @"(localdb)\MSSQLLocalDB",
        InitialCatalog = database, IntegratedSecurity = true, TrustServerCertificate = true
    }.ConnectionString;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<AppDbContext>>();
            services.RemoveAll<AppDbContext>();
            services.AddDbContext<AppDbContext>(options => options.UseSqlServer(ConnectionString));
        });
    }

    public override async ValueTask DisposeAsync()
    {
        await base.DisposeAsync();
        // Only this fixture's uniquely named test database is ever removed.
        if (!Regex.IsMatch(database, @"^GolBetTests_[a-f0-9]{32}$")) throw new InvalidOperationException();
        await using var db = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseSqlServer(ConnectionString).Options);
        await db.Database.EnsureDeletedAsync();
    }
}

public class ApplicationTests(GolBetFactory factory) : IClassFixture<GolBetFactory>
{
    private HttpClient Client() => factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
    private IServiceScope Scope() => factory.Services.CreateScope();
    private static string Decode(string text) => WebUtility.HtmlDecode(text);
    private static MatchFormDto ValidMatch() => new() { HomeTeamId = 1, AwayTeamId = 2, Date = DateTime.UtcNow.AddDays(20), HomeOdds = 2.50m, DrawOdds = 3.10m, AwayOdds = 4.25m };

    private static async Task<HttpResponseMessage> Post(HttpClient client, string formUrl, string postUrl, Dictionary<string, string> fields)
    {
        var html = await client.GetStringAsync(formUrl);
        var token = Regex.Match(html, "name=\"__RequestVerificationToken\"[^>]*value=\"([^\"]+)\"");
        Assert.True(token.Success, "The form must generate an anti-forgery token.");
        fields["__RequestVerificationToken"] = Decode(token.Groups[1].Value);
        return await client.PostAsync(postUrl, new FormUrlEncodedContent(fields));
    }

    [Theory]
    [InlineData("/")]
    [InlineData("/Matches")]
    [InlineData("/Teams")]
    [InlineData("/Matches/Create")]
    [InlineData("/Teams/Create")]
    public async Task PagesRender(string url)
    {
        using var client = Client();
        var response = await client.GetAsync(url);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("lang=\"es-CO\"", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task SeederIsIdempotentAndMigrationsAreApplied()
    {
        using var scope = Scope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var counts = (await db.Teams.CountAsync(), await db.Matches.CountAsync());
        Assert.True(counts.Item1 >= 8 && counts.Item2 >= 6);
        await DbSeeder.SeedAsync(db);
        Assert.Equal(counts, (await db.Teams.CountAsync(), await db.Matches.CountAsync()));
        Assert.Empty(await db.Database.GetPendingMigrationsAsync());
        Assert.Equal(1, await db.Teams.CountAsync(t => t.Name == "Atlético Nacional"));
        var seeded = await db.Matches.FindAsync(6);
        Assert.Equal(MatchStatus.Finished, seeded!.Status);
        Assert.Equal(2, seeded.HomeGoals);
        Assert.Equal(1, seeded.AwayGoals);
    }

    [Fact]
    public void MappingConfigurationIsValid()
    {
        using var scope = Scope();
        scope.ServiceProvider.GetRequiredService<IMapper>().ConfigurationProvider.AssertConfigurationIsValid();
    }

    [Fact]
    public async Task BoardFiltersCombineAndDetailIncludesTeams()
    {
        using var scope = Scope();
        var service = scope.ServiceProvider.GetRequiredService<IMatchService>();
        var filtered = (await service.GetBoardAsync(MatchStatus.Scheduled, 1)).ToList();
        Assert.NotEmpty(filtered);
        Assert.All(filtered, m => { Assert.Equal(MatchStatus.Scheduled, m.Status); Assert.Contains("Atlético Nacional", new[] { m.HomeTeamName, m.AwayTeamName }); });
        Assert.Empty(await service.GetBoardAsync(MatchStatus.Finished, 1));
        var detail = await service.GetDetailAsync(1);
        Assert.Equal("Atlético Nacional", detail!.HomeTeamName);
        Assert.Equal("Independiente Medellín", detail.AwayTeamName);
    }

    [Theory]
    [InlineData("/Matches/Detail/2147483647", HttpStatusCode.NotFound)]
    [InlineData("/Matches/Edit/2147483647", HttpStatusCode.NotFound)]
    [InlineData("/Teams/Edit/2147483647", HttpStatusCode.NotFound)]
    [InlineData("/Matches?status=bad", HttpStatusCode.BadRequest)]
    [InlineData("/Matches?status=100", HttpStatusCode.BadRequest)]
    public async Task MissingRecordsAndInvalidFiltersHaveExpectedStatus(string url, HttpStatusCode expected)
    {
        using var client = Client();
        Assert.Equal(expected, (await client.GetAsync(url)).StatusCode);
    }

    [Fact]
    public async Task TeamLifecycleNormalizesAuditsAndSoftDeletes()
    {
        using var scope = Scope();
        var service = scope.ServiceProvider.GetRequiredService<ITeamService>();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var name = "QA Club " + Guid.NewGuid().ToString("N")[..8];
        await service.CreateAsync(new() { Name = "  " + name + "  ", City = "  Medellín  " });
        var team = await db.Teams.SingleAsync(t => t.Name == name);
        Assert.Equal("Medellín", team.City);
        Assert.True(team.CreatedDate > DateTime.UtcNow.AddMinutes(-2));
        Assert.Null(team.ModifiedDate);
        var created = team.CreatedDate;
        await service.UpdateAsync(new() { Id = team.Id, Name = name, City = "Bogotá" });
        Assert.Equal(created, team.CreatedDate);
        Assert.NotNull(team.ModifiedDate);
        await service.DeactivateAsync(team.Id);
        Assert.False(team.IsActive);
        Assert.Null(await service.GetForEditAsync(team.Id));
        Assert.DoesNotContain(await service.GetAllAsync(), t => t.Id == team.Id);
        Assert.True(await db.Teams.AnyAsync(t => t.Id == team.Id));
    }

    [Fact]
    public async Task TeamValidationRejectsDuplicatesUnsafeUrlsAndRelatedDeactivation()
    {
        using var scope = Scope();
        var teams = scope.ServiceProvider.GetRequiredService<ITeamService>();
        await Assert.ThrowsAsync<BusinessRuleException>(() => teams.CreateAsync(new() { Name = " atletico nacional ", City = "Medellín" }));
        await Assert.ThrowsAsync<BusinessRuleException>(() => teams.CreateAsync(new() { Name = "Unsafe", City = "Cali", CrestUrl = "ftp://example.com/a.png" }));
        await Assert.ThrowsAsync<BusinessRuleException>(() => teams.CreateAsync(new() { Name = " ", City = " " }));
        await Assert.ThrowsAsync<BusinessRuleException>(() => teams.DeactivateAsync(1));
    }

    [Fact]
    public async Task MatchLifecycleConvertsTimePreservesAuditAndHidesInactiveDetail()
    {
        using var scope = Scope();
        var service = scope.ServiceProvider.GetRequiredService<IMatchService>();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var dto = ValidMatch();
        dto.Date = new DateTime(2035, 6, 10, 19, 30, 0);
        await service.CreateAsync(dto);
        var match = await db.Matches.SingleAsync(m => m.Date == new DateTime(2035, 6, 11, 0, 30, 0));
        var edit = (await service.GetForEditAsync(match.Id))!;
        Assert.Equal(dto.Date, edit.Date);
        var created = match.CreatedDate;
        edit.HomeOdds = 2.75m;
        await service.UpdateAsync(edit);
        Assert.Equal(2.75m, (await service.GetDetailAsync(match.Id))!.HomeOdds);
        Assert.Equal(created, match.CreatedDate);
        Assert.NotNull(match.ModifiedDate);
        await service.DeactivateAsync(match.Id);
        Assert.False(match.IsActive);
        Assert.Null(await service.GetDetailAsync(match.Id));
        Assert.Null(await service.GetForEditAsync(match.Id));
        Assert.DoesNotContain(await service.GetBoardAsync(), m => m.Id == match.Id);
    }

    [Fact]
    public async Task MatchRulesAreEnforcedEvenWithoutControllerValidation()
    {
        using var scope = Scope();
        var service = scope.ServiceProvider.GetRequiredService<IMatchService>();
        var dto = ValidMatch(); dto.AwayTeamId = dto.HomeTeamId;
        await Assert.ThrowsAsync<BusinessRuleException>(() => service.CreateAsync(dto));
        dto = ValidMatch(); dto.AwayTeamId = int.MaxValue;
        await Assert.ThrowsAsync<BusinessRuleException>(() => service.CreateAsync(dto));
        dto = ValidMatch(); dto.Date = DateTime.UtcNow.AddDays(-1);
        await Assert.ThrowsAsync<BusinessRuleException>(() => service.CreateAsync(dto));
        foreach (var invalid in new[] { 1m, -2m, 1000m, 2.345m })
        {
            dto = ValidMatch(); dto.HomeOdds = invalid;
            await Assert.ThrowsAsync<BusinessRuleException>(() => service.CreateAsync(dto));
        }
        await Assert.ThrowsAsync<BusinessRuleException>(() => service.GetForEditAsync(6));
        await Assert.ThrowsAsync<BusinessRuleException>(() => service.DeactivateAsync(5));
    }

    [Fact]
    public async Task InactiveTeamsCannotBeScheduledAndBetsPreventDeactivation()
    {
        using var scope = Scope();
        var teams = scope.ServiceProvider.GetRequiredService<ITeamService>();
        var matches = scope.ServiceProvider.GetRequiredService<IMatchService>();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var name = "Inactive " + Guid.NewGuid().ToString("N")[..8];
        await teams.CreateAsync(new() { Name = name, City = "Cali" });
        var team = await db.Teams.SingleAsync(t => t.Name == name);
        await teams.DeactivateAsync(team.Id);
        var dto = ValidMatch(); dto.HomeTeamId = team.Id;
        await Assert.ThrowsAsync<BusinessRuleException>(() => matches.CreateAsync(dto));
        // A future-module Bet is inserted directly only to verify the declared restriction.
        db.Bets.Add(new Bet { MatchId = 4, Amount = 100, OddsAtPlacement = 1.85m, Pick = BetPick.Home });
        await db.SaveChangesAsync();
        await Assert.ThrowsAsync<BusinessRuleException>(() => matches.DeactivateAsync(4));
    }

    [Fact]
    public async Task DatabaseEnforcesUniqueNamesAndRestrictedForeignKeys()
    {
        using var scope = Scope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.Teams.Add(new Team { Name = "ATLETICO NACIONAL", City = "Otra ciudad" });
        await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
        db.ChangeTracker.Clear();
        db.Teams.Remove((await db.Teams.FindAsync(1))!);
        await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
    }

    [Theory]
    [InlineData("2.50")]
    [InlineData("2,50")]
    public async Task MatchFormAcceptsBothDecimalSeparatorsAndUsesPrg(string odds)
    {
        using var client = Client();
        var fields = new Dictionary<string, string> { ["HomeTeamId"] = "1", ["AwayTeamId"] = "2", ["Date"] = "2036-01-10T19:00", ["HomeOdds"] = odds, ["DrawOdds"] = "3,10", ["AwayOdds"] = "4.25" };
        var response = await Post(client, "/Matches/Create", "/Matches/Create", fields);
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        using var scope = Scope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var match = await db.Matches.OrderByDescending(m => m.Id).FirstAsync();
        Assert.Equal(2.50m, match.HomeOdds);
        Assert.Equal(new DateTime(2036, 1, 11, 0, 0, 0), match.Date);
        var edit = new Dictionary<string, string>(fields) { ["Id"] = match.Id.ToString(), ["HomeOdds"] = "2,75" };
        var updated = await Post(client, $"/Matches/Edit/{match.Id}", $"/Matches/Edit/{match.Id}", edit);
        Assert.Equal(HttpStatusCode.Redirect, updated.StatusCode);
        await db.Entry(match).ReloadAsync();
        Assert.Equal(2.75m, match.HomeOdds);
    }

    [Theory]
    [InlineData("1.00", "La cuota debe ser mayor")]
    [InlineData("2.500", "máximo dos decimales")]
    [InlineData("NaN", "máximo dos decimales")]
    public async Task InvalidDecimalFormsReturnHelpfulErrors(string value, string expected)
    {
        using var client = Client();
        var response = await Post(client, "/Matches/Create", "/Matches/Create", new() { ["HomeTeamId"] = "1", ["AwayTeamId"] = "2", ["Date"] = "2036-01-10T19:00", ["HomeOdds"] = value, ["DrawOdds"] = "3", ["AwayOdds"] = "4" });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = Decode(await response.Content.ReadAsStringAsync());
        Assert.Contains(expected, body);
        Assert.Contains("Atlético Nacional", body); // Dropdowns survive errors.
    }

    [Fact]
    public async Task TeamFormsValidateCreateEditDeactivateAndRetainMessages()
    {
        using var client = Client();
        var empty = await Post(client, "/Teams/Create", "/Teams/Create", new() { ["Name"] = "", ["City"] = "" });
        Assert.Contains("El nombre es obligatorio", Decode(await empty.Content.ReadAsStringAsync()));
        var name = "HTTP Club " + Guid.NewGuid().ToString("N")[..8];
        var created = await Post(client, "/Teams/Create", "/Teams/Create", new() { ["Name"] = name, ["City"] = "Cali" });
        Assert.Equal(HttpStatusCode.Redirect, created.StatusCode);
        var list = Decode(await client.GetStringAsync("/Teams"));
        Assert.Contains("creado correctamente", list);
        using var scope = Scope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var team = await db.Teams.SingleAsync(t => t.Name == name);
        var edit = await Post(client, $"/Teams/Edit/{team.Id}", $"/Teams/Edit/{team.Id}", new() { ["Id"] = team.Id.ToString(), ["Name"] = name, ["City"] = "Bogotá" });
        Assert.Equal(HttpStatusCode.Redirect, edit.StatusCode);
        var deleted = await Post(client, "/Teams", $"/Teams/Deactivate/{team.Id}", new());
        Assert.Equal(HttpStatusCode.Redirect, deleted.StatusCode);
        await db.Entry(team).ReloadAsync();
        Assert.False(team.IsActive);
        Assert.Equal("Bogotá", team.City);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"/Teams/Edit/{team.Id}")).StatusCode);
    }

    [Fact]
    public async Task SeededLocalCrestCanBeSavedWithoutChangingIt()
    {
        using var client = Client();
        var response = await Post(client, "/Teams/Edit/1", "/Teams/Edit/1", new() { ["Id"] = "1", ["Name"] = "Atlético Nacional", ["City"] = "Medellín", ["CrestUrl"] = "/images/crests/nac.svg" });
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
    }

    [Fact]
    public async Task PostsWithoutCsrfTokenAreRejectedAndGetCannotDeactivate()
    {
        using var client = Client();
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsync("/Teams/Create", new FormUrlEncodedContent(new Dictionary<string, string> { ["Name"] = "Invalid", ["City"] = "Cali" }))).StatusCode);
        Assert.Equal(HttpStatusCode.MethodNotAllowed, (await client.GetAsync("/Matches/Deactivate/1")).StatusCode);
        var mismatch = await Post(client, "/Teams/Edit/1", "/Teams/Edit/1", new() { ["Id"] = "2", ["Name"] = "Invalid", ["City"] = "Cali" });
        Assert.Equal(HttpStatusCode.BadRequest, mismatch.StatusCode);
    }
}
