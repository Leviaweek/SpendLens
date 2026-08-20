using System.Text.Json.Serialization;

namespace SpendLensApi.Models;

public sealed record OrganizationDto(Guid Id, string Name);

public sealed record OrganizationRequest(string Name);
