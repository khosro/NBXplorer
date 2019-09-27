using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using System;

namespace EthereumXplorer.Loggging
{
	public class Logs
	{

		static Logs()
		{
			Configure(new FuncLoggerFactory(n => NullLogger.Instance));
		}
		public static void Configure(ILoggerFactory factory)
		{
			EthereumXplorer = factory.CreateLogger("EthereumXplorer");
		}
		public static ILogger EthereumXplorer
		{
			get; set;
		}

		public const int ColumnLength = 16;
	}

	public class FuncLoggerFactory : ILoggerFactory
	{
		private readonly Func<string, ILogger> createLogger;
		public FuncLoggerFactory(Func<string, ILogger> createLogger)
		{
			this.createLogger = createLogger;
		}
		public void AddProvider(ILoggerProvider provider)
		{

		}

		public ILogger CreateLogger(string categoryName)
		{
			return createLogger(categoryName);
		}

		public void Dispose()
		{

		}
	}

}
