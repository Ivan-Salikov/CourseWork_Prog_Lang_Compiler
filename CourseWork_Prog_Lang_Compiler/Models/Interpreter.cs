using System.Globalization;

namespace CourseWork_Prog_Lang_Compiler.Models
{
    /// <summary>
    /// Интерпретатор стекового типа.
    /// Выполняет программу, представленную в виде списка команд ПОЛИЗ (Польская Инверсная Запись).
    /// </summary>
    public class Interpreter
    {
        private List<string> _program;              // Код программы (ПОЛИЗ)
        private Dictionary<string, object> _memory; // Память (имя переменной -> значение)
        private Stack<object> _stack;               // Стек вычислений
        private int _instructionPointer;            // Указатель текущей инструкции (IP)

        public Interpreter()
        {
            _program = new List<string>();
            _memory = new Dictionary<string, object>();
            _stack = new Stack<object>();
        }

        /// <summary>
        /// Загружает программу и инициализирует память переменных.
        /// </summary>
        /// <param name="poliz">Список команд ПОЛИЗ.</param>
        /// <param name="identifiers">Таблица идентификаторов из лексического анализа.</param>
        public void LoadPoliz(List<string> poliz, List<TableEntry> identifiers)
        {
            _program = poliz ?? new List<string>();
            _memory.Clear();
            _stack.Clear();
            _instructionPointer = 0;

            if (identifiers != null)
            {
                foreach (var entry in identifiers)
                {
                    // Инициализируем объявленные переменные нулем (long)
                    _memory[entry.Value] = 0L;
                }
            }
        }

        /// <summary>
        /// Запускает выполнение программы.
        /// </summary>
        /// <param name="output">Метод обратного вызова для вывода данных (Writeln).</param>
        /// <param name="input">Метод обратного вызова для ввода данных (Readln).</param>
        public void Run(Action<string> output, Func<string> input)
        {
            _instructionPointer = 0;
            _stack.Clear();
            int safeCounter = 1000000; // Лимит операций для защиты от зависания

            while (_instructionPointer < _program.Count)
            {
                if (safeCounter-- <= 0) throw new Exception("Превышен лимит инструкций (возможно, бесконечный цикл).");
                string token = _program[_instructionPointer++];

                // Адрес переменной (@name)
                // Если токен начинается с @ (и это не спец. команды), значит это адрес для записи
                if (token.StartsWith("@") && !token.StartsWith("@!") && token != "@F")
                {
                    _stack.Push(token.Substring(1)); // Кладем имя переменной в стек
                    continue;
                }

                // Числовые константы
                if (double.TryParse(token, NumberStyles.Any, CultureInfo.InvariantCulture, out double num))
                {
                    // Сохраняем целые числа как long для красивого вывода
                    if (num % 1 == 0) _stack.Push((long)num);
                    else _stack.Push(num);
                    continue;
                }

                // Булевы константы
                if (token == "true") { _stack.Push(true); continue; }
                if (token == "false") { _stack.Push(false); continue; }

                // Обработка команд
                switch (token)
                {
                    // Присваивание
                    case ":=":
                        if (_stack.Count < 2) throw new Exception("Стек пуст для операции присваивания.");
                        var val = _stack.Pop();
                        var addr = _stack.Pop().ToString();
                        _memory[addr] = val; // Запись в память
                        break;

                    // Арифметика
                    case "+": BinOp((a, b) => a + b); break;
                    case "-": BinOp((a, b) => a - b); break;
                    case "*": BinOp((a, b) => a * b); break;
                    case "/": BinOp((a, b) => a / b); break;

                    // Сравнение
                    case "==": CmpOp((a, b) => a == b); break;
                    case "!=": CmpOp((a, b) => a != b); break;
                    case "<": CmpOp((a, b) => a < b); break;
                    case ">": CmpOp((a, b) => a > b); break;
                    case "<=": CmpOp((a, b) => a <= b); break;
                    case ">=": CmpOp((a, b) => a >= b); break;

                    // Логика
                    case "&&":
                        var bBool = (bool)_stack.Pop();
                        var aBool = (bool)_stack.Pop();
                        _stack.Push(aBool && bBool);
                        break;
                    case "||":
                        bBool = (bool)_stack.Pop();
                        aBool = (bool)_stack.Pop();
                        _stack.Push(aBool || bBool);
                        break;
                    case "!":
                        _stack.Push(!(bool)_stack.Pop());
                        break;

                    // Ввод / Вывод
                    case "~RL": // ReadLine
                        addr = _stack.Pop().ToString();
                        string inp = input(); // Вызов колбека UI

                        // Попытка распознать тип введенных данных
                        if (long.TryParse(inp, out long lRes)) _memory[addr] = lRes;
                        else if (double.TryParse(inp, NumberStyles.Any, CultureInfo.InvariantCulture, out double dRes)) _memory[addr] = dRes;
                        else if (bool.TryParse(inp, out bool bRes)) _memory[addr] = bRes;
                        else _memory[addr] = 0L; // Ошибка ввода -> 0
                        break;

                    case "~WL": // WriteLine
                        var outVal = _stack.Pop();
                        output(outVal.ToString()); // Вызов колбека UI
                        break;

                    // Переходы
                    case "@F": // Jump False (переход по лжи)
                        int target = int.Parse(_stack.Pop().ToString());
                        bool cond = (bool)_stack.Pop();
                        if (!cond) _instructionPointer = target; // target - индекс в массиве ПОЛИЗ
                        break;

                    case "@!": // Jump Always (безусловный переход)
                        target = int.Parse(_stack.Pop().ToString());
                        _instructionPointer = target;
                        break;

                    case ".": return; // Конец программы

                    default:
                        // Если это не команда и не число, значит это имя переменной (чтение значения)
                        if (_memory.ContainsKey(token)) _stack.Push(_memory[token]);
                        else throw new Exception($"Неизвестный токен или необъявленная переменная: {token}");
                        break;
                }
            }
        }

        // Вспомогательный метод для бинарных арифметических операций
        private void BinOp(Func<double, double, double> op)
        {
            var b = ToDouble(_stack.Pop());
            var a = ToDouble(_stack.Pop());
            double res = op(a, b);

            // Если результат целый, сохраняем как long
            if (res % 1 == 0 && res >= long.MinValue && res <= long.MaxValue) _stack.Push((long)res);
            else _stack.Push(res);
        }

        // Вспомогательный метод для операций сравнения
        private void CmpOp(Func<double, double, bool> op)
        {
            var b = ToDouble(_stack.Pop()); // Правый
            var a = ToDouble(_stack.Pop()); // Левый
            _stack.Push(op(a, b));
        }

        // Приведение объекта к double для вычислений
        private double ToDouble(object o)
        {
            if (o is long l) return (double)l;
            if (o is int i) return (double)i;
            return Convert.ToDouble(o, CultureInfo.InvariantCulture);
        }
    }
}