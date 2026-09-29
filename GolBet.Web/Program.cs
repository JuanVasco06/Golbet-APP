using GolBet.Repositories.Data;
using GolBet.Repositories.Implementations;
using GolBet.Repositories.Interfaces;
using GolBet.Services.Implementations;
using GolBet.Services.Interfaces;
using GolBet.Services.Mapping;
using Microsoft.EntityFrameworkCore;
using System.Globalization;

var culture = new CultureInfo("es-CO");
CultureInfo.DefaultThreadCurrentCulture = culture;
CultureInfo.DefaultThreadCurrentUICulture = culture;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews(options =>
{
    options.ModelBinderProviders.Insert(0, new GolBet.Web.ModelBinding.FlexibleDecimalModelBinderProvider());
    options.ModelBindingMessageProvider.SetValueMustBeANumberAccessor(name => $"{name} debe ser un número.");
    options.ModelBindingMessageProvider.SetAttemptedValueIsInvalidAccessor((value, name) => $"El valor de {name} no es válido.");
});

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Open generic registration: one line, a repository for every entity
builder.Services.AddScoped(typeof(IGenericRepository<>), typeof(GenericRepository<>));

// Specific repositories
builder.Services.AddScoped<IMatchRepository, MatchRepository>();
builder.Services.AddScoped<ITeamService, TeamService>();

// AutoMapper: scans the assembly containing MappingProfile for all profiles
builder.Services.AddAutoMapper(typeof(MappingProfile));

// Business services
builder.Services.AddScoped<IMatchService, MatchService>();

var app = builder.Build();

// Seed the database on startup
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await DbSeeder.SeedAsync(context);
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

// The HTTP profile is loopback-only and works without a development certificate.
if (!app.Environment.IsDevelopment()) app.UseHttpsRedirection();
app.UseStatusCodePagesWithReExecute("/Home/StatusCodePage", "?code={0}");
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();

public partial class Program { }
