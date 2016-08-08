using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MongoDB.Driver;
using MongoRepositoryTests.Entities;
using Should;
using Xbehave;

namespace MongoRepository.Tests
{
    public class RepositoryTests
    {
        private void DropDB()
        {
            var url = new MongoUrl(ConfigurationManager.ConnectionStrings["MongoServerSettings"].ConnectionString);
            var client = new MongoClient(url);
            client.DropDatabase(url.DatabaseName);
        }


        [Scenario]
        public void AddAndUpdateTest(IRepository<Customer> customers,
                                     IRepositoryManager<Customer> customersManager,
                                     Customer customer, Customer fetchedCustomer, Customer updatedCustomer)
        {
            "Given a clean test database, a new MongoRepository, and a MongoRepositoryManager".
                f(() =>
                {
                    DropDB();
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
                f(() => {
                            fetchedCustomer = customers.Single(c => c.FirstName == "Bob");
                });
            "Then it should have been retrieved".
                f(() => fetchedCustomer.ShouldNotBeNull());
            "Then its properties should match".
                f((() =>
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
                }));

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
            DropDB();
        }

        [Scenario]
        public async Task ComplexEntityTest()
        {
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

            Assert.IsNotNull(customer.Id);
            Assert.IsNotNull(customer.Orders[0].Items[0].Product.Id);

            // get the orders  
            var theOrders = customerRepo.Where(c => c.Id == customer.Id).Select(c => c.Orders).ToList();
            var theOrderItems = theOrders[0].Select(o => o.Items);

            Assert.IsNotNull(theOrders);
            Assert.IsNotNull(theOrderItems);

            DropDB();
        }


        [Scenario]
        public async Task BatchTest()
        {
            DropDB();

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
            Assert.AreEqual(7, count);
            foreach (Customer c in custlist)
                Assert.AreNotEqual(new string('0', 24), c.Id);

            //Update batch
            foreach (Customer c in custlist)
                c.LastName = c.FirstName;
            await customerRepo.UpdateAsync(custlist);

            foreach (Customer c in customerRepo)
                Assert.AreEqual(c.FirstName, c.LastName);

            //Delete by criteria
            await customerRepo.DeleteAsync(f => f.FirstName.StartsWith("Client"));

            count = await customerRepo.CountAsync();
            Assert.AreEqual(4, count);

            //Delete specific object
            await customerRepo.DeleteAsync(custlist[0]);

            //Test AsQueryable
            var selectedcustomers = from cust in customerRepo
                                    where cust.LastName.EndsWith("C") || cust.LastName.EndsWith("G")
                                    select cust;

            Assert.AreEqual(2, selectedcustomers.ToList().Count);

            count = await customerRepo.CountAsync();
            Assert.AreEqual(3, count);

            //Drop entire repo
            await new MongoRepositoryManager<Customer>().DropAsync();

            count = await customerRepo.CountAsync();
            Assert.AreEqual(0, count);
        }

        [Scenario]
        public async Task CollectionNamesTest()
        {
            var a = new MongoRepository<Animal>();
            var am = new MongoRepositoryManager<Animal>();
            var va = new Dog();
            Assert.IsFalse(am.Exists);
            await a.UpdateAsync(va);
            Assert.IsTrue(am.Exists);
            Assert.IsInstanceOfType(await a.GetByIdAsync(va.Id), typeof(Dog));
            Assert.AreEqual(am.Name, "AnimalsTest");
            Assert.AreEqual(a.RepositoryName, "AnimalsTest");

            var cl = new MongoRepository<CatLike>();
            var clm = new MongoRepositoryManager<CatLike>();
            var vcl = new Lion();
            Assert.IsFalse(clm.Exists);
            await cl.UpdateAsync(vcl);
            Assert.IsTrue(clm.Exists);
            Assert.IsInstanceOfType(await cl.GetByIdAsync(vcl.Id), typeof(Lion));
            Assert.AreEqual(clm.Name, "Catlikes");
            Assert.AreEqual(cl.RepositoryName, "Catlikes");

            var b = new MongoRepository<Bird>();
            var bm = new MongoRepositoryManager<Bird>();
            var vb = new Bird();
            Assert.IsFalse(bm.Exists);
            await b.UpdateAsync(vb);
            Assert.IsTrue(bm.Exists);
            Assert.IsInstanceOfType(await b.GetByIdAsync(vb.Id), typeof(Bird));
            Assert.AreEqual(bm.Name, "Birds");
            Assert.AreEqual(b.RepositoryName, "Birds");

            var l = new MongoRepository<Lion>();
            var lm = new MongoRepositoryManager<Lion>();
            var vl = new Lion();

            //Assert.IsFalse(lm.Exists);   //Should already exist (created by cl)
            await l.UpdateAsync(vl);
            Assert.IsTrue(lm.Exists);
            Assert.IsInstanceOfType(await l.GetByIdAsync(vl.Id), typeof(Lion));
            Assert.AreEqual(lm.Name, "Catlikes");
            Assert.AreEqual(l.RepositoryName, "Catlikes");

            var d = new MongoRepository<Dog>();
            var dm = new MongoRepositoryManager<Dog>();
            var vd = new Dog();

            //Assert.IsFalse(dm.Exists);
            await d.UpdateAsync(vd);
            Assert.IsTrue(dm.Exists);
            Assert.IsInstanceOfType(await d.GetByIdAsync(vd.Id), typeof(Dog));
            Assert.AreEqual(dm.Name, "AnimalsTest");
            Assert.AreEqual(d.RepositoryName, "AnimalsTest");

            var m = new MongoRepository<Bird>();
            var mm = new MongoRepositoryManager<Bird>();
            var vm = new Macaw();

            //Assert.IsFalse(mm.Exists);
            await m.UpdateAsync(vm);
            Assert.IsTrue(mm.Exists);
            Assert.IsInstanceOfType(await m.GetByIdAsync(vm.Id), typeof(Macaw));
            Assert.AreEqual(mm.Name, "Birds");
            Assert.AreEqual(m.RepositoryName, "Birds");

            var w = new MongoRepository<Whale>();
            var wm = new MongoRepositoryManager<Whale>();
            var vw = new Whale();
            Assert.IsFalse(wm.Exists);
            await w.UpdateAsync(vw);
            Assert.IsTrue(wm.Exists);
            Assert.IsInstanceOfType(await w.GetByIdAsync(vw.Id), typeof(Whale));
            Assert.AreEqual(wm.Name, "Whale");
            Assert.AreEqual(w.RepositoryName, "Whale");
        }

        [Scenario]
        public async Task CustomIdTest()
        {
            var x = new MongoRepository<CustomIDEntity>();
            var xm = new MongoRepositoryManager<CustomIDEntity>();

            await x.AddAsync(new CustomIDEntity {Id = "aaa"});

            Assert.IsTrue(xm.Exists);
            Assert.IsInstanceOfType(await x.GetByIdAsync("aaa"), typeof(CustomIDEntity));

            Assert.AreEqual("aaa", (await x.GetByIdAsync("aaa")).Id);

            await x.DeleteAsync("aaa");
            Assert.AreEqual(0, await x.CountAsync());

            var y = new MongoRepository<CustomIDEntityCustomCollection>();
            var ym = new MongoRepositoryManager<CustomIDEntityCustomCollection>();

            await y.AddAsync(new CustomIDEntityCustomCollection {Id = "xyz"});

            Assert.IsTrue(ym.Exists);
            Assert.AreEqual(ym.Name, "MyTestCollection");
            Assert.AreEqual(y.RepositoryName, "MyTestCollection");
            Assert.IsInstanceOfType(await y.GetByIdAsync("xyz"), typeof(CustomIDEntityCustomCollection));

            await y.DeleteAsync("xyz");
            Assert.AreEqual(0, await y.CountAsync());
        }

        [Scenario]
        public async Task CustomIdTypeTest()
        {
            var xint = new MongoRepository<IntCustomer, int>();
            await xint.AddAsync(new IntCustomer {Id = 1, Name = "Test A"});
            await xint.AddAsync(new IntCustomer {Id = 2, Name = "Test B"});

            var yint = await xint.GetByIdAsync(2);
            Assert.AreEqual(yint.Name, "Test B");

            await xint.DeleteAsync(2);
            Assert.AreEqual(1, await xint.CountAsync());
        }

        [Scenario]
        public async Task OverrideCollectionName()
        {
            IRepository<Customer> customerRepo =
                new MongoRepository<Customer>("mongodb://localhost/MongoRepositoryTests", "TestCustomers123");
            await customerRepo.AddAsync(new Customer {FirstName = "Test"});
            Assert.IsTrue(customerRepo.Single().FirstName.Equals("Test"));
            Assert.AreEqual("TestCustomers123", customerRepo.RepositoryName);

            IRepositoryManager<Customer> curstomerRepoManager =
                new MongoRepositoryManager<Customer>("mongodb://localhost/MongoRepositoryTests", "TestCustomers123");
            Assert.AreEqual("TestCustomers123", curstomerRepoManager.Name);
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
            var specialRepository = new MongoRepository<SpecialA>();
            Assert.IsTrue(specialRepository != null);
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
            var repo = new MongoRepository<ClassA>
            {
                new ClassB {Prop1 = "A", Prop2 = "B"},
                new ClassC {Prop1 = "A", Prop3 = "C"}
            };

            Assert.AreEqual(2, await repo.CountAsync());

            Assert.AreEqual(2, repo.OfType<ClassA>().Count());
            Assert.AreEqual(1, repo.OfType<ClassB>().Count());
            Assert.AreEqual(1, repo.OfType<ClassC>().Count());
        }

        #endregion
    }
}