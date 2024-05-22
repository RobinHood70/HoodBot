namespace RobinHood70.HoodBot.Jobs.JobModels
{
	using RobinHood70.CommonCode;

	internal sealed class CastlesTxInfo
	{
		#region Constructors
		public CastlesTxInfo(string text)
		{
			text = text.Trim(TextArrays.CurlyBrackets);
			this.OriginalText = text;
			var split = text.Split(TextArrays.Comma);
			var keyword = split[0];
			switch (keyword[0])
			{
				// Descriptions are guesses based on the letter and their function
				case 'c':
					// Child
					break;
				case 'm':
					// graMmar
					this.Term = true;
					this.Male = keyword[2] switch
					{
						'f' => false,
						'm' => true,
						_ => null
					};

					this.Singular = keyword[3] switch
					{
						'p' => false,
						's' => true,
						_ => null
					};

					break;
				case 'n':
					// seNtence
					this.Sentence = true;
					break;
				case 'p':
					// Personal
					this.Personal = true;
					break;
				case 's':
					// Simple?
					this.Term = true;
					break;
			}

			if (keyword.Length > 1)
			{
				var target = keyword[1..];
				this.Target = target switch
				{
					"a" => "Co-Requester",
					"r" => "Ruler",
					"s" => "Requester",
					_ => target
				};
			}

			this.Id = split[^1];
			if (split.Length < 2)
			{
				return;
			}

			split = split[1..^1];
			foreach (var tag in split)
			{
				switch (tag[0])
				{
					case 'a':
						// Article
						this.Article = true;
						break;
					case 'c':
						// Unknown
						break;
					case 'D':
						// Unknown
						break;
					case 'o':
						break;
					case 'p':
						this.Parent = tag[1..];
						break;
					case 's':
						// Unknown
						break;
					case 'v':
						this.Variation = true;
						break;
				}
			}
		}
		#endregion

		#region Public Properties
		public bool Article { get; }

		public string BracedId => '{' + this.Id + '}';

		public string Id { get; }

		public bool? Male { get; }

		public string OriginalText { get; }

		public string Parent { get; } = string.Empty;

		public bool Personal { get; }

		public bool Sentence { get; }

		public bool? Singular { get; }

		public string Target { get; } = string.Empty;

		public bool Term { get; }

		public bool Source { get; }

		public bool Variation { get; }
		#endregion

		#region Public Override Methods
		public override string ToString() => this.OriginalText;
		#endregion
	}
}