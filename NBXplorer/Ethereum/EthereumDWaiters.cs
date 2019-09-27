using EthereumXplorer;
using EthereumXplorer.Config;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NBitcoin.Protocol;
using NBXplorer.Configuration;
using NBXplorer.Events;
using NBXplorer.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace NBXplorer.Ethereum
{
	public class EthereumDWaiters : IHostedService
	{
		private Dictionary<string, EthereumDWaiter> _Waiters;
		private readonly RepositoryProvider repositoryProvider;

		public EthereumDWaiters(
							AddressPoolServiceAccessor addressPool,
								NBXplorerNetworkProvider networkProvider,
 							  RepositoryProvider repositoryProvider,
							  EthereumOptions config,
							  EthereumXplorerClientProvider rpcProvider,
							  EventAggregator eventAggregator)
		{
			_Waiters = new Dictionary<string, EthereumDWaiter>();
			foreach (EthereumConfig setting in config.EthereumConfigs)
			{
				EthereumXplorerNetwork network = networkProvider.GetEth(setting.CryptoCode);
				_Waiters.Add(setting.CryptoCode, new EthereumDWaiter(rpcProvider.GetEthereumClient(setting.CryptoCode),
												setting,
												network,
												repositoryProvider.GetRepository(network),
 												eventAggregator, config.SignalFilesDir));
			}
			this.repositoryProvider = repositoryProvider;
		}
		public async Task StartAsync(CancellationToken cancellationToken)
		{
			await repositoryProvider.StartAsync();
			await Task.WhenAll(_Waiters.Select(s => s.Value.StartAsync(cancellationToken)).ToArray());
		}

		public async Task StopAsync(CancellationToken cancellationToken)
		{
			await Task.WhenAll(_Waiters.Select(s => s.Value.StopAsync(cancellationToken)).ToArray());
			await repositoryProvider.DisposeAsync();
		}

		public EthereumDWaiter GetWaiter(NBXplorerNetwork network)
		{
			return GetWaiter(network.CryptoCode);
		}
		public EthereumDWaiter GetWaiter(string cryptoCode)
		{
			_Waiters.TryGetValue(cryptoCode.ToUpperInvariant(), out EthereumDWaiter waiter);
			return waiter;
		}

		public IEnumerable<EthereumDWaiter> All()
		{
			return _Waiters.Values;
		}
	}

	public class EthereumDWaiter : IHostedService
	{
		private EthereumXplorerClient _OriginalRPC;
		private EthereumXplorerNetwork _Network;
		private readonly EthereumConfig _Configuration;
		private EventAggregator _EventAggregator;
		private readonly string RPCReadyFile;
		private EthereumExplorerBehavior explorer;

		public EthereumDWaiter(
			EthereumXplorerClient rpc,
			EthereumConfig configuration,
			EthereumXplorerNetwork network,
			Repository repository,
 			EventAggregator eventAggregator, string signalFilesDir)
		{

			_OriginalRPC = rpc;
			_Configuration = configuration;
			_Network = network;
			State = BitcoinDWaiterState.NotStarted;
			_EventAggregator = eventAggregator;
			RPCReadyFile = Path.Combine(signalFilesDir, $"{network.CryptoCode.ToLowerInvariant()}_fully_synched");
		}

		public NodeState NodeState
		{
			get;
			private set;
		}

		public EthereumXplorerNetwork Network => _Network;

		public BitcoinDWaiterState State
		{
			get;
			private set;
		}

		public bool RPCAvailable => State == BitcoinDWaiterState.Ready ||
					State == BitcoinDWaiterState.CoreSynching ||
					State == BitcoinDWaiterState.NBXplorerSynching;

		private IDisposable _Subscription;
		private Task _Loop;
		private CancellationTokenSource _Cts;
		public Task StartAsync(CancellationToken cancellationToken)
		{
			if (_Disposed)
			{
				throw new ObjectDisposedException(nameof(EthereumDWaiter));
			}

			_Cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
			_Loop = StartLoop(_Cts.Token, _Tick);
			_Subscription = _EventAggregator.Subscribe<EthNewBlockEvent>(s =>
			{
				_Tick.Set();
			});
			return Task.CompletedTask;
		}

		private Signaler _Tick = new Signaler();

		private async Task StartLoop(CancellationToken token, Signaler tick)
		{
			try
			{
				int errors = 0;
				while (!token.IsCancellationRequested)
				{
					errors = Math.Min(11, errors);
					try
					{
						while (await StepAsync(token))
						{
						}
						await tick.Wait(PollingInterval, token);
						errors = 0;
					}
					catch (ConfigException) when (!token.IsCancellationRequested)
					{
						// Probably RPC errors, don't spam
						await Wait(errors, tick, token);
						errors++;
					}
					catch (Exception ex) when (!token.IsCancellationRequested)
					{
						Logs.Configuration.LogError(ex, $"{_Network.CryptoCode}: Unhandled in Waiter loop");
						await Wait(errors, tick, token);
						errors++;
					}
				}
			}
			catch when (token.IsCancellationRequested)
			{
			}
		}

		private async Task Wait(int errors, Signaler tick, CancellationToken token)
		{
			TimeSpan timeToWait = TimeSpan.FromSeconds(5.0) * (errors + 1);
			Logs.Configuration.LogInformation($"{_Network.CryptoCode}: Testing again in {(int)timeToWait.TotalSeconds} seconds");
			await tick.Wait(timeToWait, token);
		}

		public TimeSpan PollingInterval
		{
			get; set;
		} = TimeSpan.FromMinutes(1.0);

		public async Task StopAsync(CancellationToken cancellationToken)
		{
			_Disposed = true;
			_Cts.Cancel();
			_Subscription.Dispose();
			EnsureNodeDisposed();
			State = BitcoinDWaiterState.NotStarted;
			try
			{
				await _Loop;
			}
			catch { }
			EnsureRPCReadyFileDeleted();
		}

		private async Task<bool> StepAsync(CancellationToken token)
		{
			BitcoinDWaiterState oldState = State;
			switch (State)
			{
				case BitcoinDWaiterState.NotStarted:
					_OriginalRPC.Init();
					try
					{
						//TODO. Call GetBlockchainInfoAsyncEx LoadBanList
					}
					catch (Exception ex)
					{
						Logs.Configuration.LogError(ex, $"{_Network.CryptoCode}: Failed to connect to RPC");
						break;
					}
					if (!(await _OriginalRPC.GetStatusAsync(token)).IsFullySynched)
					{
						State = BitcoinDWaiterState.CoreSynching;
					}
					else
					{
						ConnectToEthereumD(token);
						State = BitcoinDWaiterState.NBXplorerSynching;
					}
					break;
				case BitcoinDWaiterState.CoreSynching:
					//TODO GetBlockchainInfoAsyncEx
					if ((await _OriginalRPC.GetStatusAsync(token)).IsFullySynched)
					{
						ConnectToEthereumD(token);
						State = BitcoinDWaiterState.NBXplorerSynching;
					}
					break;
				case BitcoinDWaiterState.NBXplorerSynching:
					if (explorer == null)
					{
						State = BitcoinDWaiterState.NotStarted;
					}
					else if (!explorer.IsSynching())
					{
						State = BitcoinDWaiterState.Ready;
					}
					break;
				case BitcoinDWaiterState.Ready:
					if (explorer == null)
					{
						State = BitcoinDWaiterState.NotStarted;
					}
					else if (explorer.IsSynching())
					{
						State = BitcoinDWaiterState.NBXplorerSynching;
					}
					break;
				default:
					break;
			}
			bool changed = oldState != State;

			if (changed)
			{
				_EventAggregator.Publish(new BitcoinDStateChangedEvent(_Network, oldState, State));
				if (State == BitcoinDWaiterState.Ready)
				{
					await File.WriteAllTextAsync(RPCReadyFile, NBitcoin.Utils.DateTimeToUnixTime(DateTimeOffset.UtcNow).ToString());
				}
			}
			if (State != BitcoinDWaiterState.Ready)
			{
				EnsureRPCReadyFileDeleted();
			}
			return changed;
		}

		private void EnsureRPCReadyFileDeleted()
		{
			if (File.Exists(RPCReadyFile))
			{
				File.Delete(RPCReadyFile);
			}
		}

		private void ConnectToEthereumD(CancellationToken cancellation)
		{
			explorer = new EthereumExplorerBehavior();
			_OriginalRPC.PendingTransactionsSubscription.GetSubscribeResponseAsObservable().Subscribe(subscriptionId =>
					Console.WriteLine("subscriptionId : " + subscriptionId)
			);

			_OriginalRPC.PendingTransactionsSubscription.GetSubscriptionDataResponsesAsObservable().Subscribe(async transactionHash =>
				_EventAggregator.Publish(new EthNewTransactionEvent(await _OriginalRPC.GetTransactionAsyncByTransactionId(transactionHash).ConfigureAwait(false), _Network.CryptoCode))
			  );
			Logs.Explorer.LogInformation($"{_Network.CryptoCode}: Connect to ConnectToEthereumD");
		}

		private void Node_StateChanged(Node node, NodeState oldState)
		{
			_Tick.Set();
		}

		private void EnsureNodeDisposed()
		{

		}

		private bool _Disposed = false;


		//private void SaveChainInCache()
		//{
		//	string suffix = _Network.CryptoCode == "BTC" ? "" : _Network.CryptoCode;
		//	string cachePath = Path.Combine(_Configuration.DataDir, $"{suffix}chain-slim.dat");
		//	string cachePathTemp = Path.Combine(_Configuration.DataDir, $"{suffix}chain-slim.dat.temp");

		//	Logs.Configuration.LogInformation($"{_Network.CryptoCode}: Saving chain to cache...");
		//	using (FileStream fs = new FileStream(cachePathTemp, FileMode.Create, FileAccess.Write, FileShare.None, 1024 * 1024))
		//	{
		//		_Chain.Save(fs);
		//		fs.Flush();
		//	}

		//	if (File.Exists(cachePath))
		//	{
		//		File.Delete(cachePath);
		//	}

		//	File.Move(cachePathTemp, cachePath);
		//	Logs.Configuration.LogInformation($"{_Network.CryptoCode}: Chain cached");
		//}

		//private void LoadChainFromCache()
		//{
		//	string suffix = _Network.CryptoCode == "BTC" ? "" : _Network.CryptoCode;
		//	{
		//		string legacyCachePath = Path.Combine(_Configuration.DataDir, $"{suffix}chain.dat");
		//		if (_Configuration.CacheChain && File.Exists(legacyCachePath))
		//		{
		//			Logs.Configuration.LogInformation($"{_Network.CryptoCode}: Loading chain from cache...");
		//			ConcurrentChain chain = new ConcurrentChain(_Network.NBitcoinNetwork);
		//			chain.Load(File.ReadAllBytes(legacyCachePath), _Network.NBitcoinNetwork);
		//			LoadSlimAndSaveToSlimFormat(chain);
		//			File.Delete(legacyCachePath);
		//			Logs.Configuration.LogInformation($"{_Network.CryptoCode}: Height: " + _Chain.Height);
		//			return;
		//		}
		//	}

		//	{
		//		string cachePath = Path.Combine(_Configuration.DataDir, $"{suffix}chain-stripped.dat");
		//		if (_Configuration.CacheChain && File.Exists(cachePath))
		//		{
		//			Logs.Configuration.LogInformation($"{_Network.CryptoCode}: Loading chain from cache...");
		//			ConcurrentChain chain = new ConcurrentChain(_Network.NBitcoinNetwork);
		//			chain.Load(File.ReadAllBytes(cachePath), _Network.NBitcoinNetwork, new ConcurrentChain.ChainSerializationFormat()
		//			{
		//				SerializeBlockHeader = false,
		//				SerializePrecomputedBlockHash = true,
		//			});
		//			LoadSlimAndSaveToSlimFormat(chain);
		//			File.Delete(cachePath);
		//			Logs.Configuration.LogInformation($"{_Network.CryptoCode}: Height: " + _Chain.Height);
		//			return;
		//		}
		//	}

		//	{
		//		string slimCachePath = Path.Combine(_Configuration.DataDir, $"{suffix}chain-slim.dat");
		//		if (_Configuration.CacheChain && File.Exists(slimCachePath))
		//		{
		//			Logs.Configuration.LogInformation($"{_Network.CryptoCode}: Loading chain from cache...");
		//			using (FileStream file = new FileStream(slimCachePath, FileMode.Open, FileAccess.Read, FileShare.None, 1024 * 1024))
		//			{
		//				_Chain.Load(file);
		//			}
		//			Logs.Configuration.LogInformation($"{_Network.CryptoCode}: Height: " + _Chain.Height);
		//			return;
		//		}
		//	}
		//}

		//private void LoadSlimAndSaveToSlimFormat(ConcurrentChain chain)
		//{
		//	foreach (ChainedBlock block in chain.ToEnumerable(false))
		//	{
		//		_Chain.TrySetTip(block.HashBlock, block.Previous?.HashBlock);
		//	}
		//	SaveChainInCache();
		//}

		//private Node GetHandshakedNode()
		//{
		//	return _Node?.State == NodeState.HandShaked ? _Node : null;
		//}

		//private ExplorerBehavior GetExplorerBehavior()
		//{
		//	return GetHandshakedNode()?.Behaviors?.Find<ExplorerBehavior>();
		//}

		//public bool Connected => GetHandshakedNode() != null;
		//public GetNetworkInfoResponse NetworkInfo { get; internal set; }

	}
}
