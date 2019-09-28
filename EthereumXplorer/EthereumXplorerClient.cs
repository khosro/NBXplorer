using EthereumXplorer.Client.Models;
using EthereumXplorer.Data;
using EthereumXplorer.Loggging;
using EthereumXplorer.Services;
using Microsoft.Extensions.Logging;
using NBitcoin;
using NBXplorer;
using Nethereum.Hex.HexTypes;
using Nethereum.JsonRpc.WebSocketStreamingClient;
using Nethereum.RPC.Eth.DTOs;
using Nethereum.RPC.Reactive.Eth.Subscriptions;
using Nethereum.Util;
using Nethereum.Web3;
using Nethereum.Web3.Accounts;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Numerics;
using System.Threading;
using System.Threading.Tasks;

namespace EthereumXplorer
{
	public class EthereumXplorerClient
	{
		private readonly Uri _rpcUri;
		private Web3 _web3;
		private readonly EthereumXplorerNetwork _Network;
		private readonly NBXplorerNetworkProvider _NetworkProvider;
		private readonly EthereumClientTransactionRepository _ethereumClientTransactionRepository;
		public EthNewPendingTransactionObservableSubscription PendingTransactionsSubscription { get; private set; }

		private readonly string _websocketUrl;
		private StreamingWebSocketClient _streamingWebSocketClient;

		public EthereumXplorerClient(Uri rpcUri, string websocketUrl, EthereumXplorerNetwork network, NBXplorerNetworkProvider networkProvider, EthereumClientTransactionRepository ethereumClientTransactionRepository)
		{
			_Network = network;
			_rpcUri = rpcUri;
			_ethereumClientTransactionRepository = ethereumClientTransactionRepository;
			_NetworkProvider = networkProvider;
			_websocketUrl = websocketUrl;
		}

		public void Init()
		{
			_web3 = new Web3(_rpcUri.AbsoluteUri);
		}

		public void CreateWebSocketClient()
		{
			_streamingWebSocketClient = new StreamingWebSocketClient(_websocketUrl);
			PendingTransactionsSubscription = new EthNewPendingTransactionObservableSubscription(_streamingWebSocketClient);
			Logs.EthereumXplorer.LogInformation($"{_Network.CryptoCode}: CreateWebSocketClient Init ...............");
		}

		public void Subscribe()
		{
			_streamingWebSocketClient.StartAsync().Wait();
			PendingTransactionsSubscription.SubscribeAsync().Wait();
			Logs.EthereumXplorer.LogInformation($"{_Network.CryptoCode}: EthereumXplorerClient Init .............");
		}

		public async Task<EthereumStatusResult> GetStatusAsync(CancellationToken cancellation = default)
		{
			HexBigInteger localEthBlockNumber = await _web3.Eth.Blocks.GetBlockNumber.SendRequestAsync().ConfigureAwait(false);
			string url = "";

			if (_NetworkProvider.NetworkType.Equals(NetworkType.Testnet))//TODO.Move them to config
			{
				url = "https://api-ropsten.etherscan.io/api?module=proxy&action=eth_blockNumber&apikey=YourApiKeyToken";
			}
			else if (_NetworkProvider.NetworkType.Equals(NetworkType.Mainnet))
			{
				url = "https://api.etherscan.io/api?module=proxy&action=eth_blockNumber&apikey=YourApiKeyToken";
			}

			long etherScanLastBlocknumber = await GetLastedBlockAsync(url);
			BigInteger diff = etherScanLastBlocknumber - localEthBlockNumber.Value;
			if (diff < 0)
			{
				diff *= -1;
			}
			return new EthereumStatusResult()
			{
				ChainHeight = etherScanLastBlocknumber == 0 /*RegTest*/? localEthBlockNumber.Value : etherScanLastBlocknumber,
				CurrentHeight = localEthBlockNumber.Value,
				CryptoCode = _Network.CryptoCode,
				IsFullySynched = etherScanLastBlocknumber == 0 /*RegTest*/ ? true : (diff > 1/*This value is based on experience*/ ? false : true)
			};
		}

		public async Task<BigInteger> GetBlockNumber()
		{
			return await _web3.Eth.Blocks.GetBlockNumber.SendRequestAsync().ConfigureAwait(false);
		}

		public async Task<BlockWithTransactions> GetBlockWithTransactionsByNumber(BigInteger blockNumber)
		{
			return await _web3.Eth.Blocks.GetBlockWithTransactionsByNumber.SendRequestAsync(new HexBigInteger(blockNumber)).ConfigureAwait(false);
		}

		public async Task<EthereumClientTransactionData> GetTransactionAsyncByTransactionId(string txid)
		{
			Nethereum.RPC.Eth.DTOs.Transaction trans = await _web3.Eth.Transactions.GetTransactionByHash.SendRequestAsync(txid).ConfigureAwait(false);
			EthereumClientTransactionData transData = trans.ToEthereumClientTransactionData();
			return transData;
		}

		public async Task<Dictionary<string, decimal>> GetBalanceByMnemonic(string mnemonic)
		{
			return await GetBalances(AddressPoolService.GenerateAddressByMnemonic(mnemonic));
		}

		public async Task<Dictionary<string, decimal>> GetBalances(IEnumerable<string> addresses)
		{
			Dictionary<string, decimal> address2Balance = new Dictionary<string, decimal>();
			foreach (string addresse in addresses)
			{
				address2Balance.Add(addresse, await GetBalance(addresse));
			}
			return address2Balance;
		}

		public async Task<decimal> GetBalance(string address)
		{
			HexBigInteger balance = await _web3.Eth.GetBalance.SendRequestAsync(address);
			return Web3.Convert.FromWei(balance.Value);
		}

		public async Task<IEnumerable<EthereumClientTransactionData>> GetTransactionsAsync(string mnemonic)
		{
			IEnumerable<string> accounts = AddressPoolService.GenerateAddressByMnemonic(mnemonic);

			return await _ethereumClientTransactionRepository.FindTransactionByAddresses(accounts);
		}

		private async Task<long> GetLastedBlockAsync(string url)
		{
			if (string.IsNullOrWhiteSpace(url))
			{
				return 0;//In RegTest, it is null or empty
			}
			HttpClient client = new HttpClient();
			HttpResponseMessage response = await client.GetAsync(url);
			response.EnsureSuccessStatusCode();
			string responseBody = await response.Content.ReadAsStringAsync();
			// Above three lines can be replaced with new helper method below
			// string responseBody = await client.GetStringAsync(uri);
			Eth_BlockNumber eth_BlockNumber = JsonConvert.DeserializeObject<Eth_BlockNumber>(responseBody);
			return eth_BlockNumber.LastBlockNumber;
		}

		public async Task<string> BroadcastAsync(EthExplorerWalletSendModel ethWalletSendModel, string mnemonic)
		{
			TransactionInput transactionInput =
			   new TransactionInput
			   {
				   Value = new HexBigInteger(Web3.Convert.ToWei(ethWalletSendModel.AmountInEther)),
				   To = ethWalletSendModel.AddressTo,
				   From = ethWalletSendModel.SelectedAccount
			   };
			if (ethWalletSendModel.Gas != null)
			{
				transactionInput.Gas = new HexBigInteger(ethWalletSendModel.Gas.Value);
			}

			if (!string.IsNullOrEmpty(ethWalletSendModel.GasPrice))
			{
				decimal parsed = decimal.Parse(ethWalletSendModel.GasPrice, CultureInfo.InvariantCulture);
				transactionInput.GasPrice = new HexBigInteger(Web3.Convert.ToWei(ethWalletSendModel.GasPrice, UnitConversion.EthUnit.Gwei));
			}

			if (ethWalletSendModel.Nonce != null)
			{
				transactionInput.Nonce = new HexBigInteger(ethWalletSendModel.Nonce.Value);
			}

			if (!string.IsNullOrEmpty(ethWalletSendModel.Data))
			{
				transactionInput.Data = ethWalletSendModel.Data;
			}

			string account = AddressPoolService.GenerateAddressInfoByMnemonic(mnemonic).SingleOrDefault(t => t.Key.Equals(ethWalletSendModel.SelectedAccount.ToLowerInvariant(), StringComparison.InvariantCulture)).Value;
			if (account != null)
			{
				string privateKey = account;
				Web3 web3 = new Web3(new Account(privateKey), _rpcUri.AbsoluteUri);

				string txnHash = await web3.Eth.TransactionManager.SendTransactionAsync(transactionInput).ConfigureAwait(false);
				return txnHash;
			}
			throw new ArgumentException($@"Account address: {transactionInput.From}, not found", nameof(transactionInput));
		}
	}

	internal class Eth_BlockNumber
	{
		public string jsonrpc { get; set; }
		public string result { get; set; }
		public long LastBlockNumber => Convert.ToInt64(result, 16);
		public int id { get; set; }
	}

}
