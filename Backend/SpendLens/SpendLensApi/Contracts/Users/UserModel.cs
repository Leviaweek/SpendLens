using SpendLensDatabase.Models.Entities;

namespace SpendLensApi.Contracts.Users;

public sealed record UserModel(string Email, string Password);