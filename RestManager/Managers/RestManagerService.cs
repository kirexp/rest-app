using Microsoft.Extensions.Options;
using RestManager.Configurations;
using RestManager.Models;

namespace RestManager.Managers;

public interface IRestManagerService
{
    void OnArrive(ClientsGroup group);
    void OnLeave(ClientsGroup leavingGroup);
    Table? Lookup(int group);
}

public class RestManagerService : IRestManagerService
{
    private readonly ILogger<RestManagerService> _logger;

    // capacity
    public List<Table> Tables { get; }

    private PriorityQueue<ClientsGroup, int> _arrivedQueue = new();

    private List<Table> _occupied = new();
    
    private object lockObject = new();
    
    private readonly Dictionary<int, Table> _groupTableMap = new();

    public RestManagerService(ILogger<RestManagerService> logger, IOptions<TablesConfiguration> configuration)
    {
        _logger = logger;
        Tables = configuration.Value.Tables;
    }

    public void OnArrive(ClientsGroup group)
    {
        lock (lockObject)
        {
            if (_groupTableMap.ContainsKey(group.GroupId))
            {
                throw new Exception("Group has already been added");
            }
            
            // first priority
            var suitableEmptyTable = Tables.FirstOrDefault(t => t.IsFullyAvailable && t.Size >= group.Size);
            if (suitableEmptyTable != null)
            {
                _logger.LogInformation($"Set group #{group.Size} to the table of size {suitableEmptyTable.Size}");
                SeatGroup(suitableEmptyTable, group);
                return;
            }
            //second priority
            var suitableSharedTable = Tables.FirstOrDefault(t => !t.IsFullyAvailable && t.CanFitMore(group.Size));
            if (suitableSharedTable != null)
            {
                _logger.LogInformation($"Set group #{group} to the table of size {suitableSharedTable.Size} with current size {suitableSharedTable.Size - suitableSharedTable.OccupiedPlaces}");
                SeatGroup(suitableSharedTable, group);
                return;
            }

            _arrivedQueue.Enqueue(group, group.Size);
        }
    }

    public void OnLeave(ClientsGroup leavingGroup)
    {
        if (!_groupTableMap.ContainsKey(leavingGroup.GroupId))
        {
            throw new Exception("Group not found");
        }

        var groupId = leavingGroup.GroupId;
        var seatsAvailable = leavingGroup.Size;
        lock (lockObject)
        {
            if (_groupTableMap.TryGetValue(groupId, out var table))
            {
                var group = table.Groups.FirstOrDefault(g => g.GroupId == groupId);
                if (group != null)
                {
                    GetRidOfTheGroup(table, group);
                    _logger.LogInformation($"Group {groupId} left Table with size {table.Size}. " +
                                           $"Table now has {table.OccupiedPlaces}/{table.Size} seats occupied.");
                    
                    while (_arrivedQueue.TryDequeue(out var nextGroup, out _))
                    {
                        seatsAvailable -= nextGroup.Size;
                        if (TryFitGroupFromQueue(nextGroup))
                        {
                            if (_arrivedQueue.TryPeek(out var arrived, out _))
                            {
                                if (seatsAvailable < arrived.Size)
                                {
                                    break;
                                }
                            }
                        };
                    }
                }
            }
            else
            {
                throw new Exception("Group is not found");
            }
        }
    }

    // return table where a given client group is seated,
    // or null if it is still queuing or has already left
    public Table? Lookup(int groupId)
    {
        return _groupTableMap.GetValueOrDefault(groupId);
    }

    public (List<Table> tables, PriorityQueue<ClientsGroup, int> queue) GetState()
    {
        return (Tables, _arrivedQueue);
    }
    
    private void SeatGroup(Table table, ClientsGroup group)
    {
        table.ArrangeGuesses(group);
        _groupTableMap.Add(group.GroupId, table);
    }

    private void GetRidOfTheGroup(Table table, ClientsGroup group)
    {
        table.Groups.Remove(group);
        table.OccupiedPlaces -= group.Size;
        _groupTableMap.Remove(group.GroupId);
    }
    
    private bool TryFitGroupFromQueue(ClientsGroup group)
    {
        var suitableTable = Tables.FirstOrDefault(t => t.CanFitMore(group.Size));
        if (suitableTable != null)
        {
            SeatGroup(suitableTable, group);
            return true;
        }
        return false;
    }
}