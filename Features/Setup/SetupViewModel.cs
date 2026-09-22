using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using GreenRetail.Data;
using GreenRetail.Data.Entities;

namespace GreenRetail.Features.Setup;

public partial class SetupViewModel : ObservableObject
{
    private readonly IDbContextFactory<PosDbContext> _dbFactory;
    private readonly ISystemSetupUseCase _setup;

    [ObservableProperty] private string branchCode = "";
    [ObservableProperty] private string branchName = "";
    [ObservableProperty] private string terminalCode = "";
    [ObservableProperty] private string terminalName = "";
    [ObservableProperty] private Branch? selectedBranch;
    [ObservableProperty] private string statusMessage = "";
    [ObservableProperty] private bool isBusy;

    public ObservableCollection<Branch> Branches { get; } = new();

    public SetupViewModel(IDbContextFactory<PosDbContext> dbFactory, ISystemSetupUseCase setup)
    {
        _dbFactory = dbFactory;
        _setup = setup;
    }

    public async Task InitializeAsync()
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var branches = await db.Branches.AsNoTracking().Where(x => x.IsActive).OrderBy(x => x.Code).ToListAsync();
        Branches.Clear();
        foreach (var branch in branches) Branches.Add(branch);
        SelectedBranch = Branches.FirstOrDefault();
    }

    [RelayCommand]
    private async Task CreateBranchAsync()
    {
        if (IsBusy) return;
        IsBusy = true;
        try
        {
            var result = await _setup.CreateBranchAsync(new CreateBranchCommand(BranchCode, BranchName));
            StatusMessage = result.IsSuccess ? $"Branch created: {result.Value.Code} — {result.Value.Name}" : result.Error!;
            if (result.IsSuccess)
            {
                BranchCode = BranchName = "";
                await InitializeAsync();
            }
        }
        finally { IsBusy = false; }
    }

    [RelayCommand]
    private async Task CreateTerminalAsync()
    {
        if (IsBusy) return;
        if (SelectedBranch is null) { StatusMessage = "Create/select a branch first."; return; }
        IsBusy = true;
        try
        {
            var result = await _setup.CreateTerminalAsync(new CreateTerminalCommand(SelectedBranch.Id, TerminalCode, TerminalName));
            StatusMessage = result.IsSuccess ? $"Terminal created: {result.Value.Code} — {result.Value.Name}" : result.Error!;
            if (result.IsSuccess) TerminalCode = TerminalName = "";
        }
        finally { IsBusy = false; }
    }
}
