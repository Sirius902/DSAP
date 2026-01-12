using System;
using System.Threading.Tasks;

namespace DSAP.Game;

public record GameAction(Action ActionToRun, TaskCompletionSource CompletionSource);