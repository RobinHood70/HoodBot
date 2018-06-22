#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Eve.Modules
{
	using System;
	using System.Collections.ObjectModel;
	using static Properties.Messages;
	using static WikiCommon.Globals;

	public class ModuleCollection<TModule> : KeyedCollection<string, TModule>
		where TModule : class, IModule
	{
		#region Public Methods
		public bool TryGetValue(string name, out TModule item)
		{
			if (this.Dictionary == null)
			{
				foreach (var testItem in this)
				{
					if (this.GetKeyForItem(testItem) == name)
					{
						item = testItem;
						return true;
					}
				}
			}
			else
			{
				return this.Dictionary.TryGetValue(name, out item);
			}

			item = null;
			return false;
		}

		public bool TryGetValue<TOutput>(string name, out TOutput item)
			where TOutput : class, TModule
		{
			if (this.TryGetValue(name, out TModule foundItem))
			{
				item = foundItem as TOutput;
				return item == null ? throw new InvalidOperationException(CurrentCulture(IncorrectModuleType, name, typeof(TOutput).Name, foundItem.GetType().Name)) : true;
			}

			item = null;
			return false;
		}
		#endregion

		#region Protected Override Methods
		protected override string GetKeyForItem(TModule item) => item?.Name;
		#endregion
	}
}
