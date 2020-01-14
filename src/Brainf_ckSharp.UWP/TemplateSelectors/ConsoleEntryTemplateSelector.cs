﻿using System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Brainf_ckSharp.Uwp.Models.Console;

#nullable enable

namespace Brainf_ckSharp.Uwp.TemplateSelectors
{
    /// <summary>
    /// A template selector for console entries
    /// </summary>
    public sealed class ConsoleEntryTemplateSelector : DataTemplateSelector
    {
        /// <summary>
        /// Gets or sets the <see cref="DataTemplate"/> for console commands
        /// </summary>
        public DataTemplate? CommandTemplate { get; set; }

        /// <summary>
        /// Gets or sets the <see cref="DataTemplate"/> for console textual results
        /// </summary>
        public DataTemplate? ResultTemplate { get; set; }

        /// <summary>
        /// Gets or sets the <see cref="DataTemplate"/> for console syntax errors
        /// </summary>
        public DataTemplate? SyntaxErrorTemplate { get; set; }

        /// <summary>
        /// Gets or sets the <see cref="DataTemplate"/> for console exceptions
        /// </summary>
        public DataTemplate? ExceptionTemplate { get; set; }

        /// <summary>
        /// Gets or sets the <see cref="DataTemplate"/> for console restart requests
        /// </summary>
        public DataTemplate? RestartTemplate { get; set; }

        /// <inheritdoc/>
        protected override DataTemplate SelectTemplateCore(object item, DependencyObject container)
        {
            return item switch
            {
                ConsoleCommand _ => CommandTemplate,
                ConsoleResult _ => ResultTemplate,
                ConsoleSyntaxError _ => SyntaxErrorTemplate,
                ConsoleException _ => ExceptionTemplate,
                ConsoleRestart _ => RestartTemplate,
                null => throw new ArgumentNullException(nameof(item), "The input item can't be null"),
                _ => throw new ArgumentException($"Unsupported item of type {item.GetType()}")
            } ?? throw new ArgumentException($"Missing template for item of type {item.GetType()}");
        }
    }
}
