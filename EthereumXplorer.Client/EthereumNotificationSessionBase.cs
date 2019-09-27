using EthereumXplorer.Client.Models.Events;
using System.Threading;
using System.Threading.Tasks;

namespace EthereumXplorer.Client
{
	public abstract class EthereumNotificationSessionBase
	{
		public EthereumNewEventBase NextEvent(CancellationToken cancellation = default)
		{
			return NextEventAsync(cancellation).GetAwaiter().GetResult();
		}
		public abstract Task<EthereumNewEventBase> NextEventAsync(CancellationToken cancellation = default);
	}
}
