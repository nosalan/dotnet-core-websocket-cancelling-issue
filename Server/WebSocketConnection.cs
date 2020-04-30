using System;
using System.IO;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CancellingIssue
{
    public class WebSocketConnection
    {
        private readonly WebSocket _webSocket;

        public WebSocketConnection(WebSocket webSocket)
        {
            _webSocket = webSocket;
        }

        public async Task<WebSocketCloseStatus?> ReceiveUntilClose()
        {
            WebSocketReceiveResult message;
            do
            {
                using (var memoryStream = new MemoryStream())
                {
                    message = await ReceiveMessage(memoryStream);
                    Console.WriteLine($"Received message, type '{message.MessageType}' {BitConverter.ToString(memoryStream.ToArray())}");
                    
                    //TODO Sleep for test
                    Thread.Sleep(TimeSpan.FromDays(1));
                }
            } while (message.MessageType != WebSocketMessageType.Close);

            return message.CloseStatus;
        }

        private async Task<WebSocketReceiveResult> ReceiveMessage(Stream memoryStream)
        {
            var readBuffer = new ArraySegment<byte>(new byte[4096]);
            WebSocketReceiveResult result;
            do
            {
                result = await _webSocket.ReceiveAsync(readBuffer, CancellationToken.None);
                memoryStream.Write(readBuffer.Array, readBuffer.Offset, result.Count);
            } while (!result.EndOfMessage);

            return result;
        }

        public async Task Send(string message)
        {
            var bytes = Encoding.UTF8.GetBytes(message);
            await _webSocket.SendAsync(new ArraySegment<byte>(bytes, 0, bytes.Length), WebSocketMessageType.Text, true,
                CancellationToken.None);
        }

        public async Task Close()
        {
            Console.Out.WriteLine("Closing session");
            await _webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, string.Empty, CancellationToken.None);
        }
    }
}