using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Text;

namespace EthereumXplorer.Client.Models.Events
{
	public class UnknownEvent : EthereumNewEventBase
	{
		public UnknownEvent()
		{

		}
		public UnknownEvent(string eventType)
		{
			_EventType = eventType;
		}
		string _EventType;
		public override string EventType => _EventType;

		public JObject Data { get; set; }
	}
}
