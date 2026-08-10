using System;
using System.IO;
using System.IO.Pipes;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace TCLauncher.Core.Services
{
    public sealed class SingleInstanceService : IDisposable
    {
        private readonly string _pipeName;
        private readonly Action<string[]> _onArguments;
        private readonly CancellationTokenSource _shutdown = new CancellationTokenSource();

        public SingleInstanceService(string pipeName, Action<string[]> onArguments)
        {
            _pipeName = pipeName;
            _onArguments = onArguments;
        }

        public void Start()
        {
            Task.Run(() => ListenAsync(_shutdown.Token));
        }

        public static async Task<bool> SendAsync(string pipeName, string[] arguments, int timeoutMilliseconds)
        {
            try
            {
                using (var client =
                       new NamedPipeClientStream(".", pipeName, PipeDirection.Out, PipeOptions.Asynchronous))
                {
                    await Task.Run(() => client.Connect(timeoutMilliseconds));
                    var bytes = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(arguments ?? new string[0]));
                    await client.WriteAsync(bytes, 0, bytes.Length);
                    await client.FlushAsync();
                    return true;
                }
            }
            catch
            {
                return false;
            }
        }

        private async Task ListenAsync(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    using (var server = new NamedPipeServerStream(_pipeName, PipeDirection.In, 1,
                               PipeTransmissionMode.Byte, PipeOptions.Asynchronous))
                    {
                        await server.WaitForConnectionAsync(cancellationToken);
                        using (var reader = new StreamReader(server, Encoding.UTF8, false, 4096, true))
                        {
                            var json = await reader.ReadToEndAsync();
                            var arguments = JsonConvert.DeserializeObject<string[]>(json) ?? new string[0];
                            _onArguments(arguments);
                        }
                    }
                }
                catch (OperationCanceledException)
                {
                    return;
                }
                catch (Exception exception)
                {
                    AppServices.Log.Warning("single_instance.pipe_error", exception.Message);
                    await Task.Delay(250, cancellationToken);
                }
            }
        }

        public void Dispose()
        {
            _shutdown.Cancel();
            _shutdown.Dispose();
        }
    }
}