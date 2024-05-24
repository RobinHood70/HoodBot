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

			/*
			var printout = new List<string>(split[..^1]);
			while (printout.Count < 4)
			{
				printout.Add(string.Empty);
			}

			printout.Add(split[^1]);
			Debug.WriteLine(string.Join('\t', printout));
			*/

			var keyword = split[0];
			switch (keyword[0])
			{
				// Descriptions are guesses based on the letter and their function
				case 'c':
					// Child
					break;
				case 'm':
					// Multiple
					this.Term = true;
					this.Male = keyword[2] switch
					{
						// There are no instances of 'f', and only one instance of 'm', but the 'm' is clearly male, so I'm assuming the rest from there.
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
					// Singular
					this.Singular = true;
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
					case 'A':
						// Indefinite Article
						this.Capitalize = true;
						this.IndefiniteArticle = true;
						break;
					case 'a':
						// Indefinite Article
						this.IndefiniteArticle = true;
						break;
					case 'c':
						// Unknown
						break;
					case 'D':
						// Unknown
						this.Capitalize = true;
						this.DefiniteArticle = true;
						break;
					case 'd':
						this.DefiniteArticle = true;
						break;
					case 'o':
						// Unknown - remainder of parameter is either nothing or a decimal number.
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
		public string BracedId => '{' + this.Id + '}';

		public bool Capitalize { get; }

		public bool DefiniteArticle { get; }

		public string Id { get; }

		public bool IndefiniteArticle { get; }

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