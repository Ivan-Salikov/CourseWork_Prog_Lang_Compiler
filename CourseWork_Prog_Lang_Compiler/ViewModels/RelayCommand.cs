using System;
using System.Windows.Input;

namespace CourseWork_Prog_Lang_Compiler.ViewModels
{
    /// <summary>
    /// Стандартная реализация интерфейса ICommand для использования в MVVM-архитектуре.
    /// Позволяет "связать" действия из ViewModel (методы) с элементами управления в View (кнопки, меню и т.д.).
    /// </summary>
    public class RelayCommand : ICommand
    {
        // Делегат, который хранит ссылку на метод, выполняющий основную логику команды.
        private readonly Action<object?> _execute;

        // Делегат, который хранит ссылку на метод, определяющий, может ли команда быть выполнена в данный момент.
        private readonly Predicate<object?>? _canExecute;

        /// <summary>
        /// Событие, которое возникает при изменении условий, влияющих на возможность выполнения команды.
        /// WPF автоматически подписывается на это событие, чтобы включать/отключать элементы управления,
        /// привязанные к этой команде.
        /// </summary>
        public event EventHandler? CanExecuteChanged
        {
            add => CommandManager.RequerySuggested += value;
            remove => CommandManager.RequerySuggested -= value;
        }

        /// <summary>
        /// Инициализирует новый экземпляр класса RelayCommand.
        /// </summary>
        /// <param name="execute">Действие, которое будет выполнено командой.</param>
        /// <param name="canExecute">Функция, которая проверяет, может ли действие быть выполнено. Если null, команда считается всегда доступной.</param>
        /// <exception cref="ArgumentNullException">Вызывается, если делегат execute равен null.</exception>
        public RelayCommand(Action<object?> execute, Predicate<object?>? canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        /// <summary>
        /// Определяет, может ли команда выполняться в ее текущем состоянии.
        /// </summary>
        /// <param name="parameter">Данные, используемые командой. Если команда не требует данных, этот объект можно установить в null.</param>
        /// <returns>true, если команда может быть выполнена; в противном случае — false.</returns>
        public bool CanExecute(object? parameter)
        {
            return _canExecute == null || _canExecute(parameter);
        }

        /// <summary>
        /// Выполняет логику команды.
        /// </summary>
        /// <param name="parameter">Данные, используемые командой. Если команда не требует данных, этот объект можно установить в null.</param>
        public void Execute(object? parameter)
        {
            _execute(parameter);
        }
    }
}