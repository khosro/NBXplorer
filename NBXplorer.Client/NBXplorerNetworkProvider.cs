using NBitcoin;
using System;
using System.Collections.Generic;
using System.Linq;
namespace NBXplorer
{
	public partial class NBXplorerNetworkProvider
	{
		public NBXplorerNetworkProvider(NetworkType networkType)
		{
			InitBitcoin(networkType);
			InitBitcore(networkType);
			InitLitecoin(networkType);
			InitDogecoin(networkType);
			InitBCash(networkType);
			InitGroestlcoin(networkType);
			InitBGold(networkType);
			InitDash(networkType);
			InitPolis(networkType);
			InitMonacoin(networkType);
			InitFeathercoin(networkType);
			InitUfo(networkType);
			InitViacoin(networkType);
			InitMonoeci(networkType);
			InitGobyte(networkType);
			InitColossus(networkType);
			InitEthereum(networkType);
			InitChaincoin(networkType);
			NetworkType = networkType;
			foreach (NBXplorerNetwork chain in _Networks.Values)
			{
				chain.DerivationStrategyFactory = new DerivationStrategy.DerivationStrategyFactory(chain.NBitcoinNetwork);
			}
		}

		public NetworkType NetworkType
		{
			get;
			private set;
		}

		public NBXplorerNetwork GetFromCryptoCode(string cryptoCode)
		{
			_Networks.TryGetValue(cryptoCode.ToUpperInvariant(), out NBXplorerNetwork network);
			return network;
		}

		public IEnumerable<NBXplorerNetwork> GetAll()
		{
			return _Networks.Values;
		}

		public IEnumerable<NBXplorerNetwork> GetAllExcludeSome()
		{
			return _Networks.Values.Where(t => !ExcludedTypes.Contains(t.GetType()));
		}

		private Dictionary<string, NBXplorerNetwork> _Networks = new Dictionary<string, NBXplorerNetwork>();
		private void Add(NBXplorerNetwork network)
		{
			_Networks.Add(network.CryptoCode, network);
		}

		private List<Type> ExcludedTypes = new List<Type>() { typeof(EthereumXplorerNetwork) };
	}
}
