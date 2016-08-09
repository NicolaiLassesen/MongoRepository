using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MongoRepositoryTests.Entities;
using Should;
using Xbehave;

namespace MongoRepository.Tests
{
    public class RepositoryTests
    {
        [Scenario]
        public void SomeStrangeTest()
        {
        }

        [Scenario]
        public void AddAndUpdateTest(IRepository<Customer> customers,
                                     IRepositoryManager<Customer> customersManager,
                                     Customer customer,
                                     Customer fetchedCustomer,
                                     Customer updatedCustomer)
        {
            "Given a clean test database, a new MongoRepository, and a MongoRepositoryManager".
                f(() =>
                {
                    MongoTestUtils.DropDb();
                    customers = new MongoRepository<Customer>();
                    customersManager = new MongoRepositoryManager<Customer>();
                });
            "Then the repository should not initially exist".
                f((() =>
                {
                    customersManager.Exists.ShouldBeFalse();
                }));

            "When inserting a new customer from Alaska".
                f((async () =>
                {
                    customer = new Customer
                    {
                        FirstName = "Bob",
                        LastName = "Dillon",
                        Phone = "0900999899",
                        Email = "Bob.dil@snailmail.com",
                        HomeAddress = new Address
                        {
                            Address1 = "North kingdom 15 west",
                            Address2 = "1 north way",
                            PostCode = "40990",
                            City = "George Town",
                            Country = "Alaska"
                        }
                    };
                    await customers.AddAsync(customer);
                }));
            "Then the repository should start to exist".
                f((() => customersManager.Exists.ShouldBeTrue()));
            "Then the customer should be given a new ID".
                f((() => customer.Id.ShouldNotBeNull()));
            "Then a customer from Alaska should exist in the repository".
                f(async () => (await customers.ExistsAsync(c => c.HomeAddress.Country == "Alaska")).ShouldBeTrue());

            "When the customer is fetched back from the repository".
                f(() => fetchedCustomer = customers.Single(c => c.FirstName == "Bob"));
            "Then it should have been retrieved".
                f(() => fetchedCustomer.ShouldNotBeNull());
            "Then its properties should match".
                f(() =>
                {
                    fetchedCustomer.Id.ShouldEqual(customer.Id);
                    fetchedCustomer.FirstName.ShouldEqual(customer.FirstName);
                    fetchedCustomer.LastName.ShouldEqual(customer.LastName);
                    fetchedCustomer.Phone.ShouldEqual(customer.Phone);
                    fetchedCustomer.Email.ShouldEqual(customer.Email);
                    fetchedCustomer.HomeAddress.Address1.ShouldEqual(customer.HomeAddress.Address1);
                    fetchedCustomer.HomeAddress.Address2.ShouldEqual(customer.HomeAddress.Address2);
                    fetchedCustomer.HomeAddress.PostCode.ShouldEqual(customer.HomeAddress.PostCode);
                    fetchedCustomer.HomeAddress.City.ShouldEqual(customer.HomeAddress.City);
                    fetchedCustomer.HomeAddress.Country.ShouldEqual(customer.HomeAddress.Country);
                });

            "When updating properties of the fetched customer".
                f(async () =>
                {
                    fetchedCustomer.Phone = "10110111";
                    fetchedCustomer.Email = "dil.bob@fastmail.org";
                    await customers.UpdateAsync(fetchedCustomer);
                });
            "When retrieving the customer again".
                f(async () => updatedCustomer = await customers.GetByIdAsync(customer.Id));
            "Then those properties should have been updated in the repository".
                f(() =>
                {
                    updatedCustomer.ShouldNotBeNull();
                    updatedCustomer.Phone.ShouldEqual(fetchedCustomer.Phone);
                    updatedCustomer.Email.ShouldEqual(fetchedCustomer.Email);
                });
            MongoTestUtils.DropDb();
        }

        [Scenario]
        public async Task ComplexEntityTest()
        {
            MongoTestUtils.DropDb();
            IRepository<Customer> customerRepo = new MongoRepository<Customer>();
            IRepository<Product> productRepo = new MongoRepository<Product>();

            var customer = new Customer
            {
                FirstName = "Erik",
                LastName = "Swaun",
                Phone = "123 99 8767",
                Email = "erick@mail.com",
                HomeAddress = new Address
                {
                    Address1 = "Main bulevard",
                    Address2 = "1 west way",
                    PostCode = "89560",
                    City = "Tempare",
                    Country = "Arizona"
                }
            };

            var order = new Order {PurchaseDate = DateTime.Now.AddDays(-2)};
            var shampoo = await productRepo.AddAsync(new Product {Name = "Palmolive Shampoo", Price = 5});
            var paste = await productRepo.AddAsync(new Product {Name = "Mcleans Paste", Price = 4});
            order.Items = new[]
            {
                new OrderItem {Product = shampoo, Quantity = 1},
                new OrderItem {Product = paste, Quantity = 2}
            };

            customer.Orders = new[]
            {
                order
            };

            await customerRepo.AddAsync(customer);

            customer.Id.ShouldNotBeNull();
            customer.Orders[0].Items[0].Product.Id.ShouldNotBeNull();

            // get the orders  
            var theOrders = customerRepo.Where(c => c.Id == customer.Id).Select(c => c.Orders).ToList();
            var theOrderItems = theOrders[0].Select(o => o.Items);

            theOrders.ShouldNotBeNull();
            theOrderItems.ShouldNotBeNull();

            MongoTestUtils.DropDb();
        }


        [Scenario]
        public async Task BatchTest()
        {
            MongoTestUtils.DropDb();

            IRepository<Customer> customerRepo = new MongoRepository<Customer>();
            var custlist = new List<Customer>(new[]
            {
                new Customer {FirstName = "Customer A"},
                new Customer {FirstName = "Client B"},
                new Customer {FirstName = "Customer C"},
                new Customer {FirstName = "Client D"},
                new Customer {FirstName = "Customer E"},
                new Customer {FirstName = "Client F"},
                new Customer {FirstName = "Customer G"}
            });

            //Insert batch
            await customerRepo.AddAsync(custlist);

            var count = await customerRepo.CountAsync();
            count.ShouldEqual(7);
            string notExpectedValue = new string('0', 24);
            foreach (Customer c in custlist)
                c.Id.ShouldNotEqual(notExpectedValue);

            //Update batch
            foreach (Customer c in custlist)
                c.LastName = c.FirstName;
            await customerRepo.UpdateAsync(custlist);

            foreach (Customer c in customerRepo)
                c.FirstName.ShouldEqual(c.LastName);

            //Delete by criteria
            await customerRepo.DeleteAsync(f => f.FirstName.StartsWith("Client"));

            count = await customerRepo.CountAsync();
            count.ShouldEqual(4);

            //Delete specific object
            await customerRepo.DeleteAsync(custlist[0]);

            //Test AsQueryable
            var selectedcustomers = from cust in customerRepo
                                    where cust.LastName.EndsWith("C") || cust.LastName.EndsWith("G")
                                    select cust;

            selectedcustomers.ToList().Count.ShouldEqual(2);

            count = await customerRepo.CountAsync();
            count.ShouldEqual(3);

            //Drop entire repo
            await new MongoRepositoryManager<Customer>().DropAsync();

            count = await customerRepo.CountAsync();
            count.ShouldEqual(0);

            MongoTestUtils.DropDb();
        }

        [Scenario]
        public async Task CollectionNamesTest()
        {
            MongoTestUtils.DropDb();

            var a = new MongoRepository<Animal>();
            var am = new MongoRepositoryManager<Animal>();
            var va = new Dog();
            am.Exists.ShouldBeFalse();
            await a.UpdateAsync(va);
            am.Exists.ShouldBeTrue();
            var animal = await a.GetByIdAsync(va.Id);
            animal.ShouldBeType<Dog>();
            am.Name.ShouldEqual("AnimalsTest");
            a.RepositoryName.ShouldEqual("AnimalsTest");

            var cl = new MongoRepository<CatLike>();
            var clm = new MongoRepositoryManager<CatLike>();
            var vcl = new Lion();
            clm.Exists.ShouldBeFalse();
            await cl.UpdateAsync(vcl);
            clm.Exists.ShouldBeTrue();
            var catLike = await cl.GetByIdAsync(vcl.Id);
            catLike.ShouldBeType<Lion>();
            clm.Name.ShouldEqual("Catlikes");
            cl.RepositoryName.ShouldEqual("Catlikes");

            var b = new MongoRepository<Bird>();
            var bm = new MongoRepositoryManager<Bird>();
            var vb = new Bird();
            bm.Exists.ShouldBeFalse();
            await b.UpdateAsync(vb);
            bm.Exists.ShouldBeTrue();
            var bird = await b.GetByIdAsync(vb.Id);
            bird.ShouldBeType<Bird>();
            bm.Name.ShouldEqual("Birds");
            b.RepositoryName.ShouldEqual("Birds");

            var l = new MongoRepository<Lion>();
            var lm = new MongoRepositoryManager<Lion>();
            var vl = new Lion();

            //Assert.IsFalse(lm.Exists);   //Should already exist (created by cl)
            await l.UpdateAsync(vl);
            lm.Exists.ShouldBeTrue();
            var lion = await l.GetByIdAsync(vl.Id);
            lion.ShouldBeType<Lion>();
            lm.Name.ShouldEqual("Catlikes");
            l.RepositoryName.ShouldEqual("Catlikes");

            var d = new MongoRepository<Dog>();
            var dm = new MongoRepositoryManager<Dog>();
            var vd = new Dog();

            //Assert.IsFalse(dm.Exists);
            await d.UpdateAsync(vd);
            dm.Exists.ShouldBeTrue();
            var dog = await d.GetByIdAsync(vd.Id);
            dog.ShouldBeType<Dog>();
            dm.Name.ShouldEqual("AnimalsTest");
            d.RepositoryName.ShouldEqual("AnimalsTest");

            var m = new MongoRepository<Bird>();
            var mm = new MongoRepositoryManager<Bird>();
            var vm = new Macaw();

            //Assert.IsFalse(mm.Exists);
            await m.UpdateAsync(vm);
            mm.Exists.ShouldBeTrue();
            var macaw = await m.GetByIdAsync(vm.Id);
            macaw.ShouldBeType<Macaw>();
            mm.Name.ShouldEqual("Birds");
            m.RepositoryName.ShouldEqual("Birds");

            var w = new MongoRepository<Whale>();
            var wm = new MongoRepositoryManager<Whale>();
            var vw = new Whale();
            wm.Exists.ShouldBeFalse();
            await w.UpdateAsync(vw);
            wm.Exists.ShouldBeTrue();
            var whale = await w.GetByIdAsync(vw.Id);
            whale.ShouldBeType<Whale>();
            wm.Name.ShouldEqual("Whale");
            w.RepositoryName.ShouldEqual("Whale");

            MongoTestUtils.DropDb();
        }

        [Scenario]
        public async Task CustomIdTest()
        {
            MongoTestUtils.DropDb();

            var x = new MongoRepository<CustomIDEntity>();
            var xm = new MongoRepositoryManager<CustomIDEntity>();

            await x.AddAsync(new CustomIDEntity {Id = "aaa"});

            xm.Exists.ShouldBeTrue();
            var aaa = await x.GetByIdAsync("aaa");
            aaa.ShouldBeType<CustomIDEntity>();
            aaa.Id.ShouldEqual("aaa");

            await x.DeleteAsync("aaa");
            0L.ShouldEqual(await x.CountAsync());

            var y = new MongoRepository<CustomIDEntityCustomCollection>();
            var ym = new MongoRepositoryManager<CustomIDEntityCustomCollection>();

            await y.AddAsync(new CustomIDEntityCustomCollection {Id = "xyz"});

            ym.Exists.ShouldBeTrue();
            ym.Name.ShouldEqual("MyTestCollection");
            y.RepositoryName.ShouldEqual("MyTestCollection");
            var xyz = await y.GetByIdAsync("xyz");
            xyz.ShouldBeType<CustomIDEntityCustomCollection>();

            await y.DeleteAsync("xyz");
            0L.ShouldEqual(await y.CountAsync());

            MongoTestUtils.DropDb();
        }

        [Scenario]
        public async Task CustomIdTypeTest()
        {
            MongoTestUtils.DropDb();

            var xint = new MongoRepository<IntCustomer, int>();
            await xint.AddAsync(new IntCustomer {Id = 1, Name = "Test A"});
            await xint.AddAsync(new IntCustomer {Id = 2, Name = "Test B"});

            var yint = await xint.GetByIdAsync(2);
            yint.Name.ShouldEqual("Test B");

            await xint.DeleteAsync(2);
            1L.ShouldEqual(await xint.CountAsync());

            MongoTestUtils.DropDb();
        }

        [Scenario]
        public async Task OverrideCollectionName()
        {
            MongoTestUtils.DropDb();

            IRepository<Customer> customerRepo =
                new MongoRepository<Customer>("mongodb://localhost/MongoRepositoryTests", "TestCustomers123");
            await customerRepo.AddAsync(new Customer {FirstName = "Test"});
            customerRepo.Single().FirstName.ShouldEqual("Test");
            customerRepo.RepositoryName.ShouldEqual("TestCustomers123");

            IRepositoryManager<Customer> customerRepoManager =
                new MongoRepositoryManager<Customer>("mongodb://localhost/MongoRepositoryTests", "TestCustomers123");
            customerRepoManager.Name.ShouldEqual("TestCustomers123");

            MongoTestUtils.DropDb();
        }

        #region Reproduce issue: https://mongorepository.codeplex.com/discussions/433878

        public abstract class BaseItem : IEntity
        {
            public string Id { get; set; }
        }

        public abstract class BaseA : BaseItem
        {
        }

        public class SpecialA : BaseA
        {
        }

        [Scenario]
        public void Discussion433878()
        {
            MongoTestUtils.DropDb();

            var specialRepository = new MongoRepository<SpecialA>();
            specialRepository.ShouldNotBeNull();

            MongoTestUtils.DropDb();
        }

        #endregion

        #region Reproduce issue: https://mongorepository.codeplex.com/discussions/572382

        public abstract class ClassA : Entity
        {
            public string Prop1 { get; set; }
        }

        public class ClassB : ClassA
        {
            public string Prop2 { get; set; }
        }

        public class ClassC : ClassA
        {
            public string Prop3 { get; set; }
        }

        [Scenario]
        public async Task Discussion572382()
        {
            MongoTestUtils.DropDb();

            var repo = new MongoRepository<ClassA>
            {
                new ClassB {Prop1 = "A", Prop2 = "B"},
                new ClassC {Prop1 = "A", Prop3 = "C"}
            };

            long count = await repo.CountAsync();
            count.ShouldEqual(2);

            repo.OfType<ClassA>().Count().ShouldEqual(2);
            repo.OfType<ClassB>().Count().ShouldEqual(1);
            repo.OfType<ClassC>().Count().ShouldEqual(1);

            MongoTestUtils.DropDb();
        }

        #endregion
    }
}