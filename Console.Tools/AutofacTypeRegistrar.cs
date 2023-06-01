using Autofac;
using Spectre.Console.Cli;

namespace Console.Tools
{
	public sealed class AutofacTypeRegistrar : ITypeRegistrar
	{
		private readonly ContainerBuilder _builder;

		public IContainer? Container { get; private set; }

		public AutofacTypeRegistrar(ContainerBuilder builder)
		{
			_builder = builder ?? throw new ArgumentNullException(nameof(builder));
		}


		public ITypeResolver Build()
		{
			Container = _builder.Build();

			return new AutofacTypeResolver(Container);
		}


		public void Register(Type service, Type implementation)
		{
			if (service is null)
			{
				throw new ArgumentNullException(nameof(service));
			}

			if (implementation is null)
			{
				throw new ArgumentNullException(nameof(implementation));
			}

			_builder.RegisterType(implementation).As(service);
		}


		public void RegisterInstance(Type service, object implementation)
		{
			if (service is null)
			{
				throw new ArgumentNullException(nameof(service));
			}

			if (implementation is null)
			{
				throw new ArgumentNullException(nameof(implementation));
			}

			_builder.RegisterInstance(implementation).As(service);
		}


		public void RegisterLazy(Type service, Func<object> factoryMethod)
		{
			if (service is null)
			{
				throw new ArgumentNullException(nameof(service));
			}

			if (factoryMethod is null)
			{
				throw new ArgumentNullException(nameof(factoryMethod));
			}

			_builder.Register((context) => factoryMethod()).As(service);
		}
	}
}