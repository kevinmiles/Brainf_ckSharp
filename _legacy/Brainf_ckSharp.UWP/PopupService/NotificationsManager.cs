﻿using System;
using System.Threading;
using System.Threading.Tasks;
using Windows.UI.Xaml.Controls.Primitives;
using Brainf_ck_sharp.Legacy.UWP.Helpers.Extensions;
using Brainf_ck_sharp.Legacy.UWP.Helpers.WindowsAPIs;
using Brainf_ck_sharp.Legacy.UWP.Messages.Flyouts;
using Brainf_ck_sharp.Legacy.UWP.PopupService.Misc;
using Brainf_ck_sharp.Legacy.UWP.PopupService.UI;
using Brainf_ck_sharp.Legacy.UWP.UserControls.InheritedControls;
using GalaSoft.MvvmLight.Messaging;
using JetBrains.Annotations;
using UICompositionAnimations;
using UICompositionAnimations.Enums;
using UICompositionAnimations.Helpers;
using UICompositionAnimations.Lights;

namespace Brainf_ck_sharp.Legacy.UWP.PopupService
{
    /// <summary>
    /// A static class that manages the in-app notifications
    /// </summary>
    public sealed class NotificationsManager
    {
        /// <summary>
        /// Gets the singleton instance to use to manage the notifications
        /// </summary>
        public static NotificationsManager Instance { get; } = new NotificationsManager();

        // Initializes the singleton instance and subscribes to the messages
        private NotificationsManager()
        {
            Messenger.Default.Register<NotificationCloseRequestMessage>(this, m => CloseNotificationPopupAsync().Forget());
        }

        /// <summary>
        /// The Popup that's currently displayed
        /// </summary>
        private Popup _CurrentPopup;

        /// <summary>
        /// Semaphore to avoid race conditions when setting the current Popup
        /// </summary>
        private readonly SemaphoreSlim NotificationSemaphore = new SemaphoreSlim(1);

        #region Public APIs

        /// <summary>
        /// Shows a notification error with the default icon and settings
        /// </summary>
        /// <param name="title">The title of the notification</param>
        /// <param name="content">The content to show in the notification</param>
        public void ShowDefaultErrorNotification([NotNull] string title, [NotNull] string content)
        {
            ShowNotification(0xE7BA.ToSegoeMDL2Icon(), title, content, NotificationType.Error);
        }

        /// <summary>
        /// Shows a custom notification
        /// </summary>
        /// <param name="icon">The icon of the notification</param>
        /// <param name="title">The title of the new notification to show</param>
        /// <param name="content">The content of the notification</param>
        /// <param name="type">The type of notification to show</param>
        /// <param name="duration">The time interval before the nofitication disappears</param>
        public void ShowNotification([NotNull] string icon, [NotNull] string title, [NotNull] string content, NotificationType type, TimeSpan? duration = null)
        {
            DispatcherHelper.RunOnUIThreadAsync(async () =>
            {
                // Set the interval to use
                TimeSpan timespan = duration ?? TimeSpan.FromSeconds(3);

                // Prepare the Popup to show
                Popup popup = new Popup
                {
                    VerticalOffset = 20,
                    HorizontalOffset = ApplicationViewHelper.CurrentWidth - 320
                };

                // Prepare the notification control
                NotificationPopup notificationPopup = new NotificationPopup(title, icon, content, type);
                popup.Child = new PopupDropShadowGrid { ContainedGrid = notificationPopup };

                // Lights setup
                LightsSourceHelper.SetIsLightsContainer(notificationPopup, true);

                // Dispose the lights
                popup.Closed += (s, e) =>
                {
                    // Dispose the lights
                    LightsSourceHelper.SetIsLightsContainer(notificationPopup, false);
                };

                // Close the previous notification, if present
                await CloseNotificationPopupAsync();

                // Wait the semaphore
                await NotificationSemaphore.WaitAsync();

                // Local timer to automatically close the notification
                Task.Delay(timespan).ContinueWith(t => CloseNotificationPopupAsync(popup), TaskScheduler.FromCurrentSynchronizationContext()).Forget();

                // Set the current Popup, show it and start its animation
                _CurrentPopup = popup;
                popup.SetVisualOpacity(0);

                // Manage the closed event
                popup.IsOpen = true;
                popup.StartCompositionFadeSlideAnimation(0, 1, TranslationAxis.X, 20, 0, 200, null, 0, EasingFunctionNames.SineEaseOut);

                // Finally release the semaphore
                NotificationSemaphore.Release();
            });
        }

        #endregion

        /// <summary>
        /// Closes the current notification, if present
        /// </summary>
        private async Task CloseNotificationPopupAsync()
        {
            await NotificationSemaphore.WaitAsync();
            if (_CurrentPopup?.IsOpen == true)
            {
                await _CurrentPopup.StartCompositionFadeSlideAnimationAsync(1, 0, TranslationAxis.Y, 0, 10, 200, null, 0, EasingFunctionNames.SineEaseOut);
                _CurrentPopup.IsOpen = false;
                _CurrentPopup = null;
            }
            NotificationSemaphore.Release();
        }

        /// <summary>
        /// Close a target notification
        /// </summary>
        /// <param name="popup">The Popup that contains the notification to close</param>
        private async Task CloseNotificationPopupAsync([CanBeNull] Popup popup)
        {
            if (popup == null) return;
            await NotificationSemaphore.WaitAsync();
            if ((_CurrentPopup != popup || _CurrentPopup == null) && popup.IsOpen)
            {
                await popup.StartCompositionFadeSlideAnimationAsync(1, 0, TranslationAxis.Y, 0, 10, 200, null, 0, EasingFunctionNames.SineEaseOut);
                popup.IsOpen = false;
                NotificationSemaphore.Release();
                return;
            }
            _CurrentPopup = null;
            await popup.StartCompositionFadeSlideAnimationAsync(1, 0, TranslationAxis.Y, 0, 10, 200, null, 0, EasingFunctionNames.SineEaseOut);
            popup.IsOpen = false;
            NotificationSemaphore.Release();
        }
    }
}