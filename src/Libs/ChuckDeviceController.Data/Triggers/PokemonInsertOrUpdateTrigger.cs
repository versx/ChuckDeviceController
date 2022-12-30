namespace ChuckDeviceController.Data.Triggers;

using System.Threading;
using System.Threading.Tasks;

using EntityFrameworkCore.Triggered;

using ChuckDeviceController.Data.Contexts;
using ChuckDeviceController.Data.Entities;

// REVIEW: Update first_seen, updated, changed, last_modified etc properties using triggers

public class PokemonInsertOrUpdateTrigger : IBeforeSaveTrigger<Pokemon>
{
    private readonly MapDbContext _context;

    public PokemonInsertOrUpdateTrigger(MapDbContext context)
    {
        _context = context;
    }

    public async Task BeforeSave(ITriggerContext<Pokemon> context, CancellationToken cancellationToken)
    {
        var date = DateTime.UtcNow.ToString("yyyy-MM-dd");
        var pokemonId = context.Entity.PokemonId;
        var form = context.Entity.Form ?? 0;

        switch (context.ChangeType)
        {
            case ChangeType.Added:
            case ChangeType.Modified:
                // Insert normal Pokemon stat
                await AddPokemonStat<PokemonStats>(new PokemonStats
                {
                    Date = date,
                    PokemonId = pokemonId,
                    FormId = form,
                }, cancellationToken);

                if (context.Entity.AttackIV != null)
                {
                    // Insert Pokemon IV stat
                    var iv = GetIV(context.Entity.AttackIV ?? 0, context.Entity.DefenseIV ?? 0, context.Entity.StaminaIV ?? 0);
                    await AddPokemonStat<PokemonIvStats>(new PokemonIvStats
                    {
                        Date = date,
                        PokemonId = pokemonId,
                        FormId = form,
                        IV = iv,
                    }, cancellationToken, iv);

                    //if (context.Entity.IV == 100)
                    if (context.Entity.AttackIV == 15 &&
                        context.Entity.DefenseIV == 15 &&
                        context.Entity.StaminaIV == 15)
                    {
                        // Insert 100% IV Pokemon IV stat
                        await AddPokemonStat<PokemonHundoStats>(new PokemonHundoStats
                        {
                            Date = date,
                            PokemonId = pokemonId,
                            FormId = form,
                        }, cancellationToken);
                    }
                }

                if (context.Entity.IsShiny ?? false)
                {
                    // Insert shiny Pokemon stat
                    await AddPokemonStat<PokemonShinyStats>(new PokemonShinyStats
                    {
                        Date = date,
                        PokemonId = pokemonId,
                        FormId = form,
                    }, cancellationToken);
                }
                break;
        }
    }

    private async Task AddPokemonStat<T>(BasePokemonStats pokemonStat, CancellationToken cancellationToken, double? iv = null)
        where T : BasePokemonStats
    {
        var exists = _context.Set<T>()
                             .Any(stat => pokemonStat.Date == stat.Date &&
                                          pokemonStat.PokemonId == stat.PokemonId &&
                                          pokemonStat.FormId == stat.FormId);
        if (exists)
        {
            var key = iv == null
                ? new object?[] { pokemonStat.Date, pokemonStat.PokemonId, pokemonStat.FormId }
                : new object?[] { pokemonStat.Date, pokemonStat.PokemonId, pokemonStat.FormId, iv };
            var existing = await _context.Set<T>()
                                         .FindAsync(key, cancellationToken: cancellationToken);
            if (existing != null)
            {
                pokemonStat.Count = existing.Count;
            }
        }
        pokemonStat.Count++;

        // If stat exists (updating) the row entry, only update the 'Count' column,
        // otherwise insert all of the entity's properties.
        //await _context.SingleMergeAsync(pokemonStat, options =>
        //    options.OnMergeUpdateInputExpression = p => new { p.Count }
        //, cancellationToken);
    }

    private static double GetIV(ushort atkIv, ushort defIv, ushort staIv)
    {
        var iv = Convert.ToDouble(atkIv + defIv + staIv) * 100.0 / 45.0;
        return Math.Round(iv, 2);
    }
}