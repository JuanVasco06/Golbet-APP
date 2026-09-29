using GolBet.Entities.Enums;
using GolBet.Services.DTOs;
using GolBet.Services.Exceptions;
using GolBet.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace GolBet.Web.Controllers;

public class MatchesController(IMatchService matches, ITeamService teams) : Controller
{
    public async Task<IActionResult> Index(MatchStatus? status, int? teamId)
    {
        if (!ModelState.IsValid || (status.HasValue && !Enum.IsDefined(status.Value)) || teamId <= 0)
            return BadRequest("Los filtros no son válidos.");
        ViewBag.CurrentStatus = status;
        ViewBag.CurrentTeamId = teamId;
        await LoadTeamsAsync();
        return View(await matches.GetBoardAsync(status, teamId));
    }

    public async Task<IActionResult> Detail(int id)
    {
        var match = await matches.GetDetailAsync(id);
        return match is null ? NotFound() : View(match);
    }

    public async Task<IActionResult> Create()
    {
        await LoadTeamsAsync();
        return View(new MatchFormDto());
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(MatchFormDto dto)
    {
        if (ModelState.IsValid)
        {
            try
            {
                await matches.CreateAsync(dto);
                TempData["Success"] = "Partido creado correctamente.";
                return RedirectToAction(nameof(Index));
            }
            catch (BusinessRuleException ex) { ModelState.AddModelError("", ex.Message); }
        }
        await LoadTeamsAsync();
        return View(dto);
    }

    public async Task<IActionResult> Edit(int id)
    {
        try
        {
            var dto = await matches.GetForEditAsync(id);
            if (dto is null) return NotFound();
            await LoadTeamsAsync();
            return View(dto);
        }
        catch (BusinessRuleException ex)
        {
            TempData["Error"] = ex.Message;
            return RedirectToAction(nameof(Detail), new { id });
        }
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit([FromRoute] int id, MatchFormDto dto)
    {
        if (id != dto.Id) return BadRequest();
        if (ModelState.IsValid)
        {
            try
            {
                await matches.UpdateAsync(dto);
                TempData["Success"] = "Partido actualizado correctamente.";
                return RedirectToAction(nameof(Detail), new { id });
            }
            catch (KeyNotFoundException) { return NotFound(); }
            catch (BusinessRuleException ex) { ModelState.AddModelError("", ex.Message); }
        }
        await LoadTeamsAsync();
        return View(dto);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Deactivate(int id)
    {
        try
        {
            await matches.DeactivateAsync(id);
            TempData["Success"] = "Partido desactivado. Su historial se conserva.";
        }
        catch (KeyNotFoundException) { return NotFound(); }
        catch (BusinessRuleException ex) { TempData["Error"] = ex.Message; }
        return RedirectToAction(nameof(Index));
    }

    private async Task LoadTeamsAsync()
        => ViewBag.Teams = new SelectList(await teams.GetAllAsync(), "Id", "Name");
}
