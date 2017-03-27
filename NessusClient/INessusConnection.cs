using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace NessusClient
{
    public interface INessusConnection: IDisposable
    {
        Task OpenAsync(CancellationToken cancellationToken);
        Task CloseAsync();
        WebRequest CreateRequest(string relativeEndpointUrl, string httpMethod = WebRequestMethods.Http.Get);
    }
}