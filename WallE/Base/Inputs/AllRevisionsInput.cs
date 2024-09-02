#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member (no intention to document this file)
namespace RobinHood70.WallE.Base
{
	using System;
	using System.Collections.Generic;

	public class AllRevisionsInput : IRevisionsInput, IGeneratorInput
	{
		#region Public Properties
		public IList<string> ContentFormat { get; } = [];

		public int? DiffTo { get; set; }

		public string? DiffToText { get; set; }

		public bool DiffToTextPreSaveTransform { get; set; }

		public DateTime? End { get; set; }

		public bool ExcludeUser { get; set; }

		public bool ExpandTemplates { get; set; }

		public bool GenerateTitles { get; set; }

		public bool GenerateXml { get; set; }

		public int Limit { get; set; }

		public int MaxItems { get; set; }

		public IEnumerable<int>? Namespaces { get; set; }

		public bool Parse { get; set; }

		public RevisionsProperties Properties { get; set; }

		public int Section { get; set; } = -1;

		public IList<string> Slots { get; } = ["main"];

		public bool SortAscending { get; set; }

		public DateTime? Start { get; set; }

		public string? User { get; set; }
		#endregion
	}
}