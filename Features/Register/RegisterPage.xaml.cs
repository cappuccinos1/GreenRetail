using System.Globalization;

namespace GreenRetail.Features.Register;

public partial class RegisterPage : ContentPage
{
    private readonly RegisterViewModel _viewModel;

    public RegisterPage(RegisterViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
        _viewModel = viewModel;
        
        // Register local converter
        Resources.Add("InvertedBoolConverter", new InvertedBoolConverter());
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.InitializeAsync();
    }
}

public class InvertedBoolConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool b) return !b;
        return true;
    }
    
    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) 
    {
        throw new NotImplementedException();
    }
}