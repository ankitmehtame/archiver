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
        /// <summary>
        /// Archives files.null Can thrown IO exceptions.
        /// </summary>
        /// <param name="sourceRoot">Source directory (should exist)</param>
        /// <param name="destRoot">Destination directory (should exist)</param>
        /// <param name="extensions">File extensions that should be archived e.g. ".mp4|.mpeg|.avi"</param>
        /// <returns>0 on success, 1 on directory not found.</returns>
        public int Archive(string sourceRoot, string destRoot, string extensions = ".*")
        {
            Log.Information($"Archiving started from source {sourceRoot} to destination {destRoot}");
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
            var topLevelDirs = Directory.GetDirectories(destRoot);
            foreach (var topLevelDir in topLevelDirs)
            {
                var allFiles = Directory.GetFiles(topLevelDir, "*.*", new EnumerationOptions { RecurseSubdirectories = false });
                var groups = allFiles
                .Where(f => Regex.IsMatch(Path.GetExtension(f), extensions))
                .GroupBy(f =>
                {
                    var fileName = Path.GetFileNameWithoutExtension(f);
                    if (fileName.EndsWith("."))
                    {
                        fileName = fileName.TrimEnd('.');
                    }
                    if (fileName.Length > 19)
                    {
                        var dateTimeText = fileName.Substring(fileName.Length - 19, 19);
                        // "2019-08-14_22-29-18"
                        try
                        {
                            var dateTime = DateTimeOffset.ParseExact(dateTimeText, "yyyy-MM-dd_HH-mm-ss", CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal);
                            var date = dateTime.Date;
                            return (DateTime?)date;
                        }
                        catch(Exception)
                        {
                            Log.Warning($"Top Level Dir: {topLevelDir}, File: {f}, FileNameWithoutExt: {fileName}, DateTimeText: {dateTimeText}");
                            throw;
                        }
                    }
                    return null;
                });

                var totalItems = allFiles.Length;
                int currentNum = 0;
                foreach (var group in groups.Where(g => g.Key != null).OrderBy(g => g.Key))
                {
                    var dateText = group.Key.Value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
                    var groupDirFullName = Path.Combine(destRoot, Path.GetDirectoryName(topLevelDir), dateText);
                    if (!Directory.Exists(groupDirFullName))
                    {
                        Log.Debug($"Creating folder {groupDirFullName} with path {groupDirFullName}");
                        var groupDir = Directory.CreateDirectory(groupDirFullName);
                    }
                    foreach (var file in group.OrderBy(f => f))
                    {
                        currentNum++;
                        var perc = Math.Round(currentNum * 100m / totalItems, 2);
                        var newFileName = Path.GetFileNameWithoutExtension(file).TrimEnd('.') + Path.GetExtension(file);
                        var newFilePath = Path.Combine(groupDirFullName, newFileName);
                        Log.Debug($"Moving file {Path.GetFileName(file)} to {groupDirFullName}  -  {currentNum}/{totalItems} ({perc}%)");
                        File.Move(file, newFilePath);
                    }
                }
            }
            Log.Information($"Archiving completed");
            return 0;
        }
    }
}