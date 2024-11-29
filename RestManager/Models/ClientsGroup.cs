
namespace RestManager.Models;

public class ClientsGroup
{
    public int Size { get; init; }

    /// <summary>
    /// Added group id for Lookup
    /// </summary>
    public int GroupId { get; init; }
    
    // for testing purposes.
    public TimeOnly Time { get; } = TimeOnly.FromDateTime(DateTime.Now);
    public ClientsGroup(int size, int groupId)
    {
        Size = size;
        GroupId = groupId;
    }

    public override string ToString()
    {
        return $"Size: {Size}, Arrived: {Time:O}";
    }
}