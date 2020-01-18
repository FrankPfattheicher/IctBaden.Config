using System;
using System.Collections.Generic;
using System.Linq;
using IctBaden.Config.Unit;
using IctBaden.Framework.Types;
using MongoDB.Bson;
using MongoDB.Driver;

namespace IctBaden.Config.Namespace
{
    public class NamespaceProviderMongoDb : NamespaceProvider
    {
        private const string DbName = "Configuration";
        private const string CollectionName = "Units";

        private readonly string _connectionString;
        private readonly MongoClient _client;
        private IMongoDatabase _db;
        private IMongoCollection<BsonDocument> _collection;

        private string _lastError;

        public NamespaceProviderMongoDb(string connectionString)
        {
            _connectionString = "mongodb://" + connectionString;
            _client = new MongoClient(_connectionString);
        }

        public override string GetPersistenceInfo()
        {
            return _connectionString;
        }

        private bool Connect()
        {
            SignalWaiting(true);

            try
            {
                _db = _client.GetDatabase(DbName);

                _collection = _db.GetCollection<BsonDocument>(CollectionName);
                if (_collection == null)
                {
                    _db.CreateCollection(CollectionName);
                    _collection = _db.GetCollection<BsonDocument>(CollectionName);
                }
            }
            catch (Exception ex)
            {
                _lastError = ex.Message;
                SignalWaiting(false);
                return false;
            }

            SignalWaiting(false);
            return true;
        }

        public override IEnumerable<ConfigurationUnit> GetChildren(ConfigurationUnit unit)
        {
            var children = new List<ConfigurationUnit>();

            var template = unit.Children.FirstOrDefault(ch => ch.IsTemplate);
            if (template == null)
                return children;

            if (!Connect())
            {
                children.Add(new ConfigurationUnit
                {
                    DataType = TypeCode.Object,
                    Id = "error",
                    DisplayName = _lastError, 
                    DisplayImage = "fas fa-bomb"
                });
                return children;
            }

            var childIdList = GetValue(unit.Id + "/Children");
            if (childIdList == null) return children;

            var childIds = childIdList.Split(new[] {';'}, StringSplitOptions.RemoveEmptyEntries);
            foreach (var childId in childIds)
            {
                var childDisplayName = GetValue(childId + "/DisplayName");
                var childClass = GetValue(childId + "/Class");
                if (string.IsNullOrEmpty(childClass))
                {
                    // folder
                    var newFolder = unit.Clone(childDisplayName, false);
                    newFolder.SetUserId(childId);
                    newFolder.Description = unit.Session.Folder.DisplayName;
                    children.Add(newFolder);
                }
                else
                {
                    var type = unit.Session.Namespace.GetUnitById(childClass);
                    if (!type.IsEmpty)
                    {
                        var newItem = type.Clone(childDisplayName, true);
                        newItem.SetUserId(childId);
                        newItem.Class = childClass;
                        newItem.Description = type.DisplayName;
                        newItem.DataType = TypeCode.Object;
                        children.Add(newItem);
                    }
                }
            }

            return children;
        }

        private string GetValue(string key)
        {
            var document = _collection.FindSync(filter: new BsonDocument("_id", key)).FirstOrDefault();
            return document != null
                ? document["value"].ToString()
                : null;
        }

        public override T GetValue<T>(ConfigurationUnit unit, T defaultValue)
        {
            if (!Connect())
                return defaultValue;

            var key = unit.Parent.FullId + "/" + unit.Id;
            var value = GetValue(key);
            return value != null
                ? UniversalConverter.ConvertTo<T>(value)
                : defaultValue;
        }

        public override void SetValue<T>(ConfigurationUnit unit, T newValue)
        {
            if (!Connect())
                return;

            var key = unit.Parent.FullId + "/" + unit.Id;
            var document = new BsonDocument
            {
                {"_id", key},
                {"value", newValue.ToString()}
            };

            _collection.ReplaceOne(
                filter: new BsonDocument("_id", key),
                options: new ReplaceOptions {IsUpsert = true},
                replacement: document);
        }

        public override void AddUserUnit(ConfigurationUnit unit)
        {
            if ((unit.Class == null) || !Connect())
                return;

            var itemClass = ConfigurationUnit.GetProperty(unit, "Class");
            itemClass.SetValue(unit.Class);
            var containerChildren = ConfigurationUnit.GetProperty(unit.Parent, "Children");
            containerChildren.SetValue(ConfigurationUnit.GetUnitListIdList(unit.Parent.Children));
            var itemDisplayName = ConfigurationUnit.GetProperty(unit, "DisplayName");
            itemDisplayName.SetValue(unit.DisplayName);
        }

        public override void RemoveUserUnit(ConfigurationUnit unit)
        {
            if ((unit.Class == null) || !Connect())
                return;

            var deleteFilter = Builders<BsonDocument>.Filter.Regex("_id", unit.Id + ".*");
            _collection.DeleteMany(deleteFilter);
        }

        public override List<SelectionValue> GetSelectionValues(ConfigurationUnit unit)
        {
            var list = new List<SelectionValue>();

            if (!Connect())
                return list;

            throw new NotImplementedException();
        }
    }
}