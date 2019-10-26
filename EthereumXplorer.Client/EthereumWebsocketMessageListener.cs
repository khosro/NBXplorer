using EthereumXplorer.Client.Models.Events;
using NBXplorer;
using NBXplorer.Models;
using Newtonsoft.Json;
using System;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
namespace EthereumXplorer.Client
{
	public class EthereumWebsocketMessageListener
	{
		private readonly WebSocket _Socket;
		public WebSocket Socket => _Socket;

		private readonly JsonSerializerSettings _SerializerSettings;
		public EthereumWebsocketMessageListener(WebSocket socket, JsonSerializerSettings serializerSettings)
		{
			_Socket = socket;
			_SerializerSettings = serializerSettings;
			byte[] buffer = new byte[ORIGINAL_BUFFER_SIZE];
			_Buffer = new ArraySegment<byte>(buffer, 0, buffer.Length);
		}

		private const int ORIGINAL_BUFFER_SIZE = 1024 * 5;
		private const int MAX_BUFFER_SIZE = 1024 * 1024 * 5;
		private ArraySegment<byte> _Buffer;
		private UTF8Encoding UTF8 = new UTF8Encoding(false, true);
		public async Task<EthereumNewEventBase> NextMessageAsync(CancellationToken cancellation)
		{
			ArraySegment<byte> buffer = _Buffer;
			byte[] array = _Buffer.Array;
			int originalSize = _Buffer.Array.Length;
			int newSize = _Buffer.Array.Length;
			while (true)
			{
				WebSocketReceiveResult message = await Socket.ReceiveAsync(buffer, cancellation).ConfigureAwait(false);
				if (message.MessageType == WebSocketMessageType.Close)
				{
					await CloseSocketAndThrow(WebSocketCloseStatus.NormalClosure, "Close message received from the peer", cancellation).ConfigureAwait(false);
					break;
				}
				if (message.MessageType != WebSocketMessageType.Text)
				{
					await CloseSocketAndThrow(WebSocketCloseStatus.InvalidMessageType, "Only Text is supported", cancellation).ConfigureAwait(false);
					break;
				}
				if (message.EndOfMessage)
				{
					buffer = new ArraySegment<byte>(array, 0, buffer.Offset + message.Count);
					try
					{
						EthereumNewEventBase o = ParseMessage(buffer);
						if (newSize != originalSize)
						{
							Array.Resize(ref array, originalSize);
						}
						return o;
					}
					catch (Exception ex)
					{
						await CloseSocketAndThrow(WebSocketCloseStatus.InvalidPayloadData, $"Invalid payload: {ex.Message}", cancellation).ConfigureAwait(false);
					}
				}
				else
				{
					if (buffer.Count - message.Count <= 0)
					{
						newSize *= 2;
						if (newSize > MAX_BUFFER_SIZE)
						{
							await CloseSocketAndThrow(WebSocketCloseStatus.MessageTooBig, "Message is too big", cancellation).ConfigureAwait(false);
						}

						Array.Resize(ref array, newSize);
						buffer = new ArraySegment<byte>(array, buffer.Offset, newSize - buffer.Offset);
					}

					buffer = buffer.Slice(message.Count, buffer.Count - message.Count);
				}
			}
			throw new InvalidOperationException("Should never happen");
		}

		private async Task CloseSocketAndThrow(WebSocketCloseStatus status, string description, CancellationToken cancellation)
		{
			byte[] array = _Buffer.Array;
			if (array.Length != ORIGINAL_BUFFER_SIZE)
			{
				Array.Resize(ref array, ORIGINAL_BUFFER_SIZE);
			}

			await Socket.CloseSocket(status, description, cancellation).ConfigureAwait(false);
			throw new WebSocketException($"The socket has been closed ({status}: {description})");
		}


		private EthereumNewEventBase ParseMessage(ArraySegment<byte> buffer)
		{
			string str = UTF8.GetString(buffer.Array, 0, buffer.Count);
			return EthereumNewEventBase.ParseEvent(str, _SerializerSettings);
		}

		public async Task Send<T>(T evt, CancellationToken cancellation = default) where T : EthereumNewEventBase
		{
			byte[] bytes = UTF8.GetBytes(evt?.ToJObject(_SerializerSettings).ToString());
			using (CancellationTokenSource cts = new CancellationTokenSource(5000))
			{
				using (CancellationTokenSource cts2 = CancellationTokenSource.CreateLinkedTokenSource(cancellation))
				{
					await Socket.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, cts2.Token).ConfigureAwait(false);
				}
			}
		}

		public async Task DisposeAsync(CancellationToken cancellation)
		{
			try
			{
				await Socket.CloseSocket(WebSocketCloseStatus.NormalClosure, "Disposing NotificationServer", cancellation).ConfigureAwait(false);
			}
			catch { }
			finally { try { Socket.Dispose(); } catch { } }
		}
	}
}

