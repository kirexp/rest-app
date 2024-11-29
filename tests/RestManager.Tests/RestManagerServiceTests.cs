using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RestManager.Configurations;
using RestManager.Managers;
using RestManager.Models;
using RestManager.Tests.Utils;
using Xunit.Abstractions;

namespace RestManager.Tests;

public class RestManagerServiceTests
{
    private ILogger<RestManagerService> _logger;

    public RestManagerServiceTests(ITestOutputHelper output)
    {
        _logger = new TestLogger<RestManagerService>("test", output.WriteLine);
    }

    public class OnArrive : RestManagerServiceTests
    {
        readonly IOptions<TablesConfiguration> SimpleCapactiy = Options.Create(new TablesConfiguration(new int [] {1, 2, 3, 4, 5, 6}));

        readonly IOptions<TablesConfiguration> SmallCapacity =  Options.Create(new TablesConfiguration(new int [] {1, 2}));

        readonly IOptions<TablesConfiguration> LargeScaleCapacity = Options.Create(new TablesConfiguration(new int [] {4, 5, 6, 4, 5, 6}));
        public OnArrive(ITestOutputHelper output) : base(output)
        {
        }

        [Fact]
        public void Should_Create_Instance()
        {
            var tables = new int[]
            {
                1,
                2,
                3,
                4,
                5,
                6
            };

            var act = () =>
            {
                var opts = Options.Create(new TablesConfiguration(tables));
                var sut = new RestManagerService(_logger, opts);
            };

            act.Should().NotThrow();
        }

        // used theory for better debug experience.
        [Theory]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(3)]
        [InlineData(4)]
        [InlineData(5)]
        [InlineData(6)]
        public void Should_Assign_Table_AccordingToRules1_1_Ideally(int groupCount)
        {
            var group = new ClientsGroup(groupCount, groupId: groupCount);
            var sut = new RestManagerService(_logger, SimpleCapactiy);

            sut.OnArrive(group);

            var state = sut.GetState();
            state.tables.Count(x => x.IsFullyOccupied).Should().Be(1);
            state.tables.Count(x => x.IsPartiallyOccupied).Should().Be(0);
            state.queue.Count.Should().Be(0);
        }

        // checking the case when guests go to the waiting list.
        [Theory]
        [InlineData(5)]
        [InlineData(6)]
        public void Should_Assign_GroupToTheQueueInCaseCompatibleTableIsOccupied(int groupCount)
        {
            var group = new ClientsGroup(groupCount,  groupId: groupCount);
            var sut = new RestManagerService(_logger, SmallCapacity);

            sut.OnArrive(group);
            var state = sut.GetState();
            state.tables.Any(c => c.IsFullyAvailable == false).Should().BeFalse();
            state.queue.Count.Should().Be(1);
        }

        /// <summary>
        /// Case when all 6 group of people have been seated without queue
        /// </summary>
        [Fact]
        public void Should_AssignSomeSeatsCase1()
        {
            for (int i = 1; i <= 5; i++)
            {
                var sut = new RestManagerService(_logger, SimpleCapactiy);
                sut.OnArrive(new ClientsGroup(i, i));

                var state = sut.GetState();

                var potentiallyOccupiedTable = state.tables.First(x => !x.IsFullyAvailable);
                potentiallyOccupiedTable.IsFullyAvailable.Should().BeFalse();
                state.queue.Count.Should().Be(0);
            }
        }


        /// <summary>
        /// Case when 3 groups arrived with capacity of 3 people
        /// </summary>
        [Fact]
        public void Should_AssignSomeSeatsCase2()
        {
            var sut = new RestManagerService(_logger, SimpleCapactiy);

            sut.OnArrive(new ClientsGroup(3, 1));
            sut.OnArrive(new ClientsGroup(3, 2));
            sut.OnArrive(new ClientsGroup(3, 3));
            sut.OnArrive(new ClientsGroup(3, 4));

            var state = sut.GetState();

            state.queue.Count.Should().Be(0);
            state.tables.Count(x => x.IsPartiallyOccupied).Should().Be(3);
            state.tables.Count(x => x.IsFullyOccupied).Should().Be(1);
        }

        /// <summary>
        /// Case when 4 groups of 4 people arrived, queue should be used for 1 group.
        /// </summary>
        [Fact]
        public void Should_AssignSomeSeatsCase3()
        {
            var sut = new RestManagerService(_logger, SimpleCapactiy);

            sut.OnArrive(new ClientsGroup(4, groupId: 1));
            sut.OnArrive(new ClientsGroup(4, groupId: 2));
            sut.OnArrive(new ClientsGroup(4, groupId: 3));
            sut.OnArrive(new ClientsGroup(4, groupId: 4));

            var state = sut.GetState();

            state.queue.Count.Should().Be(1);
            state.tables.Count(x => x.IsPartiallyOccupied).Should().Be(2);
            state.tables.Count(x => x.IsFullyOccupied).Should().Be(1);
        }

        /// <summary>
        /// Case when 7 groups of 5, 6, 2, 3, 4, 1, 1 people queue should be used for 1 group.
        /// </summary>
        [Fact]
        public void Should_AssignSomeSeatsCase4()
        {
            var sut = new RestManagerService(_logger, SimpleCapactiy);

            sut.OnArrive(new ClientsGroup(5, 1));
            sut.OnArrive(new ClientsGroup(6, 2));
            sut.OnArrive(new ClientsGroup(2, 3));
            sut.OnArrive(new ClientsGroup(3, 4));
            sut.OnArrive(new ClientsGroup(4, 5));
            sut.OnArrive(new ClientsGroup(1, 6));
            sut.OnArrive(new ClientsGroup(1, 7));

            var state = sut.GetState();

            state.tables.Count(x => x.IsFullyOccupied).Should().Be(6);
            state.tables.Count(x => x.IsPartiallyOccupied).Should().Be(0);
            state.queue.Count.Should().Be(1);
        }

        /// <summary>
        /// Case when 7 groups of 5, 6, 2, 3, 4, 1, 1 people queue should be used for 1 group. + table sharing should happen
        /// Case when tables 4,5,6,4,5,6
        /// </summary>
        [Fact]
        public void Should_AssignSomeSeatsCase5()
        {
            var sut = new RestManagerService(_logger, LargeScaleCapacity);

            sut.OnArrive(new ClientsGroup(5, 1));
            sut.OnArrive(new ClientsGroup(6, 2));
            sut.OnArrive(new ClientsGroup(2, 3));
            sut.OnArrive(new ClientsGroup(3, 4));
            sut.OnArrive(new ClientsGroup(4, 5));
            sut.OnArrive(new ClientsGroup(1, 6));
            sut.OnArrive(new ClientsGroup(1, 7));

            var state = sut.GetState();

            state.tables.Count(x => x.IsFullyOccupied).Should().Be(2); // group with 5,6
            state.tables.Count(x => x.IsPartiallyOccupied).Should().Be(4);
            state.queue.Count.Should().Be(0);
        }
    }

    public class OnLeave : RestManagerServiceTests
    {
        
        readonly IOptions<TablesConfiguration> SimpleCapactiy = Options.Create(new TablesConfiguration(new int [] {1, 2, 3, 4, 5, 6}));

        readonly IOptions<TablesConfiguration> ShorteOccupiedCapacity =  Options.Create(new TablesConfiguration(new int [] {1, 6}));

        public OnLeave(ITestOutputHelper output) : base(output)
        {
        }


        [Theory]
        [InlineData(1)]
        [InlineData(2)]
        [InlineData(3)]
        [InlineData(4)]
        [InlineData(5)]
        [InlineData(6)]
        public void Should_FreeTablesAccordingTo1_1Rule(int groups)
        {
            var group = new ClientsGroup(groups, groupId: groups);
            var sut = new RestManagerService(_logger, SimpleCapactiy);
            sut.OnArrive(group);
            sut.OnLeave(group);

            var state = sut.GetState();

            state.queue.Count.Should().Be(0);
            state.tables.Count(x => x.IsFullyAvailable).Should().Be(SimpleCapactiy.Value.Sizes.Length);
        }

        /// <summary>
        /// Case when 11 groups of 5, 6, 6, 6, 6, 4, 2, 3, 4, 4, 2 people arrive.
        /// Queue size should be 6 after arrivals. After removing group of 6 (b), queue size should be 4.
        /// Lookup for group i should still return a valid table.
        /// </summary>
        [Fact]
        public void Should_ProcessCase1()
        {
            var sut = new RestManagerService(_logger, SimpleCapactiy);
            var a = new ClientsGroup(5, 1);
            var b = new ClientsGroup(6, 2);
            var c = new ClientsGroup(6, 3);
            var d = new ClientsGroup(6, 4);
            var e = new ClientsGroup(6, 5);
            var f = new ClientsGroup(4, 6);
            var g = new ClientsGroup(2, 7);
            var h = new ClientsGroup(3, 8);
            var i = new ClientsGroup(4, 9);
            var j = new ClientsGroup(4, 10);
            var k = new ClientsGroup(2, 11);

            sut.OnArrive(a);
            sut.OnArrive(b);
            sut.OnArrive(c);
            sut.OnArrive(d);
            sut.OnArrive(e);
            sut.OnArrive(f);
            sut.OnArrive(g);
            sut.OnArrive(h);
            sut.OnArrive(i);
            sut.OnArrive(j);
            sut.OnArrive(k);

            var state = sut.GetState();
            state.queue.Count.Should().Be(6);

            sut.OnLeave(b);

            state = sut.GetState();
            var table = sut.Lookup(i.GroupId);
            table.Should().NotBeNull();
            state.queue.Count.Should().Be(4);

            // Information: test - Set group #5 to the table of size 5
            // Information: test - Set group #6 to the table of size 6
            // Information: test - Set group #4 to the table of size 4
            // Information: test - Set group #2 to the table of size 2
            // Information: test - Set group #3 to the table of size 3
            // Information: test - Group ec4bdea7-078a-4d07-a191-7f113458a11c left Table with size 6. Table now has 0/6 seats occupied.
        }

        /// <summary>
        /// Case when 11 groups of 5, 6, 6, 6, 6, 4, 2, 3, 4, 4, 2 people arrive.
        /// Queue size should be 10 after arrivals. After removing group of 5 (a), queue size should be 8.
        /// Lookup for group g should still return a valid table.
        /// </summary>
        [Fact]
        public void Should_ProcessCase2()
        {
            var sut = new RestManagerService(_logger, ShorteOccupiedCapacity);
            var a = new ClientsGroup(5, 1);
            var b = new ClientsGroup(6, 2);
            var c = new ClientsGroup(6, 3);
            var d = new ClientsGroup(6, 4);
            var e = new ClientsGroup(6, 5);
            var f = new ClientsGroup(4, 6);
            var g = new ClientsGroup(2, 7);
            var h = new ClientsGroup(3, 8);
            var i = new ClientsGroup(4, 9);
            var j = new ClientsGroup(4, 10);
            var k = new ClientsGroup(2, 11);

            sut.OnArrive(a);
            sut.OnArrive(b);
            sut.OnArrive(c);
            sut.OnArrive(d);
            sut.OnArrive(e);
            sut.OnArrive(f);
            sut.OnArrive(g);
            sut.OnArrive(h);
            sut.OnArrive(i);
            sut.OnArrive(j);
            sut.OnArrive(k);

            var state = sut.GetState();
            state.queue.Count.Should().Be(10);

            sut.OnLeave(a);

            state = sut.GetState();
            var table = sut.Lookup(g.GroupId);
            table.Should().NotBeNull();
            state.queue.Count.Should().Be(8);
        }
    }
}