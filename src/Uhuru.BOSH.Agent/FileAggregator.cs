// -----------------------------------------------------------------------
// <copyright file="FileAggregator.cs" company="Uhuru Software, Inc.">
// Copyright (c) 2011 Uhuru Software, Inc., All Rights Reserved
// </copyright>
// -----------------------------------------------------------------------

namespace Uhuru.BOSH.Agent
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
using System.IO;
    using System.Globalization;

    /// <summary>
    /// TODO: Update summary.
    /// </summary>
    public class FileAggregator
    {
        public FileAggregator()
        {
            this.UsedDirectories = new List<string>();
        }

        // Generates a tarball including all the requested entries
        // @return tarball path
        public string GenerateTarball()
        {
            string tmpDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
            try
            {
                // TODO: check if space left?
                Directory.CreateDirectory(tmpDir);

                string outDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
                Directory.CreateDirectory(outDir);

                this.UsedDirectories.Add(outDir);

                CopyFiles(tmpDir);

                string tarballPath = Path.Combine(outDir, "files.tgz");

                FileArchive.ZipFile(tmpDir, tarballPath);

                return tarballPath;

            }
            finally
            {
                if (!string.IsNullOrEmpty(tmpDir) && Directory.Exists(tmpDir))
                {
                    Directory.Delete(tmpDir, true);
                }
            }
        }

        public void Cleanup()
        {
            foreach (string dir in this.UsedDirectories)
            {
                if (Directory.Exists(dir))
                {
                    Directory.Delete(dir, true);
                }
            }
        }

        public int CopyFiles(string dstDirectory)
        {
            if (this.Matcher == null)
            {
                throw new InvalidOperationException("no matcher provided");
            }

            if (!Directory.Exists(this.Matcher.BaseDir))
            {
                throw new InvalidOperationException(string.Format(CultureInfo.InvariantCulture, "Base directory {0} not found", this.Matcher.BaseDir));
            }

            int copied = 0;

            string baseDir = Realpath(this.Matcher.BaseDir);



            foreach (string glob in this.Matcher.Globs)
            {
                //TODO: Improve this using patterns
                SearchOption searchOption = SearchOption.TopDirectoryOnly;
                string searchPattern = glob.Split('/')[1];
                if (glob.Contains("**"))
                {
                    searchOption = SearchOption.AllDirectories;
                }

                foreach (string file in Directory.GetFiles(baseDir, searchPattern, searchOption))
                {
                    string dstFilename = Path.Combine(dstDirectory, Path.GetFileName(file));
                    Directory.CreateDirectory(Path.GetDirectoryName(file));

                    File.Copy(file, dstFilename);

                    copied++;
                }
            }

            return copied;
        }

        private static string Realpath(string path)
        {
            return Path.GetFullPath(path);
        }

        private IList<string> UsedDirectories;

        public FileMatcher Matcher { get; set; }
    }
}
