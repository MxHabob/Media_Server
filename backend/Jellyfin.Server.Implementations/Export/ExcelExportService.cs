using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Jellyfin.Database.Implementations.Entities;
using Jellyfin.Server.Implementations.PinGeneration;
using Microsoft.Extensions.Logging;
using OfficeOpenXml;
using OfficeOpenXml.Style;

namespace Jellyfin.Server.Implementations.Export
{
    /// <summary>
    /// Service for exporting PIN data to Excel files.
    /// </summary>
    public class ExcelExportService
    {
        private readonly ILogger<ExcelExportService> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="ExcelExportService"/> class.
        /// </summary>
        /// <param name="logger">The logger.</param>
        public ExcelExportService(ILogger<ExcelExportService> logger)
        {
            _logger = logger;
            
            // Set EPPlus license context
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
        }

        /// <summary>
        /// Exports PIN batch data to Excel format.
        /// </summary>
        /// <param name="batch">The PIN batch to export.</param>
        /// <param name="pinBatchUsers">The PIN batch users to export.</param>
        /// <param name="includeOriginalPins">Whether to include original PINs (requires decryption).</param>
        /// <returns>A stream containing the Excel file.</returns>
        public async Task<Stream> ExportPinBatchToExcelAsync(
            PinBatch batch, 
            List<PinBatchUser> pinBatchUsers, 
            bool includeOriginalPins = false)
        {
            var stream = new MemoryStream();
            
            using var package = new ExcelPackage(stream);
            
            // Create summary sheet
            var summarySheet = package.Workbook.Worksheets.Add("Batch Summary");
            await CreateBatchSummarySheetAsync(summarySheet, batch, pinBatchUsers).ConfigureAwait(false);
            
            // Create PINs sheet
            var pinsSheet = package.Workbook.Worksheets.Add("PINs");
            await CreatePinsSheetAsync(pinsSheet, batch, pinBatchUsers, includeOriginalPins).ConfigureAwait(false);
            
            // Create statistics sheet
            var statisticsSheet = package.Workbook.Worksheets.Add("Statistics");
            await CreateStatisticsSheetAsync(statisticsSheet, batch, pinBatchUsers).ConfigureAwait(false);
            
            package.Save();
            stream.Position = 0;
            
            _logger.LogInformation("Exported PIN batch '{BatchName}' to Excel with {PinCount} PINs", 
                batch.Name, pinBatchUsers.Count);
            
            return stream;
        }

        /// <summary>
        /// Exports multiple PIN batches to Excel format.
        /// </summary>
        /// <param name="batches">The PIN batches to export.</param>
        /// <param name="batchPins">Dictionary mapping batch IDs to their PINs.</param>
        /// <param name="includeOriginalPins">Whether to include original PINs.</param>
        /// <returns>A stream containing the Excel file.</returns>
        public async Task<Stream> ExportMultipleBatchesToExcelAsync(
            List<PinBatch> batches, 
            Dictionary<Guid, List<PinBatchUser>> batchPins, 
            bool includeOriginalPins = false)
        {
            var stream = new MemoryStream();
            
            using var package = new ExcelPackage(stream);
            
            // Create overview sheet
            var overviewSheet = package.Workbook.Worksheets.Add("Overview");
            await CreateOverviewSheetAsync(overviewSheet, batches, batchPins).ConfigureAwait(false);
            
            // Create individual batch sheets
            foreach (var batch in batches)
            {
                if (batchPins.TryGetValue(batch.Id, out var pins))
                {
                    var sheetName = SanitizeSheetName(batch.Name);
                    var batchSheet = package.Workbook.Worksheets.Add(sheetName);
                    await CreatePinsSheetAsync(batchSheet, batch, pins, includeOriginalPins).ConfigureAwait(false);
                }
            }
            
            package.Save();
            stream.Position = 0;
            
            _logger.LogInformation("Exported {BatchCount} PIN batches to Excel", batches.Count);
            
            return stream;
        }

        /// <summary>
        /// Creates the batch summary sheet.
        /// </summary>
        /// <param name="worksheet">The worksheet to populate.</param>
        /// <param name="batch">The batch data.</param>
        /// <param name="pinBatchUsers">The PIN batch users.</param>
        private async Task CreateBatchSummarySheetAsync(ExcelWorksheet worksheet, PinBatch batch, List<PinBatchUser> pinBatchUsers)
        {
            // Set headers
            worksheet.Cells[1, 1].Value = "Batch Information";
            worksheet.Cells[1, 1].Style.Font.Bold = true;
            worksheet.Cells[1, 1].Style.Font.Size = 16;
            
            // Batch details
            var row = 3;
            worksheet.Cells[row, 1].Value = "Batch Name:";
            worksheet.Cells[row, 2].Value = batch.Name;
            row++;
            
            worksheet.Cells[row, 1].Value = "Description:";
            worksheet.Cells[row, 2].Value = batch.Description ?? "N/A";
            row++;
            
            worksheet.Cells[row, 1].Value = "Subscription Type:";
            worksheet.Cells[row, 2].Value = batch.SubscriptionType.ToString();
            row++;
            
            worksheet.Cells[row, 1].Value = "PIN Pattern:";
            worksheet.Cells[row, 2].Value = batch.PinPattern.ToString();
            row++;
            
            worksheet.Cells[row, 1].Value = "PIN Length:";
            worksheet.Cells[row, 2].Value = batch.PinLength;
            row++;
            
            worksheet.Cells[row, 1].Value = "Status:";
            worksheet.Cells[row, 2].Value = batch.Status.ToString();
            row++;
            
            worksheet.Cells[row, 1].Value = "Created Date:";
            worksheet.Cells[row, 2].Value = batch.CreatedDate.ToString("yyyy-MM-dd HH:mm:ss");
            row++;
            
            if (batch.ExpirationDate.HasValue)
            {
                worksheet.Cells[row, 1].Value = "Expiration Date:";
                worksheet.Cells[row, 2].Value = batch.ExpirationDate.Value.ToString("yyyy-MM-dd HH:mm:ss");
                row++;
            }
            
            // Statistics
            row += 2;
            worksheet.Cells[row, 1].Value = "Statistics";
            worksheet.Cells[row, 1].Style.Font.Bold = true;
            worksheet.Cells[row, 1].Style.Font.Size = 14;
            row++;
            
            var now = DateTime.UtcNow;
            var activePins = pinBatchUsers.Count(pbu => pbu.IsActive && (!pbu.ExpirationDate.HasValue || pbu.ExpirationDate.Value > now));
            var expiredPins = pinBatchUsers.Count(pbu => pbu.ExpirationDate.HasValue && pbu.ExpirationDate.Value <= now);
            var usedPins = pinBatchUsers.Count(pbu => pbu.UsageCount > 0);
            var totalUsage = pinBatchUsers.Sum(pbu => pbu.UsageCount);
            
            worksheet.Cells[row, 1].Value = "Total PINs:";
            worksheet.Cells[row, 2].Value = pinBatchUsers.Count;
            row++;
            
            worksheet.Cells[row, 1].Value = "Active PINs:";
            worksheet.Cells[row, 2].Value = activePins;
            row++;
            
            worksheet.Cells[row, 1].Value = "Expired PINs:";
            worksheet.Cells[row, 2].Value = expiredPins;
            row++;
            
            worksheet.Cells[row, 1].Value = "Used PINs:";
            worksheet.Cells[row, 2].Value = usedPins;
            row++;
            
            worksheet.Cells[row, 1].Value = "Unused PINs:";
            worksheet.Cells[row, 2].Value = pinBatchUsers.Count - usedPins;
            row++;
            
            worksheet.Cells[row, 1].Value = "Total Usage Count:";
            worksheet.Cells[row, 2].Value = totalUsage;
            row++;
            
            if (pinBatchUsers.Count > 0)
            {
                worksheet.Cells[row, 1].Value = "Average Usage per PIN:";
                worksheet.Cells[row, 2].Value = Math.Round((double)totalUsage / pinBatchUsers.Count, 2);
            }
            
            // Auto-fit columns
            worksheet.Cells.AutoFitColumns();
            
            await Task.CompletedTask.ConfigureAwait(false);
        }

        /// <summary>
        /// Creates the PINs sheet.
        /// </summary>
        /// <param name="worksheet">The worksheet to populate.</param>
        /// <param name="batch">The batch data.</param>
        /// <param name="pinBatchUsers">The PIN batch users.</param>
        /// <param name="includeOriginalPins">Whether to include original PINs.</param>
        private async Task CreatePinsSheetAsync(ExcelWorksheet worksheet, PinBatch batch, List<PinBatchUser> pinBatchUsers, bool includeOriginalPins)
        {
            // Headers
            var headers = new List<string> { "PIN ID", "User ID", "Status", "Created Date", "First Used", "Last Used", "Usage Count", "Expiration Date" };
            
            if (includeOriginalPins)
            {
                headers.Insert(1, "Original PIN");
            }
            
            for (int i = 0; i < headers.Count; i++)
            {
                worksheet.Cells[1, i + 1].Value = headers[i];
                worksheet.Cells[1, i + 1].Style.Font.Bold = true;
                worksheet.Cells[1, i + 1].Style.Fill.PatternType = ExcelFillStyle.Solid;
                worksheet.Cells[1, i + 1].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightGray);
            }
            
            // Data
            for (int i = 0; i < pinBatchUsers.Count; i++)
            {
                var pin = pinBatchUsers[i];
                var row = i + 2;
                var col = 1;
                
                worksheet.Cells[row, col++].Value = pin.Id.ToString();
                
                if (includeOriginalPins)
                {
                    try
                    {
                        var originalPin = DecryptPin(pin.OriginalPin);
                        worksheet.Cells[row, col++].Value = originalPin;
                    }
                    catch
                    {
                        worksheet.Cells[row, col++].Value = "N/A";
                    }
                }
                
                worksheet.Cells[row, col++].Value = pin.UserId.ToString();
                worksheet.Cells[row, col++].Value = pin.IsActive ? "Active" : "Inactive";
                worksheet.Cells[row, col++].Value = pin.CreatedDate.ToString("yyyy-MM-dd HH:mm:ss");
                worksheet.Cells[row, col++].Value = pin.FirstUsedDate?.ToString("yyyy-MM-dd HH:mm:ss") ?? "Never";
                worksheet.Cells[row, col++].Value = pin.LastUsedDate?.ToString("yyyy-MM-dd HH:mm:ss") ?? "Never";
                worksheet.Cells[row, col++].Value = pin.UsageCount;
                worksheet.Cells[row, col++].Value = pin.ExpirationDate?.ToString("yyyy-MM-dd HH:mm:ss") ?? "Lifetime";
                
                // Color coding for status
                if (!pin.IsActive)
                {
                    worksheet.Cells[row, 3].Style.Fill.PatternType = ExcelFillStyle.Solid;
                    worksheet.Cells[row, 3].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightCoral);
                }
                else if (pin.ExpirationDate.HasValue && pin.ExpirationDate.Value <= DateTime.UtcNow)
                {
                    worksheet.Cells[row, 3].Style.Fill.PatternType = ExcelFillStyle.Solid;
                    worksheet.Cells[row, 3].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightYellow);
                }
            }
            
            // Auto-fit columns
            worksheet.Cells.AutoFitColumns();
            
            await Task.CompletedTask.ConfigureAwait(false);
        }

        /// <summary>
        /// Creates the statistics sheet.
        /// </summary>
        /// <param name="worksheet">The worksheet to populate.</param>
        /// <param name="batch">The batch data.</param>
        /// <param name="pinBatchUsers">The PIN batch users.</param>
        private async Task CreateStatisticsSheetAsync(ExcelWorksheet worksheet, PinBatch batch, List<PinBatchUser> pinBatchUsers)
        {
            // Usage statistics
            worksheet.Cells[1, 1].Value = "Usage Statistics";
            worksheet.Cells[1, 1].Style.Font.Bold = true;
            worksheet.Cells[1, 1].Style.Font.Size = 16;
            
            var row = 3;
            worksheet.Cells[row, 1].Value = "Usage Count";
            worksheet.Cells[row, 2].Value = "Number of PINs";
            worksheet.Cells[row, 1].Style.Font.Bold = true;
            worksheet.Cells[row, 2].Style.Font.Bold = true;
            row++;
            
            var usageGroups = pinBatchUsers.GroupBy(pbu => pbu.UsageCount).OrderBy(g => g.Key);
            foreach (var group in usageGroups)
            {
                worksheet.Cells[row, 1].Value = group.Key;
                worksheet.Cells[row, 2].Value = group.Count();
                row++;
            }
            
            // Monthly usage (if data available)
            row += 2;
            worksheet.Cells[row, 1].Value = "Monthly Usage";
            worksheet.Cells[row, 1].Style.Font.Bold = true;
            worksheet.Cells[row, 1].Style.Font.Size = 14;
            row++;
            
            var monthlyUsage = pinBatchUsers
                .Where(pbu => pbu.FirstUsedDate.HasValue)
                .GroupBy(pbu => new { pbu.FirstUsedDate!.Value.Year, pbu.FirstUsedDate!.Value.Month })
                .OrderBy(g => g.Key.Year)
                .ThenBy(g => g.Key.Month);
            
            worksheet.Cells[row, 1].Value = "Month";
            worksheet.Cells[row, 2].Value = "New PINs";
            worksheet.Cells[row, 1].Style.Font.Bold = true;
            worksheet.Cells[row, 2].Style.Font.Bold = true;
            row++;
            
            foreach (var group in monthlyUsage)
            {
                worksheet.Cells[row, 1].Value = $"{group.Key.Year}-{group.Key.Month:D2}";
                worksheet.Cells[row, 2].Value = group.Count();
                row++;
            }
            
            // Auto-fit columns
            worksheet.Cells.AutoFitColumns();
            
            await Task.CompletedTask.ConfigureAwait(false);
        }

        /// <summary>
        /// Creates the overview sheet for multiple batches.
        /// </summary>
        /// <param name="worksheet">The worksheet to populate.</param>
        /// <param name="batches">The batches data.</param>
        /// <param name="batchPins">Dictionary mapping batch IDs to their PINs.</param>
        private async Task CreateOverviewSheetAsync(ExcelWorksheet worksheet, List<PinBatch> batches, Dictionary<Guid, List<PinBatchUser>> batchPins)
        {
            // Headers
            var headers = new[] { "Batch Name", "Subscription Type", "Status", "Total PINs", "Active PINs", "Used PINs", "Expired PINs", "Created Date", "Expiration Date" };
            
            for (int i = 0; i < headers.Length; i++)
            {
                worksheet.Cells[1, i + 1].Value = headers[i];
                worksheet.Cells[1, i + 1].Style.Font.Bold = true;
                worksheet.Cells[1, i + 1].Style.Fill.PatternType = ExcelFillStyle.Solid;
                worksheet.Cells[1, i + 1].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightGray);
            }
            
            // Data
            var now = DateTime.UtcNow;
            for (int i = 0; i < batches.Count; i++)
            {
                var batch = batches[i];
                var pins = batchPins.GetValueOrDefault(batch.Id, new List<PinBatchUser>());
                var row = i + 2;
                
                var activePins = pins.Count(pbu => pbu.IsActive && (!pbu.ExpirationDate.HasValue || pbu.ExpirationDate.Value > now));
                var expiredPins = pins.Count(pbu => pbu.ExpirationDate.HasValue && pbu.ExpirationDate.Value <= now);
                var usedPins = pins.Count(pbu => pbu.UsageCount > 0);
                
                worksheet.Cells[row, 1].Value = batch.Name;
                worksheet.Cells[row, 2].Value = batch.SubscriptionType.ToString();
                worksheet.Cells[row, 3].Value = batch.Status.ToString();
                worksheet.Cells[row, 4].Value = pins.Count;
                worksheet.Cells[row, 5].Value = activePins;
                worksheet.Cells[row, 6].Value = usedPins;
                worksheet.Cells[row, 7].Value = expiredPins;
                worksheet.Cells[row, 8].Value = batch.CreatedDate.ToString("yyyy-MM-dd HH:mm:ss");
                worksheet.Cells[row, 9].Value = batch.ExpirationDate?.ToString("yyyy-MM-dd HH:mm:ss") ?? "N/A";
                
                // Color coding for status
                switch (batch.Status)
                {
                    case BatchStatus.Active:
                        worksheet.Cells[row, 3].Style.Fill.PatternType = ExcelFillStyle.Solid;
                        worksheet.Cells[row, 3].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightGreen);
                        break;
                    case BatchStatus.Suspended:
                        worksheet.Cells[row, 3].Style.Fill.PatternType = ExcelFillStyle.Solid;
                        worksheet.Cells[row, 3].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightYellow);
                        break;
                    case BatchStatus.Expired:
                        worksheet.Cells[row, 3].Style.Fill.PatternType = ExcelFillStyle.Solid;
                        worksheet.Cells[row, 3].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightCoral);
                        break;
                }
            }
            
            // Auto-fit columns
            worksheet.Cells.AutoFitColumns();
            
            await Task.CompletedTask.ConfigureAwait(false);
        }

        /// <summary>
        /// Sanitizes a sheet name for Excel compatibility.
        /// </summary>
        /// <param name="name">The name to sanitize.</param>
        /// <returns>The sanitized name.</returns>
        private static string SanitizeSheetName(string name)
        {
            // Excel sheet names cannot contain certain characters and have length limits
            var sanitized = name.Replace("/", "_")
                               .Replace("\\", "_")
                               .Replace("*", "_")
                               .Replace("?", "_")
                               .Replace("[", "_")
                               .Replace("]", "_")
                               .Replace(":", "_");
            
            // Limit length to 31 characters (Excel limit)
            if (sanitized.Length > 31)
            {
                sanitized = sanitized.Substring(0, 31);
            }
            
            return sanitized;
        }

        /// <summary>
        /// Decrypts a PIN from storage.
        /// </summary>
        /// <param name="encryptedPin">The encrypted PIN.</param>
        /// <returns>The decrypted PIN.</returns>
        private static string DecryptPin(string? encryptedPin)
        {
            if (string.IsNullOrEmpty(encryptedPin))
            {
                return string.Empty;
            }
            
            try
            {
                var bytes = Convert.FromBase64String(encryptedPin);
                return System.Text.Encoding.UTF8.GetString(bytes);
            }
            catch
            {
                return string.Empty;
            }
        }
    }
}
