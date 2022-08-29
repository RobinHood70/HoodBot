#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Base
{
	using System;
	using System.Collections.Generic;
	using System.Globalization;
	using RobinHood70.CommonCode;

	public enum ListType
	{
		Titles,
		PageIds,
		RevisionIds
	}

	public abstract class PageSetInput
	{
		#region Fields
		private static readonly Dictionary<ListType, string> ListNames = new()
		{
			[ListType.PageIds] = "pageids",
			[ListType.RevisionIds] = "revids",
			[ListType.Titles] = "titles",
		};
		#endregion

		#region Constructors
		protected PageSetInput(PageSetInput input)
		{
			this.ConvertTitles = input.NotNull().ConvertTitles;
			this.GeneratorInput = input.GeneratorInput;
			this.ListType = input.ListType;
			this.Redirects = input.Redirects;
			this.Values = input.Values;
		}

		protected PageSetInput(IEnumerable<string> titles)
		{
			this.ListType = ListType.Titles;
			titles = titles.NotNullOrWhiteSpace();
			var values = titles.AsReadOnlyList();
			this.Values = values;
		}

		protected PageSetInput(IGeneratorInput generatorInput)
		{
			this.GeneratorInput = generatorInput.NotNull();
			this.Values = Array.Empty<string>();
		}

		protected PageSetInput(IGeneratorInput generatorInput, IEnumerable<string> titles)
			: this(titles)
		{
			this.GeneratorInput = generatorInput.NotNull();
		}

		protected PageSetInput(IEnumerable<long> ids, ListType listType)
		{
			this.ListType = listType;
			List<string> list = new();
			foreach (var id in ids.NotNull())
			{
				list.Add(id.ToString(CultureInfo.InvariantCulture));
			}

			this.Values = list;
		}

		protected PageSetInput(IGeneratorInput generatorInput, IEnumerable<long> ids, ListType listType)
			: this(ids, listType)
		{
			this.GeneratorInput = generatorInput;
		}
		#endregion

		#region Public Static Properties
		public static ICollection<string> AllTypes { get; } = new HashSet<string>(ListNames.Values, StringComparer.Ordinal);
		#endregion

		#region Public Properties
		public bool ConvertTitles { get; set; }

		public IGeneratorInput? GeneratorInput { get; }

		public ListType ListType { get; }

		public bool Redirects { get; set; }

		public string TypeName => ListNames[this.ListType];

		public IReadOnlyList<string> Values { get; }
		#endregion
	}
}
