using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using MongoDB.Driver;
using Should;
using Xbehave;

namespace MongoRepository.Tests
{
    public class SpecializedRepoComplexObjectTest
    {
        public interface ITestEntity
        {
            string Property1 { get; }
            int Property2 { get; }
        }

        public class EntityA : ITestEntity
        {
            public string SpecialAProperty { get; set; }

            #region Implementation of ITestEntity

            public string Property1 { get; set; }
            public int Property2 { get; set; }

            #endregion
        }

        public class EntityB : ITestEntity
        {
            public double SpecialBProperty { get; set; }

            #region Implementation of ITestEntity

            public string Property1 { get; set; }
            public int Property2 { get; set; }

            #endregion
        }

        [CollectionName("TestEntities")]
        public class TestEntityWrapper : Entity
        {
            public ITestEntity WrappedEntity { get; set; }
        }

        public class TestEntityRepository : MongoRepository<TestEntityWrapper>
        {
            public async Task<bool> Property1ValueExists(string value,
                                                         CancellationToken cancellationToken =
                                                             default(CancellationToken))
            {
                // This will not work because the MongoDb driver cannont instantiate the ITestEntity interface
                //var filter = Builders<TestEntityWrapper>.Filter.Eq(f=>f.WrappedEntity.Property1, value);
                // This will work, though
                var filter = Builders<TestEntityWrapper>.Filter.Eq("WrappedEntity.Property1", value);
                return await Collection.Find(filter).Limit(1).AnyAsync(cancellationToken: cancellationToken);
            }
        }

        [Scenario]
        [SuppressMessage("ReSharper", "PossibleNullReferenceException")]
        public void WrappedObjectTest(TestEntityRepository entities,
                                      IRepositoryManager<TestEntityWrapper> entitiesManager,
                                      TestEntityWrapper entityWrapper,
                                      TestEntityWrapper retrievedEntityWrapper)
        {
            "Given a clean test database, a new MongoRepository, and a MongoRepositoryManager".
                f(() =>
                {
                    MongoTestUtils.DropDb();
                    entities = new TestEntityRepository();
                    entitiesManager = new MongoRepositoryManager<TestEntityWrapper>();
                });
            "Then the repository should not initially exist".
                f((() =>
                {
                    entitiesManager.Exists.ShouldBeFalse();
                }));
            "When inserting a new entity".
                f((async () =>
                {
                    var entity = new EntityA
                    {
                        Property1 = "PropA",
                        Property2 = 42,
                        SpecialAProperty = "SpecialAProp"
                    };
                    entityWrapper = new TestEntityWrapper {WrappedEntity = entity};
                    await entities.AddAsync(entityWrapper);
                }));
            "Then the repository should start to exist".
                f(() =>
                {
                    entitiesManager.Exists.ShouldBeTrue(); 
                    MongoDbManager mgr = new MongoDbManager();
                    var collections = mgr.GetCollectionsList();
                    collections.ShouldContain("TestEntities");
                });
            "Then the entity wrapper should be given a new ID".
                f(() => entityWrapper.Id.ShouldNotBeNull());
            "Then the collection count should be 1".
                f(async () => 1L.ShouldEqual(await entities.CountAsync()));
            // The following does not work because the MongoDb driver cannot instantiate the ITestEntity interface
            //"Then an entity implementing the ITestEntity should exist in the repository".
            //    f(async () => (await entities.ExistsAsync(c => c.WrappedEntity.Property1 == "PropA")).ShouldBeTrue());
            // Instead the specialized repository must implement the desired actions using Builders
            // The reason being that the former approach using expressions queries the database for data and performs
            // the filtering clientside, while the latter approach happens entirely serverside
            "Then an entity with the value 'PropA' on Property1 should exist in the repository".
                f(async () => (await entities.Property1ValueExists("PropA")).ShouldBeTrue());
            "Then an entity wrapper holding a wrapped EntityA should be retrievable from the repository".
                f(async () =>
                {
                    retrievedEntityWrapper = await entities.GetByIdAsync(entityWrapper.Id);
                    retrievedEntityWrapper.WrappedEntity.ShouldBeType<EntityA>();
                });
            "Then that wrapped entity should have the appropriate property values".
                f((() =>
                {
                    var entity = retrievedEntityWrapper.WrappedEntity as EntityA;
                    var originalEntity = entityWrapper.WrappedEntity as EntityA;
                    entity.ShouldNotBeNull();
                    originalEntity.ShouldNotBeNull();
                    entity.Property1.ShouldEqual(originalEntity.Property1);
                    entity.Property2.ShouldEqual(originalEntity.Property2);
                    entity.SpecialAProperty.ShouldEqual(originalEntity.SpecialAProperty);
                }));
        }
    }
}