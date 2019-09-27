using Microsoft.Extensions.DependencyInjection;
using System.Threading;
using System.Threading.Tasks;

namespace XplorerUtil
{
	public static class Extensions
	{
		public static IServiceCollection AddStartupTask<T>(this IServiceCollection services) where T : class, IStartupTask
		{
			return services.AddTransient<IStartupTask, T>();
		}
	}
	public interface IStartupTask
	{
		Task ExecuteAsync(CancellationToken cancellationToken = default);
	}

}
