using EthereumXplorer;
using EthereumXplorer.Data;
using EthereumXplorer.Loggging;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Threading;
using System.Threading.Tasks;

namespace NBXplorer.Ethereum
{
	public class EthereumServiceListener : IHostedService
	{
		private EventAggregator _Aggregator;
		private TaskCompletionSource<bool> _RunningTask;
		private CancellationTokenSource _Cts;
		private EthereumClientTransactionRepository _ethereumClientTransactionRepository;
		public EthereumServiceListener(EventAggregator aggregator,
								 EthereumClientTransactionRepository ethereumClientTransactionRepository)
		{
			_Aggregator = aggregator;
			_ethereumClientTransactionRepository = ethereumClientTransactionRepository;
		}

		private CompositeDisposable leases = new CompositeDisposable();

		public Task StartAsync(CancellationToken cancellationToken)
		{
			_RunningTask = new TaskCompletionSource<bool>();
			_Cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

			leases.Add(_Aggregator.Subscribe<EthNewTransactionEvent>(async evt =>
			{
				Logs.EthereumXplorer.LogInformation($"Publish subscribe EthNewTransactionEvent ,TransactionHash : {evt.Transaction.TransactionHash}");
				await _ethereumClientTransactionRepository.SaveOrUpdateTransaction(evt.Transaction);
			}));

			leases.Add(_Aggregator.Subscribe<EthNewBlockEvent>(async evt =>
			{
				await Task.FromResult(0);
			}));


			return Task.CompletedTask;
		}

		public Task StopAsync(CancellationToken cancellationToken)
		{
			leases.Dispose();
			_Cts?.Cancel();
			return Task.WhenAny(_RunningTask?.Task, Task.Delay(-1, cancellationToken));
		}
	}
}
