using Autofac;
using Spectre.Console.Cli;

namespace Console.Tools
{
	public sealed class AutofacTypeResolver : ITypeResolver, IDisposable
	{
		private readonly IContainer _container;

		public AutofacTypeResolver(IContainer container)
		{
			_container = container ?? throw new ArgumentNullException(nameof(container));
		}

		public object? Resolve(Type? type)
		{
			if (type == null)
			{
				return null;
			}

			return _container.IsRegistered(type) ? _container.Resolve(type) : null;
		}

		
		public void Dispose()
		{
			GC.SuppressFinalize(this);

			if (_container is IDisposable disposable)
			{
				disposable.Dispose();
			}
		}
	}
}