using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System.Linq;
using System.Net;

namespace Zhibiao.FastDFS.Client
{
    public static class ServiceCollectionExtesions
    {
        public static IServiceCollection AddFastDFS(this IServiceCollection services)
        {
            var options = services.BuildServiceProvider().GetRequiredService<IOptions<FastDFSOptions>>().Value;
            OnInit(options);

            return services;
        }

        private static void OnInit(FastDFSOptions options)
        {
            if (options.Trackers == null || options.Trackers.Count < 1)
            {
                throw new MyException("tracker server is invalid.");
            }

            var connectTimeout = options.ConnectTimeout > 0 ? options.ConnectTimeout : ClientGlobal.DEFAULT_CONNECT_TIMEOUT;
            ClientGlobal.g_connect_timeout = connectTimeout * 1000;

            var networkTimeout = options.NetworkTimeout > 0 ? options.NetworkTimeout : ClientGlobal.DEFAULT_NETWORK_TIMEOUT;
            ClientGlobal.g_network_timeout = networkTimeout * 1000;

            ClientGlobal.g_charset = string.IsNullOrWhiteSpace(options.Charset) ? "ISO8859-1" : options.Charset.Trim();
            ClientGlobal.g_tracker_group = new TrackerGroup(options.Trackers
                .Select(x => new IPEndPoint(IPAddress.Parse(x.IP.Trim()), x.Port))
                .ToArray());

            ClientGlobal.g_tracker_http_port = options.TrackerHttpPort > 0 ? options.TrackerHttpPort : 80;
            ClientGlobal.g_anti_steal_token = options.AntiStealToken;
            if (ClientGlobal.g_anti_steal_token)
            {
                ClientGlobal.g_secret_key = options.SecretKey;
            }
        }
    }
}
