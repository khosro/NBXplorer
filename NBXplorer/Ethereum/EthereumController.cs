using EthereumXplorer;
using EthereumXplorer.Client;
using EthereumXplorer.Client.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NBXplorer.Ethereum;
using NBXplorer.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;

namespace NBXplorer.Controllers
{
	[Route("v1/eth")]
	[Authorize]
	public partial class EthereumController : Controller
	{
		private readonly EventAggregator _EventAggregator;
		private readonly JsonSerializerSettings _SerializerSettings;
		private readonly EthereumXplorerClientProvider _EthereumXplorerClientProvider;
		public EthereumDWaiters Waiters
		{
			get; set;
		}

		private EthereumXplorerClient _EthereumXplorerClient(string crytoCode)
		{
			return _EthereumXplorerClientProvider.GetEthereumClient(crytoCode);
		}
		public EthereumController(EventAggregator eventAggregator, EthereumDWaiters waiters, MvcNewtonsoftJsonOptions jsonOptions, EthereumXplorerClientProvider ethereumXplorerClientProvider)
		{
			_SerializerSettings = jsonOptions.SerializerSettings;
			_EventAggregator = eventAggregator;
			Waiters = waiters;
			_EthereumXplorerClientProvider = ethereumXplorerClientProvider;
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

		[HttpGet]
		[Route("cryptos/{CryptoCode}/transid/{txId}")]
		public async Task<IActionResult> GetTransactionAsyncByTransactionId(string cryptoCode, string transid)
		{
			EthereumClientTransactionData trans = await _EthereumXplorerClient(cryptoCode).GetTransactionAsyncByTransactionId(transid);
			return Json(trans);
		}

		[HttpGet]
		[Route("cryptos/{cryptoCode}/status")]
		public async Task<IActionResult> GetStatus(string cryptoCode)
		{
			EthereumXplorer.Client.Models.EthereumStatusResult statusResult = await _EthereumXplorerClient(cryptoCode).GetStatusAsync();
			return Json(statusResult);
		}

		[HttpGet]
		[Route("txs/cryptos/{CryptoCode}/mnemonic/{mnemonic}")]
		public async Task<IActionResult> GetTransactionsAsync(string cryptoCode, string mnemonic)
		{
			IEnumerable<EthereumClientTransactionData> trans = await _EthereumXplorerClient(cryptoCode).GetTransactionsAsync(mnemonic);
			return Json(trans);
		}


		[HttpGet]
		[Route("cryptos/{CryptoCode}/address/{address}")]
		public async Task<IActionResult> GetBalance(string cryptoCode, string address)
		{
			decimal value = await _EthereumXplorerClient(cryptoCode).GetBalance(address);
			return Json(value);
		}

		[HttpGet]
		[Route("cryptos/{CryptoCode}/mnemonic/{mnemonic}")]
		public async Task<IActionResult> GetBalanceByMnemonic(string cryptoCode, string mnemonic)
		{
			Dictionary<string, decimal> value = await _EthereumXplorerClient(cryptoCode).GetBalanceByMnemonic(mnemonic);
			return Json(value);
		}

		[HttpPost]
		[Route("cryptos/{cryptoCode}/mnemonic/{mnemonic}")]
		public async Task<IActionResult> BroadcastAsync(string cryptoCode,
			 string mnemonic, [FromBody]  EthExplorerWalletSendModel ethWalletSendModel)
		{
			string value = await _EthereumXplorerClient(cryptoCode).BroadcastAsync(ethWalletSendModel, mnemonic);
			return Json(value);
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



