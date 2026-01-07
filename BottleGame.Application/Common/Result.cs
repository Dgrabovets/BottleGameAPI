namespace BottleGame.Application.Common
{
    /// <summary>
    /// Результат выполнения операции без возвращаемого значения
    /// </summary>
    /// <remarks>
    /// Используется для операций, которые не возвращают данные (например, удаление, обновление).
    /// Позволяет унифицировать обработку успеха/ошибки в контроллерах.
    /// </remarks>
    public class Result
    {
        #region Properties

        /// <summary>
        /// Признак успешного выполнения операции
        /// </summary>
        public bool IsSuccess { get; protected set; }

        /// <summary>
        /// Признак неуспешного выполнения операции
        /// </summary>
        public bool IsFailure => !IsSuccess;

        /// <summary>
        /// Сообщение об ошибке (null при успехе)
        /// </summary>
        public string? Error { get; protected set; }

        /// <summary>
        /// HTTP статус-код для ответа
        /// </summary>
        public int StatusCode { get; protected set; }

        #endregion Properties

        #region Constructors

        /// <summary>
        /// Защищённый конструктор для создания результата
        /// </summary>
        /// <param name="isSuccess">Признак успеха</param>
        /// <param name="error">Сообщение об ошибке</param>
        /// <param name="statusCode">HTTP статус-код</param>
        protected Result(bool isSuccess, string? error, int statusCode)
        {
            IsSuccess = isSuccess;
            Error = error;
            StatusCode = statusCode;
        }

        #endregion Constructors

        #region Factory Methods

        /// <summary>
        /// Создаёт успешный результат
        /// </summary>
        /// <returns>Успешный результат с кодом 200</returns>
        public static Result Success() => new(true, null, 200);

        /// <summary>
        /// Создаёт успешный результат с указанным статус-кодом
        /// </summary>
        /// <param name="statusCode">HTTP статус-код</param>
        /// <returns>Успешный результат</returns>
        public static Result Success(int statusCode) => new(true, null, statusCode);

        /// <summary>
        /// Создаёт результат с ошибкой
        /// </summary>
        /// <param name="error">Сообщение об ошибке</param>
        /// <param name="statusCode">HTTP статус-код (по умолчанию 400)</param>
        /// <returns>Результат с ошибкой</returns>
        public static Result Failure(string error, int statusCode = 400) => new(false, error, statusCode);

        /// <summary>
        /// Создаёт результат "Не найдено" (404)
        /// </summary>
        /// <param name="error">Сообщение об ошибке</param>
        /// <returns>Результат с кодом 404</returns>
        public static Result NotFound(string error) => new(false, error, 404);

        /// <summary>
        /// Создаёт результат "Доступ запрещён" (403)
        /// </summary>
        /// <param name="error">Сообщение об ошибке</param>
        /// <returns>Результат с кодом 403</returns>
        public static Result Forbidden(string error) => new(false, error, 403);

        /// <summary>
        /// Создаёт результат "Не авторизован" (401)
        /// </summary>
        /// <param name="error">Сообщение об ошибке</param>
        /// <returns>Результат с кодом 401</returns>
        public static Result Unauthorized(string error = "Не авторизован") => new(false, error, 401);

        /// <summary>
        /// Создаёт результат "Конфликт" (409)
        /// </summary>
        /// <param name="error">Сообщение об ошибке</param>
        /// <returns>Результат с кодом 409</returns>
        public static Result Conflict(string error) => new(false, error, 409);

        /// <summary>
        /// Создаёт результат "Внутренняя ошибка сервера" (500)
        /// </summary>
        /// <param name="error">Сообщение об ошибке</param>
        /// <returns>Результат с кодом 500</returns>
        public static Result InternalError(string error = "Внутренняя ошибка сервера") => new(false, error, 500);

        #endregion Factory Methods
    }

    /// <summary>
    /// Результат выполнения операции с возвращаемым значением
    /// </summary>
    /// <typeparam name="T">Тип возвращаемого значения</typeparam>
    /// <remarks>
    /// Используется для операций, которые возвращают данные (например, получение, создание).
    /// Позволяет унифицировать обработку успеха/ошибки в контроллерах.
    /// </remarks>
    public class Result<T> : Result
    {
        #region Properties

        /// <summary>
        /// Возвращаемое значение (null при ошибке)
        /// </summary>
        public T? Value { get; private set; }

        #endregion Properties

        #region Constructors

        /// <summary>
        /// Защищённый конструктор для создания результата с значением
        /// </summary>
        /// <param name="isSuccess">Признак успеха</param>
        /// <param name="value">Возвращаемое значение</param>
        /// <param name="error">Сообщение об ошибке</param>
        /// <param name="statusCode">HTTP статус-код</param>
        protected Result(bool isSuccess, T? value, string? error, int statusCode)
            : base(isSuccess, error, statusCode)
        {
            Value = value;
        }

        #endregion Constructors

        #region Factory Methods

        /// <summary>
        /// Создаёт успешный результат с данными
        /// </summary>
        /// <param name="value">Возвращаемое значение</param>
        /// <returns>Успешный результат с кодом 200</returns>
        public static Result<T> Success(T value) => new(true, value, null, 200);

        /// <summary>
        /// Создаёт успешный результат с данными и указанным статус-кодом
        /// </summary>
        /// <param name="value">Возвращаемое значение</param>
        /// <param name="statusCode">HTTP статус-код</param>
        /// <returns>Успешный результат</returns>
        public static Result<T> Success(T value, int statusCode) => new(true, value, null, statusCode);

        /// <summary>
        /// Создаёт результат с ошибкой
        /// </summary>
        /// <param name="error">Сообщение об ошибке</param>
        /// <param name="statusCode">HTTP статус-код (по умолчанию 400)</param>
        /// <returns>Результат с ошибкой</returns>
        public new static Result<T> Failure(string error, int statusCode = 400) => new(false, default, error, statusCode);

        /// <summary>
        /// Создаёт результат "Не найдено" (404)
        /// </summary>
        /// <param name="error">Сообщение об ошибке</param>
        /// <returns>Результат с кодом 404</returns>
        public new static Result<T> NotFound(string error) => new(false, default, error, 404);

        /// <summary>
        /// Создаёт результат "Доступ запрещён" (403)
        /// </summary>
        /// <param name="error">Сообщение об ошибке</param>
        /// <returns>Результат с кодом 403</returns>
        public new static Result<T> Forbidden(string error) => new(false, default, error, 403);

        /// <summary>
        /// Создаёт результат "Не авторизован" (401)
        /// </summary>
        /// <param name="error">Сообщение об ошибке</param>
        /// <returns>Результат с кодом 401</returns>
        public new static Result<T> Unauthorized(string error = "Не авторизован") => new(false, default, error, 401);

        /// <summary>
        /// Создаёт результат "Конфликт" (409)
        /// </summary>
        /// <param name="error">Сообщение об ошибке</param>
        /// <returns>Результат с кодом 409</returns>
        public new static Result<T> Conflict(string error) => new(false, default, error, 409);

        /// <summary>
        /// Создаёт результат "Внутренняя ошибка сервера" (500)
        /// </summary>
        /// <param name="error">Сообщение об ошибке</param>
        /// <returns>Результат с кодом 500</returns>
        public new static Result<T> InternalError(string error = "Внутренняя ошибка сервера") => new(false, default, error, 500);

        #endregion Factory Methods

        #region Implicit Operators

        /// <summary>
        /// Неявное преобразование значения в успешный результат
        /// </summary>
        /// <param name="value">Значение для обёртки</param>
        public static implicit operator Result<T>(T value) => Success(value);

        #endregion Implicit Operators
    }
}


