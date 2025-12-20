using CourseWork_Prog_Lang_Compiler.Models;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace CourseWork_Prog_Lang_Compiler.ViewModels
{
    /// <summary>
    /// ViewModel для управления интерпретатором и отображения результатов.
    /// </summary>
    public class InterpreterViewModel : INotifyPropertyChanged
    {
        private Interpreter _interpreter;

        // Событие для синхронизации ввода данных (ожидание пользователя)
        private AutoResetEvent _inputSignal = new AutoResetEvent(false);
        private string _inputBuffer;

        public InterpreterViewModel()
        {
            RunCommand = new RelayCommand(ExecuteRun, CanExecuteRun);
            SubmitInputCommand = new RelayCommand(ExecuteSubmitInput, CanExecuteSubmitInput);
        }

        // Свойства привязки

        private ObservableCollection<PolizItem> _polizItems;
        public ObservableCollection<PolizItem> PolizItems
        {
            get => _polizItems;
            set { _polizItems = value; OnPropertyChanged(); }
        }

        private string _consoleOutput;
        public string ConsoleOutput
        {
            get => _consoleOutput;
            set { _consoleOutput = value; OnPropertyChanged(); }
        }

        private string _inputField;
        public string InputField
        {
            get => _inputField;
            set { _inputField = value; OnPropertyChanged(); }
        }

        private bool _isInputEnabled;
        public bool IsInputEnabled
        {
            get => _isInputEnabled;
            set { _isInputEnabled = value; OnPropertyChanged(); }
        }

        private string _statusText;
        public string StatusText
        {
            get => _statusText;
            set { _statusText = value; OnPropertyChanged(); }
        }

        public ICommand RunCommand { get; }
        public ICommand SubmitInputCommand { get; }

        // Логика

        /// <summary>
        /// Загружает данные ПОЛИЗ из синтаксического анализатора.
        /// </summary>
        public void LoadPoliz(List<string> polishNotation, List<TableEntry> identifiers)
        {
            // Заполнение таблицы для UI
            var displayItems = new ObservableCollection<PolizItem>();
            if (polishNotation != null)
            {
                for (int i = 0; i < polishNotation.Count; i++)
                {
                    displayItems.Add(new PolizItem { Index = i, Command = polishNotation[i] });
                }
            }
            PolizItems = displayItems;

            // Инициализация ядра интерпретатора
            if (polishNotation != null && polishNotation.Count > 0)
            {
                _interpreter = new Interpreter();
                _interpreter.LoadPoliz(polishNotation, identifiers);
                StatusText = "Программа готова к запуску.";
            }
            else
            {
                _interpreter = null;
                StatusText = "Нет данных для выполнения.";
            }

            ConsoleOutput = "";
            InputField = "";
            IsInputEnabled = false;
        }

        private async void ExecuteRun(object obj)
        {
            if (_interpreter == null) return;

            ConsoleOutput = ""; // Очистка консоли
            AppendToConsole("[СИСТЕМА] Запуск программы...\n");
            StatusText = "Выполнение...";
            IsInputEnabled = false;

            // Запуск в фоновом потоке
            await Task.Run(() =>
            {
                try
                {
                    _interpreter.Run(
                        // Колбек вывода (Writeln)
                        output: (text) =>
                        {
                            AppendToConsole($"[ВЫВОД]  {text}\n");
                        },
                        // Колбек ввода (Readln)
                        input: () =>
                        {
                            AppendToConsole("[ВВОД]   Введите значение: ");

                            // Активируем поле ввода в UI
                            SetInputEnabled(true);

                            // Ждем сигнал от кнопки "Ввод"
                            _inputSignal.WaitOne();

                            // Деактивируем поле
                            SetInputEnabled(false);

                            string result = _inputBuffer;
                            AppendToConsole($"[ЮЗЕР]   {result}\n"); // Эхо ввода пользователя
                            return result;
                        }
                    );

                    StatusText = "Выполнение успешно завершено.";
                    AppendToConsole("[СИСТЕМА] Программа завершена.\n");
                }
                catch (Exception ex)
                {
                    StatusText = "Ошибка выполнения.";
                    AppendToConsole($"\n[ОШИБКА] {ex.Message}\n");
                }
            });
        }

        private bool CanExecuteRun(object obj) => _interpreter != null;

        private void ExecuteSubmitInput(object obj)
        {
            _inputBuffer = InputField;
            InputField = "";
            _inputSignal.Set(); // Разблокируем поток интерпретатора
        }

        private bool CanExecuteSubmitInput(object obj) => IsInputEnabled;

        // --- Вспомогательные методы UI ---

        private void AppendToConsole(string text)
        {
            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                ConsoleOutput += text;
            });
        }

        private void SetInputEnabled(bool enabled)
        {
            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                IsInputEnabled = enabled;
            });
        }

        // INotifyPropertyChanged
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}