using System;
using System.Collections.Generic;
using System.Text;

namespace EthereumXplorer.Config
{
	public class EthereumOptions
	{
		public EthereumOptions()
		{
			EthereumConfigs = new List<EthereumConfig>();
		}

		public string PostgresConnectionString { get; set; }
		public string MySQLConnectionString { get; set; }
		public string DataDir { get; set; }
		public string SignalFilesDir { get; set; }
		public List<EthereumConfig> EthereumConfigs { get; set; }
	}

	public class EthereumConfig
	{
		public Uri RpcUri { get; set; }
		public string WebsocketUrl { get; set; }
		public string CryptoCode { get; set; }
	}
}
