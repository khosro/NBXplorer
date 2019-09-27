using Nethereum.RPC.Eth.DTOs;
using Nethereum.Web3;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Numerics;
using System.Text;

namespace EthereumXplorer.Models
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
		public ulong NonceValue
		{
			get
			{
				return ulong.Parse(Nonce);
			}
		}

		[NotMapped]
		public ulong BlockNumberValue
		{
			get
			{
				return ulong.Parse(Nonce);
			}
		}

		[NotMapped]
		public ulong TransactionIndexValue
		{
			get
			{
				return ulong.Parse(Nonce);
			}
		}

		[NotMapped]
		public BigInteger GasValue
		{
			get
			{
				return BigInteger.Parse(Gas);
			}
		}

		[NotMapped]
		public BigInteger GasPriceValue
		{
			get
			{
				return BigInteger.Parse(GasPrice);
			}
		}

		public void Init(Transaction transaction)
		{
			TransactionHash = transaction.TransactionHash;

			BlockNumber = transaction.BlockNumber.Value.ToString();

			BlockHash = transaction.BlockHash;

			Nonce = transaction.Nonce.Value.ToString();

			From = transaction.From;

			To = transaction.To;

			Gas = transaction.Gas.ToString();

			GasPrice = transaction.GasPrice.ToString();

			Input = transaction.Input;

			TransactionIndex = transaction.TransactionIndex.Value.ToString();

			CreatedDateTime = DateTime.UtcNow;

			if (transaction.Value != null)
			{
				Amount = Web3.Convert.FromWei(transaction.Value.Value);
			}
		}
	}
}
