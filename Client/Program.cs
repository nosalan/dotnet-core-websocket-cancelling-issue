using System;
using System.IO;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
/*
 * NOTE: This project is to prove that Close hangs (even with cancellation token) when remote is not processing timely
 */
namespace Client
{
    class Program
    {
        private static volatile bool _closeWasInitiatedByLocalSite;
        
        static async Task Main(string[] args)
        {
            var socket = await Connect();

            StartReceivingTask(socket);

            await SendSomeBytes(socket);
            await Close(socket);
        }

        private static async Task<ClientWebSocket> Connect()
        {
            var socket = new ClientWebSocket();
            var uri =
                "ws://localhost:5000/api/ws";
            await socket.ConnectAsync(new Uri(uri), CancellationToken.None);
            Console.Out.WriteLine("Connected");
            return socket;
        }

        private static void StartReceivingTask(ClientWebSocket socket)
        {
            Task.Run(async () =>
            {
                try
                {
                    var readBuffer = new ArraySegment<byte>(new byte[4 * 1024]);
                    do
                    {
                        using (var memoryStream = new MemoryStream())
                        {
                            WebSocketReceiveResult result;
                            do
                            {
                                result = await socket.ReceiveAsync(readBuffer, CancellationToken.None);
                                await memoryStream.WriteAsync(readBuffer.Array, readBuffer.Offset, result.Count,
                                    CancellationToken.None);
                            } while (!result.EndOfMessage);

                            

                            if (result.CloseStatus.HasValue)
                            {
                                if (_closeWasInitiatedByLocalSite)
                                {
                                    Console.Out.WriteLine("Remote confirmed close");
                                }
                                else
                                {
                                    Console.WriteLine("Close received");
                                    await Close(socket);
                                    break;
                                }
                            }
                            else
                            {
                                Console.WriteLine("Received msg len: " + readBuffer.Count);
                            }
                        }
                    } while (true);
                }
                catch (Exception e)
                {
                    Console.WriteLine("Exception in receiving: " + e);
                }

                Console.Out.WriteLine("Done receiving");
            });
        }

        private static async Task SendSomeBytes(ClientWebSocket socket)
        {
            byte[] b = new byte[16000];
            for (int i = 0; i < 100; i++)
            {
                try
                {
                    Console.WriteLine("Sending to ws, len:" + b.Length);
                    await socket.SendAsync(new ArraySegment<byte>(b), WebSocketMessageType.Binary, true,
                        CancellationToken.None);
                    await Task.Delay(100);
                }
                catch (Exception e)
                {
                    Console.WriteLine("Exception on send: " + e);
                }
            }

            Console.Out.WriteLine("Done sending");
        }

        private static async Task Close(ClientWebSocket socket)
        {
            _closeWasInitiatedByLocalSite = true;
            using (var ctsc = new CancellationTokenSource(TimeSpan.FromSeconds(10)))
            {
                var token = ctsc.Token;
                try
                {
                    Console.Out.WriteLine("Closing");
                    //Bug - hangs here when remote is not processing messages timely
                    //CancellationToken is not working
                    await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "", token);
                    Console.Out.WriteLine("Closed");
                }
                catch (OperationCanceledException e)
                {
                    Console.WriteLine(
                        "Exc on close: " + e.Message + ". Is our canc?: " + (e.CancellationToken == token));
                }
                catch (Exception e)
                {
                    Console.WriteLine(
                        "Another Exc on close: " + e.Message);
                }
            }
        }
    }
}