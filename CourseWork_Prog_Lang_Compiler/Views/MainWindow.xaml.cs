using CourseWork_Prog_Lang_Compiler.ViewModels;
using System.Windows;

namespace CourseWork_Prog_Lang_Compiler.Views
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            // Устанавливаем DataContext для окна
            DataContext = new MainViewModel();
        }
    }
}