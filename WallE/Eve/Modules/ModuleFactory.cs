namespace RobinHood70.WallE.Eve.Modules
{
	using System;
	using System.Collections.Generic;
	using RobinHood70.CommonCode;
	using RobinHood70.WallE.Base;

	/// <summary>Concrete implementation of <see cref="IModuleFactory" />.</summary>
	/// <seealso cref="IModuleFactory" />
	internal sealed class ModuleFactory : IModuleFactory
	{
		#region Fields
		private readonly Dictionary<Type, GeneratorFactoryMethod> generators = [];
		private readonly Dictionary<Type, PropertyFactoryMethod> properties = [];
		private readonly WikiAbstractionLayer wal;
		#endregion

		#region Constructors

		/// <summary>Initializes a new instance of the <see cref="ModuleFactory" /> class.</summary>
		/// <param name="wal">The parent wiki abstraction layer.</param>
		public ModuleFactory(WikiAbstractionLayer wal)
		{
			this.wal = wal;
		}
		#endregion

		#region Public Methods

		/// <summary>Creates property modules from the provided inputs.</summary>
		/// <param name="propertyInputs">The property inputs.</param>
		/// <returns>A set of modules that corresponds to the provided inputs.</returns>
		/// <exception cref="KeyNotFoundException">Thrown when the one or more of the property modules requested could not be found.</exception>
		public IEnumerable<IPropertyModule> CreateModules(IEnumerable<IPropertyInput> propertyInputs)
		{
			if (propertyInputs != null)
			{
				foreach (var propertyInput in propertyInputs)
				{
					var retval = this.properties.TryGetValue(propertyInput.GetType(), out var factoryMethod)
						? factoryMethod(this.wal, propertyInput)
						: throw new KeyNotFoundException(Globals.CurrentCulture(Properties.EveMessages.ParameterInvalid, nameof(this.CreateModules), propertyInput.GetType().Name));
					yield return retval;
				}
			}
		}

		/// <summary>Creates a new continuation module.</summary>
		/// <returns>A continuation module appropriate to the version of the wiki in use.</returns>
		public ContinueModule CreateContinue() => this.wal.ContinueVersion switch
		{
			1 => new ContinueModule1(),
			2 => new ContinueModule2(this.wal.SiteVersion),
			_ => new ContinueModuleUnknown(),
		};

		/// <summary>Creates a generator module from the relevant input.</summary>
		/// <typeparam name="TInput">The type of the input.</typeparam>
		/// <param name="input">The generator input.</param>
		/// <param name="pageSetGenerator">The parent pageset.</param>
		/// <returns>A module which corresponds to the input and has its IsGenerator property set.</returns>
		public IGeneratorModule CreateGenerator<TInput>(TInput input, IPageSetGenerator pageSetGenerator)
			where TInput : class, IGeneratorInput => this.generators[input.GetType()](this.wal, input, pageSetGenerator);

		/// <summary>Registers a generator factory method for use with <see cref="CreateGenerator{TInput}(TInput, IPageSetGenerator)" />.</summary>
		/// <typeparam name="T">The type of generator input that the factory method handles.</typeparam>
		/// <param name="generatorFactoryMethod">The generator factory method.</param>
		/// <returns>The current module factory (fluent interface).</returns>
		public IModuleFactory RegisterGenerator<T>(GeneratorFactoryMethod generatorFactoryMethod)
			where T : IGeneratorInput
		{
			ArgumentNullException.ThrowIfNull(generatorFactoryMethod);
			this.generators[typeof(T)] = generatorFactoryMethod;
			return this;
		}

		/// <summary>Registers a property factory method for use with <see cref="CreateModules(IEnumerable{IPropertyInput})" />.</summary>
		/// <typeparam name="T">The type of property input that the factory method handles.</typeparam>
		/// <param name="propertyFactoryMethod">The property factory method.</param>
		/// <returns>The current module factory (fluent interface).</returns>
		public IModuleFactory RegisterProperty<T>(PropertyFactoryMethod propertyFactoryMethod)
			where T : IPropertyInput
		{
			ArgumentNullException.ThrowIfNull(propertyFactoryMethod);
			this.properties[typeof(T)] = propertyFactoryMethod;
			return this;
		}
		#endregion
	}
}