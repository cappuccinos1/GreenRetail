namespace GreenRetail.BackOffice;

public partial class BackOfficePage : ContentPage
{
    public BackOfficePage(BackOfficeViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}