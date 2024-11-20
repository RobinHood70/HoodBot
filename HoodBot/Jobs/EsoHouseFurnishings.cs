namespace RobinHood70.HoodBot.Jobs
{
	using System;
	using System.Collections.Generic;
	using System.Diagnostics;
	using System.Text;
	using RobinHood70.CommonCode;
	using RobinHood70.Robby;
	using RobinHood70.Robby.Parser;
	using RobinHood70.WikiCommon.Parser;

	[method: JobInfo("House Furnishings")]
	internal sealed class EsoHouseFurnishings(JobManager jobManager) : ParsedPageJob(jobManager)
	{
		#region Fields
		private readonly Dictionary<string, string> linkSubstitutes = new(StringComparer.Ordinal)
		{
			["basket_and_bags"] = "baskets_and_bags",
			["breads_and_deserts"] = "breads_and_desserts",
			["candle"] = "candles",
			["kinick-knacks"] = "knick-knacks",
			["nightstand"] = "nightstands",
			["rugs_and_carpet"] = "rugs_and_carpets",
			["carts_and_wagons"] = "vehicles"
		};

		private readonly Dictionary<string, string> generalCategories = new(StringComparer.Ordinal)
		{
			["Captain Margaux's Place"] = "mounted_decor",
			["Grand Topal Hideaway"] = "mounted_decor",
			["Hunter's Glade"] = "tools",
			["Pariah's Pinnacle"] = "mounted_decor"
		};

		#endregion

		#region Protected Override Methods
		protected override string GetEditSummary(Page page) => "Use House Furnishings template";

		protected override void LoadPages()
		{
			var titles = new TitleCollection(this.Site, "Template:ESO Houses");
			this.Pages.GetPageLinks(titles);
			//// this.Pages.GetTitles("Online:Enchanted Snow Globe Home", "Online:Lucky Cat Landing", "Online:Potentate's Retreat", "Online:Varlaisvea Ayleid Ruins", "Online:Varlaisvea Ayleid Ruins", "Online:Stone Eagle Aerie", "Online:Pantherfang Chapel", "Online:Sweetwater Cascades");
		}

		protected override void ParseText(SiteParser parser)
		{
			var sections = parser.ToSections();
			foreach (var section in FindFurnishings(sections))
			{
				if (section.Content.Find<SiteTemplateNode>(template => template.Title.PageNameEquals("ESO House Furnishings")) is not null)
				{
					this.WriteLine($"* [[{parser.Page.Title.FullPageName()}]] has already been converted.");
					continue;
				}

				/* if (section.Content.Find<ICommentNode>() is not null)
				{
					this.WriteLine($"* [[{parser.Page.FullPageName}]] will need to be converted manually.");
					continue;
				} */

				var sb = new StringBuilder(parser.Page.Text.Length);
				var lines = section.Content.ToRaw().Split('\n');
				var itemList = new List<string>();
				foreach (var line in lines)
				{
					if (this.ParseLine(parser.Page, line.Trim(), itemList) is string newLine)
					{
						sb.AppendLine(newLine);
					}
				}

				var text = "\n" + sb.ToString().Trim();
				var insert = text.IndexOf("\n|", StringComparison.Ordinal);
				if (insert == -1)
				{
					continue;
				}

				if (insert >= 0)
				{
					text = text.Insert(insert, "\n{{ESO House Furnishings");
					insert = text.IndexOf('\n', StringComparison.Ordinal);
					insert = text.LastIndexOf("\n|", StringComparison.Ordinal);
					if (insert == -1)
					{
						throw new InvalidOperationException();
					}

					insert = text.IndexOf('\n', insert + 1);
					if (insert == -1)
					{
						insert = text.Length;
					}

					itemList.Sort(StringComparer.Ordinal);
					var insertText = string.Join('\n', itemList) + "\n";
					if (section.GetTitle().OrdinalEquals("Furnishings"))
					{
						insertText += "furnished=1\n";
					}

					insertText += "}}";
					text = text.Insert(insert, insertText) + "\n\n";

					section.Content.Clear();
					section.Content.AddText(text);
				}
			}

			parser.FromSections(sections);
			parser.UpdatePage();
		}
		#endregion

		#region Private Static Methods
		private static IEnumerable<Section> FindFurnishings(SectionCollection sections)
		{
			foreach (var section in sections)
			{
				if (section.GetTitle() is string headerText)
				{
					if (headerText.OrdinalEquals("Gallery"))
					{
						yield break;
					}

					if (headerText.Contains("furnish", StringComparison.OrdinalIgnoreCase))
					{
						yield return section;
					}
				}
			}
		}

		private static (string Name, string Number) SplitLine(string trimmedLine)
		{
			var index = trimmedLine.LastIndexOf('(');
			if (index == -1)
			{
				return (trimmedLine, "1");
			}

			var name = trimmedLine[0..index];
			var count = trimmedLine[(index + 1)..^1];

			return (name, count);
		}
		#endregion

		#region Private Methods
		private string? ParseLine(Page page, string line, List<string> itemList)
		{
			var parsedLine = new SiteParser(page, line);
			if (parsedLine.Count > 0 && parsedLine[0] is ITextNode textNode)
			{
				var text = textNode.Text;
				switch (text)
				{
					case "{|":
					case "|":
					case "|-":
					case "|}":
					case "58/350":
						return null;
					case ":*":
					case ";*":
						if (parsedLine[1] is SiteTemplateNode furnishing && furnishing.Title.PageNameEquals("Furnishing Link"))
						{
							var link = furnishing.Find(1)?.Value.ToRaw();
							var (_, count) = SplitLine(line);
							itemList.Add($"~{link}~{count}");
							return null;
						}

						break;
					default:
						if (text[0] == ';')
						{
							if (text.Length > 1 && text[1] == ';')
							{
								itemList.Sort(StringComparer.Ordinal);
								var prevList = string.Join('\n', itemList);
								itemList.Clear();
								var (link, _) = SplitLine(line[2..]);
								link = link
									.Trim()
									.Replace("<s>", string.Empty, StringComparison.OrdinalIgnoreCase)
									.Replace("</s>", string.Empty, StringComparison.OrdinalIgnoreCase)
									.Replace(' ', '_')
									.ToLowerInvariant();
								if (link.Length > 0 && link[0] == '[')
								{
									link = link[(link.IndexOf('/', StringComparison.Ordinal) + 1)..];
									link = link[0..link.IndexOf('|', StringComparison.Ordinal)];
								}

								if (this.linkSubstitutes.TryGetValue(link, out var newLink))
								{
									link = newLink;
								}

								if (link.OrdinalEquals("general") &&
									this.generalCategories.TryGetValue(page.Title.PageName, out newLink))
								{
									link = newLink;
								}

								return $"{prevList}\n|{link}=";
							}

							return null;
						}

						if (text.Contains("Item", StringComparison.OrdinalIgnoreCase) ||
						text.Contains("cont.", StringComparison.Ordinal) ||
						text.StartsWith("Includes", StringComparison.Ordinal))
						{
							return null;
						}

						this.Warn($"Page {page.Title.FullPageName()}, Unknown: {text}");
						break;
				}
			}

			if (line.Length == 0)
			{
				return line;
			}

			Debug.WriteLine($"Page {page.Title.FullPageName()}, Default: {line}");
			return line;
		}
		#endregion
	}
}