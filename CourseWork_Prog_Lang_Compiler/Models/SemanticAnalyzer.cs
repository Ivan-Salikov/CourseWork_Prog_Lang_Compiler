namespace CourseWork_Prog_Lang_Compiler.Models
{
    /// <summary>
    /// Результат семантического анализа.
    /// </summary>
    public class SemanticAnalysisResult
    {
        public bool IsSuccess { get; set; }
        public List<SemanticError> Errors { get; set; } = new();
    }

    /// <summary>
    /// Семантическая ошибка.
    /// </summary>
    public class SemanticError
    {
        public int Id { get; set; }
        public string Description { get; set; }
    }

    /// <summary>
    /// Исключение, выбрасываемое семантическим анализатором при обнаружении семантической ошибки.
    /// Используется для прерывания процесса анализа после первой найденной ошибки.
    /// </summary>
    public class SemanticAnalysisException : Exception
    {
        public SemanticAnalysisException(string message) : base(message) { }
    }

    /// <summary>
    /// Семантический анализатор.
    /// Проверяет контекстно-зависимые правила на основе результатов лексического и синтаксического анализа.
    /// Интегрирован с синтаксическим анализатором.
    /// Останавливается после обнаружения первой ошибки.
    /// </summary>
    public class SemanticAnalyzer
    {
        // Семантический стек для анализа выражений
        private Stack<string> _semanticStack = new();

        // Таблица идентификаторов (хранит тип и флаг описания)
        // Ключ - индекс из лексической таблицы идентификаторов
        private Dictionary<int, TableEntry> _identifierTable = new();

        // Таблица операций (для проверки совместимости типов)
        private Dictionary<string, Dictionary<(string, string), string>> _operationsTable = new();

        // Список ошибок
        private List<SemanticError> _errors = new();
        private int _errorIdCounter = 1;

        // Таблицы, передаваемые из лексического анализатора
        private List<TableEntry> _lexicalIdentifiers;
        private List<TableEntry> _lexicalNumbers;

        public SemanticAnalyzer(List<TableEntry> identifiers, List<TableEntry> numbers)
        {
            _lexicalIdentifiers = identifiers;
            _lexicalNumbers = numbers;

            // Инициализируем семантическую таблицу идентификаторов на основе лексической
            foreach (var entry in _lexicalIdentifiers)
            {
                // Копируем Id и Value, IsDeclared и Type устанавливаются в false и "" по умолчанию
                _identifierTable[entry.Id] = new TableEntry(entry.Id, entry.Value) { IsDeclared = false, Type = "" };
            }

            InitializeOperationsTable();
        }

        private void InitializeOperationsTable()
        {
            // Заполняем таблицу операций с типами, используемыми в грамматике: int, float, bool
            var intInt = ("int", "int");
            var floatFloat = ("float", "float");
            var intFloat = ("int", "float");
            var floatInt = ("float", "int");
            var boolBool = ("bool", "bool");

            // +, -, *
            foreach (var op in new[] { "+", "-", "*" })
            {
                _operationsTable[op] = new Dictionary<(string, string), string>
                {
                    { intInt, "int" }, { floatFloat, "float" }, { intFloat, "float" }, { floatInt, "float" }
                };
            }

            // / (всегда float при делении int)
            _operationsTable["/"] = new Dictionary<(string, string), string>
            {
                { intInt, "float" }, { floatFloat, "float" }, { intFloat, "float" }, { floatInt, "float" }
            };

            // ==, !=
            foreach (var op in new[] { "==", "!=" })
            {
                _operationsTable[op] = new Dictionary<(string, string), string>
                {
                    { intInt, "bool" }, { floatFloat, "bool" }, { intFloat, "bool" }, { floatInt, "bool" }, { boolBool, "bool" }
                };
            }

            // <, >, <=, >=
            foreach (var op in new[] { "<", ">", "<=", ">=" })
            {
                _operationsTable[op] = new Dictionary<(string, string), string>
                {
                    { intInt, "bool" }, { floatFloat, "bool" }, { intFloat, "bool" }, { floatInt, "bool" }
                };
            }

            // &&, ||
            foreach (var op in new[] { "&&", "||" })
            {
                _operationsTable[op] = new Dictionary<(string, string), string>
                {
                    { boolBool, "bool" }
                };
            }
        }

        // --- Вспомогательные методы для сообщений ---
        private string GetLexemeValue(Token token)
        {
            if (token.TableCode == 4) // ID
            {
                var entry = _lexicalIdentifiers.FirstOrDefault(x => x.Id == token.EntryIndex);
                return entry != null ? entry.Value : $"ID#{token.EntryIndex}";
            }
            if (token.TableCode == 3) // NUM
            {
                var entry = _lexicalNumbers.FirstOrDefault(x => x.Id == token.EntryIndex);
                return entry != null ? entry.Value : $"NUM#{token.EntryIndex}";
            }
            if (token.TableCode == 1) // TW
            {
                var serviceWords = new List<string> { "program", "var", "begin", "end", "int", "float", "bool", "if", "else", "while", "for", "to", "step", "next", "true", "false", "readln", "writeln" };
                if (token.EntryIndex > 0 && token.EntryIndex <= serviceWords.Count)
                {
                    return serviceWords[token.EntryIndex - 1];
                }
            }
            if (token.TableCode == 2) // TL
            {
                var delimiters = new List<string> { ";", ",", ":", ":=", "!", "!=", "==", "<", ">", "<=", ">=", "+", "-", "||", "*", "&&", "/", "(", ")", "{", "}", "." };
                if (token.EntryIndex > 0 && token.EntryIndex <= delimiters.Count)
                {
                    return delimiters[token.EntryIndex - 1];
                }
            }
            return $"({token.TableCode},{token.EntryIndex})";
        }

        private string GetOperatorSymbol(Token operatorToken)
        {
            if (operatorToken.TableCode == 2)
            {
                var delimiters = new List<string> { ";", ",", ":", ":=", "!", "!=", "==", "<", ">", "<=", ">=", "+", "-", "||", "*", "&&", "/", "(", ")", "{", "}", "." };
                if (operatorToken.EntryIndex > 0 && operatorToken.EntryIndex <= delimiters.Count)
                {
                    return delimiters[operatorToken.EntryIndex - 1];
                }
            }
            return "?";
        }

        public SemanticAnalysisResult Analyze(List<Token> tokens)
        {
            bool finalSuccess = _errors.Count == 0 && _semanticStack.Count == 0;
            return new SemanticAnalysisResult { IsSuccess = finalSuccess, Errors = _errors };
        }

        // --- Процедуры и функции для вызова из SyntaxAnalyzer ---

        public void StartDescription() { }

        // Добавлен параметр tokenPosition
        public void AddIdentifierToDescription(Token identifierToken, string type, int tokenPosition)
        {
            if (identifierToken.TableCode == 4 && _identifierTable.ContainsKey(identifierToken.EntryIndex))
            {
                var entry = _identifierTable[identifierToken.EntryIndex];
                if (entry.IsDeclared)
                {
                    AddError($"Семантическая ошибка на токене №{tokenPosition + 1}: Идентификатор '{entry.Value}' уже описан.", tokenPosition);
                }
                else
                {
                    _identifierTable[identifierToken.EntryIndex] = new TableEntry(entry.Id, entry.Value) { IsDeclared = true, Type = type };
                }
            }
            else
            {
                if (identifierToken.TableCode != 4)
                {
                    AddError($"Семантическая ошибка на токене №{tokenPosition + 1}: Ожидается токен идентификатора (4, x), но найден ({identifierToken.TableCode}, {identifierToken.EntryIndex}).", tokenPosition);
                }
                else
                {
                    AddError($"Семантическая ошибка на токене №{tokenPosition + 1}: Идентификатор ({identifierToken.TableCode}, {identifierToken.EntryIndex}) не найден в таблице лексических идентификаторов при описании.", tokenPosition);
                }
            }
        }

        public void FinishDescription(string type) { }

        // Добавлен параметр tokenPosition
        public bool ProcessIdentifier(Token identifierToken, out string type, int tokenPosition)
        {
            type = "";
            if (identifierToken.TableCode == 4 && _identifierTable.ContainsKey(identifierToken.EntryIndex))
            {
                var entry = _identifierTable[identifierToken.EntryIndex];
                if (!entry.IsDeclared)
                {
                    AddError($"Семантическая ошибка на токене №{tokenPosition + 1}: Идентификатор '{entry.Value}' используется, но не описан.", tokenPosition);
                    return false; // Ошибка
                }
                type = entry.Type;
                _semanticStack.Push(type);
                return true; // Успешно
            }
            else
            {
                if (identifierToken.TableCode != 4)
                {
                    AddError($"Семантическая ошибка на токене №{tokenPosition + 1}: Ожидается токен идентификатора (4, x), но найден ({identifierToken.TableCode}, {identifierToken.EntryIndex}).", tokenPosition);
                }
                else
                {
                    AddError($"Семантическая ошибка на токене №{tokenPosition + 1}: Идентификатор ({identifierToken.TableCode}, {identifierToken.EntryIndex}) не найден при использовании.", tokenPosition);
                }
                return false; // Ошибка
            }
        }

        // Добавлен параметр tokenPosition
        public void ProcessConstant(Token constantToken, int tokenPosition)
        {
            if (constantToken.TableCode == 3) // TN - число
            {
                string type = DetermineNumberType(constantToken.EntryIndex);
                _semanticStack.Push(type);
            }
            else if (constantToken.TableCode == 1 && (constantToken.EntryIndex == 15 || constantToken.EntryIndex == 16)) // TW - true (1,15), false (1,16)
            {
                _semanticStack.Push("bool");
            }
            else
            {
                AddError($"Семантическая ошибка на токене №{tokenPosition + 1}: Ожидается число (3, x) или булевая константа (true/false), но найден ({constantToken.TableCode}, {constantToken.EntryIndex}).", tokenPosition);
            }
        }

        private string DetermineNumberType(int entryIndex)
        {
            if (entryIndex > 0 && entryIndex <= _lexicalNumbers.Count)
            {
                string value = _lexicalNumbers[entryIndex - 1].Value;
                if (value.Contains('.') || value.ToLower().Contains('e'))
                {
                    return "float";
                }
                else
                {
                    return "int";
                }
            }
            return "unknown";
        }

        // Добавлен параметр tokenPosition
        public void ProcessBinaryOperation(Token operatorToken, int tokenPosition)
        {
            if (_semanticStack.Count < 2)
            {
                AddError($"Семантическая ошибка на токене №{tokenPosition + 1}: Недостаточно операндов для бинарной операции в семантическом стеке.", tokenPosition);
                return;
            }

            string rightType = _semanticStack.Pop();
            string leftType = _semanticStack.Pop();
            string opSymbol = GetOperatorSymbol(operatorToken);

            if (_operationsTable.ContainsKey(opSymbol) && _operationsTable[opSymbol].ContainsKey((leftType, rightType)))
            {
                string resultType = _operationsTable[opSymbol][(leftType, rightType)];
                _semanticStack.Push(resultType);
            }
            else
            {
                AddError($"Семантическая ошибка на токене №{tokenPosition + 1}: Несовместимые типы '{leftType}' и '{rightType}' для операции '{opSymbol}'.", tokenPosition);
            }
        }

        // Добавлен параметр tokenPosition
        public void ProcessUnaryOperation(Token operatorToken, int tokenPosition)
        {
            if (_semanticStack.Count < 1)
            {
                AddError($"Семантическая ошибка на токене №{tokenPosition + 1}: Недостаточно операндов для унарной операции 'not' в семантическом стеке.", tokenPosition);
                return;
            }

            string operandType = _semanticStack.Pop();
            string opSymbol = GetOperatorSymbol(operatorToken);

            if (opSymbol == "!" && operandType == "bool")
            {
                _semanticStack.Push("bool");
            }
            else
            {
                AddError($"Семантическая ошибка на токене №{tokenPosition + 1}: Несовместимый тип '{operandType}' для унарной операции '{opSymbol}'. Ожидается 'bool'.", tokenPosition);
            }
        }

        // Добавлен параметр tokenPosition
        public bool CheckExpressionType(string expectedType, int tokenPosition)
        {
            if (_semanticStack.Count < 1)
            {
                AddError($"Семантическая ошибка на токене №{tokenPosition + 1}: Нет типа выражения для проверки.", tokenPosition);
                return false;
            }

            string actualType = _semanticStack.Peek();
            if (actualType != expectedType)
            {
                AddError($"Семантическая ошибка на токене №{tokenPosition + 1}: Ожидается тип '{expectedType}', но выражение имеет тип '{actualType}'.", tokenPosition);
                return false;
            }
            return true;
        }

        // Добавлен параметр tokenPosition
        public void CheckAndConsumeType(string expectedType, int tokenPosition)
        {
            CheckExpressionType(expectedType, tokenPosition); // Проверяем тип
            if (_semanticStack.Count > 0)
            {
                _semanticStack.Pop(); // Убираем тип из стека после проверки
            }
        }

        // Добавлен параметр tokenPosition
        public void CheckAssignment(Token identifierToken, int tokenPosition)
        {
            if (_semanticStack.Count < 1)
            {
                AddError($"Семантическая ошибка на токене №{tokenPosition + 1}: Нет типа выражения для проверки присваивания.", tokenPosition);
                return;
            }

            string exprType = _semanticStack.Pop(); // Получаем тип выражения

            string varTypeFromOutParam = "";
            if (!ProcessIdentifier(identifierToken, out varTypeFromOutParam, tokenPosition)) // Передаём позицию идентификатора
            {
                // Ошибка использования идентификатора уже добавлена в ProcessIdentifier
                return;
            }

            if (_semanticStack.Count < 1)
            {
                AddError($"Семантическая ошибка на токене №{tokenPosition + 1}: Нет типа переменной для проверки присваивания (внутренняя ошибка).", tokenPosition);
                return;
            }
            string varTypeFromStack = _semanticStack.Pop(); // Извлекаем тип переменной из стека

            if (exprType != varTypeFromStack)
            {
                if (!(varTypeFromStack == "float" && exprType == "int"))
                {
                    AddError($"Семантическая ошибка на токене №{tokenPosition + 1}: Несовместимые типы при присваивании: переменная '{_identifierTable[identifierToken.EntryIndex].Value}' типа '{varTypeFromStack}', выражение типа '{exprType}'.", tokenPosition);
                }
                // Если varType == float и exprType == int, это допустимое присваивание.
            }
        }

        // Добавлен параметр tokenPosition
        public void CheckCondition(int tokenPosition)
        {
            CheckExpressionType("bool", tokenPosition);
            if (_semanticStack.Count > 0) _semanticStack.Pop(); // Убираем тип условия из стека
        }

        // Добавлен параметр tokenPosition
        public void ProcessReadStatement(Token identifierToken, int tokenPosition)
        {
            if (identifierToken.TableCode == 4 && _identifierTable.ContainsKey(identifierToken.EntryIndex))
            {
                var entry = _identifierTable[identifierToken.EntryIndex];
                if (!entry.IsDeclared)
                {
                    AddError($"Семантическая ошибка на токене №{tokenPosition + 1}: Идентификатор '{entry.Value}' в readln не описан.", tokenPosition);
                }
            }
            else
            {
                if (identifierToken.TableCode != 4)
                {
                    AddError($"Семантическая ошибка на токене №{tokenPosition + 1}: Ожидается токен идентификатора (4, x) для readln, но найден ({identifierToken.TableCode}, {identifierToken.EntryIndex}).", tokenPosition);
                }
                else
                {
                    AddError($"Семантическая ошибка на токене №{tokenPosition + 1}: Идентификатор ({identifierToken.TableCode}, {identifierToken.EntryIndex}) в readln не найден.", tokenPosition);
                }
            }
        }

        // Добавлен параметр tokenPosition
        public void ProcessWriteStatement(int tokenPosition)
        {
            if (_semanticStack.Count < 1)
            {
                AddError($"Семантическая ошибка на токене №{tokenPosition + 1}: Нет типа выражения для вывода в writeln.", tokenPosition);
            }
            else
            {
                _semanticStack.Pop(); // Убираем тип выражения из стека после вывода
            }
        }

        public bool CheckStackEmpty()
        {
            return _semanticStack.Count == 0;
        }

        // Метод для получения копии списка ошибок (для использования в SyntaxAnalyzer)
        public List<SemanticError> GetErrorsCopy()
        {
            return new List<SemanticError>(_errors);
        }

        // Метод для финальной проверки стека (для вызова из SyntaxAnalyzer)
        public void FinalizeAnalysis()
        {
            if (!CheckStackEmpty())
            {
                string types = string.Join(", ", _semanticStack);
                AddError($"Семантическая ошибка в конце программы: Стек типов не пуст ({types}). Проверьте структуру выражений.", -1); // Позиция -1 для конца программы
            }
        }

        // Добавлен параметр tokenPosition
        private void AddError(string description, int tokenPosition)
        {
            // Если tokenPosition -1, это ошибка в конце программы
            string fullDescription = tokenPosition == -1 ? description : $"{description}";
            _errors.Add(new SemanticError { Id = _errorIdCounter++, Description = fullDescription });
            throw new SemanticAnalysisException(fullDescription);
        }
    }

    // Вспомогательный класс для хранения символов операций, соответствующих индексам в TL
    public static class OperationsTableHelper
    {
        public static readonly List<string> DelimiterSymbols = new List<string>
        {
            ";", // 1
            ",", // 2
            ":", // 3
            ":=", // 4
            "!", // 5
            "!=", // 6
            "==", // 7
            "<", // 8
            ">", // 9
            "<=", // 10
            ">=", // 11
            "+", // 12
            "-", // 13
            "||", // 14
            "*", // 15
            "&&", // 16
            "/", // 17
            "(", // 18
            ")", // 19
            "{", // 20
            "}", // 21
            "."  // 22
        };
    }
}