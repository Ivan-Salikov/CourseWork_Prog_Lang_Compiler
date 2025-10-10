using CourseWork_Prog_Lang_Compiler.Models;
using System.Collections.ObjectModel;
using System.Text;

namespace CourseWork_Prog_Lang_Compiler.ViewModels
{
    internal class MainViewModel : ViewModelBase
    {
        // Поля для хранения данных
        private string _sourceCodeText = "";
        private string _analysisResultText = "";
        private string _statusText = "";

        // Свойства для привязки к UI
        public string SourceCodeText
        {
            get => _sourceCodeText;
            set
            {
                _sourceCodeText = value;
                OnPropertyChanged();
            }
        }

        public string AnalysisResultText
        {
            get => _analysisResultText;
            set
            {
                _analysisResultText = value;
                OnPropertyChanged();
            }
        }

        public string StatusText
        {
            get => _statusText;
            set
            {
                _statusText = value;
                OnPropertyChanged();
            }
        }

        // Коллекции для таблиц
        public ObservableCollection<TableEntry> ServiceWords { get; } = new();
        public ObservableCollection<TableEntry> Delimiters { get; } = new();
        public ObservableCollection<TableEntry> Numbers { get; } = new();
        public ObservableCollection<TableEntry> Identifiers { get; } = new();

        public MainViewModel()
        {
            LoadInitialData();
            LoadSampleCode();
        }

        private void LoadInitialData()
        {
            // Заполнение таблиц

            // Таблица служебных слов
            var serviceWords = new[] { "program", "var", "begin", "end","int", "float",
                "bool", "if", "else", "while", "for", "to",
                "step", "next", "true", "false", "readln", "writeln" };
            for (int i = 0; i < serviceWords.Length; i++)
            {
                ServiceWords.Add(new TableEntry(i + 1, serviceWords[i]));
            }

            // Таблица разделителей
            var delimiters = new[] { ";", ",", ":", ":=", "!", "!=", "==",
                "<", ">", "<=", ">=", "+", "-", "||",
                "*", "&&", "/", "(", ")", "{", "}", "." };
            for (int i = 0; i < delimiters.Length; i++)
            {
                Delimiters.Add(new TableEntry(i + 1, delimiters[i]));
            }
        }

        private void LoadSampleCode()
        {
            // Используем Пример 1 из вашего списка
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("program var");
            sb.AppendLine("  int n, i, factorial;");
            sb.AppendLine("begin");
            sb.AppendLine("  readln n;");
            sb.AppendLine("  factorial := 1;");
            sb.AppendLine("  for i := 1 to n step 1 begin");
            sb.AppendLine("    factorial := factorial * i;");
            sb.AppendLine("    writeln i, factorial;	{ \"Step \", i, \": \", factorial }");
            sb.AppendLine("  end next;");
            sb.AppendLine("  writeln n, factorial;	{ \"Factorial of \", n, \" is \", factorial }");
            sb.AppendLine("end.");

            SourceCodeText = sb.ToString();
        }
    }
}
