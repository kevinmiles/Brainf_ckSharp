﻿using System;
using Windows.UI.Xaml.Controls;
using Brainf_ck_sharp_UWP.Helpers.Extensions;
using Brainf_ck_sharp_UWP.PopupService.Interfaces;
using Brainf_ck_sharp_UWP.ViewModels;

namespace Brainf_ck_sharp_UWP.UserControls.Flyouts
{
    public sealed partial class IDERunResultFlyout : UserControl, IBusyWorkingContent, IAsyncLoadedContent
    {
        public IDERunResultFlyout()
        {
            this.InitializeComponent();
            DataContext = new IDERunResultFlyoutViewModel();
            ViewModel.LoadingStateChanged += (_, e) =>
            {
                WorkingStateChanged?.Invoke(this, e);
            };
            ViewModel.InitializationCompleted += (s, e) => LoadingCompleted?.Invoke(this, EventArgs.Empty);
            ViewModel.BreakpointOptionsActiveStatusChanged += (_, e) =>
            {
                if (e) ButtonsInStoryboard.Begin();
                else ButtonsOutStoryboard.Begin();
            };
        }

        public IDERunResultFlyoutViewModel ViewModel => DataContext.To<IDERunResultFlyoutViewModel>();

        /// <inheritdoc cref="IBusyWorkingContent"/>
        public event EventHandler<bool> WorkingStateChanged;

        /// <inheritdoc cref="IAsyncLoadedContent"/>
        public event EventHandler LoadingCompleted;
    }
}
