using System;
using HashLib;

namespace SpacebuxServ.Cryptology.Algorithms
{
    public class Whirlpool : IHashAlgorithm
    {
        private readonly IHash _hasher;

        public uint Multiplier { get; private set; }

        public Whirlpool()
        {
            _hasher = HashFactory.Crypto.CreateWhirlpool();

            Multiplier = 1;
        }

        public byte[] Hash(byte[] input, dynamic config)
        {
            return _hasher.ComputeBytes(input).GetBytes();
        }
    }
}