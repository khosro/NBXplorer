using EthereumXplorer.Client.Models.Events;
using NBitcoin;
using NBXplorer;
using Newtonsoft.Json;
using System;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace EthereumXplorer.Client
{
	public class EthereumWebsocketNotificationSession : EthereumNotificationSessionBase, IDisposable
	{

		private readonly EthereumExplorerClient _Client;
		public EthereumExplorerClient Client => _Client;
		internal EthereumWebsocketNotificationSession(EthereumExplorerClient client)
		{
			if (client == null)
			{
				throw new ArgumentNullException(nameof(client));
			}

			_Client = client;
		}

		internal async Task ConnectAsync(CancellationToken cancellation)
		{
			string uri = _Client.GetFullUri($"v1/eth/cryptos/{_Client.CryptoCode}/connect", null);
			uri = ToWebsocketUri(uri);
			WebSocket socket = null;
			try
			{
				socket = await ConnectAsyncCore(uri, cancellation).ConfigureAwait(false);
			}
			catch (WebSocketException) // For some reason the ErrorCode is not properly set, so we can check for error 401
			{
				if (!_Client._Auth.RefreshCache())
				{
					throw;
				}

				socket = await ConnectAsyncCore(uri, cancellation).ConfigureAwait(false);
			}
			JsonSerializerSettings settings = new JsonSerializerSettings();
			new Serializer(Network.Main).ConfigureSerializer(settings);
			_MessageListener = new EthereumWebsocketMessageListener(socket, settings);
		}

		private async Task<ClientWebSocket> ConnectAsyncCore(string uri, CancellationToken cancellation)
		{
			ClientWebSocket socket = new ClientWebSocket();
			_Client._Auth.SetWebSocketAuth(socket);
			try
			{
				await socket.ConnectAsync(new Uri(uri, UriKind.Absolute), cancellation).ConfigureAwait(false);
			}
			catch { socket.Dispose(); throw; }
			return socket;
		}

		private static string ToWebsocketUri(string uri)
		{
			if (uri.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
			{
				uri = uri.Replace("https://", "wss://");
			}

			if (uri.StartsWith("http://", StringComparison.OrdinalIgnoreCase))
			{
				uri = uri.Replace("http://", "ws://");
			}

			return uri;
		}

		private EthereumWebsocketMessageListener _MessageListener;

		public override Task<EthereumNewEventBase> NextEventAsync(CancellationToken cancellation = default)
		{
			return _MessageListener.NextMessageAsync(cancellation);
		}

		public Task DisposeAsync(CancellationToken cancellation = default)
		{
			return _MessageListener.DisposeAsync(cancellation);
		}

		public void Dispose()
		{
			DisposeAsync().GetAwaiter().GetResult();
		}
	}
}
