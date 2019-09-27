using EthereumXplorer.Config;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using NBXplorer.Ethereum;
using System;
using System.IO;
using System.Linq;

namespace NBXplorer
{
	public static class EthereumExtensions
	{
		private const string EthereumClientDbInfo = "for EthereumClient";

		public static IServiceCollection AddEthereumLike1(this IServiceCollection services)
		{
			services.AddSingleton(s => s.ConfigureEthereumConfiguration());
			services.TryAddSingleton<EthereumDWaiters>();
			services.TryAddSingleton<EthereumServiceListener>();
			return services;
		}

		private static EthereumOptions ConfigureEthereumConfiguration(this IServiceProvider serviceProvider)
		{
			IConfiguration configuration = serviceProvider.GetService<IConfiguration>();
			NBXplorerNetworkProvider nbXplorerNetworkProvider = serviceProvider.GetService<NBXplorerNetworkProvider>();
			EthereumOptions result = new EthereumOptions();

			System.Collections.Generic.IEnumerable<string> supportedChains = configuration.GetOrDefault<string>("chains", string.Empty)
				.Split(',', StringSplitOptions.RemoveEmptyEntries)
				.Select(t => t.ToUpperInvariant());

			System.Collections.Generic.IEnumerable<EthereumXplorerNetwork> supportedNetworks = nbXplorerNetworkProvider.GetAll()
				.OfType<EthereumXplorerNetwork>();

			foreach (EthereumXplorerNetwork net in supportedNetworks)
			{
				if (supportedChains.Contains(net.CryptoCode))
				{
					Uri rpcUri = configuration.GetOrDefault<Uri>($"{net.CryptoCode}.eth.rcpurl", null);
					string wsurl = configuration.GetOrDefault<string>($"{net.CryptoCode}.eth.wsurl", null);

					if (rpcUri == null && string.IsNullOrWhiteSpace(wsurl))
					{
						throw new Exception($"{net.CryptoCode} is misconfigured for rpc url({net.CryptoCode}.eth.rcpurl) and websocket url({net.CryptoCode}.eth.wsurl)");
					}

					result.EthereumConfigs.Add(new EthereumConfig { RpcUri = rpcUri, CryptoCode = net.CryptoCode, WebsocketUrl = wsurl });
				}
			}


			Configuration.ExplorerConfiguration config = serviceProvider.GetService<NBXplorer.Configuration.ExplorerConfiguration>();
			string dataDir = Path.Combine(config.DataDir, "EthClient");
			if (!Directory.Exists(dataDir))
			{
				Directory.CreateDirectory(dataDir);
			}

			result.DataDir = dataDir;

			result.PostgresConnectionString = configuration.GetOrDefault<string>("eth.postgres", null);
			result.MySQLConnectionString = configuration.GetOrDefault<string>("eth.mysql", null);

			if (string.IsNullOrWhiteSpace(result.MySQLConnectionString) && string.IsNullOrWhiteSpace(result.PostgresConnectionString))
			{
				throw new Exception($"Please provide MySQLConnectionString or PostgresConnectionString {EthereumClientDbInfo} ");
			}
			result.SignalFilesDir = result.DataDir;

			return result;
		}
	}
}
