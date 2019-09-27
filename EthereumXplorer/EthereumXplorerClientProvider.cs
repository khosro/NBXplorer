using EthereumXplorer.Config;
using EthereumXplorer.Data;
using EthereumXplorer.Loggging;
using Microsoft.Extensions.Logging;
using NBXplorer;
using System;
using System.Collections.Generic;
using System.Linq;

namespace EthereumXplorer
{
	public class EthereumXplorerClientProvider
	{
		private EthereumOptions _EthereumOptions;
		private Dictionary<string, EthereumXplorerClient> _Clients = new Dictionary<string, EthereumXplorerClient>();
		private readonly EthereumClientTransactionRepository _ethereumClientTransactionRepository;
		private NBXplorerNetworkProvider _NetworkProviders;
		public EthereumXplorerClientProvider(EthereumOptions ethereumOptions, EthereumClientTransactionRepository ethereumClientTransactionRepository
			, NBXplorerNetworkProvider networkProvider)
		{

			_NetworkProviders = networkProvider;
			_EthereumOptions = ethereumOptions;
			_ethereumClientTransactionRepository = ethereumClientTransactionRepository;
			foreach (EthereumConfig setting in _EthereumOptions.EthereumConfigs)
			{
				Logs.EthereumXplorer.LogInformation($"{setting.CryptoCode}:  Ethereum url is {(setting.RpcUri.AbsoluteUri ?? "not set")}");
				if (setting.RpcUri != null)
				{
					_Clients.TryAdd(setting.CryptoCode, CreateEthereumClient(_NetworkProviders.GetEth(setting.CryptoCode), setting));
				}
			}
		}

		private EthereumXplorerClient CreateEthereumClient(EthereumXplorerNetwork n, EthereumConfig setting)
		{
			EthereumXplorerClient client = new EthereumXplorerClient(setting.RpcUri, setting.WebsocketUrl, n, _NetworkProviders, _ethereumClientTransactionRepository);
			return client;
		}

		public EthereumXplorerClient GetEthereumClient(string cryptoCode)
		{
			EthereumXplorerNetwork network = _NetworkProviders.GetEth(cryptoCode);
			if (network == null)
			{
				return null;
			}

			_Clients.TryGetValue(network.CryptoCode, out EthereumXplorerClient client);
			return client;
		}

		public EthereumXplorerClient GetEthereumClient(EthereumXplorerNetwork network)
		{
			if (network == null)
			{
				throw new ArgumentNullException(nameof(network));
			}

			return GetEthereumClient(network.CryptoCode);
		}

		public EthereumXplorerNetwork GetFromCryptoCode(string cryptoCode)
		{
			EthereumXplorerNetwork network = _NetworkProviders.GetEth(cryptoCode);
			if (network == null)
			{
				return null;
			}

			if (_Clients.ContainsKey(network.CryptoCode))
			{
				return network;
			}

			return null;
		}

		public IEnumerable<(EthereumXplorerNetwork, EthereumXplorerClient)> GetAll()
		{
			foreach (EthereumXplorerNetwork net in _NetworkProviders.GetAll().OfType<EthereumXplorerNetwork>())
			{
				if (_Clients.TryGetValue(net.CryptoCode, out EthereumXplorerClient client))
				{
					yield return (net, client);
				}
			}
		}
	}
}
