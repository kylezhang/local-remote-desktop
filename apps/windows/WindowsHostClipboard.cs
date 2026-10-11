using System.Text.Json;
using System.Windows.Threading;
namespace LocalRemoteDesktop;

internal sealed class WindowsHostClipboard : IHostTextClipboard {
    readonly Wire wire; readonly Dispatcher dispatcher; readonly HostTextClipboard state;
    readonly CancellationTokenSource lifetime; readonly Task pump;
    internal WindowsHostClipboard(Wire wire,ITextClipboard board,Dispatcher dispatcher,Func<bool> allowed,CancellationToken token) {
        this.wire=wire; this.dispatcher=dispatcher; state=new(board,allowed);
        lifetime=CancellationTokenSource.CreateLinkedTokenSource(token); pump=Task.Run(Pump);
    }
    async Task Pump() {
        try {
            using var timer=new PeriodicTimer(TimeSpan.FromMilliseconds(500));
            while(await timer.WaitForNextTickAsync(lifetime.Token)) {
                var message=await dispatcher.InvokeAsync(state.Poll,DispatcherPriority.Background,lifetime.Token);
                if(message!=null) await wire.Json(message,lifetime.Token);
            }
        } catch(OperationCanceledException) when(lifetime.IsCancellationRequested) { }
        catch { wire.Dispose(); }
    }
    public async Task Control(JsonElement message) {
        var response=await dispatcher.InvokeAsync(()=>state.Control(message),DispatcherPriority.Normal,lifetime.Token);
        if(response!=null) await wire.Json(response,lifetime.Token);
    }
    public async ValueTask DisposeAsync() {
        lifetime.Cancel();
        await dispatcher.InvokeAsync(state.Stop);
        await pump; lifetime.Dispose();
    }
}
