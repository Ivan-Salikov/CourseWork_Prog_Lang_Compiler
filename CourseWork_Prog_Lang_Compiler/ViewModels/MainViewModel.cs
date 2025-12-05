using CourseWork_Prog_Lang_Compiler.Models;
using Microsoft.Win32;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Input;

namespace CourseWork_Prog_Lang_Compiler.ViewModels
{
    /// <summary>
    /// Главная ViewModel для всего приложения.
    /// Управляет состоянием UI, командами файлов и полного анализа.
    /// Содержит дочерние ViewModel для лексического и синтаксического анализаторов.
    /// </summary>
    public class MainViewModel : ViewModelBase
    {
        // Дочерние ViewModel
        public LexicalAnalyzerViewModel LexicalAnalyzer { get; }
        public SyntaxAnalyzerViewModel SyntaxAnalyzer { get; }

        // Команды
        public ICommand OpenFileCommand { get; }
        public ICommand SaveFileCommand { get; }
        public ICommand RunFullAnalysisCommand { get; }

        // Приватные поля
        private string _statusText = "Готов";
        private string _filePath = "";

        public MainViewModel()
        {
            LexicalAnalyzer = new LexicalAnalyzerViewModel();
            SyntaxAnalyzer = new SyntaxAnalyzerViewModel();

            // Подписываемся на событие успешного лексического анализа
            // для передачи токенов и таблиц в синтаксический анализатор
            LexicalAnalyzer.AnalysisCompleted += OnLexicalAnalysisCompleted;

            OpenFileCommand = new RelayCommand(OpenFile, CanOpenFile);
            SaveFileCommand = new RelayCommand(SaveFile, CanSaveFile);
            RunFullAnalysisCommand = new RelayCommand(RunFullAnalysis, CanRunFullAnalysis);

            LoadSampleCode();
        }

        public string StatusText
        {
            get => _statusText;
            set { _statusText = value; OnPropertyChanged(); }
        }

        // Обработчик события из LexicalAnalyzer
        private void OnLexicalAnalysisCompleted(object sender, (List<Token> tokens, List<TableEntry> identifiers, List<TableEntry> numbers) data)
        {
            // Передаем токены и таблицы в SyntaxAnalyzerViewModel
            SyntaxAnalyzer.SetInputTokensAndTables(data.tokens, data.identifiers, data.numbers);
        }

        // Команда: Открыть файл
        private bool CanOpenFile(object parameter) => true;
        private void OpenFile(object parameter)
        {
            var openFileDialog = new OpenFileDialog
            {
                Filter = "Текстовые файлы (*.txt)|*.txt|Все файлы (*.*)|*.*"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                try
                {
                    _filePath = openFileDialog.FileName;
                    LexicalAnalyzer.SourceCodeText = File.ReadAllText(_filePath, Encoding.UTF8);
                    StatusText = $"Файл открыт: {Path.GetFileName(_filePath)}";
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при открытии файла: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    StatusText = "Ошибка при открытии файла.";
                }
            }
        }

        // Команда: Сохранить файл
        private bool CanSaveFile(object parameter) => !string.IsNullOrWhiteSpace(LexicalAnalyzer?.SourceCodeText);
        private void SaveFile(object parameter)
        {
            if (string.IsNullOrEmpty(_filePath))
            {
                var saveFileDialog = new SaveFileDialog
                {
                    Filter = "Текстовые файлы (*.txt)|*.txt|Все файлы (*.*)|*.*"
                };

                if (saveFileDialog.ShowDialog() == true)
                {
                    _filePath = saveFileDialog.FileName;
                }
                else
                {
                    return;
                }
            }

            try
            {
                File.WriteAllText(_filePath, LexicalAnalyzer.SourceCodeText, Encoding.UTF8);
                StatusText = $"Файл сохранен: {Path.GetFileName(_filePath)}";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при сохранении файла: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                StatusText = "Ошибка при сохранении файла.";
            }
        }

        // Команда: Полный анализ (лексический -> синтаксический)
        private bool CanRunFullAnalysis(object parameter) => !string.IsNullOrWhiteSpace(LexicalAnalyzer?.SourceCodeText);
        private void RunFullAnalysis(object parameter)
        {
            StatusText = "Полный анализ запущен...";

            // Очищаем результаты предыдущего анализа
            LexicalAnalyzer.AnalysisResultText = "";
            LexicalAnalyzer.Numbers.Clear();
            LexicalAnalyzer.Identifiers.Clear();
            SyntaxAnalyzer.SyntaxAnalysisResultText = "";
            SyntaxAnalyzer.SyntaxErrors.Clear();
            SyntaxAnalyzer.SyntaxStatusText = "Готов";

            // Запускаем лексический анализ через команду
            if (LexicalAnalyzer.AnalyzeCommand.CanExecute(null))
            {
                LexicalAnalyzer.AnalyzeCommand.Execute(null);
            }
            else
            {
                StatusText = "Невозможно запустить лексический анализ (проверьте исходный код).";
                return; // Прерываем выполнение полного анализа
            }

            // Лексический анализ завершён (или не удался), событие AnalysisCompleted
            // (если анализ был успешен) вызвало передачу токенов и таблиц в SyntaxAnalyzer.
            // Теперь запускаем синтаксический анализ через его команду.
            if (SyntaxAnalyzer.AnalyzeSyntaxCommand.CanExecute(null))
            {
                SyntaxAnalyzer.AnalyzeSyntaxCommand.Execute(null);
            }
            else
            {
                StatusText = "Невозможно запустить синтаксический анализ (проверьте токены).";
            }

            StatusText = "Полный анализ завершён.";
        }

        // Пример загрузки образца кода
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

            LexicalAnalyzer.SourceCodeText = sb.ToString();
        }
    }
}