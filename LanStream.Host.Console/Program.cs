
using LanStream.Host.Core.Networking;
using LanStream.Host.Core.Discovery;

Console.WriteLine("LanStream Host starting...");

using CancellationTokenSource cts = new();

Console.CancelKeyPress += (sender, e) =>
{
    e.Cancel = true;
    cts.Cancel();
};

TcpServer tcpServer = new TcpServer(48000);
DiscoveryServer discoveryServer = new DiscoveryServer();

Task tcpTask = tcpServer.StartAsync(cts.Token);
Task discoveryTask = discoveryServer.StartAsync(cts.Token);

try
{
    await Task.WhenAll(tcpTask, discoveryTask);
}
finally
{
    cts.Cancel();
}
