using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace NessusClient
{
    public interface INessusConnection: IDisposable
    {
        Task OpenAsync(CancellationToken cancellationToken);
        Task CloseAsync(CancellationToken cancellationToken);
        WebRequest CreateRequest(string relativeEndpointUrl, string httpMethod, CancellationToken cancellationToken);
    }
}