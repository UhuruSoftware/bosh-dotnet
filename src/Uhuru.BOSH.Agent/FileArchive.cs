// -----------------------------------------------------------------------
// <copyright file="FileArchive.cs" company="">
// TODO: Update copyright text.
// </copyright>
// -----------------------------------------------------------------------

namespace Uhuru.BOSH.Agent
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.IO;
    using System.Reflection;
    using ICSharpCode.SharpZipLib.Tar;
    using ICSharpCode.SharpZipLib.GZip;

    /// <summary>
    /// TODO: Update summary.
    /// </summary>
    public static class FileArchive
    {
        /// <summary>
        /// Compresses a file using the .tgz format.
        /// </summary>
        /// <param name="sourceDir">The directory to compress.</param>
        /// <param name="fileName">The name of the archive to be created.</param>
        public static void ZipFile(string sourceDir, string fileName)
        {
            using (Stream outStream = File.Create(fileName))
            {
                using (Stream gzoStream = new GZipOutputStream(outStream))
                {
                    using (TarArchive tarArchive = TarArchive.CreateOutputTarArchive(gzoStream))
                    {
                        // Note that the RootPath is currently case sensitive and must be forward slashes e.g. "c:/temp"
                        // and must not end with a slash, otherwise cuts off first char of filename
                        // This is scheduled for fix in next release
                        tarArchive.RootPath = sourceDir.Replace('\\', '/');
                        if (tarArchive.RootPath.EndsWith("/"))
                            tarArchive.RootPath = tarArchive.RootPath.Remove(tarArchive.RootPath.Length - 1);

                        AddDirectoryFilesToTar(tarArchive, sourceDir, true);
                    }
                }
            }
        }


        private static void AddDirectoryFilesToTar(TarArchive tarArchive, string sourceDirectory, bool recurse)
        {

            // Optionally, write an entry for the directory itself.
            // Specify false for recursion here if we will add the directory's files individually.
            //
            TarEntry tarEntry = TarEntry.CreateEntryFromFile(sourceDirectory);
            tarArchive.WriteEntry(tarEntry, false);

            // Write each file to the tar.
            //
            string[] filenames = Directory.GetFiles(sourceDirectory);
            foreach (string filename in filenames)
            {
                tarEntry = TarEntry.CreateEntryFromFile(filename);
                tarArchive.WriteEntry(tarEntry, true);
            }

            if (recurse)
            {
                string[] directories = Directory.GetDirectories(sourceDirectory);
                foreach (string directory in directories)
                    AddDirectoryFilesToTar(tarArchive, directory, recurse);
            }
        }

        /// <summary>
        /// Extracts data from a .zip archive.
        /// </summary>
        /// <param name="targetDir">The directory to put the extracted data in.</param>
        /// <param name="zipFile">The file to extract data from.</param>
        public static void UnzipFile(string targetDir, string zipFile)
        {
            using (Stream inStream = File.OpenRead(zipFile))
            {
                using (Stream gzipStream = new GZipInputStream(inStream))
                {
                    using (TarArchive tarArchive = TarArchive.CreateInputTarArchive(gzipStream))
                    {                        
                        tarArchive.ExtractContents(targetDir);
                    }
                }
            }
        }
    }
}
