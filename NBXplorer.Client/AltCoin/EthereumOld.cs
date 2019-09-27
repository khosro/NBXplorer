//using NBitcoin;
//using NBitcoin.Crypto;
//using System.Linq;

//namespace NBXplorer.Client.AltCoin
//{
//	public class Ethereum : NetworkSetBase
//	{
//		public static Ethereum Instance { get; } = new Ethereum();

//		public override string CryptoCode => "ETH";

//		private Ethereum()
//		{

//		}
//		public class EthreumConsensusFactory : ConsensusFactory
//		{
//			private EthreumConsensusFactory()
//			{
//			}

//			public static EthreumConsensusFactory Instance { get; } = new EthreumConsensusFactory();

//			public override BlockHeader CreateBlockHeader()
//			{
//				return new EthreumBlockHeader();
//			}
//			public override Block CreateBlock()
//			{
//				return new EthreumBlock(new EthreumBlockHeader());
//			}
//		}

//#pragma warning disable CS0618 // Type or member is obsolete
//		public class EthreumBlockHeader : BlockHeader
//		{
//			private static byte[] CalculateHash(byte[] data, int offset, int count)
//			{
//				return new NBitcoin.Altcoins.HashX11.X11().ComputeBytes(data.Skip(offset).Take(count).ToArray());
//			}

//			protected override HashStreamBase CreateHashStream()
//			{
//				return BufferedHashStream.CreateFrom(CalculateHash, 80);
//			}
//		}

//		public class EthreumBlock : Block
//		{
//#pragma warning disable CS0612 // Type or member is obsolete
//			public EthreumBlock(EthreumBlockHeader h) : base(h)
//#pragma warning restore CS0612 // Type or member is obsolete
//			{

//			}
//			public override ConsensusFactory GetConsensusFactory()
//			{
//				return EthreumConsensusFactory.Instance;
//			}
//		}
//#pragma warning restore CS0618 // Type or member is obsolete


//		protected override void PostInit()
//		{
//			RegisterDefaultCookiePath("EthreumCore");
//		}

//		protected override NetworkBuilder CreateMainnet()
//		{
//			NetworkBuilder builder = new NetworkBuilder();
//			builder.SetConsensus(new Consensus()
//			{
//				ConsensusFactory = EthreumConsensusFactory.Instance,
//				SupportSegwit = false
//			})
//			.SetName("Ethreum-main");
//			return builder;
//		}

//		protected override NetworkBuilder CreateTestnet()
//		{
//			NetworkBuilder builder = new NetworkBuilder();
//			builder.SetConsensus(new Consensus()
//			{
//				ConsensusFactory = EthreumConsensusFactory.Instance,
//				SupportSegwit = false
//			})
//			.SetName("Ethreum-Testnet");
//			return builder;
//		}

//		protected override NetworkBuilder CreateRegtest()
//		{
//			NetworkBuilder builder = new NetworkBuilder();
//			builder.SetConsensus(new Consensus()
//			{
//				ConsensusFactory = EthreumConsensusFactory.Instance,
//				SupportSegwit = false
//			})
//			.SetName("Ethreum-Regtest");
//			return builder;
//		}


//	}
//}

