using EthereumXplorer.Client.Models;
using NBXplorer;
using NBXplorer.Models;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using static NBXplorer.ExplorerClient;

namespace EthereumXplorer.Client
{
	public class EthereumExplorerClient
	{
		private readonly Uri _Address;
		public Uri Address => _Address;
		private readonly NBXplorerNetwork _Network;
		public NBXplorerNetwork Network => _Network;

		internal IAuth _Auth = new NullAuthentication();
		public Serializer Serializer { get; private set; }

		#region  TODO.Check usage
		public bool IncludeTransaction
		{
			get; set;
		} = true;
		private readonly string _CryptoCode = "BTC";
		public string CryptoCode => _CryptoCode;
		#endregion
		public EthereumExplorerClient(NBXplorerNetwork network, Uri serverAddress)
		{
			serverAddress = serverAddress ?? network.DefaultSettings.DefaultUrl;
			_Address = serverAddress;
			_Network = network ?? throw new ArgumentNullException(nameof(network));
			Serializer = new Serializer(Network.NBitcoinNetwork);
			_CryptoCode = _Network.CryptoCode;
			SetCookieAuth(network.DefaultSettings.DefaultCookieFile);
		}

		private static readonly HttpClient SharedClient = new HttpClient();
		internal HttpClient Client = SharedClient;

		public void SetClient(HttpClient client)
		{
			Client = client;
		}

		public bool SetCookieAuth(string path)
		{
			if (path == null)
			{
				throw new ArgumentNullException(nameof(path));
			}

			CookieAuthentication auth = new CookieAuthentication(path);
			_Auth = auth;
			return auth.RefreshCache();
		}

		public void SetNoAuth()
		{
			_Auth = new NullAuthentication();
		}
		internal string GetFullUri(string relativePath, params object[] parameters)
		{
			relativePath = string.Format(relativePath, parameters ?? new object[0]);
			string uri = Address.AbsoluteUri;
			if (!uri.EndsWith("/", StringComparison.Ordinal))
			{
				uri += "/";
			}

			uri += relativePath;
			if (!IncludeTransaction)
			{
				if (uri.IndexOf('?') == -1)
				{
					uri += $"?includeTransaction=false";
				}
				else
				{
					uri += $"&includeTransaction=false";
				}
			}
			return uri;
		}

		public async Task<EthereumClientTransactionData> GetTransactionAsyncByTransactionId(string txId, CancellationToken cancellation = default)
		{
			if (string.IsNullOrWhiteSpace(txId))
			{
				throw new ArgumentNullException(nameof(txId));
			}
			return await SendAsync<EthereumClientTransactionData>(HttpMethod.Get, null, $"v1/eth/cryptos/{CryptoCode}/transid/{txId}", null, cancellation);
		}

		public async Task<EthereumStatusResult> GetStatusAsync(CancellationToken cancellation = default)
		{
			return await SendAsync<EthereumStatusResult>(HttpMethod.Get, null, $"v1/eth/cryptos/{CryptoCode}/status", null, cancellation);
		}

		public async Task<IEnumerable<EthereumClientTransactionData>> GetTransactionsAsync(string mnemonic, CancellationToken cancellation = default)
		{
			//TODO.It is insecure.
			return await SendAsync<IEnumerable<EthereumClientTransactionData>>(HttpMethod.Get, null, $"v1/eth/txs/cryptos/{CryptoCode}/mnemonic/{mnemonic}", null, cancellation);
		}

		public async Task<decimal> GetBalance(string address, CancellationToken cancellation = default)
		{
			return await SendAsync<decimal>(HttpMethod.Get, null, $"v1/eth/cryptos/{CryptoCode}/address/{address}", null, cancellation);
		}

		public async Task<Dictionary<string, decimal>> GetBalanceByMnemonic(string mnemonic, CancellationToken cancellation = default)
		{
			return await SendAsync<Dictionary<string, decimal>>(HttpMethod.Get, null, $"v1/eth/cryptos/{CryptoCode}/mnemonic/{mnemonic}", null, cancellation);
		}

		public async Task<Dictionary<string, decimal>> GetBalances(IEnumerable<string> addresses, CancellationToken cancellation = default)
		{
			return await SendAsync<Dictionary<string, decimal>>(HttpMethod.Get, null, $"v1/eth/cryptos/{CryptoCode}/addresses/{addresses}", null, cancellation);
		}

		public async Task<string> BroadcastAsync(EthExplorerWalletSendModel ethWalletSendModel, string mnemonic, CancellationToken cancellation = default)
		{
			return await SendAsync<string>(HttpMethod.Post, null, $"v1/eth/cryptos/{CryptoCode}/ethWalletSendModel/{ethWalletSendModel}/mnemonic/{mnemonic}", null, cancellation);
		}

		public async Task<EthereumWebsocketNotificationSession> CreateWebsocketNotificationSessionAsync(CancellationToken cancellation = default)
		{
			EthereumWebsocketNotificationSession session = new EthereumWebsocketNotificationSession(this);
			await session.ConnectAsync(cancellation).ConfigureAwait(false);
			return session;
		}

		private Task<T> GetAsync<T>(string relativePath, object[] parameters, CancellationToken cancellation)
		{
			return SendAsync<T>(HttpMethod.Get, null, relativePath, parameters, cancellation);
		}
		internal async Task<T> SendAsync<T>(HttpMethod method, object body, string relativePath, object[] parameters, CancellationToken cancellation)
		{
			HttpRequestMessage message = CreateMessage(method, body, relativePath, parameters);
			HttpResponseMessage result = await Client.SendAsync(message, cancellation).ConfigureAwait(false);
			if ((int)result.StatusCode == 404)
			{
				return default(T);
			}
			if (result.StatusCode == HttpStatusCode.GatewayTimeout || result.StatusCode == HttpStatusCode.RequestTimeout)
			{
				throw new HttpRequestException($"HTTP error {(int)result.StatusCode}", new TimeoutException());
			}
			if ((int)result.StatusCode == 401)
			{
				if (_Auth.RefreshCache())
				{
					message = CreateMessage(method, body, relativePath, parameters);
					result = await Client.SendAsync(message).ConfigureAwait(false);
				}
			}
			return await ParseResponse<T>(result).ConfigureAwait(false);
		}

		internal HttpRequestMessage CreateMessage(HttpMethod method, object body, string relativePath, object[] parameters)
		{
			string uri = GetFullUri(relativePath, parameters);
			HttpRequestMessage message = new HttpRequestMessage(method, uri);
			_Auth.SetAuthorization(message);
			if (body != null)
			{
				if (body is byte[])
				{
					message.Content = new ByteArrayContent((byte[])body);
				}
				else
				{
					message.Content = new StringContent(Serializer.ToString(body), Encoding.UTF8, "application/json");
				}
			}
			return message;
		}

		private async Task<T> ParseResponse<T>(HttpResponseMessage response)
		{
			using (response)
			{
				if (response.IsSuccessStatusCode)
				{
					if (response.Content.Headers.ContentLength == 0)
					{
						return default(T);
					}
					else if (response.Content.Headers.ContentType.MediaType.Equals("application/json", StringComparison.Ordinal))
					{
						return Serializer.ToObject<T>(await response.Content.ReadAsStringAsync().ConfigureAwait(false));
					}
					else if (response.Content.Headers.ContentType.MediaType.Equals("application/octet-stream", StringComparison.Ordinal))
					{
						return (T)(object)await response.Content.ReadAsByteArrayAsync().ConfigureAwait(false);
					}
				}

				if (response.StatusCode == System.Net.HttpStatusCode.InternalServerError)
				{
					response.EnsureSuccessStatusCode();
				}

				NBXplorerError error = Serializer.ToObject<NBXplorerError>(await response.Content.ReadAsStringAsync().ConfigureAwait(false));
				if (error == null)
				{
					response.EnsureSuccessStatusCode();
				}

				throw error.AsException();
			}
		}

		private async Task ParseResponse(HttpResponseMessage response)
		{
			using (response)
			{
				if (response.IsSuccessStatusCode)
				{
					return;
				}

				if (response.StatusCode == System.Net.HttpStatusCode.InternalServerError)
				{
					response.EnsureSuccessStatusCode();
				}

				NBXplorerError error = Serializer.ToObject<NBXplorerError>(await response.Content.ReadAsStringAsync().ConfigureAwait(false));
				if (error == null)
				{
					response.EnsureSuccessStatusCode();
				}

				throw error.AsException();
			}
		}


	}
}
