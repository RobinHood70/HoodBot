﻿namespace RobinHood70.HoodBot.Jobs.JobModels;

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using RobinHood70.CommonCode;

public class BladesCodeLine : BladesCodeLineCollection
{
	#region Static Fields
	private static readonly char[] Quote = ['"'];
	#endregion

	#region Constructors
	public BladesCodeLine(string line)
	{
		ArgumentNullException.ThrowIfNull(line);
		var textSplit = line.Split(TextArrays.EqualsSign, 2);
		if (textSplit.Length == 2)
		{
			this.Value = textSplit[1].Trim().Trim(Quote);
			line = textSplit[0];
		}

		textSplit = line.Split(TextArrays.Space, 2);
		if (textSplit.Length != 2)
		{
			throw new InvalidOperationException($"Text is not in the right format: {line}");
		}

		this.DataType = textSplit[0].Trim();
		this.Name = textSplit[1].Trim();
	}
	#endregion

	#region Public Properties
	public string DataType { get; }

	public string Name { get; set; }

	public string? Value { get; }
	#endregion

	#region Public Static Methods
	public static BladesCodeLine Parse(IList<string> lines)
	{
		ArgumentNullException.ThrowIfNull(lines);
		List<IndentedLine> codeLines = new(lines.Count);
		foreach (var line in lines)
		{
			codeLines.Add(SplitTabs(line));
		}

		var i = 0;
		var newLine = Parse(codeLines, ref i);
		return i == codeLines.Count ? newLine : throw new InvalidOperationException("Multiple objects in file.");
	}
	#endregion

	#region Public Methods
	public string BuildString()
	{
		var sb = new StringBuilder();
		sb
			.Append(this.Name)
			.Append(' ');

		if (this.Value == null)
		{
			sb.Append(this.Count == 0
				? "(declaration)"
				: "(object)");
		}
		else
		{
			sb
				.Append("= ")
				.Append(this.Value);
		}

		return sb.ToString();
	}
	#endregion

	#region Public Override Methods
	public override string? ToString() => this.BuildString();
	#endregion

	#region Private Static Methods
	private static BladesCodeLine Parse(List<IndentedLine> lines, ref int i)
	{
		var parentLine = lines[i];
		BladesCodeLine retval = new(parentLine.Text);
		i++;
		while (i < lines.Count && lines[i].Level == parentLine.Level + 1)
		{
			var text = lines[i].Text;
			if (text.StartsWith('[') && text.EndsWith(']'))
			{
				var value = int.Parse(text[1..^1], CultureInfo.InvariantCulture);
				if (value != retval.Count)
				{
					throw new InvalidOperationException("Non-linear array.");
				}

				i++;
			}

			var newLine = Parse(lines, ref i);
			if (newLine.Name.OrdinalEquals("data"))
			{
				newLine.Name = "data" + text;
			}

			retval.Add(newLine);
		}

		return retval;
	}

	private static IndentedLine SplitTabs(string line)
	{
		var tabs = 0;
		while (line[tabs] == '\t')
		{
			tabs++;
		}

		return new IndentedLine(tabs, line[tabs..]);
	}
	#endregion

	#region Private Structs
	private readonly struct IndentedLine(int level, string text)
	{
		#region Public Properties
		public int Level { get; } = level;

		public string Text { get; } = text;
		#endregion

		#region Public Override Methods
		public override string ToString() => $"{this.Level.ToStringInvariant()}, {this.Text}";
		#endregion
	}
	#endregion
}