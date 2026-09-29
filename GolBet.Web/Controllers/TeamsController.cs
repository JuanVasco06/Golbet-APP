using GolBet.Services.DTOs;
using GolBet.Services.Exceptions;
using GolBet.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace GolBet.Web.Controllers;

public class TeamsController(ITeamService teams) : Controller
{
    public async Task<IActionResult> Index() => View(await teams.GetAllAsync());
    public IActionResult Create() => View(new TeamFormDto());

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(TeamFormDto dto)
    {
        if (ModelState.IsValid)
        {
            try
            {
                await teams.CreateAsync(dto);
                TempData["Success"] = $"Equipo «{dto.Name}» creado correctamente.";
                return RedirectToAction(nameof(Index));
            }
            catch (BusinessRuleException ex) { ModelState.AddModelError("", ex.Message); }
        }
        return View(dto);
    }

    public async Task<IActionResult> Edit(int id)
    {
        var dto = await teams.GetForEditAsync(id);
        return dto is null ? NotFound() : View(dto);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit([FromRoute] int id, TeamFormDto dto)
    {
        if (id != dto.Id) return BadRequest();
        if (ModelState.IsValid)
        {
            try
            {
                await teams.UpdateAsync(dto);
                TempData["Success"] = $"Equipo «{dto.Name}» actualizado.";
                return RedirectToAction(nameof(Index));
            }
            catch (KeyNotFoundException) { return NotFound(); }
            catch (BusinessRuleException ex) { ModelState.AddModelError("", ex.Message); }
        }
        return View(dto);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Deactivate(int id)
    {
        try
        {
            await teams.DeactivateAsync(id);
            TempData["Success"] = "Equipo desactivado.";
        }
        catch (KeyNotFoundException) { return NotFound(); }
        catch (BusinessRuleException ex) { TempData["Error"] = ex.Message; }
        return RedirectToAction(nameof(Index));
    }
}
