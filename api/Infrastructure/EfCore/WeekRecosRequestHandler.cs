using Infrastructure.EfCore.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Services;
using Services.Moves;

namespace Infrastructure.EfCore;

public class WeekRecosRequestHandler : IRequestHandler<GetWeekRecosRequest, IWeekRecos>
{
    private readonly AppDbContext _dbContext;

    public WeekRecosRequestHandler(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IWeekRecos> Handle(GetWeekRecosRequest request, CancellationToken cancellationToken)
    {
        var exercises = await SelectExercises(request.Muscle, cancellationToken);
        // var recos = await SpreadIntoWorkouts(request.HeavySets, request.Workouts, cancellationToken);

        return new WeekRecos
        {
            Plan = new Dictionary<DayOfWeek, IMoveSets[]>
            {
                {
                    DayOfWeek.Monday, exercises.Select(x => new MoveSets
                    {
                        Move = new Move(x.Id, x.Name),
                        Sets = 3
                    } as IMoveSets)
                        .ToArray()
                }
            }
        };
    }

    private async Task<Task<IWeekRecos>> SpreadIntoWorkouts(int? numberOfHeavySets, int? numberOfWorkouts,
        CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    private async Task<EfMove[]> SelectExercises(Guid[]? muscleIds, CancellationToken cancellationToken)
    {
        EfMuscle[] muscles;
        if (muscleIds is not null)
            muscles = await _dbContext.Muscles
                .AsNoTracking()
                .Where(MuscleSpecs.ByIds(muscleIds))
                .ToArrayAsync(cancellationToken);
        else
            muscles = await _dbContext.Muscles
                .AsNoTracking()
                .Where(MuscleSpecs.Root())
                .ToArrayAsync(cancellationToken);

        var moves = new Dictionary<Guid, EfMove>();
        foreach (var muscle in muscles)
        {
            var movesForMuscle = await _dbContext.Moves
                .AsNoTracking()
                .Include(x => x.ActivationData)
                .Where(m => m.ActivationData!.Any(a => a.MuscleId == muscle.Id))
                .OrderBy(x => Guid.NewGuid())
                .Take(3)
                .ToListAsync(cancellationToken);

            foreach (var move in movesForMuscle)
                moves[move.Id] = move;
        }

        return moves.Select(x => x.Value).ToArray();
    }

    private int GetNumberOfWorkouts(int? desired)
    {
        const int defaultWorkouts = 4;

        return desired is null or < 0 or > 6
            ? defaultWorkouts
            : desired.Value;
    }
}

public class WeekRecos : IWeekRecos
{
    public IDictionary<DayOfWeek, IMoveSets[]> Plan { get; set; }
}

public class MoveSets : IMoveSets
{
    public IMove Move { get; set; }
    public int Sets { get; set; }
}