using System;
using System.Windows;

namespace AgenticColorCreator.Tests;

/// <summary>
/// Shared helper for WPF-dependent tests. Ensures exactly one <see cref="Application"/> exists in the
/// AppDomain (WPF forbids more than one) and that the app's style dictionary is merged once, even when
/// multiple WPF test classes run in parallel on separate STA threads.
/// </summary>
internal static class WpfTestApplication
{
	private const string StylesUri = "pack://application:,,,/AgenticColorCreator.App;component/CFStyles/CFDarkStyles.xaml";

	private static readonly object Gate = new object();

	public static void Ensure()
	{
		lock (Gate)
		{
			if (Application.Current == null)
			{
				_ = new Application
				{
					ShutdownMode = ShutdownMode.OnExplicitShutdown,
				};
			}

			var appResources = Application.Current!.Resources;
			foreach (var dict in appResources.MergedDictionaries)
			{
				if (dict.Source != null && string.Equals(dict.Source.ToString(), StylesUri, StringComparison.OrdinalIgnoreCase))
				{
					return;
				}
			}

			appResources.MergedDictionaries.Add(new ResourceDictionary
			{
				Source = new Uri(StylesUri, UriKind.Absolute),
			});
		}
	}
}
