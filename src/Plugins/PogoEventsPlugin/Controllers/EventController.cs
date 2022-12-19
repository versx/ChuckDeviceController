namespace PogoEventsPlugin.Controllers;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using Services;

[Authorize(Roles = PogoEventsPlugin.PogoEventsRole)]
public class EventController : Controller
{
    private readonly ILogger<EventController> _logger;
    private readonly IPokemonEventDataService _pokemonEventService;

    public EventController(
        ILogger<EventController> logger,
        IPokemonEventDataService pokemonEventService)
    {
        _logger = logger;
        _pokemonEventService = pokemonEventService;
    }

    public IActionResult Index()
    {
        var events = _pokemonEventService.ActiveEvents;
        return View(events);
    }

    public IActionResult Details(string name)
    {
        var pogoEvent = _pokemonEventService.ActiveEvents.FirstOrDefault(x => x.Name == name);
        if (pogoEvent == null)
        {
            ModelState.AddModelError("Event", $"Pokemon event with name '{name}' does not exist.");
            return View();
        }

        return View(pogoEvent);
    }

    public IActionResult Delete(string name)
    {
        var pogoEvent = _pokemonEventService.ActiveEvents.FirstOrDefault(x => x.Name == name);
        if (pogoEvent == null)
        {
            ModelState.AddModelError("Event", $"Pokemon event with name '{name}' does not exist.");
            return View();
        }

        return View(pogoEvent);
    }
}