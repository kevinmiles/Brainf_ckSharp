﻿using System;
using Windows.Devices.Input;
using Windows.UI.Xaml.Controls;
using Brainf_ck_sharp.Legacy.UWP.DataModels;
using Brainf_ck_sharp.Legacy.UWP.DataModels.Misc;
using Brainf_ck_sharp.Legacy.UWP.Helpers.Extensions;
using Brainf_ck_sharp.Legacy.UWP.Messages.IDE;
using Brainf_ck_sharp.Legacy.UWP.PopupService.Interfaces;
using Brainf_ck_sharp.Legacy.UWP.ViewModels.FlyoutsViewModels;
using GalaSoft.MvvmLight.Messaging;

namespace Brainf_ck_sharp.Legacy.UWP.UserControls.Flyouts.SnippetsMenu
{
    public sealed partial class TouchCodeSnippetsBrowserFlyout : UserControl, IEventConfirmedContent
    {
        public TouchCodeSnippetsBrowserFlyout()
        {
            this.InitializeComponent();
            this.DataContext = new CodeSnippetsBrowserViewModel();
            Unloaded += (s, e) =>
            {
                this.Bindings.StopTracking();
                DataContext = null;
                ContentConfirmed = null;
            };
        }

        public CodeSnippetsBrowserViewModel ViewModel => this.DataContext.To<CodeSnippetsBrowserViewModel>();

        // Raises the ContentConfirmed event when the user clicks on a code snippet
        private void ListViewBase_OnItemClick(object sender, ItemClickEventArgs e)
        {
            if (e.ClickedItem is IndexedModelWithValue<CodeSnippet> code)
            {
                Messenger.Default.Send(new CodeSnippetSelectedMessage(code.Value, PointerDeviceType.Touch));
                ContentConfirmed?.Invoke(this, EventArgs.Empty);
            }
        }

        /// <inheritdoc cref="IEventConfirmedContent"/>
        public event EventHandler ContentConfirmed;
    }
}