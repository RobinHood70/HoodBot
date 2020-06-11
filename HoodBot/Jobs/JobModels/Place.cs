namespace RobinHood70.HoodBot.Jobs.JobModels
{
	using System;
	using System.Diagnostics.CodeAnalysis;
	using RobinHood70.HoodBot.Uesp;
	using RobinHood70.Robby;
	using RobinHood70.Robby.Design;
	using static RobinHood70.CommonCode.Globals;

	#region Public Enumerations
	public enum PlaceType
	{
		Unknown,
		City,
		Settlement,
		House,
		Ship,
		Store,
	}
	#endregion

	// Not captured: description|mapname|maplink|appendicon|housesize|housestyle|price|pricefurnished|priceunfurnished|requirements|unavailable|location
	internal sealed class Place : IEquatable<Place>
	{
		#region Constructors
		public Place(VariablesPage page)
		{
			ThrowNull(page, nameof(page));
			this.Alliance = page.GetVariable("alliance");
			this.Key = page.PageName;
			this.Settlement = page.GetVariable("settlement");
			this.Title = new Title(page);
			this.TitleName = page.GetVariable("titlename");
			this.TypeText = page.GetVariable("type");
			this.Zone = page.GetVariable("zone");
		}

		// For ad-hoc places created when no place was found.
		public Place(string titleName)
		{
			this.TitleName = titleName ?? throw ArgumentNull(nameof(titleName));
			this.Key = titleName;
		}
		#endregion

		#region Public Properties
		public string? Alliance { get; private set; }

		public string Key { get; }

		public PlaceType PlaceType { get; internal set; } // Should be redundant to TypeText, but this is taken from categories as opposed to the MT variables, so could differ.

		public string? Settlement { get; private set; }

		public Title? Title { get; private set; }

		public string? TitleName { get; private set; }

		public string? TypeText { get; private set; }

		public string? Zone { get; private set; }
		#endregion

		#region Operators
		public static bool operator ==(Place? left, Place? right) => left?.Equals(right) ?? right is null;

		public static bool operator !=(Place? left, Place? right) => !(left == right);
		#endregion

		#region Public Static Methods
		public static Place Copy(string titleName, Place other)
		{
			ThrowNull(titleName, nameof(titleName));
			ThrowNull(other, nameof(other));
			var retval = new Place(titleName)
			{
				Alliance = other.Alliance,
				Settlement = other.Settlement,
				Title = other.Title,
				PlaceType = other.PlaceType,
				TypeText = other.TypeText,
				Zone = other.Zone,
			};

			return retval;
		}
		#endregion

		#region Public Methods
		public bool Equals([AllowNull] Place other) => this.Key == other?.Key;
		#endregion

		#region Public Override Methods
		public override bool Equals(object? obj) => this.Equals(obj as Place);

		public override int GetHashCode() => this.Key.GetHashCode(StringComparison.Ordinal);

		public override string? ToString() => this.Title == null ? this.TitleName : this.Title.AsLink(true);
		#endregion
	}
}
