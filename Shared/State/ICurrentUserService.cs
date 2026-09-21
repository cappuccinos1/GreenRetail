namespace GreenRetail.Shared.State;

public interface ICurrentUserService
{
    Guid? UserId { get; }
    string? UserName { get; }
    string? DisplayName { get; }
    string? Role { get; }
    bool IsAuthenticated { get; }

    event Action? Changed;

    void SetUser(Guid userId, string userName, string displayName, string role);
    void Clear();
}