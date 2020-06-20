using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Subjects;
using System.Threading;
using System.Threading.Tasks;

namespace IntelliCenterGateway
{
    public class TelnetBackgroundService : BackgroundService
    {
        private string _telnetHost;
        private int _telnetPort;
        private TelnetClient _telnetClient;
        private Subject<string> _subject = new Subject<string>();

        public TelnetBackgroundService(IConfiguration config)
        {
            _telnetHost = config.GetValue<string>("Configuration:TelnetHost");
            _telnetPort = config.GetValue<int>("Configuration:TelnetPort", 6681);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _telnetClient = new TelnetClient(_telnetHost, _telnetPort, TimeSpan.FromSeconds(1), stoppingToken);
            _telnetClient.MessageReceived += new EventHandler<string>((sender, e) => _subject.OnNext(e));
            _telnetClient.ConnectionClosed += new EventHandler((sender, e) => _subject.OnNext("Connection closed"));

            await _telnetClient.Connect();

            while (!stoppingToken.IsCancellationRequested) {                
                await _telnetClient.Send("ping"); //keepalive
                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
            }

            _telnetClient.Disconnect();
        }

        public IObservable<string> Feed()
        {
            return _subject;
        }

        public async Task Request(string cmd)
        {
            await _telnetClient.Send(cmd);
        }
    }

    public class BackgroundServiceStarter<T> : IHostedService where T : IHostedService
    {
        readonly T backgroundService;

        public BackgroundServiceStarter(T backgroundService)
        {
            this.backgroundService = backgroundService;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            return backgroundService.StartAsync(cancellationToken);
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return backgroundService.StopAsync(cancellationToken);
        }
    }
}