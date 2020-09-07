using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Net.WebSockets;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Net.Http.Headers;
using System.Net.Http.Headers;

namespace IntelliCenterGateway.Controllers
{
    [Route("[controller]")]
    public class ShortcutsController : Controller
    {
        private readonly IConfiguration _config;
        private readonly UriBuilder _wsUri;
        private readonly ClientWebSocket _ws;

        public ShortcutsController(IConfiguration config)
        {
            _config = config;
            var host = _config.GetValue<string>("Configuration:TelnetHost");
            var port = _config.GetValue<int>("Configuration:WebSocketPort", 6680);
            _wsUri = new UriBuilder("ws", host, port);
            _ws = new ClientWebSocket();
        }

        [HttpPost]
        public async Task<IActionResult> Index(IFormCollection request)
        {
            try
            {
                if (!IsValid(Request.Headers))
                    return Problem(StatusCodes.Status401Unauthorized);

                if (request.ContainsKey("Object") && request.ContainsKey("Param"))
                {
                    string result = null;
                    var obj = request["Object"].ToString();
                    var key = request["Param"].ToString();

                    if (request.ContainsKey("Value"))
                    {
                        var val = request["Value"].ToString();
                        if (await SetValueAsync(obj, key, val))
                            result = val;
                    }
                    else
                        result = await GetValueAsync(obj, key);

                    if (result == null)
                        return Problem(StatusCodes.Status400BadRequest);
                    else
                    {
                        var reply = new
                        {
                            status = StatusCodes.Status200OK,
                            result
                        };

                        return Json(reply);
                    }
                }
                else
                    return Problem(StatusCodes.Status400BadRequest);
            }
            catch (Exception)
            {
                return StatusCode(StatusCodes.Status500InternalServerError);
            }
        }

        private IActionResult Problem(int code)
        {
            var prob = new ProblemDetails { Status = code };
            return StatusCode(code, prob);
        }

        private bool IsValid(IHeaderDictionary headers)
        {
            if (headers.ContainsKey(HeaderNames.Authorization))
            {
                var authHeader = AuthenticationHeaderValue.Parse(Request.Headers[HeaderNames.Authorization]);
                var credsBytes = Convert.FromBase64String(authHeader.Parameter);
                var credsArray = Encoding.ASCII.GetString(credsBytes).Split(':', 2);

                var users = _config.GetSection("Users").GetChildren().ToDictionary(x => x.Key, x => x.Value, StringComparer.OrdinalIgnoreCase);
                return (users.ContainsKey(credsArray[0]) && users[credsArray[0]] == credsArray[1]);
            }
            else
                return false;
        }

        private async Task<bool> SetValueAsync(string objname, string key, string value)
        {
            await _ws.ConnectAsync(_wsUri.Uri, CancellationToken.None);

            var msg = await RequestAsync(SetParamCommand(objname, key, value));

            await _ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", CancellationToken.None);

            return JsonDocument.Parse(msg).RootElement.GetProperty("response").ValueEquals("200");
        }

        private async Task<string> GetValueAsync(string objname, string param)
        {
            await _ws.ConnectAsync(_wsUri.Uri, CancellationToken.None);

            var msg = await RequestAsync(GetParamCommand(objname, param));
            var val = (msg == null ? null : GetParamValue(JsonDocument.Parse(msg), param));

            await _ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", CancellationToken.None);

            return val;
        }

        private async Task<string> RequestAsync(string cmd)
        {
            byte[] messageBytes = Encoding.UTF8.GetBytes(cmd);
            await _ws.SendAsync(new ArraySegment<byte>(messageBytes), WebSocketMessageType.Text, true, CancellationToken.None);

            byte[] incomingData = new byte[1024];
            var result = await _ws.ReceiveAsync(new ArraySegment<byte>(incomingData), CancellationToken.None);

            if (result.MessageType == WebSocketMessageType.Text && result.EndOfMessage)
                return Encoding.UTF8.GetString(incomingData, 0, result.Count);
            else
                return null;
        }

        private string GetParamCommand(string objname, string param)
        {
            var cmd = new
            {
                command = "GetParamList",
                objectList = new[] {
                    new {
                        objnam = objname,
                        keys = new[] { param }
                    }
                },
                messageID = Guid.NewGuid()
            };

            return JsonSerializer.Serialize(cmd);
        }

        private string SetParamCommand(string objname, string key, string value)
        {
            var cmd = new
            {
                command = "SetParamList",
                objectList = new[] {
                    new {
                        objnam = objname,
                        @params = new Dictionary<string, string> { [key] = value }
                    }
                },
                messageID = Guid.NewGuid()
            };

            return JsonSerializer.Serialize(cmd);
        }

        private string GetParamValue(JsonDocument doc, string param)
        {
            if (doc.RootElement.GetProperty("response").ValueEquals("200") && doc.RootElement.GetProperty("objectList").GetArrayLength() > 0)
                return doc.RootElement.GetProperty("objectList")[0].GetProperty("params").GetProperty(param).GetString();
            else
                return null;
        }
    }
}
