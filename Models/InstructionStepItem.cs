using CommunityToolkit.Mvvm.ComponentModel;

namespace RecipeManager.Models;

public partial class InstructionStepItem : ObservableObject
{
    public int StepNumber { get; set; }
    public string Instruction { get; set; } = string.Empty;

    [ObservableProperty]
    private bool _isCompleted;
}
