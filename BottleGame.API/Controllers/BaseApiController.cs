using BottleGame.Application.Common;
using Microsoft.AspNetCore.Mvc;

namespace BottleGame.API.Controllers
{
    /// <summary>
    /// Базовый контроллер API с общими методами
    /// </summary>
    /// <remarks>
    /// Предоставляет методы для преобразования Result в ActionResult
    /// и другие общие методы для всех контроллеров.
    /// </remarks>
    [ApiController]
    public abstract class BaseApiController : ControllerBase
    {
        #region Protected Methods

        /// <summary>
        /// Преобразует Result без значения в ActionResult
        /// </summary>
        /// <param name="result">Результат операции</param>
        /// <returns>ActionResult с соответствующим статус-кодом</returns>
        protected IActionResult ToActionResult(Result result)
        {
            if (result.IsSuccess)
            {
                return result.StatusCode == 204
                    ? NoContent()
                    : Ok();
            }

            return result.StatusCode switch
            {
                401 => Unauthorized(result.Error),
                403 => Forbid(result.Error ?? "Доступ запрещён"),
                404 => NotFound(result.Error),
                409 => Conflict(result.Error),
                500 => StatusCode(500, result.Error),
                _ => BadRequest(result.Error)
            };
        }

        /// <summary>
        /// Преобразует Result с значением в ActionResult
        /// </summary>
        /// <typeparam name="T">Тип возвращаемого значения</typeparam>
        /// <param name="result">Результат операции</param>
        /// <returns>ActionResult с данными или ошибкой</returns>
        protected ActionResult<T> ToActionResult<T>(Result<T> result)
        {
            if (result.IsSuccess)
            {
                return result.StatusCode == 201
                    ? StatusCode(201, result.Value)
                    : Ok(result.Value);
            }

            return result.StatusCode switch
            {
                401 => Unauthorized(result.Error),
                403 => StatusCode(403, result.Error),
                404 => NotFound(result.Error),
                409 => Conflict(result.Error),
                500 => StatusCode(500, result.Error),
                _ => BadRequest(result.Error)
            };
        }

        /// <summary>
        /// Преобразует Result с значением в ActionResult с анонимным объектом
        /// </summary>
        /// <param name="result">Результат операции</param>
        /// <returns>ActionResult с данными или ошибкой</returns>
        protected IActionResult ToActionResultObject<T>(Result<T> result)
        {
            if (result.IsSuccess)
            {
                return result.StatusCode == 201
                    ? StatusCode(201, result.Value)
                    : Ok(result.Value);
            }

            return result.StatusCode switch
            {
                401 => Unauthorized(result.Error),
                403 => StatusCode(403, result.Error),
                404 => NotFound(result.Error),
                409 => Conflict(result.Error),
                500 => StatusCode(500, result.Error),
                _ => BadRequest(result.Error)
            };
        }

        #endregion Protected Methods
    }
}




