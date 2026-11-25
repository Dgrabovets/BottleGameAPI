using BottleGame.Application.Common;
using BottleGame.Application.DTOs.Requests;
using BottleGame.Application.DTOs.Responses;
using BottleGame.Application.Interfaces;
using BottleGame.Domain.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace BottleGame.API.Controllers
{
    /// <summary>
    /// Balance and transaction controller
    /// </summary>
    /// <remarks>
    /// Manages player balances, deposits, withdrawals and transaction statuses
    /// </remarks>
    /// <remarks>
    /// Initializes a new instance of <see cref="BalanceController"/>
    /// </remarks>
    /// <param name="transactionService">Service for working with transactions</param>
    [ApiController]
    [Route("api/[controller]")]
    [Tags("Balance and transactions")]
    public class BalanceController(ITransactionService transactionService) : BaseApiController
    {
        #region Fields

        /// <summary>
        /// Service for working with transactions and balances
        /// </summary>
        private readonly ITransactionService _transactionService = transactionService;

        #endregion Fields

        #region Balance Methods

        /// <summary>
        /// Gets a player's balance
        /// </summary>
        /// <param name="playerId">Player id</param>
        /// <response code="200">Returns the player's current balance</response>
        /// <response code="404">The player was not found</response>
        /// <response code="403">The player is banned</response>
        /// <response code="500">Internal server error</response>
        [Authorize(Roles = RoleNames.AdminOrPlayer)]
        [HttpGet("get-player-balance/{playerId:int}")]
        [ProducesResponseType(typeof(GetPlayerBalanceResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<GetPlayerBalanceResponse>> GetPlayerBalance([FromRoute] int playerId)
        {
            var requestingPlayerId = GetCurrentPlayerId();
            var isAdmin = User.IsInRole(RoleNames.Admin);

            var result = await _transactionService.GetPlayerBalance(playerId, requestingPlayerId, isAdmin);
            return ToActionResult(result);
        }

        #endregion Balance Methods

        #region Transaction Methods

        /// <summary>
        /// Creates a transaction
        /// </summary>
        /// <param name="playerId">Player id</param>
        /// <param name="request">Request to create a transaction</param>
        /// <response code="200">The transaction was created</response>
        /// <response code="400">Invalid request or insufficient funds</response>
        /// <response code="403">The player is banned</response>
        /// <response code="404">The player was not found</response>
        /// <response code="500">Internal server error</response>
        [Authorize(Roles = RoleNames.AdminOrPlayer)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [HttpPost("create-transaction/{playerId:int}")]
        public async Task<IActionResult> CreateTransaction([FromRoute] int playerId, [FromBody] MoneyTransactionRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var requestingPlayerId = GetCurrentPlayerId();
            var isAdmin = User.IsInRole(RoleNames.Admin);

            var result = await _transactionService.CreateTransaction(playerId, request, requestingPlayerId, isAdmin);
            return ToActionResultObject(result);
        }

        /// <summary>
        /// Changes the status of a transaction (deposit or withdrawal)
        /// </summary>
        /// <param name="transactionId">Transaction id</param>
        /// <param name="request">Request model: the new status (2 - Approved, 3 - Rejected)</param>
        /// <response code="200">The transaction status was changed</response>
        /// <response code="400">Invalid request or invalid status</response>
        /// <response code="404">The transaction was not found</response>
        /// <response code="500">Internal server error</response>
        [Authorize(Roles = RoleNames.AdminOrModerator)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [HttpPost("update-transaction-status/{transactionId:int}")]
        public async Task<IActionResult> UpdateTransactionStatus([FromRoute] int transactionId, [FromBody] ChangeTransactionStatusRequest request)
        {
            var result = await _transactionService.UpdateTransactionStatus(transactionId, request);
            return ToActionResult(result);
        }

        /// <summary>
        /// Gets all transactions of a player
        /// </summary>
        /// <param name="playerId">Player id</param>
        /// <response code="200">List of the player's transactions</response>
        /// <response code="400">Invalid request data</response>
        /// <response code="404">The player was not found</response>
        /// <response code="403">The player is banned</response>
        /// <response code="500">Internal server error</response>
        [Authorize(Roles = RoleNames.All)]
        [HttpGet("get-player-transactions/{playerId:int}")]
        [ProducesResponseType(typeof(GetPlayerTransactionsResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<GetPlayerTransactionsResponse>> GetPlayerTransactions([FromRoute] int playerId)
        {
            var requestingPlayerId = GetCurrentPlayerId();
            var isStaff = User.IsInRole(RoleNames.Admin) || User.IsInRole(RoleNames.Moderator);

            var result = await _transactionService.GetPlayerTransactions(playerId, requestingPlayerId, isStaff);
            return ToActionResult(result);
        }

        /// <summary>
        /// Gets all withdrawals
        /// </summary>
        /// <param name="typeId">Transaction type</param>
        /// <param name="page">Page number from 1; with page or pageSize the answer holds one page of transactions and X-Total-Count carries the total</param>
        /// <param name="pageSize">Transactions per page, at most 200, 20 by default</param>
        /// <response code="200">List of all transactions of the given type</response>
        /// <response code="400">Invalid request data</response>
        /// <response code="500">Internal server error</response>
        [Authorize(Roles = RoleNames.AdminOrModerator)]
        [HttpGet("get-all-transactions")]
        [ProducesResponseType(typeof(IEnumerable<GetPlayerTransactionsResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<IEnumerable<GetPlayerTransactionsResponse>>> GetAllTransactions([FromQuery] int? typeId,
                                                                                                       [FromQuery] int? page = null,
                                                                                                       [FromQuery] int? pageSize = null)
        {
            var pageRequest = PageRequest.Create(page, pageSize);
            var result = await _transactionService.GetAllTransactions(typeId, pageRequest);

            if (pageRequest is not null)
            {
                Response.Headers[TotalCountHeader] = (await _transactionService.CountTransactions(typeId)).ToString();
            }

            return ToActionResult(result);
        }

        #endregion Transaction Methods

        #region Reference Data Methods

        /// <summary>
        /// Gets all available transaction statuses
        /// </summary>
        /// <response code="200">List of all transaction statuses</response>
        /// <response code="500">Internal server error</response>
        [Authorize(Roles = RoleNames.AdminOrModerator)]
        [HttpGet("get-all-transaction-statuses")]
        [ProducesResponseType(typeof(IEnumerable<TransactionStatusOrTypeDTO>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<IEnumerable<TransactionStatusOrTypeDTO>>> GetAllTransactionStatuses()
        {
            var result = await _transactionService.GetAllTransactionStatuses();
            return ToActionResult(result);
        }

        /// <summary>
        /// Gets all possible transaction types
        /// </summary>
        /// <response code="200">List of all transaction types</response>
        /// <response code="500">Internal server error</response>
        [Authorize(Roles = RoleNames.AdminOrModerator)]
        [HttpGet("get-all-transaction-types")]
        [ProducesResponseType(typeof(IEnumerable<TransactionStatusOrTypeDTO>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<IEnumerable<TransactionStatusOrTypeDTO>>> GetAllTransactionTypes()
        {
            var result = await _transactionService.GetAllTransactionTypes();
            return ToActionResult(result);
        }

        #endregion Reference Data Methods

        #region Private Methods

        /// <summary>
        /// Gets the id of the current player from the claims
        /// </summary>
        private int? GetCurrentPlayerId()
        {
            var claim = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier);
            if (claim is not null && int.TryParse(claim.Value, out var playerId))
            {
                return playerId;
            }

            return null;
        }

        #endregion Private Methods
    }
}
