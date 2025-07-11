﻿using System;
using System.IO;
using System.Linq;
using IctBaden.Config.Namespace;
using IctBaden.Config.Session;
using IctBaden.Config.Unit;
using IctBaden.Framework.Logging;
using IctBaden.Framework.Resource;
using Microsoft.Extensions.Logging;
using Xunit;

// ReSharper disable StringLiteralTypo

namespace IctBaden.Config.Test;

public class ProfileTests : IDisposable
{
    private readonly ILogger _logger;
    private static readonly string ProfileCfg = TestResources.LoadResourceString("Profile.cfg");

    private static readonly string ProfileName =
        Path.Combine(Path.GetDirectoryName(typeof(ProfileTests).Assembly.Location)!, "TempProfile.cfg");

    public ProfileTests()
    {
        _logger = Logger.DefaultFactory.CreateLogger("Test");
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

        var ini = new NamespaceProviderProfile(_logger, ProfileName);
        session.RegisterNamespaceProvider("persistence", ini);
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

    private static ConfigurationSession CreateDefaultSessionSettings(ILogger logger)
    {
        return CreateDefaultSession(logger, TestResources.LoadResourceString("test_settings.xaml"));
    }

    private static ConfigurationSession CreateDefaultSessionTargets(ILogger logger)
    {
        var session = CreateDefaultSession(logger, string.Empty);

        var jsonCfgLoader = new ConfigurationNamespaceJsonSerializer(session);
        var assemblyName = typeof(ProfileTests).Assembly.GetName().Name;
        var unitTypesCfgName = assemblyName + ".Configuration.unit-types.json";
        var unitTypesJson = ResourceLoader.LoadString(unitTypesCfgName);
        if (unitTypesJson == null) throw new ApplicationException("Missing unit-types.json resource");
        var unitTypes = jsonCfgLoader.Load(new StringReader(unitTypesJson));
        session.UnitTypes.AddChildren(unitTypes.Children);

        var targetsCfgName = assemblyName + ".Configuration.targets.json";
        var targetsJson = ResourceLoader.LoadString(targetsCfgName);
        if (targetsJson == null) throw new ApplicationException("Missing targets.json resource");
        var targets = jsonCfgLoader.Load(new StringReader(targetsJson));
        var settingsCfgName = assemblyName + ".Configuration.settings.json";
        var settingsJson = ResourceLoader.LoadString(settingsCfgName);
        if (settingsJson == null) throw new ApplicationException("Missing settings.json resource");
        var settings = jsonCfgLoader.Load(new StringReader(settingsJson));
        session.Namespace.AddChildren([targets, settings]);

        return session;
    }

    private static ConfigurationSession CreateDefaultSessionConfigTest(ILogger logger)
    {
        var definition = TestResources.LoadResourceString("ConfigTestUnits.json");
        var session = new ConfigurationSession();
        var serializer = new ConfigurationNamespaceJsonSerializer(session);
        var root = serializer.Load(new StringReader(definition));
        session.Namespace.AddChildren(root.Children);

        Assert.True(File.Exists(ProfileName), "Test data file not deployed");

        var ini = new NamespaceProviderProfile(logger, ProfileName);
        session.RegisterNamespaceProvider("persistence", ini);
        return session;
    }

    private static ConfigurationSession CreateDefaultSession(ILogger logger, string definition)
    {
        var session = new ConfigurationSession();
        var serializer = new ConfigurationNamespaceXamlSerializer(session);

        if (!string.IsNullOrEmpty(definition))
        {
            var root = serializer.Load(new StringReader(definition));
            session.Namespace.AddChildren(root.Children);
        }

        Assert.True(File.Exists(ProfileName), "Test data file not deployed");

        var ini = new NamespaceProviderProfile(logger, ProfileName);
        session.RegisterNamespaceProvider("persistence", ini);
        return session;
    }

    [Fact]
    public void NamespaceProviderList()
    {
        var session = CreateDefaultSessionSettings(_logger);
        var providers = session.Namespace.GetNamespaceProviderList();
        Assert.Equal(2, providers.Count);
    }

    [Fact]
    public void SelectionValuesBase()
    {
        var session = CreateDefaultSessionSettings(_logger);
        session.RegisterNamespaceProvider("test", new NamespaceProviderMemory(_logger, ""));
        var unit = session.Namespace.GetUnitById("LogCycle");
        var selValues = unit.ValueList;
        Assert.NotNull(selValues);
    }

    [Fact]
    public void SelectionValuesFromProfileShouldDeserializedFromString()
    {
        var session = CreateDefaultSessionSettings(_logger);
        var unit = session.Namespace.GetUnitById("LogCycle");
        unit.NamespaceProvider = "persistence";
        var selValues = unit.ValueList;
        Assert.NotNull(selValues);
        Assert.Equal(5, selValues.Count);
    }

    [Fact]
    public void NonExistingUnit()
    {
        var session = CreateDefaultSessionTargets(_logger);
        var unit = session.Namespace.GetUnitById("??????");
        Assert.True(unit.IsEmpty, "Unknown unit should be empty");
    }

    [Fact]
    public void ConfigurationUnitProperties()
    {
        var session = CreateDefaultSessionSettings(_logger);
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
    public void AllTargetTypePropertiesShouldHaveDisplayNameAndDescription()
    {
        var session = CreateDefaultSessionTargets(_logger);
        var root = session.Namespace;

        var targets = root.GetUnitById("Targets");
        foreach (var unit in targets.Children)
        {
            foreach (var propertyUnit in unit.Children)
            {
                Assert.False(string.IsNullOrEmpty(propertyUnit.DisplayName));
            }
        }
    }


    [Fact]
    public void RegisterProvider()
    {
        var session = CreateDefaultSessionSettings(_logger);
        var customer = session.Namespace.GetUnitById("License/Customer");
        Assert.NotNull(customer);
    }

    [Fact]
    public void ReadValue()
    {
        var session = CreateDefaultSessionSettings(_logger);
        var customer = session.Namespace.GetUnitById("License/Customer");
        var value = customer.GetValue<string>();
        Assert.Equal("ICT Baden, Frank Pfattheicher", value);
        value = customer.ValueDisplayText;
        Assert.Equal("ICT Baden, Frank Pfattheicher", value);
    }

    [Fact]
    public void WriteValue()
    {
        var session = CreateDefaultSessionSettings(_logger);
        var serial = session.Namespace.GetUnitById("License/SerialNumber");
        serial.SetValue("0000-0000");

        var session2 = CreateDefaultSessionSettings(_logger);
        var serial2 = session2.Namespace.GetUnitById("License/SerialNumber");
        var value2 = serial2.GetValue<string>();
        Assert.Equal("0000-0000", value2);
    }

    [Fact]
    public void ReadUserItems()
    {
        var session = CreateDefaultSessionSettings(_logger);
        var channels = session.Namespace.GetUnitById("Settings/Channels");
        var value = channels.ChildItems.ToList();
        Assert.Equal(3, value.Count);
    }

    [Fact]
    public void FindUserFolder()
    {
        var session = CreateDefaultSessionTargets(_logger);
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
        var session = CreateDefaultSessionTargets(_logger);
        var target = session.Namespace.GetUnitByName("Smarty", false, false);
        Assert.True(target.IsItem);
    }

    [Fact]
    public void FindUserItemInFolder()
    {
        var session = CreateDefaultSessionTargets(_logger);
        var target = session.Namespace.GetUnitByName("DieGruppe", false, false);
        Assert.True(target.IsItem);
    }

    [Fact]
    public void FindUserItemInNamedFolder()
    {
        var session = CreateDefaultSessionTargets(_logger);
        var target = session.Namespace.GetUnitByName("Test/DieGruppe", false, false);
        Assert.True(target.IsItem);
    }

    [Fact]
    public void CreateFolder()
    {
        var session = CreateDefaultSessionTargets(_logger);
        var targets = session.Namespace.GetUnitById("Targets");
        var newFolder = targets.CreateFolder("CreateFolder");
        Assert.NotNull(newFolder);
        Assert.True(newFolder.IsUserUnit);
        Assert.True(newFolder.IsFolder);

        var session2 = CreateDefaultSessionTargets(_logger);
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
        var session = CreateDefaultSessionTargets(_logger);
        var targets = session.Namespace.GetUnitById("Targets");
        var folder = targets.GetUnitByName("EinOrdner", true, false);
        Assert.True(folder.IsUserUnit);
        Assert.True(folder.IsFolder);
        Assert.True(folder.Class == null);

        folder.Delete();

        var session2 = CreateDefaultSessionTargets(_logger);
        var folder2 = session2.Namespace.GetUnitByName("EinOrdner", true, false);
        Assert.True(folder2.IsEmpty);
    }

    [Fact]
    public void CreateUserItem()
    {
        var session = CreateDefaultSessionTargets(_logger);
        var targets = session.Namespace.GetUnitById("Targets");
        var template = targets.GetUnitById("sms");
        Assert.False(template.IsEmpty);
        var newItem = targets.CreateItem(template, "CreateUserItem");
        Assert.NotNull(newItem);
        Assert.True(newItem.IsItem);

        var session2 = CreateDefaultSessionTargets(_logger);
        var target = session2.Namespace.GetUnitByName("CreateUserItem", false, false);
        Assert.True(target.IsItem);
        Assert.True(target.IsUserUnit);
        Assert.True(target.Class == "sms");
        Assert.True(target.DisplayName == "CreateUserItem");
    }

    [Fact]
    public void DeleteUserItemFromUnit()
    {
        var session = CreateDefaultSessionTargets(_logger);
        var targets = session.Namespace.GetUnitById("Targets");
        var template = targets.GetUnitById("sms");
        Assert.False(template.IsEmpty);
        var newItem = targets.CreateItem(template, "DeleteUserItem");
        Assert.NotNull(newItem);
        Assert.True(newItem.IsItem);

        newItem.Delete();

        var target = session.Namespace.GetUnitByName("DeleteUserItem", false, false);
        Assert.True(target.IsEmpty, "Should not exist in same session");

        var session2 = CreateDefaultSessionTargets(_logger);
        target = session2.Namespace.GetUnitByName("DeleteUserItem", false, false);
        Assert.True(target.IsEmpty, "Should not exist in new session");
    }

    [Fact]
    public void DeleteUserItemFromSession()
    {
        var session = CreateDefaultSessionTargets(_logger);
        var targets = session.Namespace.GetUnitById("Targets");
        var template = targets.GetUnitById("sms");
        Assert.False(template.IsEmpty);
        var newItem = targets.CreateItem(template, "DeleteUserItem");
        Assert.NotNull(newItem);
        Assert.True(newItem.IsItem);

        session.DeleteUserUnit(newItem);

        var target = session.Namespace.GetUnitByName("DeleteUserItem", false, false);
        Assert.True(target.IsEmpty, "Should not exist in same session");

        var session2 = CreateDefaultSessionTargets(_logger);
        target = session2.Namespace.GetUnitByName("DeleteUserItem", false, false);
        Assert.True(target.IsEmpty, "Should not exist in new session");
    }

    [Fact]
    public void RemoveUserItemFromSession()
    {
        var session = CreateDefaultSessionTargets(_logger);
        var targets = session.Namespace.GetUnitById("Targets");
        var template = targets.GetUnitById("sms");
        Assert.False(template.IsEmpty);
        var newItem = targets.CreateItem(template, "DeleteUserItem");
        Assert.NotNull(newItem);
        Assert.True(newItem.IsItem);

        session.RemoveUserUnit(newItem);

        var target = session.Namespace.GetUnitByName("DeleteUserItem", false, false);
        Assert.True(target.IsEmpty, "Should not exist in same session");

        var session2 = CreateDefaultSessionTargets(_logger);
        target = session2.Namespace.GetUnitByName("DeleteUserItem", false, false);
        Assert.True(target.IsEmpty, "Should not exist in new session");
    }

    [Fact]
    public void RenameUserItem()
    {
        var session = CreateDefaultSessionTargets(_logger);
        var target1 = session.Namespace.GetUnitByName("DieFolge", false, false);
        Assert.True(target1.IsItem);

        target1.Rename("DasTier");

        var session2 = CreateDefaultSessionTargets(_logger);
        var target2 = session2.Namespace.GetUnitByName("DieFolge", false, false);
        Assert.True(target2.IsEmpty, "old item not deleted");
        var target3 = session2.Namespace.GetUnitByName("DasTier", false, false);
        Assert.False(target3.IsEmpty, "new name not found");
    }

    [Fact]
    public void MoveUserItemToFolder()
    {
        var session = CreateDefaultSessionTargets(_logger);
        var targets = session.Namespace.GetUnitById("Targets");
        var count = targets.Children.Count;

        // create folder in "Targets"
        const string folderName = "TargetFolder";
        var newFolder = targets.CreateFolder(folderName);
        Assert.NotNull(newFolder);
        var folderId = newFolder.Id;
        // create "TargetSms" in "Targets"
        var template = targets.GetUnitById("sms");
        Assert.False(template.IsEmpty);
        var newItem = targets.CreateItem(template, "CreateUserItem");
        Assert.NotNull(newItem);

        targets = session.Namespace.GetUnitById("Targets");
        Assert.Equal(count + 2, targets.Children.Count); // folder AND item in Targets
        Assert.Empty(newFolder.Children); // no item in folder

        // move newItem to newFolder
        var moved = newItem.MoveToFolder(newFolder);
        Assert.True(moved);

        var session2 = CreateDefaultSessionTargets(_logger);
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

        var session = CreateDefaultSessionTargets(_logger);
        var targets = session.Namespace.GetUnitById("Targets");
        var count = targets.Children.Count;

        // create folder 2 in "Targets"
        const string folderName = "TargetFolder";
        const string folderName2 = "TargetFolder2";
        var newFolder2 = targets.CreateFolder(folderName2);
        Assert.NotNull(newFolder2);

        // get first folder
        var firstFolder = session.Namespace.GetUnitByName(folderName, true, false);
        Assert.Single(firstFolder.Children);

        // move firstFolder to newFolder2
        var moved = firstFolder.MoveToFolder(newFolder2);
        Assert.True(moved);

        var session2 = CreateDefaultSessionTargets(_logger);
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
        var session = CreateDefaultSessionTargets(_logger);
        var target1 = session.Namespace.GetUnitByName("Alex", false, false);
        Assert.True(target1.IsItem);
        Assert.Equal("sms", target1.Class);

        var targets = session.Namespace.GetUnitById("Targets");
        var template = targets.GetUnitById("speaker");
        Assert.False(template.IsEmpty);
        target1.ChangeClass(template);

        var session2 = CreateDefaultSessionTargets(_logger);
        var target2 = session2.Namespace.GetUnitByName("Alex", false, false);
        Assert.True(target2.IsItem);
        Assert.Equal("speaker", target2.Class);
    }

    [Fact]
    public void NewUnitTypes()
    {
        var session = CreateDefaultSessionTargets(_logger);
        var targets = session.Namespace.GetUnitById("Targets");
        var newTypes = targets.NewUnitTemplates.ToList();
        Assert.Equal(17, newTypes.Count);
        foreach (var newType in newTypes)
        {
            Assert.True(newType.IsTemplate);
        }
    }

    [Fact]
    public void GetUserUnits()
    {
        var session = CreateDefaultSessionTargets(_logger);
        var targets = session.Namespace.GetUnitById("Targets");
        var userUnits = targets.GetUserUnits(string.Empty);
        Assert.Equal(10, userUnits.Count);
        foreach (var unit in userUnits)
        {
            Assert.True(unit.IsUserUnit);
        }
    }


    // ReSharper disable UnusedMember.Local
    private enum FilterType
    {
        ContainsText,
        StartsWithText,
        RegularExpression
    }
    // ReSharper restore UnusedMember.Local


    [Fact]
    public void SetEnumShouldGetSameValue()
    {
        const string name = "Filter1";
        const FilterType prop = FilterType.RegularExpression;

        var session = CreateDefaultSessionConfigTest(_logger);
        var filters = session.Namespace.GetUnitById("MessageFilters");
        var template = filters.GetUnitById("MessageFilterText");
        Assert.False(template.IsEmpty);
        var filter1 = filters.CreateItem(template, name);
        Assert.NotNull(filter1);

        filter1.SetPropertyValue("Type", prop);

        var userFilter = filters.GetUnitByName(name, false, true);
        Assert.NotNull(userFilter);

        var propValue = userFilter.GetPropertyValue("Type", FilterType.ContainsText);
        Assert.Equal(prop, propValue);
    }


    [Fact]
    public void TargetInFolderShouldBeHierarchicalChildOf()
    {
        var session = CreateDefaultSessionTargets(_logger);
        var targets = session.Namespace.GetUnitById("Targets");
        var template = targets.GetUnitById("sms");
        Assert.False(template.IsEmpty);

        // Targets
        //  +-Folder1
        //     +-Folder2
        //        +-Target1

        var folder1 = targets.CreateFolder("Folder1");
        Assert.NotNull(folder1);
        var folder2 = folder1.CreateFolder("Folder2");
        Assert.NotNull(folder2);
        var target1 = folder2.CreateItem(template, "Target1");
        Assert.NotNull(target1);

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

        var session = CreateDefaultSessionTargets(_logger);
        var targets = session.Namespace.GetUnitById("Targets");
        var template = targets.GetUnitById("sms");
        Assert.False(template.IsEmpty);

        // create folder 2 in "Targets"
        // Targets
        //  +-Folder1
        //     +-Folder2
        //        +-Target1

        var folder1 = targets.CreateFolder("Folder1");
        Assert.NotNull(folder1);
        var folder2 = folder1.CreateFolder("Folder2");
        Assert.NotNull(folder2);
        folder2.CreateItem(template, "Target1");

        // move Folder1 to Folder2
        var moved = folder1.MoveToFolder(folder2);
        Assert.False(moved);
    }

    [Fact]
    public void TargetWithTestClassShouldBeLoaded()
    {
        var session = CreateDefaultSessionTargets(_logger);
        var target = session.Namespace.GetUnitByName("Tester", false, false);
        Assert.False(target.IsEmpty);
        Assert.Equal("Test", target.Class);
        Assert.Equal("testClass", target.Description);
    }

    [Fact]
    public void TemplatedUnitShouldBecomeTemplateDisplayName()
    {
        var session = CreateDefaultSessionTargets(_logger);
        var unit = session.Namespace.GetUnitById("Targets/EmailSmtp/Presence");
        
        Assert.Equal("Präsenz", unit.DisplayName);
    }

    [Fact]
    public void TemplatedUnitShouldUseOwnDisplayName()
    {
        var session = CreateDefaultSessionTargets(_logger);
        var unit = session.Namespace.GetUnitById("Targets/mailto/Presence");
        
        Assert.Equal("Mail-Präsenz", unit.DisplayName);
    }
}