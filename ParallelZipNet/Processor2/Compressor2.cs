using System.IO;
using System.IO.Compression;

namespace ParallelZipNet.Processor2 {
    public static class Compressor2 {
        public static void Run(BinaryReader reader, BinaryWriter writer) {                        
            using(var gzip = new GZipStream(writer.BaseStream, CompressionMode.Compress)) {
                using(var transfer = new TransferStream(gzip)) {                
                    reader.BaseStream.CopyTo(transfer);
                }
            }
        }
    }
}