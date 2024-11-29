using RestManager.Managers;
using RestManager.Models;

namespace RestManager.Configurations;

public class TablesConfiguration
{
    public int[] Sizes { get; set; } = Array.Empty<int>();
    
    public List<Table> Tables => Sizes.Select(x=>new Table(x)).ToList();
    
    public TablesConfiguration()
    {
    }

    public TablesConfiguration(int[] sizes)
    {
        Sizes = sizes;
    }
}