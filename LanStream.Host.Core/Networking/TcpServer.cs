
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;

namespace LanStream.Host.Core.Networking;

public class TcpServer
{
    private readonly TcpListener _listener;

    public TcpServer(int port = 48000)
    {
        _listener = new TcpListener(IPAddress.Any, port);
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _listener.Start();

        Console.WriteLine("TCP server started.");

        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                TcpClient client = await _listener
                    .AcceptTcpClientAsync(cancellationToken);

                Console.WriteLine("Client connected.");

                _ = HandleClientAsync(client, cancellationToken);
            }
        }
        catch (OperationCanceledException)
        {
            // Normal shutdown.
        }
        catch (ObjectDisposedException) when (cancellationToken.IsCancellationRequested)
        {
            // Listener closed during shutdown.
        }
        finally
        {
            _listener.Stop();
        }
    }

    private async Task HandleClientAsync(
        TcpClient client,
        CancellationToken cancellationToken)
    {
        using (client)
        using (NetworkStream stream = client.GetStream())
        using (StreamReader reader = new StreamReader(stream, Encoding.UTF8))
        using (StreamWriter writer = new StreamWriter(stream, new UTF8Encoding(false)))
        {
            writer.AutoFlush = true;

            try
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    string? message = await reader.ReadLineAsync(cancellationToken);

                    if (message == null)
                        break;

                    Console.WriteLine($"Received: {message}");

                    using JsonDocument document = JsonDocument.Parse(message);

                    string? type = document.RootElement
                        .GetProperty("type")
                        .GetString();

                    if (type == "HELLO")
                    {
                        var response = new
                        {
                            type = "WELCOME",
                            version = 1
                        };

                        string json = JsonSerializer.Serialize(response);

                        await writer.WriteLineAsync(json);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // Normal shutdown.
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Client error: {ex.Message}");
            }
            finally
            {
                Console.WriteLine("Client disconnected.");
            }
        }
    }
}
