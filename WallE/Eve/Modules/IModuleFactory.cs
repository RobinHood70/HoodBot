namespace RobinHood70.WallE.Eve.Modules
{
	using System.Collections.Generic;
	using System.Diagnostics.CodeAnalysis;
	using RobinHood70.WallE.Base;

	#region Public Delegates

	/// <summary>Represents a method which creates a new <see cref="IGeneratorModule" />.</summary>
	/// <param name="wal">The parent abstraction layer.</param>
	/// <param name="input">The generator input.</param>
	/// <returns>A new generator module based on the input.</returns>
	public delegate IGeneratorModule GeneratorFactoryMethod(WikiAbstractionLayer wal, IGeneratorInput input);

	/// <summary>Represents a method which creates a new <see cref="IPropertyModule" />.</summary>
	/// <param name="wal">The parent abstraction layer.</param>
	/// <param name="input">The property input.</param>
	/// <returns>A new property module based on the input.</returns>
	public delegate IPropertyModule PropertyFactoryMethod(WikiAbstractionLayer wal, IPropertyInput input);
	#endregion

	/// <summary>Provides an interface for all operations required of a Module Factory.</summary>
	public interface IModuleFactory
	{
		#region Methods

		/// <summary>Creates a new continuation module.</summary>
		/// <returns>A continuation module appropriate to the version of the wiki in use.</returns>
		ContinueModule CreateContinue();

		/// <summary>Creates a generator module from the relevant input.</summary>
		/// <typeparam name="TInput">The type of the input.</typeparam>
		/// <param name="input">The generator input.</param>
		/// <returns>A module which corresponds to the input and has its IsGenerator property set.</returns>
		IGeneratorModule CreateGenerator<TInput>(TInput input)
			where TInput : class, IGeneratorInput;

		/// <summary>Creates property modules from the provided inputs.</summary>
		/// <param name="propertyInputs">The property inputs.</param>
		/// <returns>A set of modules that corresponds to the provided inputs. If <paramref name="propertyInputs" /> is null, this should return an empty collection.</returns>
		IEnumerable<IPropertyModule> CreateModules(IEnumerable<IPropertyInput> propertyInputs);

		/// <summary>Registers a generator factory method for use with <see cref="CreateGenerator{TInput}(TInput)" />.</summary>
		/// <typeparam name="T">The type of generator input that the factory method handles.</typeparam>
		/// <param name="generatorFactoryMethod">The generator factory method.</param>
		/// <returns>The current module factory (fluent interface).</returns>
		[SuppressMessage("Microsoft.Design", "CA1004:GenericMethodsShouldProvideTypeParameter", Justification = "Shorter and simpler than using typeof() in caller.")]
		IModuleFactory RegisterGenerator<T>(GeneratorFactoryMethod generatorFactoryMethod)
			where T : IGeneratorInput;

		/// <summary>Registers a property factory method for use with <see cref="CreateModules(IEnumerable{IPropertyInput})" />.</summary>
		/// <typeparam name="T">The type of property input that the factory method handles.</typeparam>
		/// <param name="propertyFactoryMethod">The property factory method.</param>
		/// <returns>The current module factory (fluent interface).</returns>
		[SuppressMessage("Microsoft.Design", "CA1004:GenericMethodsShouldProvideTypeParameter", Justification = "Shorter and simpler than using typeof() in caller.")]
		IModuleFactory RegisterProperty<T>(PropertyFactoryMethod propertyFactoryMethod)
			where T : IPropertyInput;
		#endregion
	}
}
