using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Security.Cryptography;

namespace AniDBmini.HashAlgorithms
{
    public class Ed2k : HashAlgorithm
    {
        public const int BLOCKSIZE = 9728000;

        public bool BlueIsRed { get { return blueIsRed; } } private bool blueIsRed;
        public byte[] RedHash { get { var hash = Hash; return redHash != null ? (byte[])redHash.Clone() : hash; } } private byte[] redHash;
        public byte[] BlueHash { get { var hash = Hash; return blueHash != null ? (byte[])blueHash.Clone() : hash; } } private byte[] blueHash;

        public event FileHashingProgressHandler FileHashingProgress = delegate { };

        private byte[] nullArray = new byte[0];
        private byte[] nullMd4Hash;

        private List<byte[]> md4HashBlocks;
        private int missing = BLOCKSIZE;
        private Md4 md4;

        private bool isHashing;
        private long size, totalBytesRead;

        public Ed2k()
        {
            md4HashBlocks = new List<byte[]>();
            md4 = new Md4();

            nullMd4Hash = md4.ComputeHash(nullArray);
            md4.Initialize();
        }

        public override bool CanReuseTransform { get { return true; } }

        public new byte[] ComputeHash(Stream stream)
        {
            size = stream.Length;
            totalBytesRead = 0;

            isHashing = true;

            // Default the buffer size to 4K.
            byte[] buffer = new byte[4096];
            int bytesRead;
            do
            {
                bytesRead = stream.Read(buffer, 0, 4096);
                if (bytesRead > 0)
                {
                    HashCore(buffer, 0, bytesRead);
                }
            } while (bytesRead > 0 && isHashing);

            if (!isHashing)
            {
                Initialize();
                return null;
            }

            HashValue = HashFinal();
            byte[] Tmp = (byte[])HashValue.Clone();
            Initialize();

            return Tmp;
        }

        public void Cancel()
        {
            isHashing = false;
        }

        protected override void HashCore(byte[] b, int offset, int length)
        {
            while (length != 0)
            {
                totalBytesRead += length;

                if (length < missing)
                {
                    md4.TransformBlock(b, offset, length, null, 0);
                    missing -= length;
                    length = 0;

                    if (missing % (4096 * 512) == 0)
                        FileHashingProgress(this, new FileHashingProgressArgs(totalBytesRead, size));
                }
                else
                {
                    md4.TransformFinalBlock(b, offset, missing);
                    md4HashBlocks.Add(md4.Hash);
                    md4.Initialize();

                    length -= missing;
                    offset += missing;
                    missing = BLOCKSIZE;
                }
            }
        }


        /// <summary>Calculates both ed2k hashes</summary>
        /// <returns>Always returns the red hash</returns>
        protected override byte[] HashFinal()
        {
            blueIsRed = false;
            redHash = null;
            blueHash = null;

            if (md4HashBlocks.Count == 0)
            {
                md4.TransformFinalBlock(nullArray, 0, 0);
                blueHash = md4.Hash;
            }
            else if (md4HashBlocks.Count == 1 && missing == BLOCKSIZE)
            {
                blueHash = md4HashBlocks[0];

                md4.TransformBlock(md4HashBlocks[0], 0, 16, null, 0);
                md4.TransformFinalBlock(md4.ComputeHash(nullArray), 0, 16);
                redHash = md4.Hash;

            }
            else
            {
                if (missing != BLOCKSIZE)
                {
                    md4.TransformFinalBlock(nullArray, 0, 0);
                    md4HashBlocks.Add(md4.Hash);
                }

                md4.Initialize();
                foreach (var md4HashBlock in md4HashBlocks) md4.TransformBlock(md4HashBlock, 0, 16, null, 0);
                var state = md4.GetState();

                md4.TransformFinalBlock(nullArray, 0, 0);
                blueHash = md4.Hash;

                if (missing == BLOCKSIZE)
                {
                    md4.Initialize(state);
                    md4.TransformFinalBlock(nullMd4Hash, 0, 16);
                    redHash = md4.Hash;
                }
            }

            if (redHash == null) blueIsRed = true;
            return redHash == null ? blueHash : redHash;
        }

        public override void Initialize()
        {
            isHashing = false;
            missing = BLOCKSIZE;
            md4.Initialize();
            md4HashBlocks.Clear();
        }
    }

    public delegate void FileHashingProgressHandler(object sender, FileHashingProgressArgs e);
    public class FileHashingProgressArgs
    {
        private double bytesRead, totalBytes;

        public FileHashingProgressArgs(double _r, double _t)
        {
            bytesRead = _r;
            totalBytes = _t;
        }

        public double ProcessedSize { get { return bytesRead; } }
        public double TotalSize { get { return totalBytes; } }
    }
}