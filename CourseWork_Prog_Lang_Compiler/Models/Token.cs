namespace CourseWork_Prog_Lang_Compiler.Models
{
    /// <summary>
    /// Представляет собой лексему, сгенерированную лексическим анализатором.
    /// Состоит из кода таблицы (n) и номера лексемы в этой таблице (k).
    /// </summary>
    public class Token
    {
        /// <summary>
        /// Получает или задает код таблицы, к которой принадлежит лексема (1 - TW, 2 - TL, 3 - TN, 4 - TI).
        /// </summary>
        public int TableCode { get; set; }

        /// <summary>
        /// Получает или задает номер лексемы в соответствующей таблице.
        /// </summary>
        public int EntryIndex { get; set; }

        public Token(int tableCode, int entryIndex)
        {
            TableCode = tableCode;
            EntryIndex = entryIndex;
        }

        public override string ToString()
        {
            return $"({TableCode}, {EntryIndex})";
        }
    }
}