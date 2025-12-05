using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace CourseWork_Prog_Lang_Compiler.ViewModels
{
    /// <summary>
    /// Базовый класс для всех ViewModel, реализующий интерфейс INotifyPropertyChanged.
    /// Этот интерфейс необходим для автоматического обновления пользовательского интерфейса (View)
    /// при изменении значения свойства в ViewModel.
    /// </summary>
    public class ViewModelBase : INotifyPropertyChanged
    {
        /// <summary>
        /// Событие, которое возникает при изменении значения свойства.
        /// Система привязок WPF подписывается на это событие, чтобы обновлять View.
        /// </summary>
        public event PropertyChangedEventHandler? PropertyChanged;

        /// <summary>
        /// Метод для вызова события PropertyChanged.
        /// Его следует вызывать в set-аксессоре каждого свойства, изменение которого должно отражаться в UI.
        /// </summary>
        /// <param name="propertyName">
        /// Имя изменившегося свойства. Атрибут [CallerMemberName] автоматически подставляет
        /// имя свойства, из которого был вызван этот метод.
        /// </param>
        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}