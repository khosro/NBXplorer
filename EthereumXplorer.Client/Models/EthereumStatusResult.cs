using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace EthereumXplorer.Client.Models
{
	public class EthereumStatusResult
	{
		public bool IsFullySynched { get; set; }
		public BigInteger CurrentHeight { get; set; }
		public BigInteger ChainHeight { get; set; }
		public string CryptoCode { get; set; }
		public string Version { get; set; }
	}
}
