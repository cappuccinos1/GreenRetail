using Microsoft.Extensions.DependencyInjection;

namespace GreenRetail.Features.Auth;

public static class AuthModule
{
    public static IServiceCollection AddAuthFeature(this IServiceCollection services)
    {
        services.AddSingleton<IPasswordHasher, Pbkdf2PasswordHasher>();
        services.AddTransient<IAuthenticateUserUseCase, AuthenticateUserUseCase>();
        services.AddTransient<IChangePasswordUseCase, ChangePasswordUseCase>();
        services.AddTransient<IOwnerRecoveryUseCase, OwnerRecoveryUseCase>();
        services.AddTransient<ChangePasswordViewModel>();
        services.AddTransient<ChangePasswordPage>();

        services.AddTransient<LoginViewModel>();
        services.AddTransient<LoginPage>();

        return services;
    }
}