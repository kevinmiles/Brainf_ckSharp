﻿using System.Collections.Generic;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Brainf_ck_sharp.Legacy.UWP.DataModels;
using Brainf_ck_sharp.Legacy.UWP.DataModels.Misc;
using Brainf_ck_sharp.Legacy.UWP.Helpers.Extensions;
using UICompositionAnimations.Helpers.PointerEvents;

namespace Brainf_ck_sharp.Legacy.UWP.UserControls.DataTemplates.JumpList.Changelog
{
    public sealed partial class ChangelogZoomedInHeaderTemplate : UserControl
    {
        public ChangelogZoomedInHeaderTemplate()
        {
            this.InitializeComponent();
            this.ManageControlPointerStates((s, e) =>
            {
                VisualStateManager.GoToState(this, e ? "Highlight" : "Default", false);
            });
            this.DataContextChanged += (s, e) =>
            {
                if (e.NewValue != null && _LastDataContext != e.NewValue)
                {
                    _LastDataContext = e.NewValue;
                    this.Bindings.Update();
                }
            };
            this.Unloaded += (s, e) =>
            {
                _LastDataContext = null;
                this.DataContext = null;
                this.Bindings.StopTracking();
            };
        }

        // Private field to skip repeated calls of Bindings.Update()
        private object _LastDataContext;

        public JumpListGroup<ChangelogReleaseInfo, IReadOnlyList<string>> ViewModel => this.DataContext.To<JumpListGroup<ChangelogReleaseInfo, IReadOnlyList<string>>>();
    }
}