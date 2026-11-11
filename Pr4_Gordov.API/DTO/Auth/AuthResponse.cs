namespace Pr4_Gordov.API.DTO.Auth
{
    public record AuthResponse
    (
        string Token,
        string Login,
        string Role
    );
}