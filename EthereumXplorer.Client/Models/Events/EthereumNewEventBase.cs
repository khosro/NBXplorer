using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;

namespace EthereumXplorer.Client.Models.Events
{
	public abstract class EthereumNewEventBase
	{
		static EthereumNewEventBase()
		{
			_TypeByName = new Dictionary<string, Type>();
			_NameByType = new Dictionary<Type, string>();
			Add("newblock", typeof(EthNewBlockEvent));
			Add("newtransaction", typeof(EthNewTransactionEvent));
		}

		private static Dictionary<string, Type> _TypeByName;
		private static Dictionary<Type, string> _NameByType;
		private static void Add(string typeName, Type type)
		{
			_TypeByName.Add(typeName, type);
			_NameByType.Add(type, typeName);
		}
		public static string GetEventTypeName(Type type)
		{
			_NameByType.TryGetValue(type, out string name);
			return name;
		}

		[JsonIgnore]
		public abstract string EventType { get; }

		public string CryptoCode
		{
			get;
			set;
		}

		[JsonIgnore]
		public long EventId { get; set; }

		public JObject ToJObject(JsonSerializerSettings settings)
		{
			string typeName = GetEventTypeName(GetType());
			if (typeName == null)
			{
				throw new InvalidOperationException($"{GetType().Name} does not have an associated typeName");
			}

			JObject jobj = new JObject();
			string serialized = JsonConvert.SerializeObject(this, settings);
			JObject data = JObject.Parse(serialized);
			if (EventId != 0)
			{
				jobj.Add(new JProperty("eventId", new JValue(EventId)));
			}

			jobj.Add(new JProperty("type", new JValue(typeName)));
			jobj.Add(new JProperty("data", data));
			return jobj;
		}

		public static EthereumNewEventBase ParseEvent(string str, JsonSerializerSettings settings)
		{
			if (str == null)
			{
				throw new ArgumentNullException(nameof(str));
			}

			JObject jobj = JObject.Parse(str);
			return ParseEvent(jobj, settings);
		}
		public static EthereumNewEventBase ParseEvent(JObject jobj, JsonSerializerSettings settings)
		{
			if (jobj == null)
			{
				throw new ArgumentNullException(nameof(jobj));
			}

			string type = (jobj["type"] as JValue)?.Value<string>();
			if (type == null)
			{
				throw new FormatException("type property not found");
			}

			bool unknown = false;
			if (!_TypeByName.TryGetValue(type, out Type typeObject))
			{
				unknown = true;
				typeObject = typeof(UnknownEvent);
			}
			JObject data = (jobj["data"] as JObject);
			if (data == null)
			{
				throw new FormatException("data property not found");
			}

			EthereumNewEventBase evt = null;
			if (unknown)
			{
				UnknownEvent unk = new UnknownEvent(type)
				{
					Data = data,
					CryptoCode = data["cryptoCode"]?.Value<string>()
				};
				evt = unk;
			}
			else
			{
				evt = (EthereumNewEventBase)JsonConvert.DeserializeObject(data.ToString(), typeObject, settings);
			}
			if (jobj["eventId"] != null)
			{
				evt.EventId = jobj["eventId"].Value<long>();
			}
			return evt;
		}

		public string ToJson(JsonSerializerSettings settings)
		{
			return JsonConvert.SerializeObject(this, settings);
		}
	}
}
