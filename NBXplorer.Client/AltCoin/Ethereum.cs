using NBitcoin;
using System;

namespace NBXplorer.Client.AltCoin
{

	public class Ethereum : INetworkSet
	{
		private Ethereum()
		{

		}
		public static Ethereum Instance { get; } = new Ethereum();

		public Network Mainnet => Network.Main;

		public Network Testnet => Network.TestNet;

		public Network Regtest => Network.RegTest;

		public string CryptoCode => "ETH";

		public Network GetNetwork(NetworkType networkType)
		{
			switch (networkType)
			{
				case NetworkType.Mainnet:
					return Mainnet;
				case NetworkType.Testnet:
					return Testnet;
				case NetworkType.Regtest:
					return Regtest;
			}
			throw new NotSupportedException(networkType.ToString());
		}
	}
}
