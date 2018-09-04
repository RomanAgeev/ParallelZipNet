using System.IO;
using System.IO.Compression;

namespace ParallelZipNet.Processor2 {
    public static class Compressor2 {
        public static void Run(BinaryReader reader, BinaryWriter writer) {                        
            using(var reduce = new TransferStream("reduce", writer.BaseStream))
            using(var gzip = new GZipStream(reduce, CompressionMode.Compress))
            // using(var gzip2 = new GZipStream(reduce, CompressionMode.Compress))
            using(var map = new TransferStream("map", gzip/*, gzip2*/)) {
                reader.BaseStream.CopyTo(map);
            }
        }
    }
}
