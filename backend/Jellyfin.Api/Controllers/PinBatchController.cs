using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Jellyfin.Api.Constants;
using Jellyfin.Api.Extensions;
using Jellyfin.Api.Models.PinBatchDtos;
using Jellyfin.Database.Implementations.Entities;
using Jellyfin.Database.Implementations.Enums;
using Jellyfin.Server.Implementations.Export;
using Jellyfin.Server.Implementations.PinGeneration;
using MediaBrowser.Common.Api;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Api.Controllers
{
    /// <summary>
    /// PIN batch management controller.
    /// </summary>
    [Route("PinBatches")]
    [Authorize(Policy = Policies.RequiresElevation)]
    public class PinBatchController : BaseJellyfinApiController
    {
        private readonly PinBatchManager _pinBatchManager;
        private readonly ExcelExportService _excelExportService;
        private readonly ILogger<PinBatchController> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="PinBatchController"/> class.
        /// </summary>
        /// <param name="pinBatchManager">The PIN batch manager.</param>
        /// <param name="excelExportService">The Excel export service.</param>
        /// <param name="logger">The logger.</param>
        public PinBatchController(
            PinBatchManager pinBatchManager,
            ExcelExportService excelExportService,
            ILogger<PinBatchController> logger)
        {
            _pinBatchManager = pinBatchManager;
            _excelExportService = excelExportService;
            _logger = logger;
        }

        /// <summary>
        /// Creates a new PIN batch.
        /// </summary>
        /// <param name="request">The batch creation request.</param>
        /// <response code="201">Batch created successfully.</response>
        /// <response code="400">Invalid request data.</response>
        /// <response code="403">Admin access required.</response>
        /// <returns>The created batch.</returns>
        [HttpPost]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<PinBatchDto>> CreateBatch([FromBody, Required] CreatePinBatchRequest request)
        {
            try
            {
                var batchSettings = new BatchSettings
                {
                    MaxConcurrentSessions = request.MaxConcurrentSessions,
                    AllowRemoteAccess = request.AllowRemoteAccess,
                    AllowTranscoding = request.AllowTranscoding,
                    AllowDownload = request.AllowDownload,
                    AllowSyncPlay = request.AllowSyncPlay,
                    MaxBitrate = request.MaxBitrate,
                    MaxParentalRating = request.MaxParentalRating,
                    Price = request.Price,
                    Currency = request.Currency,
                    Metadata = request.Metadata
                };

                var batch = await _pinBatchManager.CreateBatchAsync(
                    request.Name,
                    request.Description,
                    request.SubscriptionType,
                    request.PinPattern,
                    request.PinLength,
                    request.PinCount,
                    User.GetUserId(),
                    request.CustomCharacterSet,
                    request.ExpirationDate,
                    batchSettings).ConfigureAwait(false);

                var response = new PinBatchDto
                {
                    Id = batch.Id,
                    Name = batch.Name,
                    Description = batch.Description,
                    SubscriptionType = batch.SubscriptionType,
                    PinPattern = batch.PinPattern,
                    PinLength = batch.PinLength,
                    CustomCharacterSet = batch.CustomCharacterSet,
                    TotalPins = batch.TotalPins,
                    UsedPins = batch.UsedPins,
                    ActivePins = batch.ActivePins,
                    ExpiredPins = batch.ExpiredPins,
                    Status = batch.Status,
                    CreatedDate = batch.CreatedDate,
                    ExpirationDate = batch.ExpirationDate,
                    ModifiedDate = batch.ModifiedDate,
                    CreatedByUserId = batch.CreatedByUserId,
                    ModifiedByUserId = batch.ModifiedByUserId,
                    MaxConcurrentSessions = batch.MaxConcurrentSessions,
                    AllowRemoteAccess = batch.AllowRemoteAccess,
                    MaxBitrate = batch.MaxBitrate,
                    AllowTranscoding = batch.AllowTranscoding,
                    MaxParentalRating = batch.MaxParentalRating,
                    AllowDownload = batch.AllowDownload,
                    AllowSyncPlay = batch.AllowSyncPlay,
                    Price = batch.Price,
                    Currency = batch.Currency,
                    Metadata = batch.Metadata
                };

                return CreatedAtAction(nameof(GetBatch), new { batchId = batch.Id }, response);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Invalid request for PIN batch creation");
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating PIN batch");
                return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while creating the batch.");
            }
        }

        /// <summary>
        /// Gets all PIN batches with optional filtering.
        /// </summary>
        /// <param name="status">Optional status filter.</param>
        /// <param name="subscriptionType">Optional subscription type filter.</param>
        /// <param name="createdByUserId">Optional creator filter.</param>
        /// <response code="200">Batches returned successfully.</response>
        /// <returns>A list of PIN batches.</returns>
        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<ActionResult<List<PinBatchDto>>> GetBatches(
            [FromQuery] BatchStatus? status,
            [FromQuery] SubscriptionType? subscriptionType,
            [FromQuery] Guid? createdByUserId)
        {
            try
            {
                var batches = await _pinBatchManager.GetBatchesAsync(status, subscriptionType, createdByUserId).ConfigureAwait(false);

                var response = batches.Select(batch => new PinBatchDto
                {
                    Id = batch.Id,
                    Name = batch.Name,
                    Description = batch.Description,
                    SubscriptionType = batch.SubscriptionType,
                    PinPattern = batch.PinPattern,
                    PinLength = batch.PinLength,
                    CustomCharacterSet = batch.CustomCharacterSet,
                    TotalPins = batch.TotalPins,
                    UsedPins = batch.UsedPins,
                    ActivePins = batch.ActivePins,
                    ExpiredPins = batch.ExpiredPins,
                    Status = batch.Status,
                    CreatedDate = batch.CreatedDate,
                    ExpirationDate = batch.ExpirationDate,
                    ModifiedDate = batch.ModifiedDate,
                    CreatedByUserId = batch.CreatedByUserId,
                    ModifiedByUserId = batch.ModifiedByUserId,
                    MaxConcurrentSessions = batch.MaxConcurrentSessions,
                    AllowRemoteAccess = batch.AllowRemoteAccess,
                    MaxBitrate = batch.MaxBitrate,
                    AllowTranscoding = batch.AllowTranscoding,
                    MaxParentalRating = batch.MaxParentalRating,
                    AllowDownload = batch.AllowDownload,
                    AllowSyncPlay = batch.AllowSyncPlay,
                    Price = batch.Price,
                    Currency = batch.Currency,
                    Metadata = batch.Metadata
                }).ToList();

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving PIN batches");
                return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while retrieving batches.");
            }
        }

        /// <summary>
        /// Gets a PIN batch by ID.
        /// </summary>
        /// <param name="batchId">The batch ID.</param>
        /// <response code="200">Batch returned successfully.</response>
        /// <response code="404">Batch not found.</response>
        /// <returns>The PIN batch.</returns>
        [HttpGet("{batchId}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<PinBatchDto>> GetBatch([FromRoute, Required] Guid batchId)
        {
            try
            {
                var batch = await _pinBatchManager.GetBatchAsync(batchId).ConfigureAwait(false);

                if (batch == null)
                {
                    return NotFound("Batch not found.");
                }

                var response = new PinBatchDto
                {
                    Id = batch.Id,
                    Name = batch.Name,
                    Description = batch.Description,
                    SubscriptionType = batch.SubscriptionType,
                    PinPattern = batch.PinPattern,
                    PinLength = batch.PinLength,
                    CustomCharacterSet = batch.CustomCharacterSet,
                    TotalPins = batch.TotalPins,
                    UsedPins = batch.UsedPins,
                    ActivePins = batch.ActivePins,
                    ExpiredPins = batch.ExpiredPins,
                    Status = batch.Status,
                    CreatedDate = batch.CreatedDate,
                    ExpirationDate = batch.ExpirationDate,
                    ModifiedDate = batch.ModifiedDate,
                    CreatedByUserId = batch.CreatedByUserId,
                    ModifiedByUserId = batch.ModifiedByUserId,
                    MaxConcurrentSessions = batch.MaxConcurrentSessions,
                    AllowRemoteAccess = batch.AllowRemoteAccess,
                    MaxBitrate = batch.MaxBitrate,
                    AllowTranscoding = batch.AllowTranscoding,
                    MaxParentalRating = batch.MaxParentalRating,
                    AllowDownload = batch.AllowDownload,
                    AllowSyncPlay = batch.AllowSyncPlay,
                    Price = batch.Price,
                    Currency = batch.Currency,
                    Metadata = batch.Metadata
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving PIN batch {BatchId}", batchId);
                return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while retrieving the batch.");
            }
        }

        /// <summary>
        /// Updates a PIN batch.
        /// </summary>
        /// <param name="batchId">The batch ID.</param>
        /// <param name="request">The update request.</param>
        /// <response code="204">Batch updated successfully.</response>
        /// <response code="404">Batch not found.</response>
        /// <returns>No content on success.</returns>
        [HttpPut("{batchId}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult> UpdateBatch(
            [FromRoute, Required] Guid batchId,
            [FromBody, Required] UpdatePinBatchRequest request)
        {
            try
            {
                var success = await _pinBatchManager.UpdateBatchAsync(
                    batchId,
                    request.Name,
                    request.Description,
                    User.GetUserId()).ConfigureAwait(false);

                if (!success)
                {
                    return NotFound("Batch not found.");
                }

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating PIN batch {BatchId}", batchId);
                return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while updating the batch.");
            }
        }

        /// <summary>
        /// Activates a PIN batch.
        /// </summary>
        /// <param name="batchId">The batch ID.</param>
        /// <response code="204">Batch activated successfully.</response>
        /// <response code="404">Batch not found.</response>
        /// <returns>No content on success.</returns>
        [HttpPost("{batchId}/Activate")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult> ActivateBatch([FromRoute, Required] Guid batchId)
        {
            try
            {
                var success = await _pinBatchManager.ActivateBatchAsync(batchId).ConfigureAwait(false);

                if (!success)
                {
                    return NotFound("Batch not found.");
                }

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error activating PIN batch {BatchId}", batchId);
                return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while activating the batch.");
            }
        }

        /// <summary>
        /// Suspends a PIN batch.
        /// </summary>
        /// <param name="batchId">The batch ID.</param>
        /// <response code="204">Batch suspended successfully.</response>
        /// <response code="404">Batch not found.</response>
        /// <returns>No content on success.</returns>
        [HttpPost("{batchId}/Suspend")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult> SuspendBatch([FromRoute, Required] Guid batchId)
        {
            try
            {
                var success = await _pinBatchManager.SuspendBatchAsync(batchId).ConfigureAwait(false);

                if (!success)
                {
                    return NotFound("Batch not found.");
                }

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error suspending PIN batch {BatchId}", batchId);
                return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while suspending the batch.");
            }
        }

        /// <summary>
        /// Deletes a PIN batch.
        /// </summary>
        /// <param name="batchId">The batch ID.</param>
        /// <response code="204">Batch deleted successfully.</response>
        /// <response code="404">Batch not found.</response>
        /// <returns>No content on success.</returns>
        [HttpDelete("{batchId}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult> DeleteBatch([FromRoute, Required] Guid batchId)
        {
            try
            {
                var success = await _pinBatchManager.DeleteBatchAsync(batchId).ConfigureAwait(false);

                if (!success)
                {
                    return NotFound("Batch not found.");
                }

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting PIN batch {BatchId}", batchId);
                return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while deleting the batch.");
            }
        }

        /// <summary>
        /// Gets batch statistics.
        /// </summary>
        /// <param name="batchId">The batch ID.</param>
        /// <response code="200">Statistics returned successfully.</response>
        /// <response code="404">Batch not found.</response>
        /// <returns>Batch statistics.</returns>
        [HttpGet("{batchId}/Statistics")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<BatchStatisticsDto>> GetBatchStatistics([FromRoute, Required] Guid batchId)
        {
            try
            {
                var statistics = await _pinBatchManager.GetBatchStatisticsAsync(batchId).ConfigureAwait(false);

                if (statistics == null)
                {
                    return NotFound("Batch not found.");
                }

                var response = new BatchStatisticsDto
                {
                    BatchId = statistics.BatchId,
                    BatchName = statistics.BatchName,
                    TotalPins = statistics.TotalPins,
                    ActivePins = statistics.ActivePins,
                    ExpiredPins = statistics.ExpiredPins,
                    UsedPins = statistics.UsedPins,
                    UnusedPins = statistics.UnusedPins,
                    TotalUsageCount = statistics.TotalUsageCount,
                    AverageUsagePerPin = statistics.AverageUsagePerPin,
                    CreatedDate = statistics.CreatedDate,
                    LastActivityDate = statistics.LastActivityDate
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving statistics for PIN batch {BatchId}", batchId);
                return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while retrieving statistics.");
            }
        }

        /// <summary>
        /// Gets PINs from a batch.
        /// </summary>
        /// <param name="batchId">The batch ID.</param>
        /// <param name="includeInactive">Whether to include inactive PINs.</param>
        /// <param name="includeExpired">Whether to include expired PINs.</param>
        /// <response code="200">PINs returned successfully.</response>
        /// <returns>A list of PIN batch users.</returns>
        [HttpGet("{batchId}/Pins")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<ActionResult<List<PinBatchUserDto>>> GetBatchPins(
            [FromRoute, Required] Guid batchId,
            [FromQuery] bool includeInactive = false,
            [FromQuery] bool includeExpired = false)
        {
            try
            {
                var pins = await _pinBatchManager.GetBatchPinsAsync(batchId, includeInactive, includeExpired).ConfigureAwait(false);

                var response = pins.Select(pin => new PinBatchUserDto
                {
                    Id = pin.Id,
                    BatchId = pin.BatchId,
                    UserId = pin.UserId,
                    IsActive = pin.IsActive,
                    CreatedDate = pin.CreatedDate,
                    FirstUsedDate = pin.FirstUsedDate,
                    LastUsedDate = pin.LastUsedDate,
                    UsageCount = pin.UsageCount,
                    ExpirationDate = pin.ExpirationDate,
                    DeactivatedDate = pin.DeactivatedDate,
                    DeactivationReason = pin.DeactivationReason,
                    Metadata = pin.Metadata,
                    LastLoginIp = pin.LastLoginIp,
                    LastLoginDevice = pin.LastLoginDevice
                }).ToList();

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving PINs for batch {BatchId}", batchId);
                return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while retrieving PINs.");
            }
        }

        /// <summary>
        /// Exports a PIN batch to Excel format.
        /// </summary>
        /// <param name="batchId">The batch ID.</param>
        /// <param name="includeOriginalPins">Whether to include original PINs in the export.</param>
        /// <response code="200">Excel file returned successfully.</response>
        /// <response code="404">Batch not found.</response>
        /// <returns>Excel file containing batch data.</returns>
        [HttpGet("{batchId}/Export")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult> ExportBatchToExcel(
            [FromRoute, Required] Guid batchId,
            [FromQuery] bool includeOriginalPins = false)
        {
            try
            {
                var batch = await _pinBatchManager.GetBatchAsync(batchId).ConfigureAwait(false);
                if (batch == null)
                {
                    return NotFound("Batch not found.");
                }

                var pins = await _pinBatchManager.GetBatchPinsAsync(batchId, true, true).ConfigureAwait(false);
                var excelStream = await _excelExportService.ExportPinBatchToExcelAsync(batch, pins, includeOriginalPins).ConfigureAwait(false);

                var fileName = $"PIN_Batch_{SanitizeFileName(batch.Name)}_{DateTime.UtcNow:yyyyMMdd_HHmmss}.xlsx";
                
                return File(excelStream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting PIN batch {BatchId} to Excel", batchId);
                return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while exporting the batch.");
            }
        }

        /// <summary>
        /// Exports multiple PIN batches to Excel format.
        /// </summary>
        /// <param name="request">The export request containing batch IDs and options.</param>
        /// <response code="200">Excel file returned successfully.</response>
        /// <response code="400">Invalid request data.</response>
        /// <returns>Excel file containing multiple batches data.</returns>
        [HttpPost("Export")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult> ExportMultipleBatchesToExcel(
            [FromBody, Required] ExportBatchesRequest request)
        {
            try
            {
                if (request.BatchIds == null || !request.BatchIds.Any())
                {
                    return BadRequest("At least one batch ID must be provided.");
                }

                var batches = new List<PinBatch>();
                var batchPins = new Dictionary<Guid, List<PinBatchUser>>();

                foreach (var batchId in request.BatchIds)
                {
                    var batch = await _pinBatchManager.GetBatchAsync(batchId).ConfigureAwait(false);
                    if (batch != null)
                    {
                        batches.Add(batch);
                        var pins = await _pinBatchManager.GetBatchPinsAsync(batchId, true, true).ConfigureAwait(false);
                        batchPins[batchId] = pins;
                    }
                }

                if (!batches.Any())
                {
                    return NotFound("No valid batches found.");
                }

                var excelStream = await _excelExportService.ExportMultipleBatchesToExcelAsync(batches, batchPins, request.IncludeOriginalPins).ConfigureAwait(false);

                var fileName = $"PIN_Batches_{batches.Count}_Batches_{DateTime.UtcNow:yyyyMMdd_HHmmss}.xlsx";
                
                return File(excelStream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting multiple PIN batches to Excel");
                return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while exporting the batches.");
            }
        }

        /// <summary>
        /// Sanitizes a filename for safe file system usage.
        /// </summary>
        /// <param name="fileName">The filename to sanitize.</param>
        /// <returns>The sanitized filename.</returns>
        private static string SanitizeFileName(string fileName)
        {
            var invalidChars = System.IO.Path.GetInvalidFileNameChars();
            return string.Join("_", fileName.Split(invalidChars, StringSplitOptions.RemoveEmptyEntries)).TrimEnd('.');
        }
    }

    /// <summary>
    /// Request to export multiple PIN batches.
    /// </summary>
    public class ExportBatchesRequest
    {
        /// <summary>
        /// Gets or sets the batch IDs to export.
        /// </summary>
        [Required]
        public List<Guid> BatchIds { get; set; } = new List<Guid>();

        /// <summary>
        /// Gets or sets whether to include original PINs in the export.
        /// </summary>
        public bool IncludeOriginalPins { get; set; } = false;
    }
}
