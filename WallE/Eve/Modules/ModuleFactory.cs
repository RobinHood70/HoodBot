namespace RobinHood70.WallE.Eve.Modules
{
	using System;
	using System.Collections.Generic;
	using Base;
	using static WikiCommon.Globals;

	/// <summary>Concrete implementation of <see cref="IModuleFactory" />.</summary>
	/// <seealso cref="IModuleFactory" />
	public class ModuleFactory : IModuleFactory
	{
		#region Fields
		private readonly Dictionary<Type, GeneratorFactoryMethod> generators = new Dictionary<Type, GeneratorFactoryMethod>();
		private readonly Dictionary<Type, PropertyFactoryMethod> properties = new Dictionary<Type, PropertyFactoryMethod>();
		private readonly WikiAbstractionLayer wal;
		#endregion

		#region Constructors

		/// <summary>Initializes a new instance of the <see cref="ModuleFactory" /> class.</summary>
		/// <param name="wal">The parent wiki abstraction layer.</param>
		public ModuleFactory(WikiAbstractionLayer wal) => this.wal = wal;
		#endregion

		#region Public Methods

		/// <summary>Creates property modules from the provided inputs.</summary>
		/// <param name="propertyInputs">The property inputs.</param>
		/// <returns>A set of modules that corresponds to the provided inputs. If <paramref name="propertyInputs" /> is null, returns an empty collection.</returns>
		public ModuleCollection<IPropertyModule> CreateModules(IEnumerable<IPropertyInput> propertyInputs)
		{
			var modules = new ModuleCollection<IPropertyModule>();
			if (propertyInputs != null)
			{
				foreach (var propertyInput in propertyInputs)
				{
					if (this.properties.TryGetValue(propertyInput.GetType(), out var factoryMethod))
					{
						modules.Add(factoryMethod(this.wal, propertyInput));
					}
				}
			}

			return modules;
		}

		/// <summary>Creates a new continuation module.</summary>
		/// <returns>A continuation module appropriate to the version of the wiki in use.</returns>
		public ContinueModule CreateContinue()
		{
			switch (this.wal.ContinueVersion)
			{
				case 1:
					return new ContinueModule1();
				case 2:
					return new ContinueModule2(this.wal.SiteVersion);
				default:
					return new ContinueModuleUnknown();
			}
		}

		/// <summary>Creates a generator module from the relevant input.</summary>
		/// <typeparam name="TInput">The type of the input.</typeparam>
		/// <param name="input">The generator input.</param>
		/// <returns>A module which corresponds to the input and has its IsGenerator property set.</returns>
		public IGeneratorModule CreateGenerator<TInput>(TInput input)
			where TInput : class, IGeneratorInput
		{
			var generator = this.generators[input.GetType()](this.wal, input);
			generator.IsGenerator = true;

			return generator;
		}

		/// <summary>Registers a generator factory method for use with <see cref="CreateGenerator{TInput}(TInput)" />.</summary>
		/// <typeparam name="T">The type of generator input that the factory method handles.</typeparam>
		/// <param name="generatorFactoryMethod">The generator factory method.</param>
		/// <returns>The current module factory (fluent interface).</returns>
		public IModuleFactory RegisterGenerator<T>(GeneratorFactoryMethod generatorFactoryMethod)
			where T : IGeneratorInput
		{
			ThrowNull(generatorFactoryMethod, nameof(generatorFactoryMethod));
			this.generators.Add(typeof(T), generatorFactoryMethod);

			return this;
		}

		/// <summary>Registers a property factory method for use with <see cref="CreateModules(IEnumerable{IPropertyInput})" />.</summary>
		/// <typeparam name="T">The type of property input that the factory method handles.</typeparam>
		/// <param name="propertyFactoryMethod">The property factory method.</param>
		/// <returns>The current module factory (fluent interface).</returns>
		public IModuleFactory RegisterProperty<T>(PropertyFactoryMethod propertyFactoryMethod)
			where T : IPropertyInput
		{
			ThrowNull(propertyFactoryMethod, nameof(propertyFactoryMethod));
			this.properties.Add(typeof(T), propertyFactoryMethod);

			return this;
		}
		#endregion
	}
}