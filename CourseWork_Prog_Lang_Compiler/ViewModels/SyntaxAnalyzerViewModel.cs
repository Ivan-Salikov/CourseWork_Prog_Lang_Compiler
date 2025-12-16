using CourseWork_Prog_Lang_Compiler.Models;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows.Input;

namespace CourseWork_Prog_Lang_Compiler.ViewModels
{
    public class SyntaxAnalyzerViewModel : ViewModelBase
    {
        private string _inputTokensText = "";
        public string InputTokensText
        {
            get => _inputTokensText;
            set { _inputTokensText = value; OnPropertyChanged(); }
        }

        private string _syntaxAnalysisResultText = "";
        public string SyntaxAnalysisResultText
        {
            get => _syntaxAnalysisResultText;
            set { _syntaxAnalysisResultText = value; OnPropertyChanged(); }
        }

        private string _syntaxStatusText = "Готов";
        public string SyntaxStatusText
        {
            get => _syntaxStatusText;
            set { _syntaxStatusText = value; OnPropertyChanged(); }
        }

        public ObservableCollection<SyntaxError> SyntaxErrors { get; }
        public ObservableCollection<SemanticError> SemanticErrors { get; }
        public ICommand AnalyzeSyntaxCommand { get; }

        private readonly SyntaxAnalyzer _syntaxAnalyzer;

        private List<TableEntry> _identifiersFromLexical = new List<TableEntry>();
        private List<TableEntry> _numbersFromLexical = new List<TableEntry>();

        public SyntaxAnalyzerViewModel()
        {
            SyntaxErrors = new ObservableCollection<SyntaxError>();
            SemanticErrors = new ObservableCollection<SemanticError>();
            _syntaxAnalyzer = new SyntaxAnalyzer();
            AnalyzeSyntaxCommand = new RelayCommand(AnalyzeSyntax, CanAnalyzeSyntax);
        }

        // Метод для установки входных токенов и таблиц извне
        public void SetInputTokensAndTables(List<Token> tokens, List<TableEntry> identifiers, List<TableEntry> numbers)
        {
            if (tokens != null && tokens.Any())
            {
                InputTokensText = FormatTokensToString(tokens);

                _identifiersFromLexical = identifiers ?? new List<TableEntry>();
                _numbersFromLexical = numbers ?? new List<TableEntry>();
            }
            else
            {
                InputTokensText = "";
                _identifiersFromLexical = new List<TableEntry>();
                _numbersFromLexical = new List<TableEntry>();
            }
        }

        private bool CanAnalyzeSyntax(object parameter)
        {
            return !string.IsNullOrWhiteSpace(InputTokensText);
        }

        private void AnalyzeSyntax(object parameter)
        {
            SyntaxErrors.Clear();
            SemanticErrors.Clear();
            SyntaxAnalysisResultText = "";
            SyntaxStatusText = "Анализ...";

            try
            {
                var tokens = ParseTokensFromString(InputTokensText);
                var result = _syntaxAnalyzer.Parse(tokens, _identifiersFromLexical, _numbersFromLexical);

                if (result.IsSuccess)
                {
                    SyntaxAnalysisResultText = "Синтаксически и семантически корректно.";
                    SyntaxStatusText = "Готов";
                }
                else
                {
                    if (result.SemanticErrors.Any())
                    {
                        SyntaxAnalysisResultText = "Обнаружены семантические ошибки.";
                        SyntaxStatusText = "Ошибка";
                        foreach (var error in result.SemanticErrors)
                        {
                            SemanticErrors.Add(error);
                        }
                    }
                    else
                    {
                        SyntaxAnalysisResultText = "Обнаружены синтаксические ошибки.";
                        SyntaxStatusText = "Ошибка";
                    }

                    foreach (var error in result.Errors)
                    {
                        SyntaxErrors.Add(new SyntaxError { Id = error.Id, Description = error.Description });
                    }
                }
            }
            catch (Exception ex)
            {
                SyntaxAnalysisResultText = $"Ошибка при анализе: {ex.Message}";
                SyntaxStatusText = "Ошибка";
            }
        }

        private string FormatTokensToString(List<Token> tokens)
        {
            return string.Join(" ", tokens.Select(t => $"({t.TableCode},{t.EntryIndex})"));
        }

        private List<Token> ParseTokensFromString(string tokenString)
        {
            var tokens = new List<Token>();
            if (string.IsNullOrWhiteSpace(tokenString))
                return tokens;

            var parts = tokenString.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var part in parts)
            {
                var innerPart = part.Trim('(', ')');
                var codeIndex = innerPart.Split(',');
                if (codeIndex.Length == 2 && int.TryParse(codeIndex[0].Trim(), out int tableCode) && int.TryParse(codeIndex[1].Trim(), out int index))
                {
                    tokens.Add(new Token(tableCode, index));
                }
            }
            return tokens;
        }
    }
}