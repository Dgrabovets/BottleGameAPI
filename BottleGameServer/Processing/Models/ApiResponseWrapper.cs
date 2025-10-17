namespace BottleGameServer.Processing.Models
{
    /// <summary>
    /// Универсальная модель ответа от процессинговой площадки
    /// </summary>
    /// <typeparam name="T">Тип данных в поле data</typeparam>
    public class ApiResponseWrapper<T>
    {
        /// <summary>
        /// Статус ответа (success/error)
        /// </summary>
        public string status { get; set; } = string.Empty;

        /// <summary>
        /// Данные ответа
        /// </summary>
        public T data { get; set; } = default!;
    }
}