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
using MvvmDialogs;
using MvvmDialogs.FrameworkDialogs.FolderBrowser;
using VnManager.Models.Db;
using VnManager.Models.Db.User;
using VnManager.Models.Db.Vndb.Main;
using VnManager.ViewModels.Dialogs;

namespace VnManager.ViewModels.UserControls
{
    public class SettingsViewModel :Screen
    {

        
        public int SpoilerIndex { get; set; } = 0;
        public int MaxSexualIndex { get; set; }
        public int MaxViolenceIndex { get; set; }


        private readonly IContainer _container;
        private readonly IWindowManager _windowManager;
        private readonly IDialogService _dialogService;
        public SettingsViewModel(IContainer container, IWindowManager windowManager, IDialogService dialogService)
        {
            _container = container;
            _windowManager = windowManager;
            _dialogService = dialogService;
            FillSexualDropdown();
            FillViolenceDropdown();
            FillSpoilerDropdown();
        }

        private void FillSexualDropdown()
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
                        MaxSexualIndex = 2;
                        break;
                    default:
                        MaxSexualIndex = 0;
                        _windowManager.ShowMessageBox(App.ResMan.GetString("UserSettingsInvalid"), App.ResMan.GetString("Error"));
                        App.Logger.Warning("UserSetting Sexual Rating is invalid");
                        break;
                }
            }
        }

        private void FillViolenceDropdown()
        {
            if (App.UserSettings.SettingsVndb != null)
            {
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
                        _windowManager.ShowMessageBox(App.ResMan.GetString("UserSettingsInvalid"), App.ResMan.GetString("Error"));
                        App.Logger.Warning("UserSetting Violence Rating is invalid");
                        break;
                }
            }
        }

        private void FillSpoilerDropdown()
        {
            if (App.UserSettings.SettingsVndb != null)
            {
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
                        _windowManager.ShowMessageBox(App.ResMan.GetString("UserSettingsInvalid"), App.ResMan.GetString("Error"));
                        App.Logger.Warning("UserSetting Spoiler Level is invalid");
                        break;
                }
            }
        }

        public void SaveUserSettings(bool useEncryption)
        {

            UserSettingsVndb vndb = new UserSettingsVndb
            {
                Spoiler = (SpoilerLevel)SpoilerIndex
            };
            UserSettings settings = new UserSettings
            {
                SettingsVndb = vndb,
                RequirePasswordEntry = useEncryption,
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

        public static void LoadUserSettings()
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


        //TODO: move this to a better area, like maybe an information page?
        public void ExportUserData()
        {
            string savePath;
            var settings = new FolderBrowserDialogSettings();
            bool? result = _dialogService.ShowFolderBrowserDialog(this, settings);
            if (result == true)
            {
                savePath = settings.SelectedPath;
            }
            else
            {
                return;
            }
            var fileName = $@"{savePath}\VnManager_Export_{DateTime.UtcNow:yyyy-MMMM-dd}.db";
            var cred = CredentialManager.GetCredentials(App.CredDb);
            if (cred == null || cred.UserName.Length < 1) return;
            using (var db = new LiteDatabase($"{App.GetDbStringWithoutPass}{cred.Password}"))
            {
                var dbUserData = db.GetCollection<UserDataGames>(DbUserData.UserData_Games.ToString()).FindAll();

                using (var exportDatabase = new LiteDatabase(fileName))
                {
                    var exportUserData = exportDatabase.GetCollection<UserDataGames>(DbUserData.UserData_Games.ToString());

                    List<UserDataGames> userDataList = dbUserData.Select(item => new UserDataGames
                        {
                            Id = item.Id,
                            GameId = item.GameId,
                            GameName = item.GameName,
                            SourceType = item.SourceType,
                            LastPlayed = item.LastPlayed,
                            PlayTime = item.PlayTime,
                            Categories = item.Categories,
                            ExePath = item.ExePath,
                            IconPath = item.IconPath,
                            Arguments = item.Arguments
                        })
                        .ToList();

                    exportUserData.Insert(userDataList);
                    _windowManager.ShowMessageBox($"{App.ResMan.GetString("UserDataExportedPath")}\n{fileName}", $"{App.ResMan.GetString("UserDataExportedTitle")}");

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
                    DeleteEverything();
                    break;
                }
                default:
                    return;
            }
        }

        private void DeleteEverything()
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
            _windowManager.ShowMessageBox($"{App.ResMan.GetString("AppExit")}");
            Environment.Exit(0);
        }

    }

}
