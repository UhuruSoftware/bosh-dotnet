// -----------------------------------------------------------------------
// <copyright file="CompilePackage.cs" company="Uhuru Software, Inc.">
// Copyright (c) 2011 Uhuru Software, Inc., All Rights Reserved
// </copyright>
// -----------------------------------------------------------------------

namespace Uhuru.BOSH.Agent.Message
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.IO;
    using Uhuru.Utilities;
    using System.Security.Cryptography;
    using Uhuru.BOSH.Agent.Objects;
    using Newtonsoft.Json;
    using Uhuru.BOSH.Agent.Errors;
    using System.Diagnostics;

    /// <summary>
    /// TODO: Update summary.
    /// </summary>
    public class CompilePackage : IMessage
    {
        public bool IsLongRunning()
        {
            return true;
        }

        string blobStroreProvider;
        dynamic blobStoreOptions;
        BlobstoreClient.Clients.IClient blobStoreClient;
        string blobStoreId;
        string sha1;
        string packageName;
        string packageVersion;
        dynamic dependencies;
        int maxDiskUsage = 90;
        string compileBase;
        string installBase;
        string sourceFile;
        string compileDir;
        string logFile;
        FileLogger logger;
        string installDir;
        string compiledPackage;

        public object Process(dynamic args)
        {
            //Initialize
            this.blobStroreProvider = Config.BlobstoreProvider;
            this.blobStoreOptions = Config.BlobstoreOptions;
            this.blobStoreClient = BlobstoreClient.Blobstore.CreateClient(blobStroreProvider, blobStoreOptions);

            this.blobStoreId = args[0].Value.ToString();
            this.sha1 = args[1].Value.ToString();
            this.packageName = args[2].Value.ToString();
            this.packageVersion = args[3].Value.ToString();
            this.dependencies = args[4];

            Directory.CreateDirectory(Path.Combine(Config.BaseDir, @"data", @"tmp"));

            this.compileBase = Path.Combine(Config.BaseDir, "data", "compile");
            this.installBase = Path.Combine(Config.BaseDir, "data", "packages");
            this.logFile = Path.Combine(Config.BaseDir, "data", "tmp", Config.AgentId);
            this.logger = new FileLogger(logFile);

            this.compileDir = Path.Combine(compileBase, packageName);
            this.installDir = Path.Combine(installBase, packageName, packageVersion);
            this.compiledPackage = sourceFile + ".compiled";

            return this.Start();
        }

        private object Start()
        {
            try
            {
                this.InstallDependencies();
                this.GetAndUnpackSourcePackage();
                this.Compile();
                this.Pack();
                var uploadResult = this.Upload();

                var result = new CompileResult() { result = uploadResult };
                return result;
            }
            catch (Exception e)
            {
                this.logger.Warning("Uncaught exception: ", e.ToString());
                throw new MessageHandlerException("Uncaught exception in CompilePackage.", e);
            }
            finally
            {
                this.ClearLogFile();
                this.DeleteTempFiles();
            }
        }

        /// <summary>
        /// Delete the leftover compilation files after a compilation is done. This
        /// is done so that the reuse_compilation_vms option does not fill up a VM.
        /// </summary>
        private void DeleteTempFiles()
        {
            if (Directory.Exists(compileBase))
            {
                Directory.Delete(compileBase, true);
            }

            if (Directory.Exists(installBase))
            {
                Directory.Delete(installBase, true);
            }
        }

        private void InstallDependencies()
        {
            this.logger.Info("Installing dependencies");

            foreach (dynamic dependency in dependencies)
            {
                this.logger.Warning("TODO : install dependencies");
            }
        }

        private void GetSourcePackage()
        {
            string compileTmp = Path.Combine(this.compileBase, "tmp");
            Directory.CreateDirectory(compileTmp);
            this.sourceFile = Path.Combine(compileTmp, this.blobStoreId);
            if (File.Exists(this.sourceFile))
            {
                File.Delete(this.sourceFile);
            }

            this.blobStoreClient.Get(this.blobStoreId, new FileInfo(this.sourceFile));
        }

        private void UnpackSourcePackage()
        {
            if(Directory.Exists(this.compileDir))
            {
                Directory.Delete(this.compileDir, true);
            }
            Directory.CreateDirectory(this.compileDir);

            var sourceFileInfo = new FileInfo(this.sourceFile);

            string tarFile = Path.ChangeExtension(this.sourceFile, "tar");
            FileArchive.UnzipFile(sourceFileInfo.DirectoryName, this.sourceFile);

            if (File.Exists(tarFile))
            {
                FileArchive.UnzipFile(this.compileDir, tarFile);
            }
            else
            {
                FileArchive.UnzipFile(this.compileDir, this.sourceFile);
            }
        }

        // Directories are not good
        private void GetAndUnpackSourcePackage()
        {
            this.logger.Info("Unpacking source packages");
            Util.UnpackBlob(blobStoreId, sha1, compileBase);
        }

        /// <summary>
        /// Compiles this instance.
        /// The complie step will execute the "package" process
        /// File extensions run and the priorites are defined in PATHEXT env variable.
        /// PATHEXT=.COM;.EXE;.BAT;.CMD;.VBS;.VBE;.JS;.JSE;.WSF;.WSH;.MSC
        /// </summary>
        private void Compile()
        {
            if (Directory.Exists(this.installDir))
            {
                Directory.Delete(this.installDir, true);
            }
            Directory.CreateDirectory(this.installDir);
            // TODO: check if enough disk is available

            // Default PATHEXT env var
            string[] execExtensions = new string[] { ".com", ".exe", ".bat", ".cmd", ".vbs", ".vbe", ".js", ".jse", ".wsf", ".wsh", ".msc" };
            string packagingExecutive = "package";

            foreach (var ext in execExtensions)
            {
                if (File.Exists(Path.Combine(compileDir, "package" + ext)))
                {
                    packagingExecutive += ext;
                    break;
                }

                
            }

            if (File.Exists(packagingExecutive))
            {
                logger.Info("Compileing " + this.packageName + " " 

                var pi = new ProcessStartInfo("package");

                pi.CreateNoWindow = true;
                pi.ErrorDialog = false;
                pi.UseShellExecute = false;
                pi.RedirectStandardOutput = true;
                pi.RedirectStandardError = true;
                pi.RedirectStandardInput = true;
                pi.WorkingDirectory = this.compileDir;
                pi.EnvironmentVariables["BOSH_COMPILE_TARGET"] = this.compileDir;
                pi.EnvironmentVariables["BOSH_INSTALL_TARGET"] = this.installDir;

                var pr = System.Diagnostics.Process.Start(pi);
                pr.WaitForExit();

                string output = pr.StandardOutput.ReadToEnd();
                output += pr.StandardError.ReadToEnd();

                if (pr.ExitCode != 0)
                {
                    new MessageHandlerException("Compile Package Failure (exit code: " + pr.ExitCode + ")", output);
                }

                this.logger.Info(output);
            }
        }

        private void Pack()
        {
            this.logger.Info("Packing " + this.packageName + " " + this.packageVersion);
            Util.PackBlob(installDir, compiledPackage);
        }

        /// <summary>
        /// Clears the log file after a compilation runs.  This is needed because if
        /// reuse_compilation_vms is being used then without clearing the log then
        /// the log from each subsequent compilation will include the previous
        /// compilation's output.
        /// </summary>
        /// <param name="logFile">The log file.</param>
        private void ClearLogFile()
        {
            if (File.Exists(this.logFile))
            {
                File.Delete(this.logFile);
            }

            this.logger = new FileLogger(this.logFile);
        }

        private CompileResult.UploadResult Upload()
        {
            this.logger.Info("Uploading compiled package");
            string compiledBlobStoreId = blobStoreClient.Create(new FileInfo(compiledPackage));

            string compiledSha1;
            using (FileStream fs = File.OpenRead(compiledPackage))
            {
                using (SHA1 sha1 = new SHA1CryptoServiceProvider())
                {
                    compiledSha1 = BitConverter.ToString(sha1.ComputeHash(fs)).Replace("-", "").ToLower();
                }
            }

            this.logger.Info("Uploaded " + this.packageName + " " + this.packageVersion + " (sha1: " + compiledSha1 + ", blobstore_id: " + compiledBlobStoreId + ")");

            string compileLogId = blobStoreClient.Create(new FileInfo(logFile));

            CompileResult.UploadResult res = new CompileResult.UploadResult()
            {
                BlobstoreId = compiledBlobStoreId,
                CompileLogId = compileLogId,
                Sha1 = compiledSha1
            };

            return res;
        }
    }
}
