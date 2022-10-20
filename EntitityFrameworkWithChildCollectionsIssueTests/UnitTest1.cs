using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace EntitityFrameworkWithChildCollectionsIssueTests
{
    public class Tests
    {
        record TestData(string Name, IList<TestSubData> Children, long Id = default)
        {
            public TestData() : this(default, default) { }
        };

        record TestSubData(string Name, long Id = default)
        {
            // for DB records we need a default constructor without parameters
            public TestSubData() : this(default) { }
        };

        class TestDb : DbContext
        {
            public DbSet<TestData> TestData { get; set; } = default!;
            public DbSet<TestSubData> TestSubData { get; set; } = default!;

            public TestDb() : base(new DbContextOptionsBuilder().UseInMemoryDatabase("testDb", new InMemoryDatabaseRoot()).Options) 
            { }

            protected override void OnModelCreating(ModelBuilder modelBuilder)
            { 
            }
        }

        [Test]
        public async Task Test1()
        {
            // Create DB
            using TestDb testDb = new();

            // define data
            var sharedChild = new TestSubData("Shared", 1);
            var entry1 = new TestData("Max", 
                new List<TestSubData> {
                    sharedChild,
                    new TestSubData("unique #1",2),
                });
            var entry2 = new TestData("Max",
                new List<TestSubData> {
                    sharedChild,
                    new TestSubData("unique #2",3),
                });

            // now add the data, but try to replace existing sub-data with already existing entries
            testDb.Add(entry1);
            testDb.Add(entry2);

            // Save DB
            await testDb.SaveChangesAsync();

            // ReadData and assert
            Assert.That(testDb.TestData.Count(), Is.EqualTo(2));  // 2x TestData
            Assert.That(testDb.TestSubData.Count(), Is.EqualTo(3));  // 3x TestSubData

            var data1 = testDb.TestData.Include(x => x.Children).ToArray();
            Assert.That(data1[0].Children.Count, Is.EqualTo(2));
            Assert.That(data1[1].Children.Count, Is.EqualTo(2));
        }
    }
}