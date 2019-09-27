using EthereumXplorer;
using EthereumXplorer.Client;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NBXplorer.Ethereum;
using NBXplorer.Models;
using Newtonsoft.Json;
using System;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;

namespace NBXplorer.Controllers
{
	[Route("v1/eth")]
	[Authorize]
	public class EthereumController : Controller
	{
		private readonly EventAggregator _EventAggregator;
		private readonly JsonSerializerSettings _SerializerSettings;

		public EthereumDWaiters Waiters
		{
			get; set;
		}
		public EthereumController(
  			EventAggregator eventAggregator,
			EthereumDWaiters waiters,
 			KeyPathTemplates keyPathTemplates,

			MvcNewtonsoftJsonOptions jsonOptions)
		{
			_SerializerSettings = jsonOptions.SerializerSettings;
			_EventAggregator = eventAggregator;
			Waiters = waiters;
		}

		[HttpGet]
		[Route("cryptos/{cryptoCode}/connect")]
		public async Task<IActionResult> ConnectWebSocket(
		string cryptoCode,
		bool includeTransaction = true,
		CancellationToken cancellation = default)
		{
			if (!HttpContext.WebSockets.IsWebSocketRequest)
			{
				return NotFound();
			}

			GetNetwork(cryptoCode, false); // Internally check if cryptoCode is correct

			EthereumWebsocketMessageListener server = new EthereumWebsocketMessageListener(await HttpContext.WebSockets.AcceptWebSocketAsync(), _SerializerSettings);
			CompositeDisposable subscriptions = new CompositeDisposable();
			subscriptions.Add(_EventAggregator.Subscribe<EthNewBlockEvent>(async o =>
			{
				await server.Send(o);
			}));
			subscriptions.Add(_EventAggregator.Subscribe<EthNewTransactionEvent>(async o =>
			{
				EthereumDWaiter network = Waiters.GetWaiter(o.CryptoCode);
				if (network == null)
				{
					return;
				}
				await server.Send(o);
			}));

			try
			{
				while (server.Socket.State == WebSocketState.Open)
				{ }
			}
			catch (Exception) { }
			finally { subscriptions.Dispose(); await server.DisposeAsync(cancellation); }
			return new EmptyResult();
		}

		private NBXplorerNetwork GetNetwork(string cryptoCode, bool checkRPC)
		{
			if (cryptoCode == null)
			{
				throw new ArgumentNullException(nameof(cryptoCode));
			}

			cryptoCode = cryptoCode.ToUpperInvariant();
			NBXplorerNetwork network = Waiters.GetWaiter(cryptoCode)?.Network;
			if (network == null)
			{
				throw new NBXplorerException(new NBXplorerError(404, "cryptoCode-not-supported", $"{cryptoCode} is not supported"));
			}

			if (checkRPC)
			{
				EthereumDWaiter waiter = Waiters.GetWaiter(network);
				if (waiter == null || !waiter.RPCAvailable)
				{
					throw new NBXplorerError(400, "rpc-unavailable", $"The RPC interface is currently not available.").AsException();
				}
			}
			return network;
		}
	}
}
