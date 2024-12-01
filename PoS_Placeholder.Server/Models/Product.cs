using System.ComponentModel.DataAnnotations;

namespace PoS_Placeholder.Server.Models;

public class Product
{
    [Key]
    public int Id { get; set; }
    
    public string Name { get; set; }
    
    public string ItemGroup { get; set; }
    
}