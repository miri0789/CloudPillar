using Shared.Entities.Twin;

namespace Backend.BEApi.Services.interfaces;

public interface IChangeSpecService
{
    Task AssignChangeSpecAsync(AssignChangeSpec changeSpec);
}

