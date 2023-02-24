namespace OnboardingTap;

public class UserModel
{
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public List<string> AccessRolesPermissions { get; set; } = new List<string>();
}