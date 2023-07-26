using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace LiteDB {
    public class LCollection<T> {
        private readonly ILiteCollection<T> _collection;

        private LDB Database { get; }

        public LCollection(ILiteCollection<T> collection, LDB databaseWrapper) {
            _collection = collection;
            Database = databaseWrapper;
        }

        public int Count() => _collection.Count();
        public int Count(BsonExpression predicate) => _collection.Count(predicate);

        public int Count(string predicate, BsonDocument parameters) => _collection.Count(predicate, parameters);

        public int Count(string predicate, params BsonValue[] args) => _collection.Count(predicate, args);

        public int Count(Expression<Func<T, bool>> predicate) => _collection.Count(predicate);

        public int Count(LiteDB.Query query) => _collection.Count(query);

        public long LongCount() => _collection.LongCount();

        public long LongCount(BsonExpression predicate) => _collection.LongCount(predicate);

        public long LongCount(string predicate, BsonDocument parameters) =>
            _collection.LongCount(predicate, parameters);

        public long LongCount(string predicate, params BsonValue[] args) => _collection.LongCount(predicate, args);

        public long LongCount(Expression<Func<T, bool>> predicate) => _collection.LongCount(predicate);

        public long LongCount(LiteDB.Query query) => _collection.LongCount(query);

        public bool Exists(BsonExpression predicate) => _collection.Exists(predicate);

        public bool Exists(string predicate, BsonDocument parameters) => _collection.Exists(predicate, parameters);

        public bool Exists(string predicate, params BsonValue[] args) => _collection.Exists(predicate, args);

        public bool Exists(Expression<Func<T, bool>> predicate) => _collection.Exists(predicate);

        public bool Exists(LiteDB.Query query) => _collection.Exists(query);

        public BsonValue Min(BsonExpression keySelector) => _collection.Min(keySelector);

        public BsonValue Min() => _collection.Min();

        public K Min<K>(Expression<Func<T, K>> keySelector) => _collection.Min(keySelector);

        public BsonValue Max(BsonExpression keySelector) => _collection.Max(keySelector);

        public BsonValue Max() => _collection.Max();

        public K Max<K>(Expression<Func<T, K>> keySelector) => _collection.Max(keySelector);

        public BsonValue Insert(T document) => _collection.Insert(document);

        public BsonValue InsertWithRef(T document) {
            // Fetch all properties with a BsonRef attribute
            HandleRefs(document, nameof(ILiteCollection<T>.Insert));

            // Finally insert the main document
            return Database.GetCollection<T>().Insert(document);
        }

        private void HandleRefs(T document, string methodName) {
            var refProps = typeof(T)
                .GetMembers()
                .Where(m => m.MemberType == MemberTypes.Property || m.MemberType == MemberTypes.Field)
                .Where(m => Attribute.IsDefined(m, typeof(BsonRefAttribute)))
                .Select(m => m switch {
                    FieldInfo f => (member: m, value: f.GetValue(document), type: f.FieldType),
                    PropertyInfo p => (member: m, value: p.GetValue(document), type: p.PropertyType),
                });

            foreach (var vp in refProps) {
                var value = vp.value;
                var type = vp.type;

                // Check if the property value is not null and the property type is a class (excluding string)
                if (value != null && type.IsClass && type != typeof(string)) {
                    var bsonRefAttr = (BsonRefAttribute)vp.member.GetCustomAttribute(typeof(BsonRefAttribute));
                    
                    // If type is a collection, get the element type
                    if (typeof(System.Collections.IEnumerable).IsAssignableFrom(type) && type != typeof(string)) {
                        type = type.IsArray ? type.GetElementType() : type.GetGenericArguments()[0];
                    }

                    // Get the method
                    var method = typeof(LDB).GetMethods()
                        .First(m => m.Name == "GetCollection" 
                                    && m.IsGenericMethod
                                    && m.GetParameters().Length == 2)
                        .MakeGenericMethod(type);
                    // Call it with your parameters
                    var refCollection = method.Invoke(Database, new object[] { bsonRefAttr.Collection, null });

                    // If it's a collection (an array or a list)
                    if (value is System.Collections.IEnumerable enumerable && value.GetType() != typeof(string)) {
                        foreach (var item in enumerable) {
                            // insert each item into its collection
                            var insertMethod = refCollection.GetType().GetMethod(methodName);
                            insertMethod.Invoke(refCollection, new object[] { item });
                        }
                    }
                    else {
                        // insert the item into its collection
                        var insertMethod = refCollection.GetType().GetMethod(methodName);
                        insertMethod.Invoke(refCollection, new object[] { value });
                    }
                }
            }
        }

        public bool Upsert(T document) => _collection.Upsert(document);
        public bool UpsertWithRef(T document) {
            HandleRefs(document, nameof(ILiteCollection<T>.Upsert));
            return _collection.Upsert(document);
        }

        public int Insert(IEnumerable<T> documents) => _collection.Insert(documents);

        public int InsertBulk(IEnumerable<T> documents, int batchSize = 5000) =>
            _collection.InsertBulk(documents, batchSize);


        public LCollection<T> Include<K>(Expression<Func<T, K>> keySelector) =>
            new LCollection<T>(_collection.Include(keySelector), Database);

        public LCollection<T> Include(BsonExpression keySelector) =>
            new LCollection<T>(_collection.Include(keySelector), Database);

        public bool EnsureIndex(string name, BsonExpression expression, bool unique = false) =>
            _collection.EnsureIndex(name, expression, unique);

        public bool EnsureIndex(BsonExpression expression, bool unique = false) =>
            _collection.EnsureIndex(expression, unique);

        public bool EnsureIndex<K>(Expression<Func<T, K>> keySelector, bool unique = false) =>
            _collection.EnsureIndex(keySelector, unique);

        public bool EnsureIndex<K>(string name, Expression<Func<T, K>> keySelector, bool unique = false) =>
            _collection.EnsureIndex(name, keySelector, unique);

        public bool Update(T document) => _collection.Update(document);

        public bool Delete(BsonValue id) => _collection.Delete(id);
        public int DeleteAll() => _collection.DeleteAll();

        public int DeleteMany(BsonExpression predicate) => _collection.DeleteMany(predicate);

        public int DeleteMany(string predicate, BsonDocument parameters) =>
            _collection.DeleteMany(predicate, parameters);

        public int DeleteMany(string predicate, params BsonValue[] args) => _collection.DeleteMany(predicate, args);

        public int DeleteMany(Expression<Func<T, bool>> predicate) => _collection.DeleteMany(predicate);

        public bool DropIndex(string name) => _collection.DropIndex(name);

        public ILiteQueryable<T> Query() => _collection.Query();

        public IEnumerable<T> Find(BsonExpression predicate, int skip = 0, int limit = 2147483647) =>
            _collection.Find(predicate, skip, limit);

        public IEnumerable<T> Find(LiteDB.Query query, int skip = 0, int limit = 2147483647) =>
            _collection.Find(query, skip, limit);

        public IEnumerable<T> Find(Expression<Func<T, bool>> predicate, int skip = 0, int limit = 2147483647) =>
            _collection.Find(predicate, skip, limit);

        public T FindById(BsonValue id) => _collection.FindById(id);

        public T FindOne(BsonExpression predicate) => _collection.FindOne(predicate);

        public T FindOne(string predicate, BsonDocument parameters) => _collection.FindOne(predicate, parameters);

        public T FindOne(BsonExpression predicate, params BsonValue[] args) => _collection.FindOne(predicate, args);

        public T FindOne(Expression<Func<T, bool>> predicate) => _collection.FindOne(predicate);

        public T FindOne(LiteDB.Query query) => _collection.FindOne(query);

        public IEnumerable<T> FindAll() => _collection.FindAll();
    }
}