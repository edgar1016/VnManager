﻿// Copyright (c) micah686. All rights reserved.
// Licensed under the MIT License. See LICENSE file in the project root for full license information.

using System;
using Stylet;
using System.Globalization;
using System.Windows;
using System.Windows.Media;
using VnManager.ViewModels.Dialogs;
using VnManager.ViewModels.UserControls;
using VnManager.ViewModels.Windows;

namespace VnManager.ViewModels
{
    
    public class RootViewModel : Conductor<IScreen>, INavigationControllerDelegate
    {
        public static StatusBarViewModel StatusBarPage { get; set; }

        private readonly IWindowManager _windowManager;
        private readonly Func<MainGridViewModel> _mainGridVmFactory;
        private readonly Func<SettingsViewModel> _settingsVmFactory;
        private readonly Func<AboutViewModel> _aboutVmFactory;
        private readonly Func<ImportViewModel> _importVm;
        private readonly Func<SetEnterPasswordViewModel> _enterPassVm;

        private int _windowButtonPressedCounter = 0;

        public static string WindowTitle => FormatWindowTitle();

        #region SettingsPressed
        private bool _isSettingsPressed;
        public bool IsSettingsPressed
        {
            get => _isSettingsPressed;
            set
            {
                if (_windowButtonPressedCounter == 0 || _windowButtonPressedCounter > 0 && !value)
                {
                    SetAndNotify(ref _isSettingsPressed, value);
                    if (_isSettingsPressed)
                    {
                        _windowButtonPressedCounter += 1;
                        SettingsIconColor = Brushes.LimeGreen;
                        ActivateSettingsClick();
                    }
                    else
                    {
                        _windowButtonPressedCounter -= 1;
                        var result = Application.Current.TryFindResource(AdonisUI.Colors.ForegroundColor);
                        SettingsIconColor = result == null ? Brushes.LightSteelBlue : new SolidColorBrush((Color)result);
                        ActivateMainClick();
                    }
                }

            }
        }

        public System.Windows.Media.Brush SettingsIconColor { get; set; }
        #endregion

        #region DebugPressed
        private bool _debugPressed;
        public bool DebugPressed
        {
            get => _debugPressed;
            set
            {
                if (_windowButtonPressedCounter == 0 || _windowButtonPressedCounter > 0 && !value)
                {
                    SetAndNotify(ref _debugPressed, value);
                    if (_debugPressed)
                    {
                        _windowButtonPressedCounter += 1;
                        DebugClick();
                    }
                    else
                    {
                        _windowButtonPressedCounter -= 1;
                        ActivateMainClick();
                    }
                }
            }
        }

        #endregion

        #region AboutPressed
        private bool _aboutPressed;
        public bool AboutPressed
        {
            get => _aboutPressed;
            set
            {
                if (_windowButtonPressedCounter == 0 || _windowButtonPressedCounter > 0 && !value)
                {
                    SetAndNotify(ref _aboutPressed, value);
                    if (_aboutPressed)
                    {
                        _windowButtonPressedCounter += 1;
                        ActivateAboutClick();
                    }
                    else
                    {
                        _windowButtonPressedCounter -= 1;
                        ActivateMainClick();
                    }
                }
            }
        }
        #endregion



#if DEBUG
        private readonly Func<DebugViewModel> _debugVmFactory;
        public RootViewModel(IWindowManager windowManager, Func<MainGridViewModel> mainGridFactory, Func<SettingsViewModel> settingsFactory, 
            Func<ImportViewModel> import, Func<SetEnterPasswordViewModel> enterPass, Func<AboutViewModel> about, Func<DebugViewModel> debugFactory)
        {
            _windowManager = windowManager;
            _mainGridVmFactory = mainGridFactory;
            _settingsVmFactory = settingsFactory;
            _aboutVmFactory = about;

            _importVm = import;
            _enterPassVm = enterPass;
            _debugVmFactory = debugFactory;
            
            StatusBarPage = new StatusBarViewModel();
        }
#else
        public RootViewModel(IWindowManager windowManager, Func<MainGridViewModel> mainGridFactory, Func<SettingsViewModel> settingsFactory,
            Func<ImportViewModel> import, Func<SetEnterPasswordViewModel> enterPass, Func<AboutViewModel> about)
        {
            _windowManager = windowManager;
            _mainGridVmFactory = mainGridFactory;
            _settingsVmFactory = settingsFactory;
            _aboutVmFactory = about;

            _importVm = import;
            _enterPassVm = enterPass;

            StatusBarPage = new StatusBarViewModel();
        }
#endif



        protected override void OnViewLoaded()
        {
            _ = new Initializers.Startup(_windowManager, _importVm, _enterPassVm);
            var mainGridVm = _mainGridVmFactory();
            ActivateItem(mainGridVm);
            var result = Application.Current.TryFindResource(AdonisUI.Colors.ForegroundColor);
            SettingsIconColor = result == null ? System.Windows.Media.Brushes.LightSteelBlue : new SolidColorBrush((System.Windows.Media.Color)result);
        }

        /// <summary>
        /// Creates a formatted window title
        /// </summary>
        /// <returns></returns>
        private static string FormatWindowTitle()
        {
            var appName = App.ResMan.GetString("ApplicationTitle", CultureInfo.InvariantCulture);
            var appVersion = App.VersionString;

            var formatted = string.Format(CultureInfo.InvariantCulture, $"{appName} {appVersion}");
            return formatted;
        }

        
        public void ActivateSettingsClick()
        {
            ActivateItem(_settingsVmFactory());
        }

        public void ActivateAboutClick()
        {
            _windowManager.ShowDialog(_aboutVmFactory());
        }
        
        public void ActivateMainClick()
        {
            ActivateItem(_mainGridVmFactory());
        }


        public void DebugClick()
        {
#if DEBUG
            ActivateItem(_debugVmFactory());
#else
            //Do Nothing
#endif
        }

        public void NavigateTo(IScreen screen)
        {
            this.ActivateItem(screen);
        }
    }
}
