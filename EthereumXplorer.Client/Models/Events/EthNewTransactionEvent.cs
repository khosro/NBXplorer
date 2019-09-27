
using EthereumXplorer.Client.Models.Events;
using EthereumXplorer.Models;

namespace EthereumXplorer
{
	public class EthNewTransactionEvent : EthereumNewEventBase
	{
		public EthNewTransactionEvent(EthereumClientTransactionData transaction)
		{
			Transaction = transaction;
		}

		public EthereumClientTransactionData Transaction { get; set; }

		public override string EventType => "newtransaction";
	}
}
