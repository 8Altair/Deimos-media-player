using System.Diagnostics;
using System.Windows.Input;


namespace Deimos.UI.ViewModels;

public sealed class RelayCommand : ICommand
{
    private readonly Action<object?> _execute;  // Logic to run when the command is executed
    private readonly Predicate<object?>? _canExecute;   // Optional guard that determines if execution is allowed

    /// <summary>
    /// Creates a command with an execute action and optional can-execute predicate.
    /// </summary>
    public RelayCommand(Action<object?> execute, Predicate<object?>? canExecute = null)
    {
        _execute = execute;
        _canExecute = canExecute;
        Debug.WriteLine("RelayCommand created.");
    }

    public event EventHandler? CanExecuteChanged;   // Notifies the UI that command availability may have changed

    /// <summary>
    /// Tells WPF whether the command can run for the given parameter.
    /// </summary>
    public bool CanExecute(object? parameter)
    {
        var result = _canExecute?.Invoke(parameter) ?? true;
        Debug.WriteLine($"RelayCommand.CanExecute: {result}");
        return result;
    }

    /// <summary>
    /// Executes the command action with the given parameter.
    /// </summary>
    public void Execute(object? parameter)
    {
        Debug.WriteLine("RelayCommand.Execute invoked.");
        _execute(parameter);
    }

    /// <summary>
    /// Forces the UI to re-query CanExecute and refresh enabled state.
    /// </summary>
    public void RaiseCanExecuteChanged()
    {
        Debug.WriteLine("RelayCommand.CanExecuteChanged raised.");
        CanExecuteChanged?.Invoke(this, EventArgs.Empty);
    }
}
