using EthereumXplorer.Client.Models.Events;
using System.Numerics;

namespace EthereumXplorer
{
	public class EthNewBlockEvent : EthereumNewEventBase
	{
		public EthNewBlockEvent(BigInteger blockNumber)
		{
			BlockNumber = blockNumber;
		}

		public BigInteger BlockNumber { get; }

		public override string EventType => "newblock";

	}
}
