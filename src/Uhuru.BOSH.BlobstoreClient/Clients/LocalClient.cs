﻿// -----------------------------------------------------------------------
// <copyright file="LocalClient.cs" company="">
// TODO: Update copyright text.
// </copyright>
// -----------------------------------------------------------------------

namespace Uhuru.BOSH.BlobstoreClient.Clients
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.IO;

    /// <summary>
    /// TODO: Update summary.
    /// </summary>
    public class LocalClient : BaseClient
    {
        //  class LocalClient < BaseClient
        //    CHUNK_SIZE = 1024*1024

        //    def initialize(options)
        //      super(options)
        //      @blobstore_path = @options[:blobstore_path]
        //      raise "No blobstore path given" if @blobstore_path.nil?
        //      FileUtils.mkdir_p(@blobstore_path) unless File.directory?(@blobstore_path)
        //    end

        //    def create_file(file)
        //      id = UUIDTools::UUID.random_create.to_s
        //      dst = File.join(@blobstore_path, id)
        //      File.open(dst, 'w') do |fh|
        //        until file.eof?
        //          fh.write(file.read(CHUNK_SIZE))
        //        end
        //      end
        //      id
        //    end

        //    def get_file(id, file)
        //      src = File.join(@blobstore_path, id)

        //      begin
        //        File.open(src, 'r') do |src_fh|
        //          until src_fh.eof?
        //            file.write(src_fh.read(CHUNK_SIZE))
        //          end
        //        end
        //      end
        //    rescue Errno::ENOENT
        //      raise NotFound, "Blobstore object '#{id}' not found"
        //    end

        //    def delete(id)
        //      file = File.join(@blobstore_path, id)
        //      FileUtils.rm(file)
        //    rescue Errno::ENOENT
        //      raise NotFound, "Blobstore object '#{id}' not found"
        //    end

        //  end
        //end

        public LocalClient(object options)
            : base(options)
        {
        }

        public override string Create(string contents)
        {
            throw new NotImplementedException();
        }

        public override string Get(string id)
        {
            throw new NotImplementedException();
        }

        public override void Get(string id, FileInfo filePath)
        {
            throw new NotImplementedException();
        }

        public override void Delete(string id)
        {
            throw new NotImplementedException();
        }

    }
}
