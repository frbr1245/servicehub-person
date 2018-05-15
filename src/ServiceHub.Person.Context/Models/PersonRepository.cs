﻿using System;
using MongoDB.Bson;
using MongoDB.Driver;
using System.Collections.Generic;
using Microsoft.Extensions.Options;
using System.Threading.Tasks;
using System.Net.Http;
using Newtonsoft.Json;
using ServiceHub.Person.Context.Models;
using System.Net.Http.Headers;
using ServiceHub.Person.Context.Interfaces;
using System.Data;
using ServiceHub.Person.Library.Models;

namespace ServiceHub.Person.Context.Models 
{
    public class PersonRepository : IRepository<Person>
    {
        public const string MongoDbIdName = "_id";

        protected readonly IMongoClient _client;

        protected readonly IMongoDatabase _db;

        private readonly IMongoCollection<Person> _collection;
        
        private readonly HttpClient _salesforceapi;

        private readonly string _baseUrl;

        private readonly MetaData _metadata;

        private readonly string _MetaDataCollection;
        private readonly string _metadataId;

        public PersonRepository()
        {
            _client = new MongoClient(Settings.ConnectionString);
            _salesforceapi = new HttpClient();
            _baseUrl = Settings.BaseURL;
            _MetaDataCollection = Settings.MetaDataCollectionName;
            _metadataId = Settings.MetaDataId;
            if (_client != null)
            {
                _db = _client.GetDatabase(Settings.Database);
                _collection = _db.GetCollection<Person>(Settings.CollectionName);

                // Obtaining metadata
                _metadata = _db.GetCollection<MetaData>(Settings.MetaDataCollectionName)
                                .Find(p=> p.ModelId == Settings.MetaDataId).FirstOrDefault();
            }
        }

        public async Task<IEnumerable<Person>> GetAll()
        {
            return await _collection.Find(new BsonDocument()).ToListAsync();
        }

        public async Task<Person> GetById(string id)
        {
            long newId;
            try
            {
                newId = Convert.ToInt64(id);
            }
            catch (Exception ex)
            {
                throw new ArgumentException("Invalid ID", ex);
            }
            Person result = await _collection.Find(p => p.PersonID == newId).FirstAsync();
            return result;
        }

        private async Task<List<Person>> ReadFromSalesForce()
        {
            var result = await _salesforceapi.GetAsync( _baseUrl);

            if (result.IsSuccessStatusCode)
            {
                var content = await result.Content.ReadAsStringAsync();
                List<Person> personlist = null;

                if  (content != null  )
                {               
                    personlist = JsonConvert.DeserializeObject<List<Person>>(content);
                }
                return personlist;
            }
            else
                return null;
        }

        public void UpdateMongoDB(List<Person> personlist)
        {
            // Get the contacts in the Person collection, check for existing contacts.
            // If not present, add to collection.
            var mongoContacts = _collection.Find(_ => true).ToList();
            foreach (var person in personlist)
            {

                var existingContact = mongoContacts.Find(item => person.ModelId == item.ModelId);

                if (existingContact == null)
                {
                    _collection.InsertOne(person);
                }
            }

            foreach (var mongoContact in mongoContacts)
            {
                var existingContact = personlist.Find(item => mongoContact.ModelId == item.ModelId);
                if (existingContact == null)
                {
                    _collection.DeleteMany(Builders<Person>.Filter.Eq("_id", new ObjectId(mongoContact.ModelId)));
                }

            }
        }
        
        public async Task Create(Person model)
        {
            if (model == null)
            {
                throw new ArgumentNullException(nameof(model));
            }
            model.ModelId = null;
            model.LastModified = DateTime.UtcNow;
            await _collection.InsertOneAsync(model);
        }

        public async Task<bool> UpdateById(string id, Person model)
        {
            ObjectId theObjectId;
            try
            {
                theObjectId = new ObjectId(id);
            }
            catch (Exception ex)
            {
                throw new ArgumentException("Invalid ID", nameof(id), ex);
            }

            if (model == null)
            {
                throw new ArgumentNullException(nameof(model));
            }
            model.ModelId = id;
            model.LastModified = DateTime.Now;
            //find a document which contains the passed model.
            FilterDefinition<Person> filter = Builders<Person>.Filter.Eq(MongoDbIdName, theObjectId);
            ReplaceOneResult result = await _collection.ReplaceOneAsync(filter, model);
            return (result.IsAcknowledged && result.ModifiedCount == 1);            
        }

        public async Task<bool> DeleteById(string id)
        {
            ObjectId theObjectId;
            try
            {
                theObjectId = new ObjectId(id);
            }
            catch (Exception ex)
            {
                throw new ArgumentException("Invalid ID", ex);
            }
            //filter based on given model
            FilterDefinition<Person> filter = Builders<Person>.Filter.Eq(MongoDbIdName, theObjectId);
            DeleteResult result = await _collection.DeleteOneAsync(filter);
            return (result.IsAcknowledged && result.DeletedCount == 1);
        }

        public void UpdateRepository()
        {
            var updateList = this.ReadFromSalesForce().GetAwaiter().GetResult(); 
            if(updateList != null)
            {
                this.UpdateMongoDB(updateList);
                var theObjectId = new ObjectId(_metadataId);
                _metadata.LastModified = DateTime.Now;
                //find a document which contains the passed model.
                FilterDefinition<MetaData> filter = Builders<MetaData>.Filter.Eq(MongoDbIdName, theObjectId);
                ReplaceOneResult result = _db.GetCollection<MetaData>(_MetaDataCollection)
                                            .ReplaceOne(filter, _metadata);
                if(!(result.IsAcknowledged && result.ModifiedCount == 1))
                {
                    throw new DBConcurrencyException("Global time not updated.");
                }                                
            }
        }
        public DateTime LastGlobalUpdateTime()
        {
            return _metadata.LastModified;
        }
    }
}