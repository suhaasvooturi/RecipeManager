using CommunityToolkit.Mvvm.ComponentModel;

namespace RecipeManager.Models;

public partial class CategoryItem : ObservableObject
{
    public string Name { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Icon { get; set; } = string.Empty;

    [ObservableProperty]
    private bool _isSelected;
}
