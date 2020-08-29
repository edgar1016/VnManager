﻿using Stylet;
using StyletIoC;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using VndbSharp.Models.Common;
using System.Xml;
using System.Xml.Serialization;
using VnManager.Models.Settings;
using System.IO;
using VnManager.Helpers;
using System.Linq;
using System.Reflection;
using System.Resources;
using System.Threading.Tasks;
using System.Windows;
using AdysTech.CredentialManager;
using LiteDB;
using VnManager.Models.Db.Vndb.Main;
using VnManager.ViewModels.Dialogs;

namespace VnManager.ViewModels.UserControls
{
    public class SettingsViewModel :Screen
    {

        private bool _didChangeNsfwContentVisible = false;

        private bool _nsfwSavedContentVisible;
        public bool NsfwContentSavedVisible
        {
            get => _nsfwSavedContentVisible;
            set
            {
                _nsfwSavedContentVisible = value;
                SetAndNotify(ref _nsfwSavedContentVisible, value);
                _didChangeNsfwContentVisible = true;
            }
        }

        #region SpoilerList
        
        public int SpoilerIndex { get; set; } = 0;
        public int MaxSexualIndex { get; set; }
        public int MaxViolenceIndex { get; set; }
        #endregion


        private readonly IContainer _container;
        private readonly IWindowManager _windowManager;
        public SettingsViewModel(IContainer container, IWindowManager windowManager)
        {
            _container = container;
            _windowManager = windowManager;
            //NsfwEnabled = App.UserSettings.IsNsfwEnabled;
            NsfwContentSavedVisible = App.UserSettings.IsVisibleSavedNsfwContent;
            FillDropdown();
            
        }

        private void FillDropdown()
        {
            if (App.UserSettings.SettingsVndb != null)
            {
               
                switch (App.UserSettings.MaxSexualRating)
                {
                    case SexualRating.Safe:
                        MaxSexualIndex = 0;
                        break;
                    case SexualRating.Suggestive:
                        MaxSexualIndex = 1;
                        break;
                    case SexualRating.Explicit:
                        MaxSexualIndex = 1;
                        break;
                    default:
                        MaxSexualIndex = 0;
                        throw new ArgumentOutOfRangeException();
                }
                
                switch (App.UserSettings.MaxViolenceRating)
                {
                    case ViolenceRating.Tame:
                        MaxViolenceIndex = 0;
                        break;
                    case ViolenceRating.Violent:
                        MaxViolenceIndex = 1;
                        break;
                    case ViolenceRating.Brutal:
                        MaxViolenceIndex = 2;
                        break;
                    default:
                        MaxViolenceIndex = 0;
                        throw new ArgumentOutOfRangeException();
                }
                
                switch (App.UserSettings.SettingsVndb.Spoiler)
                {
                    case SpoilerLevel.None:
                        SpoilerIndex = 0;
                        break;
                    case SpoilerLevel.Minor:
                        SpoilerIndex = 1;
                        break;
                    case SpoilerLevel.Major:
                        SpoilerIndex = 2;
                        break;
                    default:
                        SpoilerIndex = 0;
                        throw new ArgumentOutOfRangeException();
                }

            }
        }

        public void SaveUserSettings(bool useEncryption)
        {
            if (_didChangeNsfwContentVisible == true)
            {
                var message = App.ResMan.GetString("ChangeNsfwVisibilityMessage")
                    ?.Replace(@"\n", Environment.NewLine);
                var result = _windowManager.ShowMessageBox($"{message}",
                    App.ResMan.GetString("ChangeNsfwVisibilityTitle"), MessageBoxButton.YesNo);
                if (result == MessageBoxResult.Yes)
                {
                    DeleteNsfwImages();
                }
                else
                {
                    return;
                }

            }


            UserSettingsVndb vndb = new UserSettingsVndb
            {
                Spoiler = (SpoilerLevel)SpoilerIndex
            };
            UserSettings settings = new UserSettings
            {
                IsVisibleSavedNsfwContent = NsfwContentSavedVisible,
                SettingsVndb = vndb,
                EncryptionEnabled = useEncryption,
                MaxSexualRating = (SexualRating)MaxSexualIndex,
                MaxViolenceRating = (ViolenceRating)MaxViolenceIndex
            };

            try
            {
                UserSettingsHelper.SaveUserSettings(settings);
                App.UserSettings = settings;

                _windowManager.ShowMessageBox(App.ResMan.GetString("SettingsSavedMessage"), App.ResMan.GetString("SettingsSavedTitle"));
            }
            catch (Exception ex)
            {
                App.Logger.Error(ex, "Couldn't write to config file");
                throw;
            }
        }

        public void LoadUserSettings()
        {
            try
            {
                var settings = UserSettingsHelper.ReadUserSettings();
                App.UserSettings = settings;
                
            }
            catch (Exception ex)
            {
                App.Logger.Error(ex, "Couldn't load config file");
                throw;
            }
        }


        public void DeleteNsfwImages()
        {
            //Use CheckWriteAccess to see if you can delete from the images
            var cred = CredentialManager.GetCredentials(App.CredDb);
            if (cred == null || cred.UserName.Length < 1) return;
            using (var db = new LiteDatabase($"{App.GetDbStringWithoutPass}{cred.Password}"))
            {
                List<VnInfoScreens> vnScreens = db.GetCollection<VnInfoScreens>("VnInfo_Screens").Query().Where(x => NsfwHelper.IsNsfw(x.ImageRating) == true).ToList();
                List<VnInfo> vnCovers = db.GetCollection<VnInfo>("VnInfo").Query().Where(x => NsfwHelper.IsNsfw(x.ImageRating) == true).ToList();

                ResetNsfwScreenshots(vnScreens);
                ResetNsfwCoverImages(vnCovers);
            }

        }

        private void ResetNsfwScreenshots(List<VnInfoScreens> vnScreens)
        {
            foreach (var screen in vnScreens)
            {
                var directory = Path.Combine(App.AssetDirPath, @$"sources\vndb\images\screenshots\{screen.VnId}");
                var imageFile = $@"{directory}\{Path.GetFileName(screen.ImageUrl)}";
                var thumbFile = $@"{directory}\thumbs\{Path.GetFileName(screen.ImageUrl)}";

                if (App.UserSettings.IsVisibleSavedNsfwContent == false)
                {
                    if (File.Exists(imageFile) && File.Exists(thumbFile))
                    {
                        Secure.EncFile(imageFile);

                        Secure.EncFile(thumbFile);
                    }
                }
                else
                {
                    if (File.Exists($"{imageFile}.aes") && File.Exists($"{thumbFile}.aes"))
                    {
                        Secure.DecFile(imageFile);

                        Secure.DecFile(thumbFile);
                    }

                }
            }
        }

        private void ResetNsfwCoverImages(List<VnInfo> vnCovers)
        {
            foreach (var cover in vnCovers)
            {
                var directory = Path.Combine(App.AssetDirPath, @"sources\vndb\images\cover");
                var imageFile = $@"{directory}\{Path.GetFileName(cover.ImageLink)}";
                if (App.UserSettings.IsVisibleSavedNsfwContent == false)
                {
                    if (File.Exists(imageFile))
                    {
                        Secure.EncFile(imageFile);
                    }
                }
                else
                {
                    if (File.Exists(imageFile))
                    {
                        Secure.DecFile(imageFile);
                    }
                }

            }
        }




        public void ResetApplication()
        {
            var warning = _container.Get<DeleteEverythingViewModel>();
            bool? result = _windowManager.ShowDialog(warning);
            switch (result)
            {
                case null:
                    return;
                case true:
                {
                    if (App.AssetDirPath.Equals(App.ConfigDirPath))
                    {
                        Directory.Delete(App.AssetDirPath, true);
                    }
                    else
                    {
                        Directory.Delete(App.AssetDirPath, true);
                        Directory.Delete(App.ConfigDirPath, true);
                    }
                    CredentialManager.RemoveCredentials(App.CredDb);
                    CredentialManager.RemoveCredentials(App.CredFile);
                    Environment.Exit(0);
                    break;
                }
                default:
                    return;
            }
        }


    }

}
