using FamilyConnect.Domain.Entities;
using FamilyConnect.Application.Contracts;

namespace FamilyConnect.Application.Abstractions;

public interface ITokenService
{
    TokenResponse CreateTokens(AppUser user);
}
