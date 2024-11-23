namespace RobinHood70.HoodBot.Controls;

using System;
using System.Windows.Controls;

// Taken from https://stackoverflow.com/a/21755059/502255
public class ScrollingTextBoxControl : TextBox
{
	protected override void OnInitialized(EventArgs e)
	{
		base.OnInitialized(e);
		this.VerticalScrollBarVisibility = ScrollBarVisibility.Auto;
		this.HorizontalScrollBarVisibility = ScrollBarVisibility.Auto;
	}

	protected override void OnTextChanged(TextChangedEventArgs e)
	{
		base.OnTextChanged(e);
		this.CaretIndex = this.Text.Length;
		this.ScrollToEnd();
	}
}