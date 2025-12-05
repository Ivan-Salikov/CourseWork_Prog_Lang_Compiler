using System;
using System.Collections.Generic;
using System.Linq;

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
    /// Представляет комплексный результат работы синтаксического анализатора.
    /// Содержит информацию об успехе анализа и список синтаксических ошибок.
    /// </summary>
    public class SyntaxAnalysisResult
    {
        /// <summary>
        /// Получает или задает значение, указывающее, успешно ли завершился синтаксический анализ.
        /// `true` - если анализ прошел без синтаксических ошибок, иначе `false`.
        /// </summary>
        public bool IsSuccess { get; set; }

        /// <summary>
        /// Получает или задает список синтаксических ошибок, обнаруженных в ходе анализа.
        /// Заполняется в случае наличия ошибок.
        /// </summary>
        public List<SyntaxError> Errors { get; set; } = new();
    }

    /// <summary>
    /// Синтаксический анализатор, реализующий метод рекурсивного спуска.
    /// Принимает на вход последовательность токенов, сгенерированных лексическим анализатором,
    /// и проверяет синтаксическую корректность программы в соответствии с грамматикой языка М.
    /// Останавливается после обнаружения первой ошибки.
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
                return new SyntaxAnalysisResult { IsSuccess = false, Errors = errors };
            }

            // Если исключение не было выброшено, анализ успешен
            return new SyntaxAnalysisResult { IsSuccess = true, Errors = errors };
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
                // или обрабатываем ситуацию конца в EQ.
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
            // true (1,15), false (1,16)
            return _currentToken.TableCode == 1 && (_currentToken.EntryIndex == 15 || _currentToken.EntryIndex == 16);
        }

        // Метод для проверки "близости" строк (проверяет расстояние Левенштейна <= 1)
        private bool IsWordClose(string word1, string word2)
        {
            if (string.IsNullOrEmpty(word1) || string.IsNullOrEmpty(word2))
                return false;

            int len1 = word1.Length;
            int len2 = word2.Length;
            int maxLen = Math.Max(len1, len2);

            if (maxLen == 0) return true; // Обе пустые
            if (maxLen == 1) return word1 == word2; // Обе длины 1

            if (Math.Abs(len1 - len2) > 1) return false;

            int i = 0, j = 0;
            int differences = 0;

            while (i < len1 && j < len2)
            {
                if (word1[i] != word2[j])
                {
                    differences++;
                    if (differences > 1) return false;

                    // Если длины разные, возможно, это вставка/удаление
                    if (len1 > len2)
                    {
                        i++; // Пропускаем символ в word1 (удаление из word2)
                    }
                    else if (len1 < len2)
                    {
                        j++; // Пропускаем символ в word2 (вставка в word2)
                    }
                    else // len1 == len2, значит замена
                    {
                        i++;
                        j++;
                    }
                }
                else
                {
                    i++;
                    j++;
                }
            }

            // Если остались необработанные символы, это ещё различие
            differences += (len1 - i) + (len2 - j);

            return differences == 1;
        }


        // Метод для поиска близкого ключевого слова в списке ожидаемых
        private string FindClosestExpectedKeyword(List<string> expectedKeywords, string foundWord)
        {
            foreach (var expected in expectedKeywords)
            {
                if (IsWordClose(expected, foundWord))
                {
                    return expected;
                }
            }
            return null; // Не найдено близкое
        }

        // Ошибка для случаев, когда ожидается ключевое слово
        private void ERRWithExpectedKeywords(List<string> expectedKeywords)
        {
            string foundTokenStr = TokenToString(_currentToken);
            string foundType = _currentToken.TableCode == 1 ? "служебное слово" : "лексема";

            // Проверка является ли найденный токен ключевым словом
            bool isFoundTokenAKeyword = _currentToken.TableCode == 1 && _currentToken.EntryIndex > 0 && _currentToken.EntryIndex <= _serviceWords.Count;
            string foundWordValue = isFoundTokenAKeyword ? _serviceWords[_currentToken.EntryIndex - 1] : null;

            string message;
            if (isFoundTokenAKeyword)
            {
                // Найденный токен - ключевое слово
                string closestExpected = FindClosestExpectedKeyword(expectedKeywords, foundWordValue);
                if (closestExpected != null)
                {
                    // Найдено близкое по написанию ключевое слово
                    message = $"Возможно, имелось в виду ключевое слово '{closestExpected}', но найдено '{foundWordValue}' на токене №{_currentIndex + 1}.";
                }
                else
                {
                    // Ключевое слово не близко к ожидаемому
                    message = $"Ожидается одно из ключевых слов ({string.Join(", ", expectedKeywords)}), но обнаружено ключевое слово '{foundWordValue}' на токене №{_currentIndex + 1}.";
                }
            }
            else
            {
                // Найденный токен - не ключевое слово
                message = $"Ожидается одно из ключевых слов ({string.Join(", ", expectedKeywords)}), но обнаружено {foundType} '{foundTokenStr}' на токене №{_currentIndex + 1}.";
            }

            throw new SyntaxAnalysisException(message);
        }

        // Стандартная ошибка
        private void ERR(string expected = "")
        {
            string found = $"'{TokenToString(_currentToken)}'";
            string message;
            if (!string.IsNullOrEmpty(expected))
            {
                message = $"Ожидается {expected}, но обнаружен {found} на токене №{_currentIndex + 1}.";
            }
            else
            {
                message = $"Синтаксическая ошибка на токене №{_currentIndex + 1}: неожиданный токен {found}.";
            }
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
            if (EQ(1, 1)) // 'program' (1,1)
            {
                gl();
            }
            else
            {
                ERRWithExpectedKeywords(new List<string> { _serviceWords[0] });
            }
            if (EQ(1, 2)) // 'var' (1,2)
            {
                gl();
            }
            else
            {
                ERRWithExpectedKeywords(new List<string> { _serviceWords[1] });
            }

            DeclarationSection();

            if (EQ(1, 3)) // 'begin' (1,3)
            {
                gl();
            }
            else
            {
                ERRWithExpectedKeywords(new List<string> { _serviceWords[2] });
            }

            StatementList();

            if (EQ(1, 4)) // 'end' (1,4)
            {
                gl();
            }
            else
            {
                ERRWithExpectedKeywords(new List<string> { _serviceWords[3] });
            }

            if (EQ(2, 22)) // '.' (2,22)
            {
                gl();
            }
            else
            {
                ERR("'.' (2,22)");
            }
        }

        // DeclarationSection → Declaration | DeclarationSection Declaration
        // Эквивалентная форма: Declaration {Declaration}
        private void DeclarationSection()
        {
            // Проверяем, начинается ли первое Declaration с Type.
            if (!EQ(1, 5) && !EQ(1, 6) && !EQ(1, 7)) // Не 'int', 'float', 'bool'
            {
                if (EQ(1, 3)) // 'begin'
                {
                    ERRWithExpectedKeywords(new List<string> { _serviceWords[4], _serviceWords[5], _serviceWords[6] });
                    return;
                }
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
            Type();

            // Обработка первого идентификатора
            if (ID())
            {
                gl();
            }
            else
            {
                ERR("идентификатор (TI, x)"); // Ожидается идентификатор
            }

            // Цикл для последующих идентификаторов
            while (true)
            {
                if (EQ(2, 2)) // ','
                {
                    gl();
                    if (ID())
                    {
                        gl();
                    }
                    else
                    {
                        ERR("идентификатор (TI, x)");
                    }
                }
                else if (ID())
                {
                    ERR("',' (2,2)"); // Ожидается запятая перед следующим идентификатором
                }
                else
                {
                    break;
                }
            }

            if (EQ(2, 1)) // ';'
            {
                gl();
            }
            else
            {
                ERR("';' (2,1)"); // Ожидается точка с запятой после списка идентификаторов
            }
        }

        // Type → int | float | bool
        private void Type()
        {
            if (EQ(1, 5) || EQ(1, 6) || EQ(1, 7)) // 'int' (1,5), 'float' (1,6), 'bool' (1,7)
            {
                gl();
            }
            else
            {
                ERRWithExpectedKeywords(new List<string> { _serviceWords[4], _serviceWords[5], _serviceWords[6] });
            }
        }

        // StatementList → Statement {; Statement}
        private void StatementList()
        {
            if (!EQ(1, 3) && !ID() && !EQ(1, 8) && !EQ(1, 11) && !EQ(1, 10) && !EQ(1, 17) && !EQ(1, 18)) // Не 'begin', ID, 'if', 'for', 'while', 'readln', 'writeln'
            {
                if (EQ(1, 4)) // 'end'
                {
                    return;
                }
                else
                {
                    // Ошибка: неожиданный токен в начале StatementList
                    ERRWithExpectedKeywords(new List<string> { _serviceWords[2], _serviceWords[7], _serviceWords[10], _serviceWords[9], _serviceWords[16], _serviceWords[17], _serviceWords[3] });
                    return;
                }
            }

            Statement();

            // Цикл для последующих операторов
            while (true)
            {
                if (EQ(2, 1)) // ';'
                {
                    gl();

                    if (EQ(1, 3) || ID() || EQ(1, 8) || EQ(1, 11) || EQ(1, 10) || EQ(1, 17) || EQ(1, 18)) // 'begin', ID, 'if', 'for', 'while', 'readln', 'writeln'
                    {
                        Statement();
                    }
                    else
                    {
                        if (EQ(1, 4)) // 'end' (1,4)
                        {
                            break;
                        }
                        else
                        {
                            if (ID())
                            {
                                string currentIdentifierValue = TokenToString(_currentToken);
                                string expectedKeyword = _serviceWords[3];
                                if (IsWordClose(currentIdentifierValue, expectedKeyword))
                                {
                                    throw new SyntaxAnalysisException($"Возможно, имелось в виду ключевое слово '{expectedKeyword}', но найдено идентификатор '{currentIdentifierValue}' на токене №{_currentIndex + 1}.");
                                }
                            }
                            ERRWithExpectedKeywords(new List<string> { _serviceWords[2], _serviceWords[7], _serviceWords[10], _serviceWords[11], _serviceWords[16], _serviceWords[17], _serviceWords[3] }); // begin, if, for, while, readln, writeln, end
                        }
                    }
                }
                else // Не ';'
                {
                    if (EQ(1, 4)) // 'end'
                    {
                        break;
                    }
                    else
                    {
                        if (ID())
                        {
                            string currentIdentifierValue = TokenToString(_currentToken);
                            string expectedKeyword = _serviceWords[3];
                            if (IsWordClose(currentIdentifierValue, expectedKeyword))
                            {
                                throw new SyntaxAnalysisException($"Возможно, имелось в виду ключевое слово '{expectedKeyword}', но найдено идентификатор '{currentIdentifierValue}' на токене №{_currentIndex + 1}.");
                            }
                        }
                        ERRWithExpectedKeywords(new List<string> { _serviceWords[4] });
                    }
                }
            }
        }

        // Statement → CompoundStatement | AssignmentStatement | IfStatement | ForStatement | WhileStatement | InputStatement | OutputStatement
        private void Statement()
        {
            if (EQ(1, 3)) // 'begin' (начало составного оператора)
            {
                CompoundStatement();
            }
            else if (EQ(1, 8)) // 'if' (условный оператор)
            {
                IfStatement();
            }
            else if (EQ(1, 11)) // 'for' (цикл for)
            {
                ForStatement();
            }
            else if (EQ(1, 10)) // 'while' (цикл while)
            {
                WhileStatement();
            }
            else if (EQ(1, 17)) // 'readln' (ввод)
            {
                InputStatement();
            }
            else if (EQ(1, 18)) // 'writeln' (вывод)
            {
                OutputStatement();
            }
            else if (ID()) // Начинается с идентификатора (присваивание)
            {
                // Проверка, не является ли идентификатор опечаткой одного из ключевых слов оператора
                string currentIdentifierValue = TokenToString(_currentToken);

                // Список ключевых слов, которые могут начинать Statement
                var expectedKeywordsForStatement = new List<string>
                {
                    _serviceWords[2], // "begin"
                    _serviceWords[7], // "if"
                    _serviceWords[10], // "for"
                    _serviceWords[9], // "while"
                    _serviceWords[16], // "readln"
                    _serviceWords[17], // "writeln"
                    _serviceWords[3]   // "end"
                };

                string closestExpected = FindClosestExpectedKeyword(expectedKeywordsForStatement, currentIdentifierValue);
                if (closestExpected != null)
                {
                    // Это опечатка одного из ключевых слов
                    throw new SyntaxAnalysisException($"Возможно, имелось в виду ключевое слово '{closestExpected}', но найден идентификатор '{currentIdentifierValue}' на токене №{_currentIndex + 1}.");
                }

                // Если идентификатор не похож на ключевое слово, продолжаем как присваивание
                AssignmentStatement();
            }
            else
            {
                // Ни одно из ключевых слов не подошло, и это не ID.
                ERRWithExpectedKeywords(new List<string> { _serviceWords[2], _serviceWords[7], _serviceWords[10], _serviceWords[9], _serviceWords[16], _serviceWords[17] });
            }
        }

        // AssignmentStatement → Identifier := Expression
        private void AssignmentStatement()
        {
            if (ID())
            {
                gl();
            }
            else
            {
                ERR("идентификатор (TI, x)"); // Ожидается идентификатор слева от :=
            }

            if (EQ(2, 4)) // ':=' (2,4)
            {
                gl();
            }
            else
            {
                ERR("':=' (2,4)"); // Ожидается присваивание ':='
            }

            Expression();
        }

        // ForStatement → for AssignmentStatement to Expression step Expression Statement next | for AssignmentStatement to Expression Statement next
        private void ForStatement()
        {
            if (EQ(1, 11)) // 'for' (1,11)
            {
                gl();
            }
            else
            {
                ERRWithExpectedKeywords(new List<string> { _serviceWords[10] });
            }

            AssignmentStatement(); // Инициализация цикла

            if (EQ(1, 12)) // 'to' (1,12)
            {
                gl();
            }
            else
            {
                ERRWithExpectedKeywords(new List<string> { _serviceWords[11] });
            }

            Expression(); // Условие окончания

            // Проверка, не является ли следующий токен опечаткой 'step'
            if (ID())
            {
                string currentIdentifierValue = TokenToString(_currentToken);
                string expectedKeyword = _serviceWords[12];
                if (IsWordClose(currentIdentifierValue, expectedKeyword))
                {
                    throw new SyntaxAnalysisException($"Возможно, имелось в виду ключевое слово '{expectedKeyword}', но найдено идентификатор '{currentIdentifierValue}' на токене №{_currentIndex + 1}.");
                }
                Expression();
            }
            else if (EQ(1, 13)) // 'step' (1,13)
            {
                gl();
                Expression(); // Шаг
            }

            Statement(); // Тело цикла

            if (EQ(1, 14)) // 'next' (1,14)
            {
                gl();
            }
            else
            {
                ERRWithExpectedKeywords(new List<string> { _serviceWords[13] });
            }
        }

        // WhileStatement → while ( Expression ) Statement
        private void WhileStatement()
        {
            if (EQ(1, 10)) // 'while' (1,10)
            {
                gl();
            }
            else
            {
                ERRWithExpectedKeywords(new List<string> { _serviceWords[9] });
            }

            if (EQ(2, 18)) // '(' (2,18)
            {
                gl();
            }
            else
            {
                ERR("'(' (2,18)");
            }

            Expression(); // Условие

            if (EQ(2, 19)) // ')' (2,19)
            {
                gl();
            }
            else
            {
                ERR("')' (2,19)");
            }

            Statement(); // Тело цикла
        }

        // InputStatement → readln IdentifierList
        private void InputStatement()
        {
            if (EQ(1, 17)) // 'readln' (1,17)
            {
                gl();
            }
            else
            {
                ERRWithExpectedKeywords(new List<string> { _serviceWords[16] });
            }

            // Обработка списка идентификаторов для ввода
            if (ID())
            {
                gl();
            }
            else
            {
                ERR("идентификатор (TI, x)"); // Ожидается идентификатор
            }

            // Цикл для последующих идентификаторов
            while (true)
            {
                if (EQ(2, 2)) // ','
                {
                    gl();
                    if (ID())
                    {
                        gl();
                    }
                    else
                    {
                        ERR("идентификатор (TI, x)"); // После запятой ожидается идентификатор
                    }
                }
                else if (ID())
                {
                    ERR("',' (2,2)"); // Ожидается запятая перед следующим идентификатором
                }
                else
                {
                    break;
                }
            }
        }

        // OutputStatement → writeln ExpressionList
        private void OutputStatement()
        {
            if (EQ(1, 18)) // 'writeln' (1,18)
            {
                gl();
            }
            else
            {
                ERRWithExpectedKeywords(new List<string> { _serviceWords[17] });
            }

            // Обработка списка выражений для вывода
            Expression();

            // Цикл для последующих выражений
            while (true)
            {
                if (EQ(2, 2)) // ','
                {
                    gl();
                    Expression();
                }
                else if ( // Проверка, является ли следующий токен началом нового выражения без запятой
                    ID() || NUM() || BOOL_CONST() || EQ(2, 5) || EQ(2, 18)
                )
                {
                    ERR("',' (2,2)"); // Ожидается запятая перед следующим выражением
                    Expression();
                }
                else
                {
                    break;
                }
            }
        }

        // IfStatement → if ( Expression ) Statement else Statement | if ( Expression ) Statement
        private void IfStatement()
        {
            if (EQ(1, 8)) // 'if' (1,8)
            {
                gl();
            }
            else
            {
                ERRWithExpectedKeywords(new List<string> { _serviceWords[7] }); // "if"
            }

            if (EQ(2, 18)) // '(' (2,18)
            {
                gl();
            }
            else
            {
                ERR("'(' (2,18)");
            }

            Expression(); // Условие

            if (EQ(2, 19)) // ')' (2,19)
            {
                gl();
            }
            else
            {
                ERR("')' (2,19)");
            }

            Statement(); // Оператор после условия

            if (EQ(1, 9)) // 'else' (1,9)
            {
                gl();
                Statement();
            }
        }

        // CompoundStatement → begin StatementList end
        private void CompoundStatement()
        {
            if (EQ(1, 3)) // 'begin' (1,3)
            {
                gl();
            }
            else
            {
                ERRWithExpectedKeywords(new List<string> { _serviceWords[2] }); // "begin"
            }

            StatementList();

            if (EQ(1, 4)) // 'end' (1,4)
            {
                gl();
            }
            else
            {
                ERRWithExpectedKeywords(new List<string> { _serviceWords[3] }); // "end"
            }
        }

        // Expression → Operand | Expression RelationalOperator Operand
        private void Expression()
        {
            Operand();

            // Повторяем, пока после Operand идет RelationalOperator
            while (EQ(2, 6) || EQ(2, 7) || EQ(2, 8) || EQ(2, 9) || EQ(2, 10) || EQ(2, 11)) // !=, ==, <, >, <=, >=
            {
                gl();
                Operand();
            }
        }

        // Operand → Term | Operand AdditiveOperator Term
        private void Operand()
        {
            Term();

            // Повторяем, пока после Term идет AdditiveOperator
            while (EQ(2, 12) || EQ(2, 13) || EQ(2, 14)) // +, -, ||
            {
                gl();
                Term();
            }
        }

        // Term → Factor | Term MultiplicativeOperator Factor
        private void Term()
        {
            Factor();

            // Повторяем, пока после Factor идет MultiplicativeOperator
            while (EQ(2, 15) || EQ(2, 17) || EQ(2, 16)) // *, /, &&
            {
                gl();
                Factor();
            }
        }

        // Factor → Identifier | Number | BooleanConstant | ! Factor | ( Expression )
        private void Factor()
        {
            if (ID() || NUM() || BOOL_CONST())
            {
                gl();
            }
            else if (EQ(2, 5)) // '!' (2,5)
            {
                gl();
                Factor();
            }
            else if (EQ(2, 18)) // '(' (2,18)
            {
                gl();
                Expression(); // Рекурсивный вызов для выражения в скобках
                if (EQ(2, 19)) // ')' (2,19)
                {
                    gl();
                }
                else
                {
                    ERR("')' (2,19)"); // Ожидается закрывающая скобка
                }
            }
            else
            {
                ERR("идентификатор (TI, x), число (TN, x), булевая константа (true/false), '!' или '('");
            }
        }
    }
}