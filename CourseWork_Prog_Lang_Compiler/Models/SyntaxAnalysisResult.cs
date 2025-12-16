using System.Collections.Generic;

namespace CourseWork_Prog_Lang_Compiler.Models
{
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

        /// <summary>
        /// Получает или задает список семантических ошибок, обнаруженных в ходе анализа.
        /// Заполняется в случае наличия ошибок.
        /// </summary>
        public List<SemanticError> SemanticErrors { get; set; } = new();
    }
}