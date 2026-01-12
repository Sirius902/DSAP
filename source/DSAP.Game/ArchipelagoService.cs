using System.Threading.Tasks;
using DSAP.Game.Protos;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Windows.Win32.Foundation;
using Windows.Win32.UI.WindowsAndMessaging;
using PInvoke = Windows.Win32.PInvoke;

namespace DSAP.Game;

public class ArchipelagoService : ArchipelagoAction.ArchipelagoActionBase
{
    public override async Task<Empty> HomewardBone(Empty request, ServerCallContext context)
    {
        var tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        ActionQueue.PendingActions.Enqueue(new GameAction(() =>
        {
            PInvoke.MessageBox(HWND.Null, "Homeward Bone Executed!", "DSAP.Game", MESSAGEBOX_STYLE.MB_OK);
        }, tcs));

        await tcs.Task;

        return new Empty();
    }
}