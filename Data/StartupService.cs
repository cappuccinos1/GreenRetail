namespace GreenRetail.Data;

public interface IStartupService
{
    Task InitializeAsync(CancellationToken cancellationToken = default);
}

public sealed class StartupService : IStartupService
{
    private readonly IDatabaseInitializer _databaseInitializer;
    private readonly SemaphoreSlim _gate = new(1, 1);

    private bool _initialized;

    public StartupService(IDatabaseInitializer databaseInitializer)
    {
        _databaseInitializer = databaseInitializer;
    }

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken);

        try
        {
            if (_initialized)
                return;

            await _databaseInitializer.InitializeAsync(cancellationToken);
            _initialized = true;
        }
        finally
        {
            _gate.Release();
        }
    }
}