using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace CourseWork_Prog_Lang_Compiler.ViewModels
{
    internal class ViewModelBase
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
