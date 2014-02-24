// -----------------------------------------------------------------------
// <copyright file="Helpers.cs" company="">
// TODO: Update copyright text.
// </copyright>
// -----------------------------------------------------------------------

namespace Uhuru.BOSH.Agent.ApplyPlan
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Globalization;
    using Alphaleonis.Win32.Filesystem;

    /// <summary>
    /// Helper Class for apply plan.
    /// </summary>
    public static class Helpers
    {
        public static void ValidateSpec(dynamic spec)
        {
            var required = new string[] { "name", "version", "sha1", "blobstore_id" };
            foreach (var requiredKey in required)
            {
                if (spec[requiredKey] == null)
                {
                    throw new ArgumentException(String.Format(CultureInfo.InvariantCulture, "Invalid spec. {0} is missing", requiredKey));
                }
            }
        }

        public static void FetchBits(string installPath, string blobstoreId, string checksum)
        {
            Directory.CreateDirectory(installPath);
            BOSH.Agent.Util.UnpackBlob(blobstoreId, checksum, installPath);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Symlink")]
        public static void FetchBitsAndSymlink(string installPath, string linkPath, string blobstoreId, string checksum)
        {
            FetchBits(installPath, blobstoreId, checksum);
            BOSH.Agent.Util.CreateSymLink(installPath, linkPath);
        }
    }


}
