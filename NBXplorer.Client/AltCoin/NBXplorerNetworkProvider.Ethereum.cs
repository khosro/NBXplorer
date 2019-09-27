using NBitcoin;

namespace NBXplorer
{
	public partial class NBXplorerNetworkProvider
	{
		private void InitEthereum(NetworkType networkType)
		{
			Add(new EthereumXplorerNetwork(NBXplorer.Client.AltCoin.Ethereum.Instance, networkType)
			{
				MinRPCVersion = 160000
			});
		}

		public EthereumXplorerNetwork GetEth()
		{
			return GetEth(NBXplorer.Client.AltCoin.Ethereum.Instance.CryptoCode);
		}
	}
}
