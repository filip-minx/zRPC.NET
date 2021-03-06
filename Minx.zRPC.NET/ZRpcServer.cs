using System;
using System.Collections.Generic;
using System.Linq;
using NetMQ;
using NetMQ.Sockets;
using Newtonsoft.Json;

namespace Minx.zRPC.NET
{
    public class ZRpcServer : IDisposable
    {
        private static readonly JsonSerializerSettings SerializerSettings = new JsonSerializerSettings()
        {
            TypeNameHandling = TypeNameHandling.All,
            Converters = new List<JsonConverter>()
            {
                new Int32Converter()
            }
        };

        private Dictionary<string, object> services = new Dictionary<string, object>();
        private NetMQPoller poller;
        private ResponseSocket socket;

        public ZRpcServer(string address, int port)
        {
            socket = new ResponseSocket($"@tcp://{address}:{port}");

            socket.ReceiveReady += HandleProcedureInvocationRequest;

            poller = new NetMQPoller()
            {
                socket
            };

            poller.RunAsync();
        }

        private void HandleProcedureInvocationRequest(object sender, NetMQSocketEventArgs e)
        {
            var invocationJson = e.Socket.ReceiveFrameString();

            var invocation = JsonConvert.DeserializeObject<Invocation>(invocationJson, SerializerSettings);

            var result = Invoke(invocation);

            var resultJson = JsonConvert.SerializeObject(result, SerializerSettings);

            e.Socket.SendFrame(resultJson);
        }

        public void RegisterService<TInterface, TImplementation>(TImplementation implementation)
            where TImplementation : class, TInterface
        {
            services.Add(typeof(TInterface).FullName, implementation);
        }

        public InvocationResult Invoke(Invocation invocation)
        {
            var service = services[invocation.TypeName];

            var argumentsTypes = invocation.Arguments
                .Select(a => a.GetType())
                .ToArray();

            var methodInfo = service
                .GetType()
                .GetMethod(invocation.MethodName, argumentsTypes);

            var result = methodInfo.Invoke(service, invocation.Arguments);

            return new InvocationResult()
            {
                Result = result,
                Invocation = invocation
            };
        }

        public void Dispose()
        {
            poller?.Dispose();
            poller = null;

            socket?.Dispose();
            socket = null;
        }
    }
}
