
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;

namespace LanStream.Host.Core.Discovery;

public class DiscoveryServer
{
    private const int DiscoveryPort = 48002;
    private const int TcpPort = 48000;

    public async Task StartAsync(CancellationToken token)
    {
        using UdpClient udp = new UdpClient(
            new IPEndPoint(IPAddress.Any, DiscoveryPort)
        );

        Console.WriteLine(
            $"Discovery server listening on UDP {DiscoveryPort}"
        );

        try
        {
            while (!token.IsCancellationRequested)
            {
                UdpReceiveResult request =
                    await udp.ReceiveAsync(token);

                string message = Encoding.UTF8.GetString(
                    request.Buffer
                );

                // Ignore unknown discovery requests.
                try
                {
                    using JsonDocument json =
                        JsonDocument.Parse(message);

                    var root = json.RootElement;

                    if (root.GetProperty("type").GetString()
                        != "LANSTREAM_DISCOVER")
                        continue;

                    if (root.GetProperty("version").GetInt32() != 1)
                        continue;
                }
                catch (JsonException)
                {
                    continue;
                }
                catch (KeyNotFoundException)
                {
                    continue;
                }

                var response = new
                {
                    type = "LANSTREAM_HOST",
                    version = 1,
                    name = Environment.MachineName,
                    tcpPort = TcpPort
                };

                byte[] bytes = Encoding.UTF8.GetBytes(
                    JsonSerializer.Serialize(response)
                );

                await udp.SendAsync(
                    bytes,
                    request.RemoteEndPoint,
                    token
                );

                Console.WriteLine(
                    $"Discovery response sent to " +
                    request.RemoteEndPoint
                );
            }
        }
        catch (OperationCanceledException)
        {
            // Normal application shutdown.
        }
    }
}
