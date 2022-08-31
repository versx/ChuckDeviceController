namespace ChuckDeviceController.Data.Triggers
{
    using System.Threading;
    using System.Threading.Tasks;

    using EntityFrameworkCore.Triggered;

    using ChuckDeviceController.Data.Contexts;
    using ChuckDeviceController.Data.Entities;

    public class PokemonInsertedTrigger : IAfterSaveTrigger<Pokemon>
    {
        private readonly MapContext _context;

        public PokemonInsertedTrigger(MapContext context)
        {
            _context = context;
        }

        public async Task AfterSave(ITriggerContext<Pokemon> context, CancellationToken cancellationToken)
        {
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
}