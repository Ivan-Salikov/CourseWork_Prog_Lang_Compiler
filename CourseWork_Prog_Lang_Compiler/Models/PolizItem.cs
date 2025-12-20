namespace CourseWork_Prog_Lang_Compiler.Models
{
    /// <summary>
    /// Представляет собой одну команду ПОЛИЗа для отображения в пользовательском интерфейсе.
    /// Используется для вывода списка команд с их индексами.
    /// </summary>
    public class PolizItem
    {
        /// <summary>
        /// Индекс команды в массиве ПОЛИЗа.
        /// </summary>
        public int Index { get; set; }

        /// <summary>
        /// Текстовое представление команды (операция, операнд или адрес).
        /// </summary>
        public string Command { get; set; }
    }
}