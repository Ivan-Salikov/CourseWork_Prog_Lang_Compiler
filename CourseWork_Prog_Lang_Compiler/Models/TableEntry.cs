namespace CourseWork_Prog_Lang_Compiler.Models
{
    /// <summary>
    /// Представляет собой запись в таблице лексем (идентификаторов или чисел).
    /// Содержит номер лексемы (Id) и её строковое значение (Value).
    /// </summary>
    public class TableEntry
    {
        /// <summary>
        /// Получает или задает номер лексемы в таблице.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Получает или задает строковое значение лексемы.
        /// </summary>
        public string Value { get; set; }

        /// <summary>
        /// Получает или задает флаг, указывающий, описана ли переменная (для семантической проверки).
        /// </summary>
        public bool IsDeclared { get; set; } = false;

        /// <summary>
        /// Получает или задает тип переменной (для семантической проверки).
        /// </summary>
        public string Type { get; set; } = "";

        public TableEntry(int id, string value)
        {
            Id = id;
            Value = value;
        }

        public override string ToString()
        {
            return $"({Id}, {Value}, Declared: {IsDeclared}, Type: {Type})";
        }
    }
}