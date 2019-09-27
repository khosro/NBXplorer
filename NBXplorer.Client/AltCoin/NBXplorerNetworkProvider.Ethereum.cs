using NBitcoin;
using System;
using System.Collections.Generic;
using System.Linq;

namespace NBXplorer
{
	public partial class NBXplorerNetworkProvider
	{
		private void InitEthereum(NetworkType networkType)
		{
			Add(new EthereumXplorerNetwork(NBXplorer.Client.AltCoin.Ethereum.Instance, networkType));
		}

		public EthereumXplorerNetwork GetEth()
		{
			return GetEth(NBXplorer.Client.AltCoin.Ethereum.Instance.CryptoCode);
		}

		public EthereumXplorerNetwork GetEth(string cryptoCode)
		{
			return GetEths().SingleOrDefault(t => t.CryptoCode.Equals(cryptoCode, StringComparison.InvariantCulture));
		}

		public IEnumerable<EthereumXplorerNetwork> GetEths()
		{
			return GetAll().OfType<EthereumXplorerNetwork>();
		}
	}
}
