using System.Collections.Generic;

namespace CourseWork_Prog_Lang_Compiler.Models
{
    /// <summary>
    /// Представляет комплексный результат работы лексического анализатора.
    /// Содержит информацию об успехе анализа, список токенов, таблицы
    /// и сообщение об ошибке в случае неудачи.
    /// </summary>
    public class AnalysisResult
    {
        /// <summary>
        /// Получает или задает значение, указывающее, успешно ли завершился лексический анализ.
        /// `true` - если анализ прошел без ошибок, иначе `false`.
        /// </summary>
        public bool IsSuccess { get; set; }

        /// <summary>
        /// Получает или задает сообщение об ошибке, возникшей в ходе анализа.
        /// Заполняется только в том случае, если IsSuccess имеет значение `false`.
        /// </summary>
        public string ErrorMessage { get; set; } = string.Empty;

        /// <summary>
        /// Получает или задает список токенов (лексем), распознанных в исходном коде.
        /// Заполняется в случае успешного анализа.
        /// </summary>
        public List<Token> Tokens { get; set; } = new();

        /// <summary>
        /// Получает или задает финальную версию таблицы идентификаторов,
        /// сформированную в ходе анализа.
        /// </summary>
        public List<TableEntry> IdentifiersTable { get; set; } = new();

        /// <summary>
        /// Получает или задает финальную версию таблицы чисел (констант),
        /// сформированную в ходе анализа.
        /// </summary>
        public List<TableEntry> NumbersTable { get; set; } = new();
    }
}