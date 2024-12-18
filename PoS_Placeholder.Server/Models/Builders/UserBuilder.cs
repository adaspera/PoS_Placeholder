using PoS_Placeholder.Server.Models.Dto;
using PoS_Placeholder.Server.Models.Enum;

namespace PoS_Placeholder.Server.Models.Builders;

public class UserBuilder
{
    private readonly User _user = new User();
    
    public UserBuilder FromCreateDto(RegisterEmployeeDto dto, int businessId)
    {

        _user.UserName = dto.Email;
        _user.Email = dto.Email;
        _user.PhoneNumber = dto.PhoneNumber;
        _user.FirstName = dto.FirstName;
        _user.LastName = dto.LastName;
        _user.AvailabilityStatus = AvailabilityStatus.Available;
        _user.BusinessId = businessId;

        return this;
    }

    public UserBuilder FromUpdateDto(User user, UpdateEmployeeDto dto)
    {
        _user.Id = user.Id;
        _user.BusinessId = user.BusinessId;
        _user.Orders = user.Orders;
        _user.UserWorkTimes = user.UserWorkTimes;
        
        _user.Email = dto.Email ?? user.Email;
        _user.PhoneNumber = dto.PhoneNumber ?? user.PhoneNumber;
        _user.FirstName = dto.FirstName ?? user.FirstName;
        _user.LastName = dto.LastName ?? user.LastName;
        _user.AvailabilityStatus = dto.AvailabilityStatus ?? user.AvailabilityStatus;

        return this;
    }
}