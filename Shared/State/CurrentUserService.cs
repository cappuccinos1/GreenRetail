namespace GreenRetail.Shared.State;

public sealed class CurrentUserService : ICurrentUserService
{
    public Guid? UserId { get; private set; }
    public string? UserName { get; private set; }
    public string? DisplayName { get; private set; }
    public string? Role { get; private set; }

    public bool IsAuthenticated => UserId.HasValue;

    public event Action? Changed;

    public void SetUser(Guid userId, string userName, string displayName, string role)
    {
        UserId = userId;
        UserName = userName;
        DisplayName = displayName;
        Role = role;
        Changed?.Invoke();
    }

    public void Clear()
    {
        UserId = null;
        UserName = null;
        DisplayName = null;
        Role = null;
        Changed?.Invoke();
    }
}