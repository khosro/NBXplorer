using NBXplorer.DerivationStrategy;
using System;
using System.Collections.Generic;
using System.Text;

namespace NBXplorer.Models
{
	public class CreatePSBTRequest1 : CreatePSBTRequest
	{
		public DerivationStrategyBase Strategy { get; set; }
	}

	public class CreatePSBTRequest1s  
	{
		public IEnumerable<CreatePSBTRequest1> Requests { get; set; }
	}
}
