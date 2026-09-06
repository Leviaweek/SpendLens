using System.Linq.Expressions;

namespace SpendLensDatabase.Models.AuthData.Users;

public sealed record UserDto(Guid Id, string Email, DateTime CreatedAt)
{
    public static Expression<Func<User, UserDto>> FromUser =>
        user => new UserDto(user.Id, user.Email, user.CreatedAt);
}

