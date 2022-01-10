﻿namespace RobinHood70.WikiCommon.Parser.Basic
{
	using System;
	using System.Diagnostics.CodeAnalysis;
	using RobinHood70.CommonCode;
	using RobinHood70.WikiCommon.Parser;

	/// <summary>Represents a wikitext (HTML) comment.</summary>
	public class CommentNode : ICommentNode
	{
		#region Fields
		private string comment = string.Empty;
		#endregion

		#region Constructors

		/// <summary>Initializes a new instance of the <see cref="CommentNode"/> class.</summary>
		/// <param name="comment">The comment.</param>
		public CommentNode(string comment)
		{
			this.Comment = comment;
		}
		#endregion

		#region Public Properties

		/// <inheritdoc/>
		[AllowNull]
		public string Comment
		{
			get => this.comment;
			set
			{
				value ??= string.Empty;
				if (!string.Equals(this.comment, value, StringComparison.Ordinal))
				{
					this.comment = value;
				}
			}
		}
		#endregion

		#region Public Methods

		/// <summary>Accepts a visitor to process the node.</summary>
		/// <param name="visitor">The visiting class.</param>
		public void Accept(IWikiNodeVisitor visitor) => visitor?.Visit(this);
		#endregion

		#region Public Override Methods

		/// <summary>Returns a <see cref="string"/> that represents this instance.</summary>
		/// <returns>A <see cref="string"/> that represents this instance.</returns>
		public override string ToString() => "<!--" + this.Comment.Ellipsis(20) + "-->";
		#endregion
	}
}
