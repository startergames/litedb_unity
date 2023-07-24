using System;
using System.Collections.Generic;
using System.IO;

namespace LiteDB {
    public class LDB : IDisposable {
        private readonly LiteDatabase _db;

        public LDB(string connectionString, BsonMapper mapper = null) {
            _db = new LiteDatabase(connectionString, mapper ?? BsonMapper.Global);
        }

        public LCollection<T> GetCollection<T>(BsonAutoId autoId) => new(_db.GetCollection<T>(autoId), this);
        public LCollection<T> GetCollection<T>(string name = null, BsonAutoId bsonAutoId = BsonAutoId.ObjectId)
            => new LCollection<T>(_db.GetCollection<T>(name, bsonAutoId), this);

        public LCollection<BsonDocument> GetCollection(string name = null, BsonAutoId bsonAutoId = BsonAutoId.ObjectId)
            => new LCollection<BsonDocument>(_db.GetCollection(name, bsonAutoId), this);

        public LCollection<T> C<T>(string name = null, BsonAutoId bsonAutoId = BsonAutoId.ObjectId)
            => GetCollection<T>(name, bsonAutoId);

        public LCollection<T> GetCollectionAutoInclude<T>(string name = null,
            BsonAutoId bsonAutoId = BsonAutoId.ObjectId, int maxDepth = 5)
            => new LCollection<T>(_db.GetCollectionAutoInclude<T>(name, bsonAutoId, maxDepth), this);

        public LCollection<T> Ci<T>(string name = null, BsonAutoId bsonAutoId = BsonAutoId.ObjectId, int maxDepth = 5)
            => GetCollectionAutoInclude<T>(name, bsonAutoId, maxDepth);


        public IEnumerable<string> GetCollectionNames() => _db.GetCollectionNames();

        public bool CollectionExists(string name) => _db.CollectionExists(name);

        public bool DropCollection(string name) => _db.DropCollection(name);

        public bool RenameCollection(string oldName, string newName) => _db.RenameCollection(oldName, newName);

        public bool BeginTrans() => _db.BeginTrans();
        public bool Commit() => _db.Commit();
        public bool Rollback() => _db.Rollback();

        public IBsonDataReader Execute(TextReader command, BsonDocument parameters = null) =>
            _db.Execute(command, parameters);

        public IBsonDataReader Execute(string command, BsonDocument args = null) => _db.Execute(command, args);
        public IBsonDataReader Execute(string command, params BsonValue[] args) => _db.Execute(command, args);
        public void Dispose() => _db.Dispose();
    }
}