﻿using System.ComponentModel.DataAnnotations;

namespace PoS_Placeholder.Server.Models.Dto;

public class UpdateProductVariationDto
{
    [Required]
    [Key]
    public int Id { get; set; }
    
    [MaxLength(255)]
    public string Name { get; set; }

    [Range(0.01, double.MaxValue, ErrorMessage = "Price must be greater than zero.")]
    public decimal Price { get; set; }

    public IFormFile? PictureFile { get; set; }
    
    [Required]
    public int ProductId { get; set; }
}