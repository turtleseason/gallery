namespace Gallery.Util
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Security;

    using Gallery.Entities;

    public interface IFileSystemUtil
    {
        IEnumerable<GalleryFile>? GetFiles(string path);

        IEnumerable<DriveInfo>? GetAvailableDrives();

        IEnumerable<string>? GetDirectories(string path);
    }

    // General I/O exceptions https://docs.microsoft.com/en-us/dotnet/standard/io/handling-io-errors
    //
    // Todo:    - Can exception handling be reused between methods at all?
    //          - Log exceptions? (They already get printed in the debug output though)
    public class FileSystemUtil : IFileSystemUtil
    {
        /// Returns the list of files in the given directory, or null if reading the directory fails.
        public IEnumerable<GalleryFile>? GetFiles(string path)
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

            return paths?.Select(path => new GalleryFile { FullPath = path });
        }

        /// Returns the given directory's list of subdirectories, or null if an I/O error occurred.
        public IEnumerable<string>? GetDirectories(string path)
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

        /// Returns the available drives on the system, or null if an I/O error occurred.
        //
        // Todo: return GalleryFile objects like GetFiles?
        public IEnumerable<DriveInfo>? GetAvailableDrives()
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
    }
}
