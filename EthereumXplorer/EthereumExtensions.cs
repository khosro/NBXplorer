using EthereumXplorer.Client.Models;
using EthereumXplorer.Config;
using EthereumXplorer.Data;
using EthereumXplorer.Loggging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Nethereum.Web3;
using System;
using System.IO;
using XplorerUtil;

namespace EthereumXplorer
{
	public static class EthereumExtensions
	{
		private const string EthereumClientDbInfo = "for EthereumClient";

		public static IServiceCollection AddEthereumLike(this IServiceCollection services)
		{
			Database(services);
			services.TryAddSingleton<EthereumXplorerClientProvider>();

			services.AddStartupTask<EthereumDataMigrationStartupTask>();

			return services;
		}

		private static void Database(IServiceCollection services)
		{
			services.TryAddSingleton<EthereumClientApplicationDbContextFactory>(o =>
			{
				EthereumOptions opts = o.GetRequiredService<EthereumOptions>();
				EthereumClientApplicationDbContextFactory dbContext = null;
				if (!string.IsNullOrEmpty(opts.PostgresConnectionString))
				{
					Logs.EthereumXplorer.LogInformation($"Postgres DB used ({opts.PostgresConnectionString}) {EthereumClientDbInfo}");
					dbContext = new EthereumClientApplicationDbContextFactory(DatabaseType.Postgres, opts.PostgresConnectionString);
				}
				else if (!string.IsNullOrEmpty(opts.MySQLConnectionString))
				{
					Logs.EthereumXplorer.LogInformation($"MySQL DB used ({opts.MySQLConnectionString}) {EthereumClientDbInfo}");
					Logs.EthereumXplorer.LogWarning($"MySQL is not widely tested and should be considered experimental, we advise you to use postgres instead. {EthereumClientDbInfo}");
					dbContext = new EthereumClientApplicationDbContextFactory(DatabaseType.MySQL, opts.MySQLConnectionString);
				}
				else
				{
					string connStr = "Data Source=" + Path.Combine(opts.DataDir, "sqllite.db");
					Logs.EthereumXplorer.LogInformation($"SQLite DB used ({connStr}) {EthereumClientDbInfo}");
					Logs.EthereumXplorer.LogWarning($"SQLite not widely tested and should be considered experimental, we advise you to use postgres instead. {EthereumClientDbInfo}");
					dbContext = new EthereumClientApplicationDbContextFactory(DatabaseType.Sqlite, connStr);
				}

				return dbContext;
			});

			services.AddDbContext<EthereumClientApplicationDbContext>((provider, o) =>
			{
				EthereumClientApplicationDbContextFactory factory = provider.GetRequiredService<EthereumClientApplicationDbContextFactory>();
				factory.ConfigureBuilder(o);
			});

			services.TryAddSingleton<EthereumClientTransactionRepository>();
		}

		public static EthereumClientTransactionData ToEthereumClientTransactionData(this Nethereum.RPC.Eth.DTOs.Transaction transaction)
		{
			return new EthereumClientTransactionData()
			{
				TransactionHash = transaction.TransactionHash,

				BlockNumber = transaction.BlockNumber.Value.ToString(),

				BlockHash = transaction.BlockHash,

				Nonce = transaction.Nonce.Value.ToString(),

				From = transaction.From,

				To = transaction.To,

				Gas = transaction.Gas.ToString(),

				GasPrice = transaction.GasPrice.ToString(),

				Input = transaction.Input,

				TransactionIndex = transaction.TransactionIndex.Value.ToString(),

				CreatedDateTime = DateTime.UtcNow,

				Amount = transaction.Value != null ? Web3.Convert.FromWei(transaction.Value.Value) : 0
			};
		}
	}
}
