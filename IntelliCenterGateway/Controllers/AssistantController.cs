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

namespace IntelliCenterGateway.Controllers
{
    [Route("[controller]")]
    public class AssistantController : Controller
    {
        private readonly IConfiguration _config;
        private readonly UriBuilder _wsUri;
        private readonly ClientWebSocket _ws;

        public AssistantController(IConfiguration config)
        {
            _config = config;
            var host = _config.GetValue<string>("Configuration:TelnetHost");
            var port = _config.GetValue<int>("Configuration:WebSocketPort", 6680);
            _wsUri = new UriBuilder("ws", host, port);
            _ws = new ClientWebSocket();
        }

        [HttpPost]
        public async Task<IActionResult> Index([FromBody] JsonElement request)
        {
            try
            {
                if (!IsValid(request))
                    return BadRequest();

                var intent = request.GetProperty("intent").GetProperty("name").GetString();
                string text = await GetTextAsync(intent);

                if (text == null)
                    return BadRequest();
                else
                {
                    var reply = new
                    {
                        prompt = new
                        {
                            firstSimple = new { speech = text }
                        }
                    };

                    return Json(reply);
                }
            }
            catch (Exception)
            {
                return StatusCode(StatusCodes.Status500InternalServerError);
            }
        }

        private bool IsValid(JsonElement request)
        {
            //Basic check in lieu of proper signature validation in middleware
            var name = _config.GetValue<string>("Assistant:HandlerName", null);
            if (name == null)
                return true;
            else
                return name.Equals(request.GetProperty("handler").GetProperty("name").GetString());
        }

        private async Task<string> GetTextAsync(string intent)
        {
            var intents = _config.GetSection("Assistant:Intents").Get<Dictionary<string, string>>();
            if (!intents.ContainsKey(intent))
                return null;

            var augment = _config.GetValue<bool>("Assistant:Augment", false);

            if (intent == "Temp")
            {
                var temp = await GetValueAsync(intents[intent], "PROBE");
                if (temp == null)
                    return "Sorry I'm unable to get the water temperature right now";
                else
                {
                    var extra = Int16.Parse(temp) switch
                    {
                        var n when n < 78 => "which is cold",
                        var n when n > 88 => "which is hot",
                        _ => "which is ideal"
                    };
                    return $"The water temperature is currently {temp} degrees {(augment ? extra : "")}";
                }
            }
            else if (intent == "Salt")
            {
                var salt = await GetValueAsync(intents[intent], "SALT");
                if (salt == null)
                    return "Sorry I'm unable to get the salt level right now";
                else
                {
                    var extra = Int16.Parse(salt) switch
                    {
                        var n when n < 2700 => "which is low",
                        var n when n > 3400 => "which is high",
                        _ => "which is optimal"
                    };
                    return $"The salt level is currently {salt} parts per million {(augment ? extra : "")}";
                }
            }
            else if (intent == "Chem")
            {
                var chem = await GetValueAsync(intents[intent], "PHVAL");
                if (chem == null)
                    return "Sorry I'm unable to get the pH value right now";
                else
                {
                    var extra = Int16.Parse(chem) switch
                    {
                        var n when n <= 6 => "which is acidic",
                        var n when n >= 8 => "which is alkaline",
                        _ => "which is neutral"
                    };
                    return $"The pH value is currently {chem} {(augment ? extra : "")}";
                }
            }
            else
                return null;
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

        private string GetParamValue(JsonDocument doc, string param)
        {
            if (doc.RootElement.GetProperty("response").ValueEquals("200") && doc.RootElement.GetProperty("objectList").GetArrayLength() > 0)
                return doc.RootElement.GetProperty("objectList")[0].GetProperty("params").GetProperty(param).GetString();
            else
                return null;
        }
    }
}
