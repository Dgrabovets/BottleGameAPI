using BottleGame.Application.Common;
using Microsoft.AspNetCore.Mvc;

namespace BottleGame.API.Controllers
{
    /// <summary>
    /// Base API controller with common methods
    /// </summary>
    /// <remarks>
    /// Provides methods that convert a Result to an ActionResult
    /// and other common methods for all controllers
    /// </remarks>
    [ApiController]
    public abstract class BaseApiController : ControllerBase
    {
        #region Protected Methods

        /// <summary>
        /// Converts a Result without a value to an ActionResult
        /// </summary>
        /// <param name="result">Operation result</param>
        /// <returns>ActionResult with the matching status code</returns>
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
                403 => Forbid(result.Error ?? "Access denied"),
                404 => NotFound(result.Error),
                409 => Conflict(result.Error),
                500 => StatusCode(500, result.Error),
                _ => BadRequest(result.Error)
            };
        }

        /// <summary>
        /// Converts a Result with a value to an ActionResult
        /// </summary>
        /// <typeparam name="T">Type of the returned value</typeparam>
        /// <param name="result">Operation result</param>
        /// <returns>ActionResult with the data or an error</returns>
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
        /// Converts a Result with a value to an ActionResult with an anonymous object
        /// </summary>
        /// <param name="result">Operation result</param>
        /// <returns>ActionResult with the data or an error</returns>
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




