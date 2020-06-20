using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using System.Diagnostics;
using System.Threading;
using System.Threading.Channels;

namespace IntelliCenterGateway
{
    public class GatewayHub : Hub
    {
        private readonly TelnetBackgroundService _telnetService;

        public GatewayHub(TelnetBackgroundService telnetService)
        {
            _telnetService = telnetService;
        }

        public Task Request(string cmd)
        {
            return _telnetService.Request(cmd);
        }

        public ChannelReader<string> Feed()
        {
            return _telnetService.Feed().AsChannelReader(10);
        }
    }

    public static class IObservableExtensions
    {
        public static ChannelReader<T> AsChannelReader<T>(this IObservable<T> observable, int? maxBufferSize = null)
        {
            var channel = maxBufferSize != null ? Channel.CreateBounded<T>(maxBufferSize.Value) : Channel.CreateUnbounded<T>();
            var disposable = observable.Subscribe(
                value => channel.Writer.TryWrite(value),
                error => channel.Writer.TryComplete(error),
                () => channel.Writer.TryComplete());

            channel.Reader.Completion.ContinueWith(task => disposable.Dispose());

            return channel.Reader;
        }
    }
}
