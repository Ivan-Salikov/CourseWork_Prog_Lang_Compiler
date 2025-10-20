namespace CourseWork_Prog_Lang_Compiler.Models
{
    /// <summary>
    /// Представляет лексему (токен), распознанную анализатором.
    /// </summary>
    /// <param name="TableCode">Код таблицы (1-SW, 2-DL, 3-NUM, 4-ID).</param>
    /// <param name="EntryIndex">Индекс лексемы в соответствующей таблице.</param>
    public record Token(int TableCode, int EntryIndex);
}