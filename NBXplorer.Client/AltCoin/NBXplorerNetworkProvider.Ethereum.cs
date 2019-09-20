using NBitcoin;
using System;
using System.Collections.Generic;
using System.Text;


namespace NBXplorer
{
	public partial class NBXplorerNetworkProvider
	{
		private void InitEthereum(NetworkType networkType)
		{
			Add(new NBXplorerNetwork(NBXplorer.Client.AltCoin.Ethereum.Instance, networkType)
			{
				MinRPCVersion = 160000
			});
		}

		public NBXplorerNetwork GetEth()
		{
			return GetFromCryptoCode(NBXplorer.Client.AltCoin.Ethereum.Instance.CryptoCode);
		}
	}
}
