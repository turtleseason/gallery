namespace Gallery.Util
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;

    using Gallery.Entities;

    public interface IFileSystemUtil
    {
        IEnumerable<GalleryFile>? GetFiles(string path);

        IEnumerable<DriveInfo>? GetAvailableDrives();

        IEnumerable<string>? GetDirectories(string path);

        void DeleteDirectory(string path);
    }

    // Log exceptions? (They already get printed in the debug output though)

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
            catch (Exception e) when (IsIoException(e))
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
            catch (Exception e) when (IsIoException(e))
            {
                childDirectories = null;
            }

            return childDirectories;
        }

        /// Returns the available drives on the system, or null if an I/O error occurred.
        public IEnumerable<DriveInfo>? GetAvailableDrives()
        {
            IEnumerable<DriveInfo>? drives;
            try
            {
                drives = DriveInfo.GetDrives().Where(driveInfo => driveInfo.IsReady);
            }
            catch (Exception e) when (IsIoException(e))
            {
                drives = null;
            }

            return drives;
        }

        /// Attempts to delete the directory and any files in it.
        /// (Assumes the directory does not contain any other directories.)
        public void DeleteDirectory(string path)
        {
            try
            {
                IEnumerable<string> paths = Directory.EnumerateFiles(path);
                foreach (string filePath in paths)
                {
                    File.Delete(filePath);
                }
            }
            catch (Exception e) when (IsIoException(e))
            {
                Trace.TraceError($"Failed to delete files in {path}.");
            }

            try
            {
                Directory.Delete(path);
            }
            catch (Exception e) when (IsIoException(e))
            {
                Trace.TraceError($"Failed to delete {path}.");
            }
        }

        /// General I/O exceptions https://docs.microsoft.com/en-us/dotnet/standard/io/handling-io-errors
        private bool IsIoException(Exception e)
        {
            return e is FileNotFoundException
                     or DirectoryNotFoundException
                     or DriveNotFoundException
                     or PathTooLongException
                     or OperationCanceledException
                     or UnauthorizedAccessException
                     or IOException;
        }
    }
}
