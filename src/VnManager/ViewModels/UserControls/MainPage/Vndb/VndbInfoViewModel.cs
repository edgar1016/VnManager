﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using AdysTech.CredentialManager;
using LiteDB;
using MahApps.Metro.IconPacks;
using Stylet;
using VndbSharp.Models.Common;
using VnManager.Helpers;
using VnManager.Models.Db.User;
using VnManager.Models.Db.Vndb.Main;
using VnManager.Models.Db;
using VnManager.Models.Db.Vndb.TagTrait;
using VnManager.Models.Settings;
using Brush = System.Windows.Media.Brush;
using Color = System.Windows.Media.Color;
using ColorConverter = System.Windows.Media.ColorConverter;

namespace VnManager.ViewModels.UserControls.MainPage.Vndb
{
    public class VndbInfoViewModel : Screen
    {
        #region Binding Properties
        public BitmapSource CoverImage { get; set; }
        public string Title { get; set; }
        public string MainTitle { get; set; }
        public string JpnTitle { get; set; }
        public string Aliases { get; set; }
        public string ReleasedDate { get; set; }
        public string VnLength { get; set; }
        public string Popularity { get; set; }
        public string Rating { get; set; }
        public BindableCollection<BitmapSource> LanguageCollection { get; set; } = new BindableCollection<BitmapSource>();
        public BindableCollection<TagTraitBinding> TagBinding { get; set; } = new BindableCollection<TagTraitBinding>();

        public Brush ZZZ { get; set; }
        
        public List<Inline> DescriptionInLine { get; set; }

        public string LastPlayed { get; set; }
        public string PlayTime { get; set; }

        #endregion

        #region Relation Binding

        public BindableCollection<VnRelationsBinding> VnRelations { get; set; } = new BindableCollection<VnRelationsBinding>();

        #endregion

        protected override void OnViewLoaded()
        {
            LoadMainData();
            LoadUserData();
            LoadRelations();
            GetTags();
        }

        private void LoadMainData()
        {
            if (VndbContentViewModel.VnId == 0) return;
            var cred = CredentialManager.GetCredentials(App.CredDb);
            if (cred == null || cred.UserName.Length < 1) return;
            using (var db = new LiteDatabase($"{App.GetDbStringWithoutPass}{cred.Password}"))
            {
                var vnInfoEntry = db.GetCollection<VnInfo>(DbVnInfo.VnInfo.ToString()).Query().Where(x => x.VnId == VndbContentViewModel.VnId).FirstOrDefault();
                Title = vnInfoEntry.Title;
                MainTitle = $"Title: {vnInfoEntry.Title}";
                JpnTitle = $"Original Title: {vnInfoEntry.Original}";
                Aliases = $"Aliases: {vnInfoEntry.Aliases}";
                ReleasedDate = $"Released: {TimeDateChanger.GetHumanDate(DateTime.Parse(vnInfoEntry.Released, CultureInfo.InvariantCulture))}";
                VnLength = $"Length: {vnInfoEntry.Length}";
                Popularity = vnInfoEntry.Popularity.ToString();//make a UI use this double?
                Rating = vnInfoEntry.Rating.ToString(CultureInfo.InvariantCulture);
                LoadLanguages(ref vnInfoEntry);
                var coverPath = $@"{App.AssetDirPath}\sources\vndb\images\cover\{Path.GetFileName(vnInfoEntry.ImageLink)}";
                CoverImage = ImageHelper.CreateBitmapFromPath(coverPath);

                DescriptionInLine = BBCodeHelper.Helper(vnInfoEntry.Description);

            }
        }

        private void LoadUserData()
        {
            var cred = CredentialManager.GetCredentials(App.CredDb);
            if (cred == null || cred.UserName.Length < 1) return;
            using var db = new LiteDatabase($"{App.GetDbStringWithoutPass}{cred.Password}");
            var dbUserData = db.GetCollection<UserDataGames>(DbUserData.UserData_Games.ToString()).Query()
                .Where(x => x.Id == VndbContentViewModel.UserDataId).FirstOrDefault();
            if (dbUserData != null)
            {
                LastPlayed = TimeDateChanger.GetHumanDate(dbUserData.LastPlayed);
                PlayTime = TimeDateChanger.GetHumanTime(dbUserData.PlayTime);
            }
        }


        private void GetTags()
        {
            List<VnInfoTags> tagList;
            List<VnTagData> tagDump;

            var cred = CredentialManager.GetCredentials(App.CredDb);
            if (cred == null || cred.UserName.Length < 1) return;
            using (var db = new LiteDatabase($"{App.GetDbStringWithoutPass}{cred.Password}"))
            {
                
                tagList = db.GetCollection<VnInfoTags>(DbVnInfo.VnInfo_Tags.ToString()).Query()
                    .Where(x => x.VnId == VndbContentViewModel.VnId).ToList();
                tagDump = db.GetCollection<VnTagData>(DbVnDump.VnDump_TagData.ToString()).Query().ToList();

                tagList = tagList.Where(t => t.Spoiler <= App.UserSettings.SettingsVndb.Spoiler).ToList();
                tagList = tagList.OrderByDescending(x => x.Spoiler).ToList();
                
            }

            var tagBindingList = new List<TagTraitBinding>();

            var tagsWithParent = GetParentTags(tagList, tagDump).ToList();
            tagsWithParent.RemoveAll(x => x.Parent.Contains("Sexual") && App.UserSettings.MaxSexualRating < SexualRating.Explicit);

            var noSpoilerTags = tagsWithParent.Where(x => x.Spoiler == SpoilerLevel.None).ToList();
            var minorSpoilerTags = tagsWithParent.Where(x => x.Spoiler == SpoilerLevel.Minor).ToList();
            var majorSpoilerTags = tagsWithParent.Where(x => x.Spoiler == SpoilerLevel.Major).ToList();

            var sexualTags = tagsWithParent.Where(x => x.Parent.Contains("Sexual")).ToList();


            var tempList = (from tag in noSpoilerTags let colorText = Colors.WhiteSmoke.ToString() select (tag.Parent, tag.Child, colorText)).ToList();
            tempList.AddRange(from tag in minorSpoilerTags let colorText = Colors.Gold.ToString() select (tag.Parent, tag.Child, colorText));
            tempList.AddRange(from tag in majorSpoilerTags let colorText = Colors.OrangeRed.ToString() select (tag.Parent, tag.Child, colorText));
            tempList.AddRange(from tag in sexualTags let colorText = Colors.HotPink.ToString() select (tag.Parent, tag.Child, colorText));


            foreach (var group in tempList.GroupBy(x => x.Parent))
            {
                var tuple = group.Select(tag => new Tuple<string, string>(tag.Child, tag.colorText)).ToList();
                var ttb = new TagTraitBinding() { Parent = group.Key, Children = tuple };
                tagBindingList.Add(ttb);
            }
            
            TagBinding.AddRange(tagBindingList);
            
        }
        
        private static List<(string Parent, string Child, SpoilerLevel Spoiler)> GetParentTags(List<VnInfoTags> tagList, List<VnTagData> tagDump)
        {
            List<(string Parent, string Child, SpoilerLevel Spoiler)> tagInfoList = new List<(string Parent, string Child, SpoilerLevel Spoiler)>();
            foreach (var tag in tagList)
            {
                var tagName = tagDump.FirstOrDefault(x => x.TagId == tag.TagId)?.Name;
                if(string.IsNullOrEmpty(tagName))continue;
                var tagData = tagDump.FirstOrDefault(x => x.TagId == tag.TagId);
                while (tagData != null && tagData.Parents.Length > 0)
                {
                    tagData = tagDump.FirstOrDefault(x => x.TagId == tagData.Parents.Last());
                }
                var parentTag = tagData?.Name;
                if (parentTag == tagName)
                {
                    parentTag = null;
                }
                tagInfoList.Add((parentTag, tagName, tag.Spoiler));
            }
            return tagInfoList;
        }
        
        
        
        private void LoadRelations()
        {
            if (VndbContentViewModel.VnId == 0) return;
            var cred = CredentialManager.GetCredentials(App.CredDb);
            if (cred == null || cred.UserName.Length < 1) return;
            using (var db = new LiteDatabase($"{App.GetDbStringWithoutPass}{cred.Password}"))
            {
                var vnRelations = db.GetCollection<VnInfoRelations>(DbVnInfo.VnInfo_Relations.ToString()).Query()
                    .Where(x => x.VnId == VndbContentViewModel.VnId && x.Official.ToUpper(CultureInfo.InvariantCulture) == "YES").ToList();
                foreach (var relation in vnRelations)
                {
                    var entry = new VnRelationsBinding { RelTitle = relation.Title, RelRelation = relation.Relation };
                    VnRelations.Add(entry);
                }
            }
        }

        

        private void LoadLanguages(ref VnInfo vnInfoEntry)
        {
            foreach (var language in GetLanguages(vnInfoEntry.Languages))
            {
                LanguageCollection.Add(new BitmapImage(new Uri(language)));
            }
        }

        private static IEnumerable<string> GetLanguages(string csv)
        {
            string[] list = csv.Split(',');
            return list.Select(lang => File.Exists($@"{App.ExecutableDirPath}\Resources\flags\{lang}.png")
                    ? $@"{App.ExecutableDirPath}\Resources\flags\{lang}.png"
                    : $@"{App.ExecutableDirPath}\Resources\flags\_unknown.png")
                .ToList();
        }


        public class VnRelationsBinding
        {
            public string RelTitle { get; set; }
            public string RelRelation { get; set; }
        }

    }
}
