using System;
using System.IO;
using System.Linq;
using IctBaden.Config.Namespace;
using IctBaden.Config.Session;
using IctBaden.Config.Unit;
using Xunit;
// ReSharper disable StringLiteralTypo

namespace IctBaden.Config.Test
{
    public class ProfileTests : IDisposable
    {
        private static readonly string ProfileCfg = TestResources.LoadResourceString("Profile.cfg");
        private static readonly string ProfileName = 
            Path.Combine(Path.GetDirectoryName(typeof(ProfileTests).Assembly.Location)!, "TempProfile.cfg");

        public ProfileTests()
        {
            Dispose();
            File.WriteAllText(ProfileName, ProfileCfg);
        }

        public void Dispose()
        {
            if (File.Exists(ProfileName))
            {
                File.Delete(ProfileName);
            }
        }

        [Fact]
        public void Create()
        {
            var root = new ConfigurationUnit { DisplayName = "Test" };
            var session = new ConfigurationSession();
            Assert.NotNull(session);
            session.Namespace.AddChild(root);

            var ini = new NamespaceProviderProfile(ProfileName);
            session.RegisterNamespaceProvider("usr", ini);
        }

        [Fact]
        public void LoadNamespace()
        {
            var session = new ConfigurationSession();
            var serializer = new ConfigurationNamespaceXamlSerializer(session);
            var root = serializer.Load(new StringReader(TestResources.LoadResourceString("test_settings.xaml")));
            Assert.NotNull(session.Namespace);
            session.Namespace.AddChild(root);
        }

        private static ConfigurationSession CreateDefaultSessionSettings()
        {
            return CreateDefaultSession(TestResources.LoadResourceString("test_settings.xaml"));
        }
        private static ConfigurationSession CreateDefaultSessionTargets()
        {
            return CreateDefaultSession(TestResources.LoadResourceString("test_targets.xaml"));
        }
        private static ConfigurationSession CreateDefaultSessionConfigTest()
        {
            var definition = TestResources.LoadResourceString("ConfigTestUnits.json");
            var session = new ConfigurationSession();
            var serializer = new ConfigurationNamespaceJsonSerializer(session);
            var root = serializer.Load(new StringReader(definition));
            session.Namespace.AddChildren(root.Children);

            Assert.True(File.Exists(ProfileName), "Test data file not deployed");

            var ini = new NamespaceProviderProfile(ProfileName);
            session.RegisterNamespaceProvider("persistence", ini);
            return session;
        }
        private static ConfigurationSession CreateDefaultSession(string definition)
        {
            var session = new ConfigurationSession();
            var serializer = new ConfigurationNamespaceXamlSerializer(session);
            var root = serializer.Load(new StringReader(definition));
            session.Namespace.AddChildren(root.Children);

            Assert.True(File.Exists(ProfileName), "Test data file not deployed");

            var ini = new NamespaceProviderProfile(ProfileName);
            session.RegisterNamespaceProvider("usr", ini);
            return session;
        }

        [Fact]
        public void NamespaceProviderList()
        {
            var session = CreateDefaultSessionSettings();
            var providers = session.Namespace.GetNamespaceProviderList();
            Assert.Equal(2, providers.Count);
        }

        [Fact]
        public void SelectionValuesBase()
        {
            var session = CreateDefaultSessionSettings();
            session.RegisterNamespaceProvider("test", new NamespaceProviderMemory(""));
            var unit = session.Namespace.GetUnitById("LogCycle");
            var selValues = unit.ValueList;
            Assert.NotNull(selValues);
        }

        [Fact]
        public void SelectionValuesFromProfileShouldDeserializedFromString()
        {
            var session = CreateDefaultSessionSettings();
            var unit = session.Namespace.GetUnitById("LogCycle");
            unit.NamespaceProvider = "usr";
            var selValues = unit.ValueList;
            Assert.NotNull(selValues);
            Assert.Equal(5, selValues.Count);
        }

        [Fact]
        public void NonExistingUnit()
        {
            var session = CreateDefaultSessionTargets();
            var unit = session.Namespace.GetUnitById("??????");
            Assert.True(unit.IsEmpty, "Unknown unit should be empty");
        }

        [Fact]
        public void ConfigurationUnitProperties()
        {
            var session = CreateDefaultSessionSettings();
            var root = session.Namespace;

            {
                var provider = root.GetUnitById("Provider");
                Assert.False(provider.IsEmpty, "Provider should not be empty");
                Assert.False(provider.IsUserUnit, "Provider is not an user unit");
                Assert.False(provider.IsRoot, "Provider is not a root unit");
                Assert.False(provider.IsParent, "Provider is not an parent unit");
                Assert.False(provider.IsFolder, "Provider is not an folder unit");
                Assert.True(provider.IsItem, "Provider is an item unit");
                Assert.False(provider.IsProperty, "Provider is not a property unit");
                Assert.False(provider.IsTemplate, "Provider is not a template unit");
            }

            {
                var email = root.GetUnitById("ProviderEmailSmtp");
                Assert.False(email.IsEmpty, "Email should not be empty");
                Assert.False(email.IsUserUnit, "Email is not an user unit");
                Assert.False(email.IsRoot, "Email is not a root unit");
                Assert.False(email.IsParent, "Email is not an parent unit");
                Assert.False(email.IsFolder, "Email is not an folder unit");
                Assert.True(email.IsItem, "Email is an item unit");
                Assert.False(email.IsProperty, "Email is not an property unit");
                Assert.False(email.IsTemplate, "Email is not a template unit");
            }

            {
                var host = root.GetUnitById("Host");
                Assert.False(host.IsEmpty, "Host should not be empty");
                Assert.False(host.IsUserUnit, "Host is not an user unit");
                Assert.False(host.IsRoot, "Host is not a root unit");
                Assert.False(host.IsParent, "Host is not an parent unit");
                Assert.False(host.IsFolder, "Host is not an folder unit");
                Assert.False(host.IsItem, "Host is not an item unit");
                Assert.True(host.IsProperty, "Host is a property unit");
                Assert.False(host.IsTemplate, "Host is not a template unit");
            }

            {
                var channels = root.GetUnitById("Channels");
                Assert.False(channels.IsEmpty, "Channels should not be empty");
                Assert.False(channels.IsUserUnit, "Channels is not an user unit");
                Assert.False(channels.IsRoot, "Channels is not a root unit");
                Assert.False(channels.IsParent, "Channels is not a public parent unit");
                Assert.False(channels.IsFolder, "Channels is not a public folder unit");
                Assert.True(channels.IsItem, "Channels is an item unit");
                Assert.False(channels.IsProperty, "Channels is not an property unit");
                Assert.False(channels.IsTemplate, "Channels is not a template unit");
            }

            {
                var isdn = root.GetUnitById("ChannelIsdn");
                Assert.False(isdn.IsEmpty, "ChannelIsdn should not be empty");
                Assert.False(isdn.IsUserUnit, "ChannelIsdn is not an user unit");
                Assert.False(isdn.IsRoot, "ChannelIsdn is not a root unit");
                Assert.False(isdn.IsParent, "ChannelIsdn is not an parent unit");
                Assert.False(isdn.IsFolder, "ChannelIsdn is not an folder unit");
                Assert.False(isdn.IsItem, "ChannelIsdn is not an item unit");
                Assert.False(isdn.IsProperty, "ChannelIsdn is a property unit");
                Assert.True(isdn.IsTemplate, "ChannelIsdn is a template unit");
            }
        }

        [Fact]
        public void RegisterProvider()
        {
            var session = CreateDefaultSessionSettings();
            var customer = session.Namespace.GetUnitById("License/Customer");
            Assert.NotNull(customer);
        }

        [Fact]
        public void ReadValue()
        {
            var session = CreateDefaultSessionSettings();
            var customer = session.Namespace.GetUnitById("License/Customer");
            var value = customer.GetValue<string>();
            Assert.Equal("ICT Baden, Frank Pfattheicher", value);
            value = customer.ValueDisplayText;
            Assert.Equal("ICT Baden, Frank Pfattheicher", value);
        }

        [Fact]
        public void WriteValue()
        {
            var session = CreateDefaultSessionSettings();
            var serial = session.Namespace.GetUnitById("License/SerialNumber");
            serial.SetValue("0000-0000");

            var session2 = CreateDefaultSessionSettings();
            var serial2 = session2.Namespace.GetUnitById("License/SerialNumber");
            var value2 = serial2.GetValue<string>();
            Assert.Equal("0000-0000", value2);
        }

        [Fact]
        public void ReadUserItems()
        {
            var session = CreateDefaultSessionSettings();
            var channels = session.Namespace.GetUnitById("Settings/Channels");
            var value = channels.ChildItems.ToList();
            Assert.Equal(3, value.Count);
        }

        [Fact]
        public void FindUserFolder()
        {
            var session = CreateDefaultSessionTargets();
            var targets = session.Namespace.GetUnitById("Targets");
            var folder = targets.GetUnitByName("Test", true, false);
            Assert.False(folder.IsEmpty, "Folder could not be created");
            Assert.True(folder.IsFolder, "Folder could be a folder");

            var folder2 = session.Namespace.GetUnitByName("Empfänger/Test", true, false);
            Assert.True(folder2.IsFolder);
        }

        [Fact]
        public void FindUserItem()
        {
            var session = CreateDefaultSessionTargets();
            var target = session.Namespace.GetUnitByName("Smarty", false, false);
            Assert.True(target.IsItem);
        }

        [Fact]
        public void FindUserItemInFolder()
        {
            var session = CreateDefaultSessionTargets();
            var target = session.Namespace.GetUnitByName("DieGruppe", false, false);
            Assert.True(target.IsItem);
        }

        [Fact]
        public void FindUserItemInNamedFolder()
        {
            var session = CreateDefaultSessionTargets();
            var target = session.Namespace.GetUnitByName("Test/DieGruppe", false, false);
            Assert.True(target.IsItem);
        }

        [Fact]
        public void CreateFolder()
        {
            var session = CreateDefaultSessionTargets();
            var targets = session.Namespace.GetUnitById("Targets");
            var newFolder = targets.CreateFolder("CreateFolder");
            Assert.True(newFolder.IsUserUnit);
            Assert.True(newFolder.IsFolder);

            var session2 = CreateDefaultSessionTargets();
            var folder = session2.Namespace.GetUnitByName("CreateFolder", true, false);
            Assert.False(folder.IsEmpty, "folder does not exist");
            Assert.True(folder.IsUserUnit);
            Assert.True(folder.IsFolder);
            Assert.True(folder.Class == null);
            Assert.True(folder.DisplayName == "CreateFolder");
        }

        [Fact]
        public void DeleteFolder()
        {
            var session = CreateDefaultSessionTargets();
            var targets = session.Namespace.GetUnitById("Targets");
            var folder = targets.GetUnitByName("EinOrdner", true, false);
            Assert.True(folder.IsUserUnit);
            Assert.True(folder.IsFolder);
            Assert.True(folder.Class == null);

            folder.Delete();

            var session2 = CreateDefaultSessionTargets();
            var folder2 = session2.Namespace.GetUnitByName("EinOrdner", true, false);
            Assert.True(folder2.IsEmpty);
        }

        [Fact]
        public void CreateUserItem()
        {
            var session = CreateDefaultSessionTargets();
            var targets = session.Namespace.GetUnitById("Targets");
            var template = targets.GetUnitById("TargetSms");
            var newItem = targets.CreateItem(template, "CreateUserItem");
            Assert.True(newItem.IsItem);

            var session2 = CreateDefaultSessionTargets();
            var target = session2.Namespace.GetUnitByName("CreateUserItem", false, false);
            Assert.True(target.IsItem);
            Assert.True(target.IsUserUnit);
            Assert.True(target.Class == "TargetSms");
            Assert.True(target.DisplayName == "CreateUserItem");
        }

        [Fact]
        public void DeleteUserItem()
        {
            var session = CreateDefaultSessionTargets();
            var targets = session.Namespace.GetUnitById("Targets");
            var template = targets.GetUnitById("TargetSms");
            var newItem = targets.CreateItem(template, "DeleteUserItem");
            Assert.True(newItem.IsItem);

            newItem.Delete();

            var session2 = CreateDefaultSessionTargets();
            var target = session2.Namespace.GetUnitByName("DeleteUserItem", false, false);
            Assert.True(target.IsEmpty);
        }

        [Fact]
        public void RenameUserItem()
        {
            var session = CreateDefaultSessionTargets();
            var target1 = session.Namespace.GetUnitByName("DieFolge", false, false);
            Assert.True(target1.IsItem);

            target1.Rename("DasTier");

            var session2 = CreateDefaultSessionTargets();
            var target2 = session2.Namespace.GetUnitByName("DieFolge", false, false);
            Assert.True(target2.IsEmpty, "old item not deleted");
            var target3 = session2.Namespace.GetUnitByName("DasTier", false, false);
            Assert.False(target3.IsEmpty, "new name not found");
        }

        [Fact]
        public void MoveUserItemToFolder()
        {
            var session = CreateDefaultSessionTargets();
            var targets = session.Namespace.GetUnitById("Targets");
            var count = targets.Children.Count;

            // create folder in "Targets"
            const string folderName = "TargetFolder";
            var newFolder = targets.CreateFolder(folderName);
            var folderId = newFolder.Id;
            // create "TargetSms" in "Targets"
            var template = targets.GetUnitById("TargetSms");
            var newItem = targets.CreateItem(template, "CreateUserItem");

            targets = session.Namespace.GetUnitById("Targets");
            Assert.Equal(count + 2, targets.Children.Count);  // folder AND item in Targets
            Assert.Empty(newFolder.Children);   // no item in folder
            
            // move newItem to newFolder
            var moved = newItem.MoveToFolder(newFolder);
            Assert.True(moved);

            var session2 = CreateDefaultSessionTargets();
            var targets2 = session2.Namespace.GetUnitById("Targets");
            Assert.Equal(count + 1, targets2.Children.Count);
            var folder2 = targets2.GetUnitById(folderId);
            Assert.Single(folder2.Children);
            var folder3 = session2.Namespace.GetUnitByName(folderName, true, false);
            Assert.Single(folder3.Children);
        }

        [Fact]
        public void MoveFolderToFolderShouldIncludeFolderContents()
        {
            MoveUserItemToFolder();
            
            var session = CreateDefaultSessionTargets();
            var targets = session.Namespace.GetUnitById("Targets");
            var count = targets.Children.Count;

            // create folder 2 in "Targets"
            const string folderName = "TargetFolder";
            const string folderName2 = "TargetFolder2";
            var newFolder2 = targets.CreateFolder(folderName2);
            
            // get first folder
            var firstFolder = session.Namespace.GetUnitByName(folderName, true, false);
            Assert.Single(firstFolder.Children);
            
            // move firstFolder to newFolder2
            var moved = firstFolder.MoveToFolder(newFolder2);
            Assert.True(moved);
            
            var session2 = CreateDefaultSessionTargets();
            var targets2 = session2.Namespace.GetUnitById("Targets");
            Assert.Equal(count, targets2.Children.Count);
            
            var folder1 = session.Namespace.GetUnitByName(folderName, true, false);
            Assert.Single(folder1.Children);
            var folder2 = session.Namespace.GetUnitByName(folderName2, true, false);
            Assert.Single(folder2.Children);
        }

        [Fact]
        public void UserItemChangeClass()
        {
            var session = CreateDefaultSessionTargets();
            var target1 = session.Namespace.GetUnitByName("Alex", false, false);
            Assert.True(target1.IsItem);
            Assert.Equal("TargetSms", target1.Class);

            var targets = session.Namespace.GetUnitById("Targets");
            var template = targets.GetUnitById("TargetSpeaker");
            target1.ChangeClass(template);

            var session2 = CreateDefaultSessionTargets();
            var target2 = session2.Namespace.GetUnitByName("Alex", false, false);
            Assert.True(target2.IsItem);
            Assert.Equal("TargetSpeaker", target2.Class);
        }

        [Fact]
        public void NewUnitTypes()
        {
            var session = CreateDefaultSessionTargets();
            var targets = session.Namespace.GetUnitById("Targets");
            var newTypes = targets.NewUnitTemplates.ToList();
            Assert.Equal(9, newTypes.Count);
            foreach (var newType in newTypes)
            {
                Assert.True(newType.IsTemplate);
            }
        }

        [Fact]
        public void GetUserUnits()
        {
            var session = CreateDefaultSessionTargets();
            var targets = session.Namespace.GetUnitById("Targets");
            var userUnits = targets.GetUserUnits(null);
            Assert.Equal(9, userUnits.Count);
            foreach (var unit in userUnits)
            {
                Assert.True(unit.IsUserUnit);
            }
        }


        private enum FilterType
        {
            // ReSharper disable once UnusedMember.Local
            ContainsText, StartsWithText, RegularExpression
        }
        
        
        [Fact]
        public void SetEnumShouldGetSameValue()
        {
            const string name = "Filter1";
            const FilterType prop = FilterType.RegularExpression;
            
            var session = CreateDefaultSessionConfigTest();
            var filters = session.Namespace.GetUnitById("MessageFilters");
            var template = filters.GetUnitById("MessageFilterText");
            var filter1 = filters.CreateItem(template, name);
            
            filter1.SetPropertyValue("Type", prop);

            var userFilter = filters.GetUnitByName(name, false, true);
            Assert.NotNull(userFilter);

            var propValue = userFilter.GetPropertyValue("Type", FilterType.ContainsText);
            Assert.Equal(prop, propValue);
        }


        [Fact]
        public void TargetInFolderShouldBeHierarchicalChildOf()
        {
            var session = CreateDefaultSessionTargets();
            var targets = session.Namespace.GetUnitById("Targets");
            var template = targets.GetUnitById("TargetSms");
            
            // Targets
            //  +-Folder1
            //     +-Folder2
            //        +-Target1
            
            var folder1 = targets.CreateFolder("Folder1");
            var folder2 = folder1.CreateFolder("Folder2");
            var target1 = folder2.CreateItem(template, "Target1");

            Assert.True(folder1.IsHierarchicalChildOf(targets));

            Assert.True(folder2.IsHierarchicalChildOf(targets));
            Assert.True(folder2.IsHierarchicalChildOf(folder1));
            
            Assert.True(target1.IsHierarchicalChildOf(targets));
            Assert.True(target1.IsHierarchicalChildOf(folder1));
            Assert.True(target1.IsHierarchicalChildOf(folder2));

            Assert.False(folder1.IsHierarchicalChildOf(folder2));
            Assert.False(folder1.IsHierarchicalChildOf(target1));
            
            Assert.False(folder2.IsHierarchicalChildOf(target1));
        }

        [Fact]
        public void MoveFolderToFolderShouldFailIfTargetIsChildOfSource()
        {
            MoveUserItemToFolder();
            
            var session = CreateDefaultSessionTargets();
            var targets = session.Namespace.GetUnitById("Targets");
            var template = targets.GetUnitById("TargetSms");

            // create folder 2 in "Targets"
            // Targets
            //  +-Folder1
            //     +-Folder2
            //        +-Target1

            var folder1 = targets.CreateFolder("Folder1");
            var folder2 = folder1.CreateFolder("Folder2");
            folder2.CreateItem(template, "Target1");
            
            // move Folder1 to Folder2
            var moved = folder1.MoveToFolder(folder2);
            Assert.False(moved);
        }

    }
}
