namespace EthereumXplorer.Client.Models
{
	public class EthExplorerWalletSendModel
	{
		private string _addressTo;

		public string AddressTo { get => _addressTo == null ? "" : _addressTo; set => _addressTo = value; }
		public decimal AmountInEther { get; set; }
		public string GasPrice { get; set; }
		public decimal CurrentBalance { get; set; }
		public string CryptoCode { get; set; }
		public string Error { get; set; }
		public ulong? Gas { get; set; }
		public ulong? Nonce { get; set; }
		public string Data { get; set; }
		public string SelectedAccount { get; set; }
	}
}
