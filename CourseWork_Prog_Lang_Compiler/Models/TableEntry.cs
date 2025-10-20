namespace CourseWork_Prog_Lang_Compiler.Models
{
    /// <summary>
    /// Представляет одну запись (строку) в таблице лексем (служебных слов, разделителей, чисел или идентификаторов).
    /// Использование `record` обеспечивает неизменяемость (immutability) после создания.
    /// </summary>
    /// <param name="Id">Уникальный номер (индекс) лексемы в пределах ее таблицы.</param>
    /// <param name="Value">Строковое представление лексемы.</param>
    public record TableEntry(int Id, string Value);
}