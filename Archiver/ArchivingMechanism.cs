using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Serilog;

namespace Archiver
{
    public class ArchivingMechanism
    {
        public const string DefaultFileNameDateFormat = "*yyyy-MM-dd_HH-mm-ss";
        public const int DefaultRetentionDays = 15;
        public const string DefaultExtensions = ".*";

        /// <summary>
        /// Archives files.null Can thrown IO exceptions.
        /// </summary>
        /// <param name="sourceRoot">Source directory (should exist)</param>
        /// <param name="destRoot">Destination directory (should exist)</param>
        /// <param name="extensions">File extensions that should be archived e.g. ".mp4|.mpeg|.avi"</param>
        /// <param name="retentionDays">Number of days to retain files locally</param>
        /// <param name="fileNameDateFormats">Formats of the file name to extract date from</param>
        /// <param name="recurseSubDirs">Recurse sub directories in source path</param>
        /// <param name="isDemo">If in demo mode, it will not create folders or move files, but will log everything as if it is performing real actions</param>
        /// <returns>0 on success, 1 on directory not found, 2 on date format not specified or not valid</returns>
        public int Archive(string sourceRoot, string destRoot, string[] fileNameDateFormats, string extensions = DefaultExtensions, int retentionDays = DefaultRetentionDays, bool recurseSubDirs = false, bool isDemo = false)
        {
            var fileNameDateFormatsAsText = fileNameDateFormats == null ? string.Empty : string.Join(", ", fileNameDateFormats);
            Log.Debug($"Archive called with {nameof(sourceRoot)}={sourceRoot} {nameof(destRoot)}={destRoot} {nameof(fileNameDateFormats)}=[{fileNameDateFormatsAsText}] {nameof(extensions)}={extensions} {nameof(retentionDays)}={retentionDays} {nameof(recurseSubDirs)}={recurseSubDirs} {nameof(isDemo)}={isDemo}");
            Log.Information($"Archiving started from source {sourceRoot} to destination {destRoot}");
            var today = DateTimeOffset.Now.Date;

            fileNameDateFormats = fileNameDateFormats?.Length > 0 ? fileNameDateFormats : new [] { DefaultFileNameDateFormat };
            
            if (!Directory.Exists(sourceRoot))
            {
                Log.Warning($"Source dir {sourceRoot} does not exist");
                return 1;
            }
            if (!Directory.Exists(destRoot))
            {
                Log.Warning($"Destination dir {destRoot} does not exist");
                return 1;
            }
            foreach(var fileNameDateFormat in fileNameDateFormats)
            {
                if (fileNameDateFormat == null || !((fileNameDateFormat.StartsWith("*") && !fileNameDateFormat.EndsWith("*")) || (!fileNameDateFormat.StartsWith("*") && fileNameDateFormat.EndsWith("*"))))
                {
                    Log.Warning($"File name date format is not valid");
                    return 2;
                }
            }
            var totalItemsSuccessfullyArchived = 0;
            var totalItemsToArchive = 0;
            try
            {
                var topLevelDirs = Directory.GetDirectories(sourceRoot);
                foreach (var topLevelDir in topLevelDirs)
                {
                    var allFiles = Directory.GetFiles(topLevelDir, "*.*", new EnumerationOptions { RecurseSubdirectories = recurseSubDirs });
                    var groups = allFiles
                    .Where(f => Regex.IsMatch(Path.GetExtension(f), extensions))
                    .Select(f => new { FilePath = f, Date = GetDate(GetProperFileNameWithoutExt(f), fileNameDateFormats) })
                    .Where(x => x.Date != null && x.Date < today.Subtract(TimeSpan.FromDays(retentionDays)))
                    .GroupBy(x => x.Date.Value);

                    var minMaxDates = groups.Any() ? new { MinDate = groups.Min(g => g.Key), MaxDate = groups.Max(g => g.Key) } : null;
                    if (minMaxDates != null)
                    {
                        var minDateText = minMaxDates.MinDate.ToString("yyyy-MM-dd");
                        var maxDateText = minMaxDates.MaxDate.ToString("yyyy-MM-dd");
                        Log.Information($"Found oldest file to archive for date {minDateText} and latest for date {maxDateText} in {topLevelDir}");
                    }
                    else
                    {
                        Log.Information($"No file found to archive in {topLevelDir}");
                    }

                    var totalItemsAtThisLevel = allFiles.Length;
                    int currentNum = 0;
                    foreach (var group in groups.OrderBy(g => g.Key))
                    {
                        var dateText = group.Key.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
                        var groupDirFullName = Path.Combine(destRoot, Path.GetFileName(topLevelDir), dateText);
                        if (!Directory.Exists(groupDirFullName))
                        {
                            Log.Information($"Creating folder {groupDirFullName}");
                            if (!isDemo)
                            {
                                Directory.CreateDirectory(groupDirFullName);
                            }
                        }
                        foreach (var item in group.OrderBy(f => f.FilePath))
                        {
                            currentNum++;
                            totalItemsToArchive++;
                            var perc = Math.Round(currentNum * 100m / totalItemsAtThisLevel, 2);
                            var file = item.FilePath;
                            var newFileName = GetProperFileNameWithoutExt(file) + Path.GetExtension(file);
                            var newFilePath = Path.Combine(groupDirFullName, newFileName);
                            Log.Debug($"Moving file {Path.GetFileName(file)} to {groupDirFullName}  -  {currentNum}/{totalItemsAtThisLevel} ({perc}%)");
                            if (!isDemo)
                            {
                                File.Move(file, newFilePath, true);
                            }
                            totalItemsSuccessfullyArchived++;
                        }
                    }
                }
                return 0;
            }
            catch
            {
                Log.Warning("There has been an error during archival process");
                throw;
            }
            finally
            {
                Log.Information($"Archiving completed - archived {totalItemsSuccessfullyArchived} out of {totalItemsToArchive}");
            }
        }

        private static string GetProperFileNameWithoutExt(string filePath)
        {
            var fileName = Path.GetFileNameWithoutExtension(filePath);
            return fileName.TrimEnd('.');
        }

        private DateTime? GetDate(string fileNameWithoutExt, string[] fileNameDateFormats)
        {
            var date = fileNameDateFormats.Select(fileNameDateFormat => GetDate(fileNameWithoutExt, fileNameDateFormat, false))
                .FirstOrDefault(d => d != null);
            if (date == null)
            {
                var formatsAsText = string.Join(", ", fileNameDateFormats);
                Log.Warning($"Could not parse date for FileNameWithoutExt: {fileNameWithoutExt}, DateFormats: {formatsAsText}");
            }
            return date;
        }

        private DateTime? GetDate(string fileNameWithoutExt, string fileNameDateFormat, bool shouldLog)
        {
            string dateText;
            string dateFormat;
            // "*2019-08-14_22-29-18"
            try
            {
                if (fileNameDateFormat.StartsWith("*") && fileNameDateFormat.Length > 1 && fileNameWithoutExt.Length > 1 && fileNameWithoutExt.Length >= fileNameDateFormat.Length - 1)
                {
                    dateFormat = fileNameDateFormat.Substring(1);
                    dateText = fileNameWithoutExt.Substring(fileNameWithoutExt.Length - dateFormat.Length);
                }
                else if (fileNameDateFormat.EndsWith("*") && fileNameDateFormat.Length > 1 && fileNameWithoutExt.Length > 1 && fileNameWithoutExt.Length >= fileNameWithoutExt.Length - 1)
                {
                    dateFormat = fileNameDateFormat.Substring(0, fileNameDateFormat.Length - 1);
                    dateText = fileNameWithoutExt.Substring(0, dateFormat.Length);
                }
                else
                {
                    if (shouldLog)
                    {
                        Log.Warning($"Could not parse date for FileNameWithoutExt: {fileNameWithoutExt}, FileNameDateFormat: {fileNameDateFormat}");
                    }
                    return null;
                }
            }
            catch(Exception ex)
            {
                if (shouldLog)
                {
                    Log.Warning($"Unable to find date text for FileNameWithoutExt: {fileNameWithoutExt}, FileNameDateFormat: {fileNameDateFormat}. Exception - {ex}");
                }
                return null;
            }
            try
            {
                var dateTime = DateTimeOffset.ParseExact(dateText, dateFormat, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal);
                var date = dateTime.Date;
                return (DateTime?)date;
            }
            catch(Exception)
            {
                if (shouldLog)
                {
                  Log.Warning($"Could not parse date for FileNameWithoutExt: {fileNameWithoutExt}, DateText: {dateText}, DateFormat: {dateFormat}");
                }
                return null;
            }
        }
    }
}