namespace SpendLensApi.Models;

public sealed record UserDto(Guid Id, string Email, DateTime CreatedAt);

public sealed record UserRequest(string Email, string Password);

public sealed record RegisterRequest(UserRequest User, OrganizationRequest Organization);