namespace Pr4_Gordov.API.DTO.Auth
{
    public record RegisterRequest(
        string Login,
        string Password,
        string ConfirmPassword
    );

}    