namespace TodoPlugin.Controllers;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

using Data.Contexts;
using Data.Entities;

/// <summary>
///     Example controller for todo list.
/// </summary>
/// <remarks>
/// Credits:
///   - https://docs.microsoft.com/en-us/aspnet/core/tutorials/min-web-api?view=aspnetcore-6.0&tabs=visual-studio#add-the-api-code
///   - https://docs.microsoft.com/en-us/aspnet/core/tutorials/first-web-api?view=aspnetcore-6.0&tabs=visual-studio
/// </remarks>
[Controller]
//[Authorize(Roles = "ShouldFail")]
[Authorize(Roles = TodoPlugin.TodoRole)]
public class TodoController : Controller
{
    private readonly ILogger<TodoController> _logger;
    private readonly TodoDbContext _context;

    public TodoController(
        ILogger<TodoController> logger,
        TodoDbContext context)
    {
        _logger = logger;
        _context = context;
    }

    // GET: TodoController
    public ActionResult Index()
    {
        var todos = _context.Todos.ToList();
        return View(todos);
    }

    // GET: TodoController/Complete
    public ActionResult Complete()
    {
        var todosComplete = _context.Todos.Where(x => x.IsComplete)
                                          .ToList();
        return View(todosComplete);
    }

    // GET: TodoController/Create
    public ActionResult Create()
    {
        return View();
    }

    // POST: TodoController/Create
    [HttpPost]
    public async Task<ActionResult> Create(Todo todo)
    {
        if (_context.Todos.Any(x => x.Name == todo.Name))
        {
            _logger.LogError($"Todo already exists with name '{todo.Name}'");
            ModelState.AddModelError("Todo", $"Todo already exists with name '{todo.Name}'");
            return View(todo);
        }

        await _context.Todos.AddAsync(todo);
        await _context.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }

    // GET: TodoController/Edit/123
    public async Task<ActionResult> Edit(uint id)
    {
        var todo = await _context.Todos.FindAsync(id);
        if (todo == null)
        {
            _logger.LogError($"Failed to find todo with id '{id}'");
            return RedirectToAction(nameof(Index));
        }
        return View(todo);
    }

    // POST: TodoController/Edit/123
    [HttpPost]
    public async Task<ActionResult> Edit(uint id, Todo todo)
    {
        var dbTodo = await _context.Todos.FindAsync(id);
        if (dbTodo == null)
        {
            _logger.LogError($"Todo does not exist with id '{id}'");
            ModelState.AddModelError("Todo", $"Todo does not exist with id '{id}'");
            return View(todo);
        }

        if (!_context.Todos.Any(x => x.Name == todo.Name))
        {
            _logger.LogError($"Todo already exists with name '{todo.Name}'");
            ModelState.AddModelError("Todo", $"Todo already exists with name '{todo.Name}'");
            return View(todo);
        }

        dbTodo.Name = todo.Name;
        dbTodo.IsComplete = todo.IsComplete;

        _context.Todos.Update(todo);
        await _context.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }

    // GET: TodoController/Delete/123
    public async Task<ActionResult> Delete(uint id)
    {
        var todo = await _context.Todos.FindAsync(id);
        if (todo == null)
        {
            _logger.LogError($"Failed to find todo with id '{id}'");
            return RedirectToAction(nameof(Index));
        }
        return View(todo);
    }

    // POST: TodoController/Delete/123
    [HttpPost]
    public async Task<ActionResult> Delete(uint id, IFormCollection collection)
    {
        var dbTodo = await _context.Todos.FindAsync(id);
        if (dbTodo == null)
        {
            _logger.LogError($"Todo does not exist with id '{id}'");
            ModelState.AddModelError("Todo", $"Todo does not exist with id '{id}'");
            return RedirectToAction(nameof(Index));
        }

        _context.Todos.Remove(dbTodo);
        await _context.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }
}