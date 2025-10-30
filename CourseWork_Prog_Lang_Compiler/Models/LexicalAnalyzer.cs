using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Text;

namespace CourseWork_Prog_Lang_Compiler.Models
{
    /// <summary>
    /// Лексический анализатор (сканер), который преобразует исходный код в последовательность токенов.
    /// Работает на основе конечного автомата, определенного диаграммой состояний.
    /// </summary>
    public class LexicalAnalyzer
    {
        // Перечисление для всех состояний конечного автомата, согласно диаграмме.
        private enum State
        {
            H, I, N2, N8, N10, N16, C, CE1, CE2, CE3,
            L1, L2, L3, P, FP, B, O, D, HX, EXP,
            ED1, ED2, ED3, ES1, ES2, OG, V, EV, ER
        }

        private char CH;                 // 1) CH - очередной входной символ
        private StringBuilder S = new(); // 2) S - буфер для накапливания символов лексемы
        private State CS;                // 3) CS - текущее состояние буфера накопления лексем с возможными значениями

        private string? _customErrorMessage = null; // Поле для хранения специфического сообщения об ошибке.

        private string _sourceText = "";
        private int _currentIndex = 0;
        private int _currentLine = 1;

        // Таблицы лексем
        private readonly Dictionary<string, int> _serviceWords = new(); // TW - таблица служебных слов М-языка
        private readonly Dictionary<string, int> _delimiters = new();   // TL – таблица ограничителей М-языка
        private readonly Dictionary<string, int> _numbers = new();      // TN - таблица идентификаторов программы
        private readonly Dictionary<string, int> _identifiers = new();  // TI - таблица чисел, используемых в программе

        // Списки для сохранения порядка добавления (для вывода в UI)
        private readonly List<string> _numberList = new();
        private readonly List<string> _identifierList = new();

        // Список распознанных токенов для вывода результата
        private readonly List<Token> _tokens = new();

        public LexicalAnalyzer()
        {
            InitializeTables();
        }

        // Инициализация таблиц служебных слов и разделителей при создании анализатора.
        private void InitializeTables()
        {
            var serviceWords = new[] { "program", "var", "begin", "end", "int", "float",
                "bool", "if", "else", "while", "for", "to",
                "step", "next", "true", "false", "readln", "writeln" };
            for (int i = 0; i < serviceWords.Length; i++) _serviceWords.Add(serviceWords[i], i + 1);

            var delimiters = new[] { ";", ",", ":", ":=", "!", "!=", "==",
                "<", ">", "<=", ">=", "+", "-", "||",
                "*", "&&", "/", "(", ")", "{", "}", "." };
            for (int i = 0; i < delimiters.Length; i++) _delimiters.Add(delimiters[i], i + 1);
        }

        /// <summary>
        /// Главный метод, запускающий лексический анализ исходного текста.
        /// </summary>
        /// <param name="sourceText">Текст программы для анализа.</param>
        /// <returns>Объект AnalysisResult с результатами анализа.</returns>
        public AnalysisResult Analyze(string sourceText)
        {
            Reset();
            _sourceText = sourceText + "\n\0";

            CS = State.H; // Устанавливаем начальное состояние

            // Главный цикл автомата: работает до перехода в конечное состояние (V) или состояние ошибки (ER).
            do
            {
                gc(); // Считываем следующий символ

                switch (CS)
                {
                    case State.H: HandleStateH(); break;
                    case State.I: HandleStateI(); break;
                    case State.EV: HandleStateEV(); break;
                    case State.C: HandleStateC(); break;
                    case State.CE1: HandleStateCE1(); break;
                    case State.CE2: HandleStateCE2(); break;
                    case State.CE3: HandleStateCE3(); break;
                    case State.L1: HandleStateL1(); break;
                    case State.L2: HandleStateL2(); break;
                    case State.L3: HandleStateL3(); break;
                    case State.N2: HandleStateN2(); break;
                    case State.N8: HandleStateN8(); break;
                    case State.N10: HandleStateN10(); break;
                    case State.N16: HandleStateN16(); break;
                    case State.B: HandleStateB(); break;
                    case State.O: HandleStateO(); break;
                    case State.D: HandleStateD(); break;
                    case State.HX: HandleStateHX(); break;
                    case State.P: HandleStateP(); break;
                    case State.FP: HandleStateFP(); break;
                    case State.EXP: HandleStateEXP(); break;
                    case State.ES1: HandleStateES1(); break;
                    case State.ES2: HandleStateES2(); break;
                    case State.ED1: HandleStateED1(); break;
                    case State.ED2: HandleStateED2(); break;
                    case State.ED3: HandleStateED3(); break;
                    case State.OG: HandleStateOG(); break;
                }
            } while (CS != State.V && CS != State.ER);

            // Формирование результата анализа
            if (CS == State.ER)
            {
                // Если было установлено специальное сообщение, используем его
                if (!string.IsNullOrEmpty(_customErrorMessage))
                {
                    return new AnalysisResult
                    {
                        IsSuccess = false,
                        // Добавляем номер строки к нашему кастомному сообщению
                        ErrorMessage = $"Ошибка на строке {_currentLine}: {_customErrorMessage}"
                    };
                }

                // Иначе используем стандартное сообщение
                string errorLexeme = S.Length > 0 ? S.ToString() : "";
                return new AnalysisResult
                {
                    IsSuccess = false,
                    ErrorMessage = $"Ошибка на строке {_currentLine}: Неверная лексема '{errorLexeme}{CH}'."
                };
            }

            return new AnalysisResult
            {
                IsSuccess = true,
                Tokens = _tokens,
                IdentifiersTable = _identifierList.Select((val, i) => new TableEntry(i + 1, val)).ToList(),
                NumbersTable = _numberList.Select((val, i) => new TableEntry(i + 1, val)).ToList()
            };
        }

        #region State Handlers

        // Начальное состояние. Пропускает пробелы и определяет тип следующей лексемы.
        private void HandleStateH()
        {
            while (char.IsWhiteSpace(CH)) { gc(); }
            if (CH == '\0') { CS = State.V; return; }
            nill();
            if (let()) { add(); CS = State.I; }
            else if (CH >= '0' && CH <= '1') { add(); CS = State.N2; }
            else if (CH >= '2' && CH <= '7') { add(); CS = State.N8; }
            else if (CH >= '8' && CH <= '9') { add(); CS = State.N10; }
            else if (CH == '{') { CS = State.C; }
            else if (CH == '<') { add(); CS = State.L1; }
            else if (CH == '>') { add(); CS = State.L2; }
            else if (CH == '!') { add(); CS = State.L3; }
            else if (CH == '.') { add(); CS = State.P; }
            else { CS = State.OG; ungetc(); }
        }

        // Обработка идентификатора или служебного слова.
        private void HandleStateI()
        {
            while (let() || digit()) { add(); gc(); }
            ungetc();

            _serviceWords.TryGetValue(S.ToString(), out int z);
            if (z == 4) { CS = State.EV; }
            else if (z != 0) { @out(1, z); CS = State.H; }
            else { int idIndex = put(_identifiers, _identifierList); @out(4, idIndex); CS = State.H; }
        }

        // Особое состояние после распознавания "end".
        private void HandleStateEV()
        {
            if (CH == '.') { @out(1, 4); @out(2, 22); CS = State.V; }
            else { @out(1, 4); ungetc(); CS = State.H; }
        }

        // Обработка комментария {...}.
        private void HandleStateC()
        {
            if (CH == 'e') { CS = State.CE1; }
            else if (CH == '}') { CS = State.H; }
            else if (CH == '\0') { _customErrorMessage = "Обнаружен конец файла внутри незакрытого комментария."; CS = State.ER; }
        }
        private void HandleStateCE1() { if (CH == 'n') { CS = State.CE2; } else if (CH == '}') { CS = State.H; } else { CS = State.C; } }
        private void HandleStateCE2() { if (CH == 'd') { CS = State.CE3; } else if (CH == '}') { CS = State.H; } else { CS = State.C; } }
        private void HandleStateCE3() { if (CH == '.') { _customErrorMessage = "Обнаружен конец программы 'end.' внутри незакрытого комментария.";  CS = State.ER; } else if (CH == '}') { CS = State.H; } else { CS = State.C; } }

        // Обработка операторов сравнения.
        private void HandleStateL1() { if (CH == '=') { add(); @out(2, 11); CS = State.H; } else { ungetc(); @out(2, 8); CS = State.H; } }
        private void HandleStateL2() { if (CH == '=') { add(); @out(2, 10); CS = State.H; } else { ungetc(); @out(2, 9); CS = State.H; } }
        private void HandleStateL3() { if (CH == '=') { add(); @out(2, 6); CS = State.H; } else { ungetc(); @out(2, 5); CS = State.H; } }

        // --- БЛОК ОБРАБОТКИ ЧИСЕЛ ---
        private void HandleStateN2()
        {
            while (CH is '0' or '1') { add(); gc(); }
            if (CH is 'E' or 'e') { add(); CS = State.EXP; }
            else if (CH is 'B' or 'b') { add(); CS = State.B; }
            else if (CH is 'O' or 'o') { add(); CS = State.O; }
            else if (CH is 'D' or 'd') { add(); CS = State.D; }
            else if (CH is 'H' or 'h') { add(); CS = State.HX; }
            else if (AFH()) { add(); CS = State.N16; }
            else if (CH >= '2' && CH <= '7') { add(); CS = State.N8; }
            else if (CH is '8' or '9') { add(); CS = State.N10; }
            else if (CH == '.') { add(); CS = State.FP; }
            else if (let()) { CS = State.ER; }
            else { ungetc(); decimalPut(); CS = State.H; }
        }
        private void HandleStateN8()
        {
            while (CH >= '0' && CH <= '7') { add(); gc(); }
            if (CH is 'E' or 'e') { add(); CS = State.EXP; }
            else if (CH is 'O' or 'o') { add(); CS = State.O; }
            else if (CH is 'D' or 'd') { add(); CS = State.D; }
            else if (CH is 'H' or 'h') { add(); CS = State.HX; }
            else if (AFH()) { add(); CS = State.N16; }
            else if (CH is '8' or '9') { add(); CS = State.N10; }
            else if (CH == '.') { add(); CS = State.FP; }
            else if (let()) { CS = State.ER; }
            else { ungetc(); decimalPut(); CS = State.H; }
        }
        private void HandleStateN10()
        {
            while (digit()) { add(); gc(); }
            if (CH is 'E' or 'e') { add(); CS = State.EXP; }
            else if (CH is 'D' or 'd') { add(); CS = State.D; }
            else if (CH is 'H' or 'h') { add(); CS = State.HX; }
            else if (AFH()) { add(); CS = State.N16; }
            else if (CH == '.') { add(); CS = State.FP; }
            else if (let()) { CS = State.ER; }
            else { ungetc(); decimalPut(); CS = State.H; }
        }
        private void HandleStateN16()
        {
            while (check_hex()) { add(); gc(); }
            if (CH is 'H' or 'h') { add(); CS = State.HX; }
            else { CS = State.ER; }
        }

        private void HandleStateB()
        {
            if (CH is 'H' or 'h') // Если видим "101bH"
            {
                add();
                CS = State.HX;
            }
            else // Если после 'b' идет любой другой символ (пробел, ';', и т.д.)
            {
                ungetc();
                translate(2);
                CS = State.H;
            }
        }
        private void HandleStateO()
        {
            // 'O' не является hex-цифрой, неоднозначности нет. Любая буква/цифра после - ошибка.
            if (let() || digit())
            {
                CS = State.ER;
            }
            else
            {
                ungetc();
                translate(8);
                CS = State.H;
            }
        }
        private void HandleStateD()
        {
            if (CH is 'H' or 'h') // Если видим "34dh"
            {
                add();
                CS = State.HX;
            }
            else // Если после 'd' идет любой другой символ
            {
                ungetc();
                if (S.Length > 0 && (S[S.Length - 1] == 'd' || S[S.Length - 1] == 'D'))
                {
                    S.Length--; // Удаляем суффикс 'd'
                }
                decimalPut();
                CS = State.H;
            }
        }

        // HX - финальное состояние для шестнадцатеричных чисел
        private void HandleStateHX()
        {
            ungetc();
            translate(16);
            CS = State.H;
        }

        private void HandleStateP() { if (digit()) { add(); CS = State.FP; } else { ungetc(); S.Clear(); S.Append("."); int z = look(_delimiters); @out(2, z); CS = State.H; } }
        private void HandleStateFP() { while (digit()) { add(); gc(); } if (CH is 'E' or 'e') { add(); CS = State.ES2; } else if (let()) { CS = State.ER; } else { ungetc(); convert(); CS = State.H; } }
        private void HandleStateEXP()
        {
            if (digit())
            {
                add();
                CS = State.ED1;
            }
            else if (CH is '+' or '-')
            {
                add();
                CS = State.ES1;
            }
            else if (CH is 'H' or 'h') // Случай типа "12Eh"
            {
                add();
                CS = State.HX;
            }
            else if (check_hex()) // Случай типа "12EFh" (где F - следующий символ)
            {
                add(); // Добавляем 'F'
                CS = State.N16;
            }
            else
            {
                CS = State.ER;
            }
        }
        private void HandleStateES1() { if (digit()) { add(); CS = State.ED2; } else { CS = State.ER; } }
        private void HandleStateES2() { if (digit()) { add(); CS = State.ED3; } else if (CH is '+' or '-') { add(); CS = State.ES1; } else if (let() || CH == '.') { CS = State.ER; } else { CS = State.ER; } }
        private void HandleStateED1() { while (digit()) { add(); gc(); } if (invLet()) { CS = State.ER; } else { ungetc(); convert(); CS = State.H; } }
        private void HandleStateED2() { while (digit()) { add(); gc(); } if (let() || CH == '.') { CS = State.ER; } else { ungetc(); convert(); CS = State.H; } }
        private void HandleStateED3() { while (digit()) { add(); gc(); } if (let() || CH == '.') { CS = State.ER; } else { ungetc(); convert(); CS = State.H; } }

        // Обработка ограничителей.
        private void HandleStateOG()
        {
            nill(); add();
            if (":|&=".Contains(CH))
            {
                gc();
                string twoCharDelim = S.ToString() + CH;
                if (_delimiters.ContainsKey(twoCharDelim))
                { S.Append(CH); @out(2, look(_delimiters)); CS = State.H; return; }
                ungetc();
            }
            int z = look(_delimiters);
            if (z != 0) { @out(2, z); CS = State.H; }
            else { CS = State.ER; }
        }
        #endregion

        #region Helper Functions
        // 1) gc – процедура считывания очередного символа
        private void gc() { if (_currentIndex < _sourceText.Length) { CH = _sourceText[_currentIndex++]; if (CH == '\n') { _currentLine++; } } else { CH = '\0'; } }
        // ungetc – процедура возврата символа в поток для повторного чтения
        private void ungetc() => _currentIndex--;
        // 2) let – логическая функция, проверяющая, является ли СН буквой
        private bool let() => char.IsLetter(CH);
        // 3) digit - логическая функция, проверяющая, является ли СН цифрой
        private bool digit() => char.IsDigit(CH);
        // 4) nill – процедура очистки буфера S
        private void nill() => S.Clear();
        // 5) add – процедура добавления очередного символа в конец буфера S
        private void add() => S.Append(CH);
        // 6) look(t) – функция поиска лексемы из буфера S в таблице t
        private int look(Dictionary<string, int> table) { table.TryGetValue(S.ToString(), out int index); return index; }
        // 7) put(t) – процедура записи лексемы из буфера S в таблицу t
        private int put(Dictionary<string, int> table, List<string> orderedList)
        {
            string lexeme = S.ToString();
            if (table.TryGetValue(lexeme, out int index)) return index;
            int newIndex = table.Count + 1;
            table.Add(lexeme, newIndex);
            orderedList.Add(lexeme);
            return newIndex;
        }
        // 8) out(n, k) – процедура записи пары чисел (n, k). Имя изменено на @out, так как 'out' - зарезервированное слово в C#.
        private void @out(int tableCode, int entryIndex) { _tokens.Add(new Token(tableCode, entryIndex)); }
        // 9) check_hex – логическая функция, проверяющая на шестнадцатеричную цифру
        private bool check_hex() => "0123456789ABCDEFabcdef".Contains(CH);
        // 10) AFH – логическая функция, проверяющая на A..F
        private bool AFH() => "ABCDEFabcdef".Contains(CH);
        // invLet – проверяет, является ли символ недопустимой буквой после числа(например, '123G').
        private bool invLet() => char.IsLetter(CH) && !"BbOoDdHhEe".Contains(CH);
        // 11) translate(base) – процедура перевода числа из системы счисления
        private void translate(int numberBase)
        {
            string numberStr = S.ToString();
            if ("BbOoHh".Contains(numberStr.Last())) { numberStr = numberStr.Substring(0, numberStr.Length - 1); }
            try
            {
                long val = Convert.ToInt64(numberStr, numberBase);
                S.Clear(); S.Append(val.ToString());
                int z = put(_numbers, _numberList);
                @out(3, z);
            }
            catch { CS = State.ER; }
        }
        // 12) convert – процедура преобразования действительного/экспоненциального числа
        private void convert()
        {
            try
            {
                string originalString = S.ToString();

                // Используем NumberStyles.Float, который включает в себя
                // поддержку экспоненциальной нотации (в том числе без знака после 'E').
                var numberStyle = NumberStyles.Float | NumberStyles.AllowDecimalPoint;
                var d = double.Parse(originalString, numberStyle, CultureInfo.InvariantCulture);

                S.Clear();
                string convertedString = d.ToString("G", CultureInfo.InvariantCulture);

                // Добавляем ".0" для чисел, которые были введены с точкой, но потеряли ее после конвертации (например, 987.)
                // Исключаем случаи, когда число уже в экспоненциальной нотации.
                if (!convertedString.Contains('.') && !convertedString.ToUpper().Contains('E') && originalString.Contains('.'))
                {
                    convertedString += ".0";
                }

                S.Append(convertedString);

                int z = put(_numbers, _numberList);
                @out(3, z);
            }
            catch (FormatException)
            {
                CS = State.ER;
            }
        }
        // Вспомогательная функция для добавления десятичного числа в таблицу
        private void decimalPut() { int z = put(_numbers, _numberList); @out(3, z); }
        // Сброс состояния анализатора для повторного анализа
        private void Reset()
        {
            _currentIndex = 0; _currentLine = 1;
            S.Clear(); _tokens.Clear();
            _identifiers.Clear(); _numbers.Clear();
            _identifierList.Clear(); _numberList.Clear();
            _customErrorMessage = null;
        }
        #endregion
    }
}