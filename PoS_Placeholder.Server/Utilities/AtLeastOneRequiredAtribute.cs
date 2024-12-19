using System.ComponentModel.DataAnnotations;
using PoS_Placeholder.Server.Models.Dto;

namespace PoS_Placeholder.Server.Utilities;

public class AtLeastOneRequiredAttribute : ValidationAttribute
{
    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        var orderDto = (CreateOrderDto)validationContext.ObjectInstance;

        bool hasOrderItems = orderDto.OrderItems.Count > 0;
        bool hasOrderServiceIds = orderDto.OrderServiceIds != null && orderDto.OrderServiceIds.Count > 0;

        if (hasOrderItems || hasOrderServiceIds)
        {
            return ValidationResult.Success;
        }

        return new ValidationResult("Order must have at least 1 service or product.");
    }
}
