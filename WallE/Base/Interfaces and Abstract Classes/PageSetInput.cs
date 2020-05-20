#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Base
{
	using System;
	using System.Collections.Generic;
	using System.Globalization;
	using RobinHood70.CommonCode;
	using static RobinHood70.CommonCode.Globals;
	using static RobinHood70.WallE.ProjectGlobals;

	public enum ListType
	{
		Titles,
		PageIds,
		RevisionIds
	}

	public abstract class PageSetInput
	{
		#region Fields
		private static readonly Dictionary<ListType, string> ListNames = new Dictionary<ListType, string>()
		{
			[ListType.PageIds] = "pageids",
			[ListType.RevisionIds] = "revids",
			[ListType.Titles] = "titles",
		};
		#endregion

		#region Constructors
		protected PageSetInput(PageSetInput input)
		{
			ThrowNull(input, nameof(input));
			this.ConvertTitles = input.ConvertTitles;
			this.GeneratorInput = input.GeneratorInput;
			this.ListType = input.ListType;
			this.Redirects = input.Redirects;
			this.Values = input.Values;
		}

		protected PageSetInput(IEnumerable<string> titles)
		{
			ThrowNullOrWhiteSpace(titles, nameof(titles));
			this.ListType = ListType.Titles;
			this.Values = titles.AsReadOnlyList();
		}

		protected PageSetInput(IGeneratorInput generatorInput)
		{
			ThrowNull(generatorInput, nameof(generatorInput));
			this.GeneratorInput = generatorInput;
			this.Values = Array.Empty<string>();
		}

		protected PageSetInput(IGeneratorInput generatorInput, IEnumerable<string> titles)
			: this(titles)
		{
			ThrowNull(generatorInput, nameof(generatorInput));
			this.GeneratorInput = generatorInput;
		}

		protected PageSetInput(IEnumerable<long> ids, ListType listType)
		{
			ThrowNull(ids, nameof(ids));
			this.ListType = listType;
			var list = new List<string>();
			foreach (var id in ids)
			{
				list.Add(id.ToString(CultureInfo.InvariantCulture));
			}

			this.Values = list;
		}

		protected PageSetInput(IGeneratorInput generatorInput, IEnumerable<long> ids, ListType listType)
			: this(ids, listType) => this.GeneratorInput = generatorInput;
		#endregion

		#region Public Static Properties
		public static HashSet<string> AllTypes { get; } = new HashSet<string>(ListNames.Values);
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
