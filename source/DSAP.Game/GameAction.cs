using System;
using System.Threading.Tasks;

namespace DSAP.Game;

public record GameAction(Func<bool> ActionToRun, TaskCompletionSource CompletionSource);