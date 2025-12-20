namespace CourseWork_Prog_Lang_Compiler.Models
{
    /// <summary>
    /// Исключение, выбрасываемое синтаксическим анализатором при обнаружении синтаксической ошибки.
    /// Используется для прерывания процесса анализа после первой найденной ошибки.
    /// </summary>
    public class SyntaxAnalysisException : Exception
    {
        public SyntaxAnalysisException(string message) : base(message)
        {
        }
    }

    /// <summary>
    /// Синтаксический анализатор, реализующий метод рекурсивного спуска.
    /// Принимает на вход последовательность токенов, сгенерированных лексическим анализатором,
    /// и проверяет синтаксическую корректность программы в соответствии с грамматикой языка М.
    /// Также выполняет семантическую проверку и генерацию ПОЛИЗ.
    /// Останавливается после обнаружения первой ошибки (синтаксической или семантической).
    /// </summary>
    public class SyntaxAnalyzer
    {
        private List<Token> _tokens;        // Входной список токенов
        private int _currentIndex;          // Индекс текущего токена
        private Token _currentToken;        // Текущая лексема (LEX)

        // Поля для хранения таблиц лексем
        private readonly List<string> _serviceWords;
        private readonly List<string> _delimiters;
        private List<string> _identifiers;
        private List<string> _numbers;

        private SemanticAnalyzer _semanticAnalyzer;

        // Поля для генерации ПОЛИЗ
        private List<string> _polishNotation = new List<string>();
        private Stack<int> _patchStack = new Stack<int>();

        // Константы операций ПОЛИЗ
        private const string OP_JUMP_FALSE = "@F";
        private const string OP_JUMP_ALWAYS = "@!";
        private const string OP_READ = "~RL";
        private const string OP_WRITE = "~WL";
        private const string OP_PROGRAM_END = ".";

        public SyntaxAnalyzer()
        {
            _serviceWords = new List<string> { "program", "var", "begin", "end", "int", "float", "bool", "if", "else", "while", "for", "to", "step", "next", "true", "false", "readln", "writeln" };
            _delimiters = new List<string> { ";", ",", ":", ":=", "!", "!=", "==", "<", ">", "<=", ">=", "+", "-", "||", "*", "&&", "/", "(", ")", "{", "}", "." };

            _identifiers = new List<string>();
            _numbers = new List<string>();
        }

        public SyntaxAnalysisResult Parse(List<Token> tokens, List<TableEntry> identifiers, List<TableEntry> numbers)
        {
            // Инициализируем состояние анализатора
            _tokens = tokens ?? new List<Token>();
            _currentIndex = 0;

            // Загружаем таблицы идентификаторов и чисел
            _identifiers = identifiers.OrderBy(e => e.Id).Select(e => e.Value).ToList();
            _numbers = numbers.OrderBy(e => e.Id).Select(e => e.Value).ToList();

            // Создаем семантический анализатор с таблицами из лексического анализатора
            _semanticAnalyzer = new SemanticAnalyzer(identifiers, numbers);

            // Очистка ПОЛИЗ
            _polishNotation.Clear();
            _patchStack.Clear();

            // Устанавливаем начальную лексему, если список не пуст
            if (_tokens.Count > 0)
            {
                _currentToken = _tokens[0];
            }
            else
            {
                return new SyntaxAnalysisResult { IsSuccess = false, Errors = new List<SyntaxError> { new SyntaxError { Id = 1, Description = "Ожидается начало программы (например, 'program')." } } };
            }

            var errors = new List<SyntaxError>();
            int errorIdCounter = 1;

            try
            {
                // Запускаем процедуру для начального нетерминала (Prog)
                Prog();
            }
            catch (SyntaxAnalysisException ex)
            {
                errors.Add(new SyntaxError { Id = errorIdCounter++, Description = ex.Message });
                // Прерываем анализ
                var finalSemanticResultFromSA = new SemanticAnalysisResult { IsSuccess = false, Errors = new List<SemanticError>() };
                return new SyntaxAnalysisResult { IsSuccess = false, Errors = errors, SemanticErrors = finalSemanticResultFromSA.Errors };
            }
            catch (SemanticAnalysisException)
            {
                // Ошибки семантики уже собраны
                var finalSemanticResultFromSA = new SemanticAnalysisResult { IsSuccess = false, Errors = _semanticAnalyzer.GetErrorsCopy() };
                return new SyntaxAnalysisResult { IsSuccess = false, Errors = errors, SemanticErrors = finalSemanticResultFromSA.Errors };
            }

            // Проверяем, пуст ли семантический стек в конце программы
            try
            {
                _semanticAnalyzer.FinalizeAnalysis();
            }
            catch (SemanticAnalysisException)
            {
                var finalSemanticResultFromSA = new SemanticAnalysisResult { IsSuccess = false, Errors = _semanticAnalyzer.GetErrorsCopy() };
                return new SyntaxAnalysisResult { IsSuccess = false, Errors = errors, SemanticErrors = finalSemanticResultFromSA.Errors };
            }

            // Добавляем конец программы
            GenOp(OP_PROGRAM_END);

            // Если исключение не было выброшено, анализ завершён
            bool finalSyntaxSuccess = errors.Count == 0 && _currentIndex >= _tokens.Count;
            var finalSemanticResult = _semanticAnalyzer.GetErrorsCopy();

            return new SyntaxAnalysisResult
            {
                IsSuccess = finalSyntaxSuccess && finalSemanticResult.Count == 0,
                Errors = errors,
                SemanticErrors = finalSemanticResult,
                PolishNotation = new List<string>(_polishNotation)
            };
        }

        // --- Внутренние методы анализатора ---
        // Процедура считывания очередной лексемы (gl)
        private void gl()
        {
            _currentIndex++;
            if (_currentIndex < _tokens.Count)
            {
                _currentToken = _tokens[_currentIndex];
            }
            else
            {
                // Если достигли конца, устанавливаем специальный "пустой" токен
                _currentToken = new Token(0, 0);
            }
        }

        // Логическая функция проверки текущей лексемы (EQ(S))
        // Принимает код таблицы (n) и ожидаемый индекс (k) для лексемы S
        private bool EQ(int expectedTableCode, int expectedEntryIndex)
        {
            if (_currentIndex >= _tokens.Count || _currentIndex < 0) return false;
            return _currentToken.TableCode == expectedTableCode && _currentToken.EntryIndex == expectedEntryIndex;
        }

        // Логическая функция проверки, является ли текущая лексема идентификатором (ID)
        private bool ID()
        {
            if (_currentIndex >= _tokens.Count || _currentIndex < 0) return false;
            return _currentToken.TableCode == 4; // TI - таблица идентификаторов (4)
        }

        // Логическая функция проверки, является ли текущая лексема числом (NUM)
        private bool NUM()
        {
            if (_currentIndex >= _tokens.Count || _currentIndex < 0) return false;
            return _currentToken.TableCode == 3; // TN - таблица чисел (3)
        }

        // Логическая функция проверки, является ли текущая лексема булевой константой (TRUE или FALSE)
        private bool BOOL_CONST()
        {
            if (_currentIndex >= _tokens.Count || _currentIndex < 0) return false;
            return _currentToken.TableCode == 1 && (_currentToken.EntryIndex == 15 || _currentToken.EntryIndex == 16);
        }

        // --- Методы генерации ПОЛИЗ ---
        private void GenOp(string operation) => _polishNotation.Add(operation);
        private void GenId(string name, bool asAddress = false) => _polishNotation.Add(asAddress ? "@" + name : name);
        private void GenNum(string value) => _polishNotation.Add(value);
        private void PushPatchPoint()
        {
            _patchStack.Push(_polishNotation.Count);
            _polishNotation.Add("?");
        }
        private void Patch()
        {
            int patchIndex = _patchStack.Pop();
            _polishNotation[patchIndex] = _polishNotation.Count.ToString();
        }

        // --- Методы для улучшенной диагностики ---
        // Метод для проверки "близости" строк (проверяет расстояние Левенштейна <= 1)
        private bool IsWordClose(string word1, string word2)
        {
            if (string.IsNullOrEmpty(word1) || string.IsNullOrEmpty(word2)) return false;
            int len1 = word1.Length; int len2 = word2.Length;
            if (Math.Max(len1, len2) == 0) return true;
            if (Math.Abs(len1 - len2) > 1) return false;
            int i = 0, j = 0, diff = 0;
            while (i < len1 && j < len2)
            {
                if (word1[i] != word2[j]) { diff++; if (diff > 1) return false; if (len1 > len2) i++; else if (len1 < len2) j++; else { i++; j++; } } else { i++; j++; }
            }
            diff += (len1 - i) + (len2 - j);
            return diff == 1;
        }

        // Метод для поиска близкого ключевого слова в списке ожидаемых
        private string FindClosestExpectedKeyword(List<string> expectedKeywords, string foundWord)
        {
            foreach (var expected in expectedKeywords)
            {
                if (IsWordClose(expected, foundWord)) return expected;
            }
            return null;
        }

        // Oшибка для случаев, когда ожидается ключевое слово или идентификатор
        private void ERRWithExpectedKeywords(List<string> expectedKeywords)
        {
            string foundTokenStr = TokenToString(_currentToken);
            string foundType = _currentToken.TableCode == 1 ? "служебное слово" : "лексема";
            string message;

            if (_currentToken.TableCode == 4 || _currentToken.TableCode == 1)
            {
                string closest = FindClosestExpectedKeyword(expectedKeywords, foundTokenStr);
                if (closest != null)
                {
                    message = $"Возможно, имелось в виду ключевое слово '{closest}', но найдено '{foundTokenStr}' на токене №{_currentIndex + 1}.";
                    throw new SyntaxAnalysisException(message);
                }
            }

            message = $"Ожидается одно из ключевых слов ({string.Join(", ", expectedKeywords)}), но обнаружено {foundType} '{foundTokenStr}' на токене №{_currentIndex + 1}.";
            throw new SyntaxAnalysisException(message);
        }

        // Стандартная ошибка
        private void ERR(string expected = "")
        {
            string found = $"'{TokenToString(_currentToken)}'";
            string message = !string.IsNullOrEmpty(expected)
                ? $"Ожидается {expected}, но обнаружен {found} на токене №{_currentIndex + 1}."
                : $"Синтаксическая ошибка на токене №{_currentIndex + 1}: неожиданный токен {found}.";
            throw new SyntaxAnalysisException(message);
        }

        private string TokenToString(Token token)
        {
            if (token.TableCode == 0 && token.EntryIndex == 0) return "конец файла";
            try
            {
                return token.TableCode switch
                {
                    1 => _serviceWords[token.EntryIndex - 1],
                    2 => _delimiters[token.EntryIndex - 1],
                    3 => _numbers[token.EntryIndex - 1],
                    4 => _identifiers[token.EntryIndex - 1],
                    _ => $"(неизвестный токен {token.TableCode},{token.EntryIndex})"
                };
            }
            catch (ArgumentOutOfRangeException)
            {
                return $"(неверный индекс {token.TableCode},{token.EntryIndex})";
            }
        }

        // --- Процедуры для нетерминалов грамматики ---
        // Prog → program var DeclarationSection begin StatementList end .
        private void Prog()
        {
            if (EQ(1, 1)) gl(); else ERRWithExpectedKeywords(new List<string> { _serviceWords[0] });    // 'program' (1,1)
            if (EQ(1, 2)) gl(); else ERRWithExpectedKeywords(new List<string> { _serviceWords[1] });    // 'var' (1,2)

            DeclarationSection();

            if (EQ(1, 3)) gl(); else ERRWithExpectedKeywords(new List<string> { _serviceWords[2] });    // 'begin' (1,3)

            StatementList();

            if (EQ(1, 4)) gl(); else ERRWithExpectedKeywords(new List<string> { _serviceWords[3] });    // 'end' (1,4)

            if (EQ(2, 22)) gl(); else ERR("'.' (2,22)");    // '.' (2,22)
        }

        // DeclarationSection → Declaration | DeclarationSection Declaration
        // Эквивалентная форма: Declaration {Declaration}
        private void DeclarationSection()
        {
            // Проверяем, начинается ли первое Declaration с Type.
            if (!EQ(1, 5) && !EQ(1, 6) && !EQ(1, 7)) // Не 'int', 'float', 'bool'
            {
                if (EQ(1, 3)) return;   // 'begin'
                else
                {
                    ERRWithExpectedKeywords(new List<string> { _serviceWords[4], _serviceWords[5], _serviceWords[6] });
                    return;
                }
            }

            Declaration();

            // Цикл для последующих Declaration
            while (EQ(1, 5) || EQ(1, 6) || EQ(1, 7)) // 'int', 'float', 'bool'
            {
                Declaration();
            }
        }

        // Declaration → Type IdentifierList ;
        private void Declaration()
        {
            string declaredType = TokenToString(_currentToken);
            Type();
            _semanticAnalyzer.StartDescription();

            // Обработка первого идентификатора
            if (ID())
            {
                _semanticAnalyzer.AddIdentifierToDescription(_currentToken, declaredType, _currentIndex);
                gl();
            }
            else ERR("идентификатор (TI, x)"); // Ожидается идентификатор

            // Цикл для последующих идентификаторов
            while (true)
            {
                if (EQ(2, 2)) // ','
                {
                    gl();
                    if (ID())
                    {
                        _semanticAnalyzer.AddIdentifierToDescription(_currentToken, declaredType, _currentIndex);
                        gl();
                    }
                    else ERR("идентификатор (TI, x)");
                }
                else if (ID()) ERR("',' (2,2)");    // Ожидается запятая перед следующим идентификатором
                else break;
            }

            _semanticAnalyzer.FinishDescription(declaredType);

            if (EQ(2, 1)) gl(); else ERR("';' (2,1)");  // Ожидается точка с запятой после списка идентификаторов
        }

        // Type → int | float | bool
        private void Type()
        {
            if (EQ(1, 5) || EQ(1, 6) || EQ(1, 7)) gl(); // 'int' (1,5), 'float' (1,6), 'bool' (1,7)
            else ERRWithExpectedKeywords(new List<string> { _serviceWords[4], _serviceWords[5], _serviceWords[6] });
        }

        // StatementList → Statement {; Statement}
        private void StatementList()
        {
            if (EQ(1, 4)) return; // 'end'
            Statement();

            // Цикл для последующих операторов
            while (true)
            {
                if (EQ(2, 1)) // ';'
                {
                    gl();
                    if (EQ(1, 4)) break; // 'end' (1,4)
                    if (ID())
                    {
                        if (IsWordClose(TokenToString(_currentToken), "end")) throw new SyntaxAnalysisException($"Возможно, имелось в виду ключевое слово 'end', но найдено '{TokenToString(_currentToken)}' на токене №{_currentIndex + 1}.");
                    }
                    Statement();
                }
                else if (EQ(1, 4)) break; else ERR("';' (2,1)");
            }
        }

        // Statement → CompoundStatement | AssignmentStatement | IfStatement | ForStatement | WhileStatement | InputStatement | OutputStatement
        private void Statement()
        {
            if (EQ(1, 3)) CompoundStatement();      // 'begin' (начало составного оператора)
            else if (EQ(1, 8)) IfStatement();       // 'if' (условный оператор)
            else if (EQ(1, 11)) ForStatement();     // 'for' (цикл for)
            else if (EQ(1, 10)) WhileStatement();   // 'while' (цикл while)
            else if (EQ(1, 17)) InputStatement();   // 'readln' (ввод)
            else if (EQ(1, 18)) OutputStatement();  // 'writeln' (вывод)
            else if (ID()) // Начинается с идентификатора (присваивание)
            {
                // Проверка, не является ли идентификатор опечаткой одного из ключевых слов оператора
                string idVal = TokenToString(_currentToken);
                // Список ключевых слов, которые могут начинать Statement
                var keywords = new List<string> { "begin", "if", "for", "while", "readln", "writeln", "end" };
                string closest = FindClosestExpectedKeyword(keywords, idVal);
                if (closest != null) throw new SyntaxAnalysisException($"Возможно, имелось в виду ключевое слово '{closest}', но найдено идентификатор '{idVal}' на токене №{_currentIndex + 1}.");

                AssignmentStatement();
            }
            else ERRWithExpectedKeywords(new List<string> { _serviceWords[2], _serviceWords[7], _serviceWords[10], _serviceWords[9], _serviceWords[16], _serviceWords[17] });
        }

        // AssignmentStatement → Identifier := Expression
        private void AssignmentStatement()
        {
            Token identifierToken = _currentToken;
            int pos = _currentIndex;
            if (ID())
            {
                GenId(TokenToString(identifierToken), true); // Генерируем адрес @x
                gl();
            }
            else ERR("идентификатор (TI, x)"); // Ожидается идентификатор слева от :=

            if (EQ(2, 4)) gl(); else ERR("':=' (2,4)"); // Ожидается присваивание ':='

            Expression();

            _semanticAnalyzer.CheckAssignment(identifierToken, pos);
            GenOp(":=");
        }

        // ForStatement → for AssignmentStatement to Expression [step Expression] Statement next
        private void ForStatement()
        {
            if (EQ(1, 11)) gl(); else ERRWithExpectedKeywords(new List<string> { _serviceWords[10] });  // 'for' (1,11)

            Token counterToken = _currentToken;
            string counterName = TokenToString(counterToken);

            AssignmentStatement(); // Инициализация цикла

            if (EQ(1, 12)) gl(); else ERRWithExpectedKeywords(new List<string> { _serviceWords[11] });  // 'to' (1,12)

            // Метка начала цикла (для пересчета условия)
            int startConditionAddress = _polishNotation.Count;

            // Условие окончания (i <= limit)
            GenId(counterName);
            Expression(); // limit (выражение вычисляется заново)
            _semanticAnalyzer.CheckAndConsumeType("int", _currentIndex);

            GenOp("<=");

            PushPatchPoint(); // Переход по лжи
            GenOp(OP_JUMP_FALSE);

            // Обработка Step (вырезаем код и переносим в конец)
            List<string> stepCommands = new List<string>();

            if (ID())
            {
                string val = TokenToString(_currentToken);
                if (IsWordClose(val, "step")) throw new SyntaxAnalysisException($"Возможно, имелось в виду ключевое слово 'step', но найдено '{val}' на токене №{_currentIndex + 1}.");
                throw new SyntaxAnalysisException($"Ожидается 'step', но найдено '{val}' на токене №{_currentIndex + 1}.");
            }
            else if (EQ(1, 13)) // 'step' (1,13)
            {
                gl();
                int startStepPos = _polishNotation.Count;
                Expression(); // Генерируем код шага в основной список
                _semanticAnalyzer.CheckAndConsumeType("int", _currentIndex);
                int endStepPos = _polishNotation.Count;

                // Переносим во временный список
                for (int i = startStepPos; i < endStepPos; i++) stepCommands.Add(_polishNotation[i]);
                _polishNotation.RemoveRange(startStepPos, endStepPos - startStepPos);
            }
            else
            {
                stepCommands.Add("1"); // Шаг по умолчанию
            }

            Statement(); // Тело цикла

            // Инкремент в конце (i := i + step)
            GenId(counterName, true); // @i
            GenId(counterName);       // i
            _polishNotation.AddRange(stepCommands);
            GenOp("+");
            GenOp(":=");

            // Переход в начало
            GenNum(startConditionAddress.ToString());
            GenOp(OP_JUMP_ALWAYS);

            // Патчим выход
            Patch();

            if (EQ(1, 14)) gl(); else ERRWithExpectedKeywords(new List<string> { _serviceWords[13] });
        }

        // WhileStatement → while ( Expression ) Statement
        private void WhileStatement()
        {
            if (EQ(1, 10)) gl(); else ERRWithExpectedKeywords(new List<string> { _serviceWords[9] });   // 'while' (1,10)
            if (EQ(2, 18)) gl(); else ERR("'(' (2,18)");

            int start = _polishNotation.Count;
            Expression(); // Условие
            _semanticAnalyzer.CheckCondition(_currentIndex);

            PushPatchPoint();
            GenOp(OP_JUMP_FALSE);

            if (EQ(2, 19)) gl(); else ERR("')' (2,19)");

            Statement(); // Тело цикла

            GenNum(start.ToString());
            GenOp(OP_JUMP_ALWAYS);

            Patch();
        }

        // InputStatement → readln IdentifierList
        private void InputStatement()
        {
            if (EQ(1, 17)) gl(); else ERRWithExpectedKeywords(new List<string> { _serviceWords[16] }); // 'readln' (1,17)

            // Обработка списка идентификаторов для ввода
            if (ID())
            {
                _semanticAnalyzer.ProcessReadStatement(_currentToken, _currentIndex);
                GenId(TokenToString(_currentToken), true); GenOp(OP_READ); gl();
            }
            else ERR("идентификатор (TI, x)"); // Ожидается идентификатор

            // Цикл для последующих идентификаторов
            while (true)
            {
                if (EQ(2, 2)) // ','
                {
                    gl();
                    if (ID())
                    {
                        _semanticAnalyzer.ProcessReadStatement(_currentToken, _currentIndex);
                        GenId(TokenToString(_currentToken), true); GenOp(OP_READ); gl();
                    }
                    else ERR("идентификатор (TI, x)"); // После запятой ожидается идентификатор
                }
                else if (ID()) ERR("',' (2,2)"); // Ожидается запятая перед следующим идентификатором
                else break;
            }
        }

        // OutputStatement → writeln ExpressionList
        private void OutputStatement()
        {
            if (EQ(1, 18)) gl(); else ERRWithExpectedKeywords(new List<string> { _serviceWords[17] }); // 'writeln' (1,18)

            // Обработка списка выражений для вывода
            Expression();
            _semanticAnalyzer.ProcessWriteStatement(_currentIndex);
            GenOp(OP_WRITE);

            // Цикл для последующих выражений
            while (true)
            {
                if (EQ(2, 2)) // ','
                {
                    gl();
                    // Проверка наличия следующего выражения
                    if (ID() || NUM() || BOOL_CONST() || EQ(2, 5) || EQ(2, 18))
                    {
                        Expression();
                        _semanticAnalyzer.ProcessWriteStatement(_currentIndex);
                        GenOp(OP_WRITE);
                    }
                    else ERR("выражение");
                }
                else if (ID() || NUM() || BOOL_CONST() || EQ(2, 5) || EQ(2, 18))
                {
                    ERR("',' (2,2)"); // Ожидается запятая перед следующим выражением
                }
                else break;
            }
        }

        // IfStatement → if ( Expression ) Statement else Statement | if ( Expression ) Statement
        private void IfStatement()
        {
            if (EQ(1, 8)) gl(); else ERRWithExpectedKeywords(new List<string> { _serviceWords[7] }); // 'if' (1,8)
            if (EQ(2, 18)) gl(); else ERR("'(' (2,18)");

            Expression(); // Условие
            _semanticAnalyzer.CheckCondition(_currentIndex);

            PushPatchPoint(); // [Patch1]
            GenOp(OP_JUMP_FALSE);

            if (EQ(2, 19)) gl(); else ERR("')' (2,19)");

            Statement();

            if (EQ(1, 9)) // 'else'
            {
                gl();
                int patch1 = _patchStack.Pop();
                PushPatchPoint(); // [Patch2]
                GenOp(OP_JUMP_ALWAYS);

                _polishNotation[patch1] = _polishNotation.Count.ToString();

                Statement(); // Оператор после условия
                Patch(); // Patch2
            }
            else
            {
                Patch(); // Patch1
            }
        }

        // CompoundStatement → begin StatementList end
        private void CompoundStatement()
        {
            if (EQ(1, 3)) gl(); else ERRWithExpectedKeywords(new List<string> { _serviceWords[2] }); // 'begin' (1,3)
            StatementList();
            if (EQ(1, 4)) gl(); else ERRWithExpectedKeywords(new List<string> { _serviceWords[3] }); // 'end' (1,4)
        }

        // Expression → Operand | Expression RelationalOperator Operand
        private void Expression()
        {
            Operand();

            // Повторяем, пока после Operand идет RelationalOperator
            while (EQ(2, 6) || EQ(2, 7) || EQ(2, 8) || EQ(2, 9) || EQ(2, 10) || EQ(2, 11)) // !=, ==, <, >, <=, >=
            {
                Token op = _currentToken; int p = _currentIndex; gl();
                Operand(); // Сначала разбираем второй операнд
                // Теперь вызываем проверку (в стеке 2 типа)
                _semanticAnalyzer.ProcessBinaryOperation(op, p);
                GenOp(TokenToString(op));
            }
        }

        // Operand → Term | Operand AdditiveOperator Term
        private void Operand()
        {
            Term();

            // Повторяем, пока после Term идет AdditiveOperator
            while (EQ(2, 12) || EQ(2, 13) || EQ(2, 14)) // +, -, ||
            {
                Token op = _currentToken; int p = _currentIndex; gl();
                Term(); // Сначала разбираем второй операнд
                // Теперь вызываем проверку (в стеке 2 типа)
                _semanticAnalyzer.ProcessBinaryOperation(op, p);
                GenOp(TokenToString(op));
            }
        }

        // Term → Factor | Term MultiplicativeOperator Factor
        private void Term()
        {
            Factor();

            // Повторяем, пока после Factor идет MultiplicativeOperator
            while (EQ(2, 15) || EQ(2, 17) || EQ(2, 16)) // *, /, &&
            {
                Token op = _currentToken; int p = _currentIndex; gl();
                Factor(); // Сначала разбираем второй операнд
                // Теперь вызываем проверку (в стеке 2 типа)
                _semanticAnalyzer.ProcessBinaryOperation(op, p);
                GenOp(TokenToString(op));
            }
        }

        // Factor → Identifier | Number | BooleanConstant | ! Factor | ( Expression )
        private void Factor()
        {
            if (ID())
            {
                string type;
                _semanticAnalyzer.ProcessIdentifier(_currentToken, out type, _currentIndex);
                GenId(TokenToString(_currentToken));
                gl();
            }
            else if (NUM())
            {
                _semanticAnalyzer.ProcessConstant(_currentToken, _currentIndex);
                GenNum(TokenToString(_currentToken));
                gl();
            }
            else if (BOOL_CONST())
            {
                _semanticAnalyzer.ProcessConstant(_currentToken, _currentIndex);
                GenId(TokenToString(_currentToken));
                gl();
            }
            else if (EQ(2, 5)) // '!' (2,5)
            {
                Token op = _currentToken; int p = _currentIndex; gl();
                Factor();
                _semanticAnalyzer.ProcessUnaryOperation(op, p);
                GenOp("!");
            }
            else if (EQ(2, 18)) // '(' (2,18)
            {
                gl();
                Expression(); // Рекурсивный вызов для выражения в скобках
                if (EQ(2, 19)) gl(); else ERR("')' (2,19)");
            }
            else
            {
                ERR("идентификатор, число, булевая константа, '!' или '('");
            }
        }
    }
}