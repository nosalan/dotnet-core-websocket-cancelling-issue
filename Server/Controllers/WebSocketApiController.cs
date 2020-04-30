using System;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Mvc;

namespace CancellingIssue.Controllers
{
    [Route("/api/ws")]
    [ApiController]
    public class WebSocketApiController : ControllerBase
    {
        [HttpGet]
        public async Task<IActionResult> Get()
        {
            var context = ControllerContext.HttpContext;

            if (context.WebSockets.IsWebSocketRequest)
            {
                var webSocket = await context.WebSockets.AcceptWebSocketAsync();
                Console.WriteLine($"Accepted connection '{context.Connection.Id}'");
                var connection = new WebSocketConnection(webSocket);

                await connection.ReceiveUntilClose();
                await connection.Close();
                return new EmptyResult();
            }
            else
            {
                return new StatusCodeResult((int) HttpStatusCode.BadRequest);
            }
        }
    }
}