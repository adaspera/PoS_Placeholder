using System.ComponentModel.DataAnnotations;

namespace PoS_Placeholder.Server.Models.Dto;

public class UpdateProductDto
{
    [Key]
    public int Id { get; set; }
    
    [MaxLength(255)]
    public string? Name { get; set; }

    [MaxLength(255)]
    public string? ItemGroup { get; set; }
}