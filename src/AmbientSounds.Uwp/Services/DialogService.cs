﻿using AmbientSounds.Constants;
using AmbientSounds.Controls;
using AmbientSounds.Converters;
using Microsoft.Toolkit.Diagnostics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Windows.ApplicationModel.Resources;
using Windows.UI.Xaml.Controls;

#nullable enable

namespace AmbientSounds.Services.Uwp
{
    /// <summary>
    /// Class for opening dialogs.
    /// </summary>
    public class DialogService : IDialogService
    {
        private readonly IUserSettings _userSettings;

        public DialogService(IUserSettings userSettings)
        {
            Guard.IsNotNull(userSettings, nameof(userSettings));

            _userSettings = userSettings;
        }

        /// <summary>
        /// UWP apps crash if more than one content dialog
        /// is opened at the same time. This flag
        /// will be used to help ensure only one
        /// dialog is open at a time.
        /// </summary>
        public static bool IsDialogOpen;

        /// <inheritdoc/>
        public async Task OpenSettingsAsync()
        {
            if (IsDialogOpen)
                return;

            IsDialogOpen = true;
            var resources = ResourceLoader.GetForCurrentView();
            var dialog = new ContentDialog()
            {
                RequestedTheme = _userSettings.Get<string>(UserSettingsConstants.Theme).ToTheme(),
                Title = resources.GetString("SettingsText"),
                CloseButtonText = resources.GetString("CloseText"),
                Content = new SettingsControl()
            };

            void OnSettingsSet(object sender, string key)
            {
                if (key == UserSettingsConstants.Theme)
                {
                    dialog.RequestedTheme = _userSettings.Get<string>(UserSettingsConstants.Theme).ToTheme();
                }
            }

            _userSettings.SettingSet += OnSettingsSet;
            await dialog.ShowAsync();
            _userSettings.SettingSet -= OnSettingsSet;
            IsDialogOpen = false;

        }

        /// <inheritdoc/>
        public async Task<string> RenameAsync(string currentName)
        {
            if (IsDialogOpen)
                return currentName;

            IsDialogOpen = true;
            var resources = ResourceLoader.GetForCurrentView();
            var inputBoxControl = new RenameInputBox() { Input = currentName };
            bool enterClicked = false;
            var dialog = new ContentDialog()
            {
                Title = resources.GetString("RenameText"),
                CloseButtonText = resources.GetString("CancelText"),
                PrimaryButtonText = resources.GetString("RenameText"),
                Content = inputBoxControl
            };
            inputBoxControl.EnterClicked += (s, e) => { dialog.Hide(); enterClicked = true; };

            var result = await dialog.ShowAsync();
            IsDialogOpen = false;

            return result == ContentDialogResult.Primary || enterClicked ? inputBoxControl.Input : currentName;
        }

        /// <inheritdoc/>
        public async Task<IList<string>> OpenShareResultsAsync(IList<string> soundIds)
        {
            if (IsDialogOpen || soundIds is not { Count: > 0 }) 
                return new List<string>();

            IsDialogOpen = true;
            var resources = ResourceLoader.GetForCurrentView();

            var content = new ShareResultsControl();
            content.LoadSoundsAsync(soundIds);

            var dialog = new ContentDialog()
            {
                Title = resources.GetString("MissingSoundsTitle"),
                CloseButtonText = resources.GetString("CancelText"),
                PrimaryButtonText = resources.GetString("PlayerPlayText"),
                Content = content
            };

            var result = await dialog.ShowAsync();
            IsDialogOpen = false;

            return result == ContentDialogResult.Primary
                ? content.ViewModel.Sounds.Where(static x => x.IsInstalled).Select(static x => x.Id).ToList()
                : new List<string>();
        }
    }
}
