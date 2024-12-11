using PoS_Placeholder.Server.Models.Dto;

namespace PoS_Placeholder.Server.Models.Builders;

public class DiscountBuilder
{
    private readonly Discount _discount = new Discount();

    public DiscountBuilder FromCreateDto(CreateDiscountDto dto, int businessId)
    {
        _discount.Amount = dto.Amount;
        _discount.StartDate = dto.StartDate;
        _discount.EndDate = dto.EndDate;
        _discount.IsPercentage = dto.IsPercentage;
        _discount.BusinessId = businessId;
        
        return this;
    }

    public DiscountBuilder FromUpdateDto(UpdateDiscountDto dto, Discount existingDiscount)
    {
        _discount.Id = existingDiscount.Id;
        _discount.BusinessId = existingDiscount.BusinessId;

        _discount.Amount = dto.Amount ?? existingDiscount.Amount;
        _discount.StartDate = dto.StartDate ?? existingDiscount.StartDate;
        _discount.EndDate = dto.EndDate ?? existingDiscount.EndDate;
        _discount.IsPercentage = dto.IsPercentage;

        return this;
    }

    public Discount Build()
    {
        return _discount;
    }
}
