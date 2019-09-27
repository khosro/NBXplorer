using System.Collections.Generic;
using Nethereum.HdWallet;
using Nethereum.Web3.Accounts;

namespace EthereumXplorer.Services
{
	public class AddressPoolService
	{
		public static IEnumerable<string> GenerateAddressByMnemonic(string mnemonic, string pass = null)
		{
			if (string.IsNullOrWhiteSpace(mnemonic))
			{
				return new List<string>();
			}
			mnemonic = mnemonic.Trim();
			Wallet wallet = new Wallet(mnemonic, pass);
			List<string> accounts = new List<string>();
			for (int i = 0; i < 10; i++)
			{
				Account account = wallet.GetAccount(i);
				accounts.Add(account.Address.ToLowerInvariant());
			}
			return accounts;
		}

		public static Dictionary<string, string> GenerateAddressInfoByMnemonic(string mnemonic, string pass = null)
		{
			if (string.IsNullOrWhiteSpace(mnemonic))
			{
				return new Dictionary<string, string>();
			}
			mnemonic = mnemonic.Trim();
			Wallet wallet = new Wallet(mnemonic, pass);
			Dictionary<string, string> accounts = new Dictionary<string, string>();
			for (int i = 0; i < 10; i++)
			{
				Account account = wallet.GetAccount(i);
				accounts.Add(account.Address.ToLowerInvariant(), account.PrivateKey);
			}
			return accounts;
		}
	}
}
