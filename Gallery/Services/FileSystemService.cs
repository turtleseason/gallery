/// Used for file system access
/// (A lot of these are just wrappers around System.IO methods with exception handling;
///  technically they don't really need to be in a "service", but it at least keeps the code cleaner elsewhere)

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security;

namespace Gallery.Services
{
    // General I/O exceptions https://docs.microsoft.com/en-us/dotnet/standard/io/handling-io-errors
    //
    // Todo:    - Can exception handling be reused between methods at all?
    //          - Log exceptions? (Something is already printing them in the debug output)
    class FileSystemService
    {
        /// Returns the list of files in the given directory, or null if the given path can't be loaded.
        //
        // (Should it just return an empty list [and log something] if access fails?)
        public static IEnumerable<Models.GalleryFile>? GetFiles(string path)
        {
            IEnumerable<string>? paths;
            try
            {
                paths = Directory.EnumerateFiles(path);
            }
            catch (Exception e) when (e is FileNotFoundException
                                        or DirectoryNotFoundException
                                        or DriveNotFoundException
                                        or PathTooLongException
                                        or OperationCanceledException
                                        or UnauthorizedAccessException
                                        or SecurityException  // Can this still be thrown in .NET 5? Directory.EnumerateFiles() doc includes it
                                        or IOException)
            {
                paths = null;
            }

            return paths?.Select(x => new Models.GalleryFile(x));
        }

        /// Returns the available drives on the system, or null if an I/O error occurred.
        // 
        // Todo: return GalleryFile objects like GetFiles?
        public static IEnumerable<DriveInfo>? GetAvailableDrives()
        {
            IEnumerable<DriveInfo>? drives;
            try
            {
                drives = DriveInfo.GetDrives().Where(driveInfo => driveInfo.IsReady);
            }
            catch (Exception e) when (e is FileNotFoundException
                                        or DirectoryNotFoundException
                                        or DriveNotFoundException
                                        or PathTooLongException
                                        or OperationCanceledException
                                        or UnauthorizedAccessException
                                        or IOException)
            {
                drives = null;
            }

            return drives;
        }

        /// Returns the given directory's list of subdirectories, or null if an I/O error occurred.
        public static IEnumerable<string>? GetDirectories(string path)
        {
            IEnumerable<string>? childDirectories;
            try
            {
                childDirectories = Directory.EnumerateDirectories(path);
            }
            catch (Exception e) when (e is FileNotFoundException
                                        or DirectoryNotFoundException
                                        or DriveNotFoundException
                                        or PathTooLongException
                                        or OperationCanceledException
                                        or UnauthorizedAccessException
                                        or SecurityException
                                        or IOException)
            {
                childDirectories = null;
            }

            return childDirectories;
        }
    }
}
