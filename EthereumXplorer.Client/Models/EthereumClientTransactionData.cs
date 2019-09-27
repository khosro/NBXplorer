using System;
using System.ComponentModel.DataAnnotations.Schema;
using System.Numerics;

namespace EthereumXplorer.Client.Models
{
	public class EthereumClientTransactionData
	{
		public string Id { get; set; }

		public string TransactionHash { get; set; }
		public string BlockHash { get; set; }
		public string From { get; set; }
		public string To { get; set; }
		public decimal Amount { get; set; }
		public string Input { get; set; }

		public string Nonce { get; set; }//ulong
		public string BlockNumber { get; set; }//ulong
		public string TransactionIndex { get; set; }//ulong
		public string Gas { get; set; }//BigInteger
		public string GasPrice { get; set; }//BigInteger

		public DateTime CreatedDateTime { get; set; }

		[NotMapped]
		public ulong NonceValue => ulong.Parse(Nonce);

		[NotMapped]
		public ulong BlockNumberValue => ulong.Parse(Nonce);

		[NotMapped]
		public ulong TransactionIndexValue => ulong.Parse(Nonce);

		[NotMapped]
		public BigInteger GasValue => BigInteger.Parse(Gas);

		[NotMapped]
		public BigInteger GasPriceValue => BigInteger.Parse(GasPrice);
	}
}
