using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using Utilities;

namespace SparseConverter
{
    class Program
    {
        static void Main(string[] args)
        {
            args = CommandLineParser.GetCommandLineArgsIgnoreEscape();
            if (args.Length < 2)
            {
                PrintHelp();
                return;
            }

            string inputPath = args[1];
            if (string.Equals(args[0], "/c", StringComparison.InvariantCultureIgnoreCase) ||
                string.Equals(args[0], "/compress", StringComparison.InvariantCultureIgnoreCase))
            {
                if (args.Length == 4)
                {
                    long maxSparseSize = ParseStandardSizeString(args[3]);
                    long minSparseSize = SparseHeader.Length + 3 * ChunkHeader.Length + SparseCompressionHelper.BlockSize;
                    if (maxSparseSize < minSparseSize)
                    {
                        throw new ArgumentException("<max-sparse-size> must be greater than 66KB");
                    }
                    Compress(inputPath, args[2], maxSparseSize);
                    return;
                }
            }
            else if (string.Equals(args[0], "/d", StringComparison.InvariantCultureIgnoreCase) ||
                string.Equals(args[0], "/decompress", StringComparison.InvariantCultureIgnoreCase))
            {
                if (args.Length == 3)
                {
                    var sparseList = GetSparseList(inputPath);
                    Decompress(sparseList, args[2]);
                    return;
                }
                if (args.Length == 4)
                {
                    Decompress(inputPath, args[2], args[3]);
                    return;
                }
            }
            else if (string.Equals(args[0], "/stats", StringComparison.InvariantCultureIgnoreCase))
            {
                if (args.Length == 2)
                {
                    PrintSparseImageStatistics(inputPath);
                    return;
                }
            }
            PrintHelp();
        }

        private static void PrintHelp()
        {
            Console.WriteLine("SparseConverter v" + System.Reflection.Assembly.GetExecutingAssembly().GetName().Version);
            Console.WriteLine("Copyright 2014 Tal Aloni (tal.aloni.il@gmail.com)");
            Console.WriteLine("Copyright 2024 Louie Velarde");
            Console.WriteLine();
            Console.WriteLine("Usage:");
            Console.WriteLine("SparseConverter /c[ompress] <image-path> <output-folder> <max-sparse-size>");
            Console.WriteLine("SparseConverter /d[ecompress] <first-sparse-path> <output-image-path>");
            Console.WriteLine("SparseConverter /d[ecompress] <dat-path> <output-image-path> <transfer-list-path>");
            Console.WriteLine("SparseConverter /stats <sparse-path>");
        }

        private static long ParseStandardSizeString(string value)
        {
            if (value.ToUpper().EndsWith("TB"))
            {
                return (long)1024 * 1024 * 1024 * 1024 * Conversion.ToInt64(value.Substring(0, value.Length - 2), -1);
            }
            else if (value.ToUpper().EndsWith("GB"))
            {
                return 1024 * 1024 * 1024 * Conversion.ToInt64(value.Substring(0, value.Length - 2), -1);
            }
            else if (value.ToUpper().EndsWith("MB"))
            {
                return 1024 * 1024 * Conversion.ToInt64(value.Substring(0, value.Length - 2), -1);
            }
            else if (value.ToUpper().EndsWith("KB"))
            {
                return 1024 * Conversion.ToInt64(value.Substring(0, value.Length - 2), -1);
            }
            if (value.ToUpper().EndsWith("B"))
            {
                return Conversion.ToInt64(value.Substring(0, value.Length - 1), -1);
            }
            else
            {
                return Conversion.ToInt64(value, -1);
            }
        }

        private static void Compress(string inputPath, string outputPath, long maxSparseSize)
        {
            using (var input = new FileStream(inputPath, FileMode.Open))
            {
                if (input.Length % SparseCompressionHelper.BlockSize > 0)
                {
                    throw new InvalidDataException($"{inputPath} size is not a multiple of {SparseCompressionHelper.BlockSize} bytes.");
                }
                Directory.CreateDirectory(outputPath);

                string prefix = $"{Path.GetFileName(inputPath)}_sparsechunk";

                int sparseIndex = 0;
                bool complete;
                do
                {
                    string sparsePath = Path.Combine(outputPath, $"{prefix}{++sparseIndex}");
                    using (var output = new FileStream(sparsePath, FileMode.CreateNew))
                    {
                        Console.WriteLine($"Writing {Path.GetFullPath(sparsePath)}...");
                        complete = SparseCompressionHelper.WriteCompressedSparse(input, output, maxSparseSize);
                    }
                }
                while (!complete);
            }
        }

        private static List<string> GetSparseList(string inputPath)
        {
            var sparseList = new List<string>();

            var regex = new Regex(@"(.+?)(\d+)");
            var match = regex.Match(inputPath);

            if (match.Success)
            {
                string prefix = match.Groups[1].Value;

                int sparseIndex = Convert.ToInt32(match.Groups[2].Value);
                string sparsePath = inputPath;
                do
                {
                    sparseList.Add(sparsePath);
                    sparsePath = prefix + ++sparseIndex;
                }
                while (File.Exists(sparsePath));
            }
            else
            {
                sparseList.Add(inputPath);
            }

            return sparseList;
        }

        private static void Decompress(List<string> sparseList, string outputPath)
        {
            string outputDirectory = Path.GetDirectoryName(Path.GetFullPath(outputPath));
            if (outputDirectory != null && outputDirectory.Length > 0)
            {
                Directory.CreateDirectory(outputDirectory);
            }

            using (var output = new FileStream(outputPath, FileMode.CreateNew))
            {
                Console.WriteLine($"Writing {Path.GetFullPath(outputPath)}...");
                foreach (string sparsePath in sparseList)
                {
                    using (var input = new FileStream(sparsePath, FileMode.Open))
                    {
                        Console.WriteLine($"Processing {Path.GetFullPath(sparsePath)}...");
                        SparseDecompressionHelper.DecompressSparse(input, output);
                    }
                }
                Console.WriteLine("DONE!");
            }
        }

        private static void Decompress(string inputPath, string outputPath, string transferListPath)
        {
            string outputDirectory = Path.GetDirectoryName(Path.GetFullPath(outputPath));
            if (outputDirectory != null && outputDirectory.Length > 0)
            {
                Directory.CreateDirectory(outputDirectory);
            }

            using (FileStream
                input = new FileStream(inputPath, FileMode.Open),
                output = new FileStream(outputPath, FileMode.CreateNew),
                transferList = new FileStream(transferListPath, FileMode.Open))
            {
                Console.WriteLine($"Writing {Path.GetFullPath(outputPath)}...");
                SparseDataHelper.DecompressSparse(input, output, transferList);
                Console.WriteLine("DONE!");
            }
        }

        private static void PrintSparseImageStatistics(string path)
        {
            using (var stream = new FileStream(path, FileMode.Open))
            {
                var sparseHeader = SparseHeader.Read(stream) ?? throw new InvalidDataException("Invalid Sparse Image Format");
                Console.WriteLine("Total Blocks: " + sparseHeader.TotalBlocks);
                Console.WriteLine("Total Chunks: " + sparseHeader.TotalChunks);

                long outputSize = 0;
                for (uint index = 0; index < sparseHeader.TotalChunks; ++index)
                {
                    var chunkHeader = ChunkHeader.Read(stream);
                    Console.Write($"Chunk Type: {chunkHeader.ChunkType}, Size: {chunkHeader.ChunkSize}, Total Size: {chunkHeader.TotalSize}");

                    int dataLength = (int)(chunkHeader.ChunkSize * sparseHeader.BlockSize);
                    switch (chunkHeader.ChunkType)
                    {
                        case ChunkType.Raw:
                            SparseDecompressionHelper.ReadBytes(stream, dataLength);
                            Console.WriteLine();
                            outputSize += dataLength;
                            break;
                        case ChunkType.Fill:
                            byte[] fillBytes = SparseDecompressionHelper.ReadBytes(stream, 4);
                            uint fill = LittleEndianConverter.ToUInt32(fillBytes, 0);
                            Console.WriteLine($", Value: 0x{fill:X8}");
                            outputSize += dataLength;
                            break;
                        case ChunkType.DontCare:
                            Console.WriteLine();
                            break;
                        case ChunkType.CRC:
                            byte[] crcBytes = SparseDecompressionHelper.ReadBytes(stream, 4);
                            uint crc = LittleEndianConverter.ToUInt32(crcBytes, 0);
                            Console.WriteLine($", Value: 0x{crc:X8}");
                            break;
                        default:
                            Console.WriteLine();
                            Console.WriteLine("Error: Invalid Chunk Type");
                            return;
                    }
                }
                Console.WriteLine($"Output Size: {outputSize}");
            }
        }
    }
}
