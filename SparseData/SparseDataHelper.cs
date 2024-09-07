/* Copyright (C) 2024 Louie Velarde. All rights reserved.
 * 
 * You can redistribute this program and/or modify it under the terms of
 * the GNU Lesser Public License as published by the Free Software Foundation,
 * either version 3 of the License, or (at your option) any later version.
 */
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace SparseConverter
{
    public class SparseDataHelper
    {
        public static void DecompressSparse(Stream dat, Stream output, Stream transferList)
        {
            (int version, _, var commands) = ParseTransferList(transferList);

            switch (version)
            {
                case 1: Console.WriteLine("Android Lollipop 5.0 detected!"); break;
                case 2: Console.WriteLine("Android Lollipop 5.1 detected!"); break;
                case 3: Console.WriteLine("Android Marshmallow 6.x detected!"); break;
                case 4: Console.WriteLine("Android Nougat 7.x / Oreo 8.x detected!"); break;
                default: throw new InvalidDataException($"Unsupported Version: {version}");
            }

            const int BLOCK_SIZE = 4096;
            int maxBlock = 0;
            byte[] buffer = new byte[BLOCK_SIZE];

            foreach ((string cmd, int[] blocks) in commands)
            {
                maxBlock = Math.Max(maxBlock, blocks.Max());
                if (!"new".Equals(cmd))
                {
                    Console.WriteLine($"Skipping command {cmd}...");
                    continue;
                }

                for (int i = 1, len = blocks.Length; i < len; i += 2)
                {
                    int beg = blocks[i];
                    int end = blocks[i + 1];
                    int blockCount = end - beg;

                    Console.WriteLine($"Copying {blockCount} blocks into position {beg}...");
                    output.Seek(1L * beg * BLOCK_SIZE, SeekOrigin.Begin);

                    while (blockCount > 0)
                    {
                        output.Write(buffer, 0, dat.Read(buffer, 0, BLOCK_SIZE));
                        --blockCount;
                    }
                }
            }
            output.Flush();

            long fileSize = 1L * maxBlock * BLOCK_SIZE;
            if (output.Length < fileSize)
            {
                output.SetLength(fileSize);
            }
        }

        private static Tuple<int, int, List<Tuple<string, int[]>>> ParseTransferList(Stream transferList)
        {
            var EXPECTED_COMMANDS = Array.AsReadOnly(new string[] { "erase", "new", "zero" });

            using (var sr = new StreamReader(transferList))
            {
                int version = int.Parse(sr.ReadLine());
                int newBlocks = int.Parse(sr.ReadLine());

                if (version >= 2)
                {
                    sr.ReadLine(); // Number of stash entries that are needed simultaneously
                    sr.ReadLine(); // Maximum number of blocks that will be stashed simultaneously
                }

                var commands = new List<Tuple<string, int[]>>();
                for (string line; (line = sr.ReadLine()) != null;)
                {
                    string[] parts = line.Split(' ');
                    string cmd = parts[0];

                    if (EXPECTED_COMMANDS.Contains(cmd))
                    {
                        int[] args = Array.ConvertAll(parts[1].Split(','), int.Parse);
                        if (args.Length != args[0] + 1)
                        {
                            throw new InvalidDataException($"Invalid Command: {line}");
                        }
                        commands.Add(Tuple.Create(cmd, args));
                    }
                    else
                    {
                        if (!int.TryParse(cmd, out _))
                        {
                            throw new InvalidDataException($"Unsupported Command: {cmd}");
                        }
                    }
                }
                return Tuple.Create(version, newBlocks, commands);
            }
        }
    }
}
