namespace ChuckDeviceController.Data.Triggers
{
    using System.Threading;
    using System.Threading.Tasks;

    using EntityFrameworkCore.Triggered;

    using ChuckDeviceController.Data.Contexts;
    using ChuckDeviceController.Data.Entities;

    // TODO: Update first_seen, updated, changed, last_modified etc properties
    /*
BEGIN
    INSERT INTO pokemon_stats (pokemon_id, count, date)
    VALUES
        (NEW.pokemon_id, 1, DATE(FROM_UNIXTIME(NEW.expire_timestamp)))
    ON DUPLICATE KEY UPDATE
        count = count + 1;
    IF (NEW.iv IS NOT NULL) THEN BEGIN
        INSERT INTO pokemon_iv_stats (pokemon_id, count, date)
        VALUES
            (NEW.pokemon_id, 1, DATE(FROM_UNIXTIME(NEW.expire_timestamp)))
        ON DUPLICATE KEY UPDATE
            count = count + 1;
        END;
    END IF;
    IF (NEW.shiny = 1) THEN BEGIN
        INSERT INTO pokemon_shiny_stats (pokemon_id, count, date)
        VALUES
            (NEW.pokemon_id, 1, DATE(FROM_UNIXTIME(NEW.expire_timestamp)))
        ON DUPLICATE KEY UPDATE
            count = count + 1;
        END;
    END IF;
    IF (NEW.iv = 100) THEN BEGIN
        INSERT INTO pokemon_hundo_stats (pokemon_id, count, date)
        VALUES
            (NEW.pokemon_id, 1, DATE(FROM_UNIXTIME(NEW.expire_timestamp)))
        ON DUPLICATE KEY UPDATE
            count = count + 1;
        END;
    END IF;
END
     */

    public class PokemonInsertedTrigger : IBeforeSaveTrigger<Pokemon>
    {
        private readonly MapContext _context;

        public PokemonInsertedTrigger(MapContext context)
        {
            _context = context;
        }

        public async Task BeforeSave(ITriggerContext<Pokemon> context, CancellationToken cancellationToken)
        {
            if (context.ChangeType == ChangeType.Added)
            {
                var pokemonStat = new PokemonStats
                {
                    Date = DateTime.UtcNow,
                    PokemonId = context.Entity.PokemonId,
                    FormId = context.Entity.Form ?? 0,
                    Count = 1,
                };
                await _context.AddAsync(pokemonStat, cancellationToken);

                if (context.Entity.IV != null)
                {
                    var pokemonIvStat = new PokemonIvStats
                    {
                        Date = DateTime.UtcNow,
                        PokemonId = context.Entity.PokemonId,
                        FormId = context.Entity.Form ?? 0,
                        Count = 1,
                    };
                    await _context.AddAsync(pokemonIvStat, cancellationToken);

                    //if (context.Entity.IV == 100)
                    if (context.Entity.AttackIV == 15 &&
                        context.Entity.DefenseIV == 15 &&
                        context.Entity.StaminaIV == 15)
                    {
                        var pokemonHundoStat = new PokemonHundoStats
                        {
                            Date = DateTime.UtcNow,
                            PokemonId = context.Entity.PokemonId,
                            FormId = context.Entity.Form ?? 0,
                            Count = 1,
                        };
                        await _context.AddAsync(pokemonHundoStat, cancellationToken);
                    }
                }

                if (context.Entity.IsShiny ?? false)
                {
                    var pokemonShinyStat = new PokemonShinyStats
                    {
                        Date = DateTime.UtcNow,
                        PokemonId = context.Entity.PokemonId,
                        FormId = context.Entity.Form ?? 0,
                        Count = 1,
                    };
                    await _context.AddAsync(pokemonShinyStat, cancellationToken);
                }

                await _context.SaveChangesAsync(cancellationToken);
            }
            // INSERT new pokemon
            // ON DUP increment count
            // If IV NOT NULL
            // INSERT INTO iv_stats
            // ON DUP increment count
            // If IV=100
            // INSERT INTO hundo_stats
            // ON DUP increment count

            await Task.CompletedTask;
        }
    }

    public class PokemonUpdatedTrigger : IBeforeSaveTrigger<Pokemon>
    {
        public async Task BeforeSave(ITriggerContext<Pokemon> context, CancellationToken cancellationToken)
        {
            if (context.ChangeType == ChangeType.Modified)
            {
            }
            await Task.CompletedTask;
        }
    }
}