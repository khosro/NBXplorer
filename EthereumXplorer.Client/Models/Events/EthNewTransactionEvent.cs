using EthereumXplorer.Client.Models;
using EthereumXplorer.Client.Models.Events;

namespace EthereumXplorer
{
	public class EthNewTransactionEvent : EthereumNewEventBase
	{
		public EthNewTransactionEvent(EthereumClientTransactionData transaction, string crytoCode)
		{
			Transaction = transaction;
			CryptoCode = crytoCode;
		}

		public EthereumClientTransactionData Transaction { get; set; }

		public override string EventType => "newtransaction";
	}
}
