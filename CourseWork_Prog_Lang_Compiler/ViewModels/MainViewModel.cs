using CourseWork_Prog_Lang_Compiler.Models;
using System.Collections.ObjectModel;
using System.Text;
using System.Windows.Input;

namespace CourseWork_Prog_Lang_Compiler.ViewModels
{
    /// <summary>
    /// ViewModel для главного окна приложения.
    /// Управляет состоянием UI, обрабатывает команды пользователя и связывает View с Model (LexicalAnalyzer).
    /// </summary>
    internal class MainViewModel : ViewModelBase
    {
        /// <summary>
        /// Экземпляр лексического анализатора, который выполняет всю основную логику.
        /// </summary>
        private readonly LexicalAnalyzer _analyzer;

        // Приватные поля для хранения значений, к которым привязаны свойства.
        private string _sourceCodeText = "";
        private string _analysisResultText = "";
        private string _statusText = "Готово";

        #region Public Properties for Data Binding

        /// <summary>
        /// Получает или задает исходный код программы, введенный пользователем.
        /// </summary>
        public string SourceCodeText
        {
            get => _sourceCodeText;
            set
            {
                _sourceCodeText = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Получает или задает текстовое представление результата анализа (список токенов или сообщение об ошибке).
        /// </summary>
        public string AnalysisResultText
        {
            get => _analysisResultText;
            set
            {
                _analysisResultText = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Получает или задает текст для строки состояния.
        /// </summary>
        public string StatusText
        {
            get => _statusText;
            set
            {
                _statusText = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Коллекция для отображения таблицы служебных слов.
        /// </summary>
        public ObservableCollection<TableEntry> ServiceWords { get; } = new();
        /// <summary>
        /// Коллекция для отображения таблицы разделителей.
        /// </summary>
        public ObservableCollection<TableEntry> Delimiters { get; } = new();
        /// <summary>
        /// Коллекция для отображения таблицы чисел, сформированной в ходе анализа.
        /// </summary>
        public ObservableCollection<TableEntry> Numbers { get; } = new();
        /// <summary>
        /// Коллекция для отображения таблицы идентификаторов, сформированной в ходе анализа.
        /// </summary>
        public ObservableCollection<TableEntry> Identifiers { get; } = new();

        /// <summary>
        /// Команда, привязанная к кнопке "Произвести лексический анализ".
        /// </summary>
        public ICommand AnalyzeCommand { get; }

        #endregion

        /// <summary>
        /// Инициализирует новый экземпляр класса MainViewModel.
        /// </summary>
        public MainViewModel()
        {
            _analyzer = new LexicalAnalyzer();
            AnalyzeCommand = new RelayCommand(ExecuteAnalyze, CanExecuteAnalyze);

            LoadInitialData();
            LoadSampleCode();
        }

        /// <summary>
        /// Определяет, может ли быть выполнена команда анализа.
        /// </summary>
        private bool CanExecuteAnalyze(object? parameter)
        {
            return !string.IsNullOrWhiteSpace(SourceCodeText);
        }

        /// <summary>
        /// Выполняет лексический анализ при вызове команды.
        /// </summary>
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
        }

        /// <summary>
        /// Загружает статические данные (служебные слова, разделители) при запуске.
        /// </summary>
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

        /// <summary>
        /// Загружает пример кода в текстовое поле для удобства демонстрации и отладки.
        /// </summary>
        private void LoadSampleCode()
        {
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