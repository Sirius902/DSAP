using System;
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
        await using var registration = context.CancellationToken.Register(() => tcs.TrySetCanceled());

        try
        {
            ActionQueue.PendingActions.Enqueue(new GameAction(() =>
            {
                if (!Helpers.IsInGame()) return false;
                Commands.HomewardBone();
                return true;
            }, tcs));

            await tcs.Task;
        }
        catch (OperationCanceledException)
        {
        }

        return new Empty();
    }

    public override async Task<Empty> AddItem(AddItemMsg request, ServerCallContext context)
    {
        var tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        await using var registration = context.CancellationToken.Register(() => tcs.TrySetCanceled());

        try
        {
            ActionQueue.PendingActions.Enqueue(new GameAction(() =>
            {
                if (!Helpers.IsInGame()) return false;
                Commands.AddItem(request.Category, request.Id, request.Quantity, request.ShowMessage);
                return true;
            }, tcs));

            await tcs.Task;
        }
        catch (OperationCanceledException)
        {
        }

        return new Empty();
    }
}