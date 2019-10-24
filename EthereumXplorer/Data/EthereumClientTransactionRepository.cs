﻿using EthereumXplorer.Client.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace EthereumXplorer.Data
{
	public class EthereumClientTransactionRepository
	{
		private EthereumClientApplicationDbContextFactory _ContextFactory;

		public EthereumClientTransactionRepository(EthereumClientApplicationDbContextFactory contextFactory)
		{
			_ContextFactory = contextFactory ?? throw new ArgumentNullException(nameof(contextFactory));
		}

		public async Task<IEnumerable<EthereumClientTransactionData>> FindTransactionByAddresses(IEnumerable<string> addresses)
		{
			if (addresses == null || !addresses.Any())
			{
				return new List<EthereumClientTransactionData>();
			}

			addresses = addresses.ToList().ConvertAll(d => d.ToLowerInvariant());
			IEnumerable<EthereumClientTransactionData> transactions;
			using (EthereumClientApplicationDbContext ctx = _ContextFactory.CreateContext())
			{
				IQueryable<EthereumClientTransactionData> result = ctx.EthereumClientTransactions.Where(t => addresses.Contains(t.From) || addresses.Contains(t.To));
				transactions = await result.ToArrayAsync();
				foreach (EthereumClientTransactionData trans in transactions)
				{
					if (addresses.Any(t => t.Equals(trans.From.ToLowerInvariant())))
					{
						trans.Amount *= -1;
					}
				}
			}
			return transactions;
		}

		public async Task SaveOrUpdateTransaction(EthereumClientTransactionData entity)
		{
			using (EthereumClientApplicationDbContext ctx = _ContextFactory.CreateContext())
			{
				var existing = await ctx.EthereumClientTransactions.SingleOrDefaultAsync(t => t.TransactionHash == entity.TransactionHash).ConfigureAwait(false);

				if (existing == null)
				{
					ctx.Add(entity);
				}
				else
				{
					ctx.Entry(existing).CurrentValues.SetValues(entity);
				}
				await ctx.SaveChangesAsync().ConfigureAwait(false);
			}
		}
	}
}