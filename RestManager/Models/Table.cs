namespace RestManager.Models;

public class Table
{
    public int Size { get; init; }
    
    public int OccupiedPlaces { get; set; }
    
    public bool IsFullyAvailable => OccupiedPlaces == 0;
    
    public bool IsPartiallyOccupied => OccupiedPlaces > 0 && OccupiedPlaces != Size;
    
    public bool IsFullyOccupied => OccupiedPlaces == Size;
    
    public List<ClientsGroup> Groups { get; private set; } = new();
    public Table(int size)
    {
        Size = size;
    }
    
    public bool CanFitMore(int groupSize) => (Size - OccupiedPlaces) >= groupSize;

    public void ArrangeGuesses(ClientsGroup group)
    {
        if (Size < group.Size)
        {
            throw new Exception("Table is too small");
        }
        Groups.Add(group);
        OccupiedPlaces += group.Size;
    } 
}