using CourseWork_Prog_Lang_Compiler.Models;
using System.Collections.ObjectModel;
using System.Text;
using System.Windows.Input;

namespace CourseWork_Prog_Lang_Compiler.ViewModels
{
    public class LexicalAnalyzerViewModel : ViewModelBase
    {
        // Событие для уведомления о завершении анализа, передаёт токены и таблицы
        public event EventHandler<(List<Token> tokens, List<TableEntry> identifiers, List<TableEntry> numbers)> AnalysisCompleted;

        private readonly LexicalAnalyzer _analyzer;
        private string _sourceCodeText = "";
        private string _analysisResultText = "";
        private string _statusText = "Готово";

        public string SourceCodeText
        {
            get => _sourceCodeText;
            set { _sourceCodeText = value; OnPropertyChanged(); }
        }

        public string AnalysisResultText
        {
            get => _analysisResultText;
            set { _analysisResultText = value; OnPropertyChanged(); }
        }

        public string StatusText
        {
            get => _statusText;
            set { _statusText = value; OnPropertyChanged(); }
        }

        public ObservableCollection<TableEntry> ServiceWords { get; } = new();
        public ObservableCollection<TableEntry> Delimiters { get; } = new();
        public ObservableCollection<TableEntry> Numbers { get; } = new();
        public ObservableCollection<TableEntry> Identifiers { get; } = new();

        public ICommand AnalyzeCommand { get; }

        public LexicalAnalyzerViewModel()
        {
            _analyzer = new LexicalAnalyzer();
            AnalyzeCommand = new RelayCommand(ExecuteAnalyze, CanExecuteAnalyze);
            LoadInitialData();
        }

        private bool CanExecuteAnalyze(object? parameter)
        {
            return !string.IsNullOrWhiteSpace(SourceCodeText);
        }

        private void ExecuteAnalyze(object? parameter)
        {
            StatusText = "Анализ запущен...";

            // Очистка результатов предыдущего анализа
            AnalysisResultText = "";
            Numbers.Clear();
            Identifiers.Clear();

            // Запуск анализатора
            AnalysisResult result = _analyzer.Analyze(SourceCodeText);

            // Отображение результатов в UI
            if (result.IsSuccess)
            {
                StringBuilder resultBuilder = new StringBuilder();
                const int tokensPerLine = 8;
                int currentTokenCount = 0;

                foreach (var token in result.Tokens)
                {
                    resultBuilder.Append($"({token.TableCode},{token.EntryIndex}) ");
                    currentTokenCount++;

                    if (currentTokenCount >= tokensPerLine)
                    {
                        resultBuilder.AppendLine();
                        currentTokenCount = 0;
                    }
                }
                AnalysisResultText = resultBuilder.ToString();

                // Заполняем таблицы чисел и идентификаторов
                foreach (var entry in result.NumbersTable)
                {
                    Numbers.Add(entry);
                }
                foreach (var entry in result.IdentifiersTable)
                {
                    Identifiers.Add(entry);
                }

                StatusText = "Лексический анализ успешно завершен";
            }
            else
            {
                AnalysisResultText = result.ErrorMessage;
                StatusText = "Ошибка лексического анализа";
            }

            // Вызываем событие, передав токены и таблицы
            if (result.IsSuccess)
            {
                AnalysisCompleted?.Invoke(this, (result.Tokens, result.IdentifiersTable, result.NumbersTable));
            }
        }

        private void LoadInitialData()
        {
            var serviceWords = new[] { "program", "var", "begin", "end","int", "float",
                "bool", "if", "else", "while", "for", "to",
                "step", "next", "true", "false", "readln", "writeln" };
            for (int i = 0; i < serviceWords.Length; i++)
            {
                ServiceWords.Add(new TableEntry(i + 1, serviceWords[i]));
            }

            var delimiters = new[] { ";", ",", ":", ":=", "!", "!=", "==",
                "<", ">", "<=", ">=", "+", "-", "||",
                "*", "&&", "/", "(", ")", "{", "}", "." };
            for (int i = 0; i < delimiters.Length; i++)
            {
                Delimiters.Add(new TableEntry(i + 1, delimiters[i]));
            }
        }
    }
}