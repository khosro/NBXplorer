using NBitcoin;

namespace NBXplorer
{
	public class EthereumXplorerNetwork : NBXplorerNetwork
	{
		public EthereumXplorerNetwork(INetworkSet networkSet, NBitcoin.NetworkType networkType) :
			base(networkSet, networkType)
		{ }
	}
}
