﻿using System.ComponentModel.DataAnnotations;

namespace PoS_Placeholder.Server.Models.Dto;

public class CreateProductDto
{
    [Required]
    [MaxLength(255)]
    public string Name { get; set; }

    [Required]
    [MaxLength(255)]
    public string ItemGroup { get; set; }
}