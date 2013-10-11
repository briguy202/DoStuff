using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DoStuff
{
	public class OperationType
	{
		public string Name { get; set; }
		public string ID { get; set; }
		public bool PromptBeforeExecuting { get; set; }
		public string Description { get; set; }
		public string DefaultSourceDescription { get; set; }
		public string DefaultSourceValue { get; set; }
		public string DefaultParametersDescription { get; set; }
		public string DefaultParametersValue { get; set; }
		public Func<string, string, string, string> Execute { get; set; }

		public OperationType()
		{
			this.PromptBeforeExecuting = false;
		}
	}
}