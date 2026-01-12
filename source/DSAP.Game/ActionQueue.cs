using System.Collections.Concurrent;

namespace DSAP.Game;

internal static class ActionQueue
{
    public static readonly ConcurrentQueue<GameAction> PendingActions = new();
}