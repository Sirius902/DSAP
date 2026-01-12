using System.Threading.Tasks;
using DSAP.Game.Protos;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;

namespace DSAP.Game;

public class ArchipelagoService : ArchipelagoAction.ArchipelagoActionBase
{
    public override async Task<Empty> HomewardBone(Empty request, ServerCallContext context)
    {
        var tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        ActionQueue.PendingActions.Enqueue(new GameAction(() =>
        {
            if (!Helpers.IsInGame()) return false;
            Commands.ExecuteHomewardBone();
            return true;
        }, tcs));

        await tcs.Task;

        return new Empty();
    }
}