using System;
using System.Collections.Generic;
using System.Linq;
using SpacebuxServ.Utils.Extensions;
using SpacebuxServ.Utils.Numerics;
using HashLib;
using Org.BouncyCastle.Crypto.Digests;

namespace SpacebuxServ.Cryptology.Algorithms
{
    public class M7 : IHashAlgorithm
    {
        public uint Multiplier { get; private set; }

        private readonly IHash _hashSha256;
        private readonly IHash _hashSha512;
		private readonly SpacebuxServ.Cryptology.Algorithms.SHA3Managed _hashSha3;
        private readonly IHash _hashWhirlpool;
        private readonly IHash _hashHavel256;
        private readonly TigerDigest _hashTiger;
        private readonly IHash _hashRipeMd160;

        public M7()
        {
            _hashSha256 = HashFactory.Crypto.CreateSHA256();
            _hashSha512 = HashFactory.Crypto.CreateSHA512();
            _hashWhirlpool = HashFactory.Crypto.CreateWhirlpool();
            _hashHavel256 = HashFactory.Crypto.CreateHaval_5_256();
            _hashRipeMd160 = HashFactory.Crypto.CreateRIPEMD160();

			_hashSha3 = new SpacebuxServ.Cryptology.Algorithms.SHA3Managed(512);
            _hashTiger = new TigerDigest();

			Multiplier = (UInt32)Math.Pow(2,16);
        }

        public byte[] Hash(byte[] input, dynamic config)
        {
            var hashResults = new List<byte[]>();

            hashResults.Add(_hashSha256.ComputeBytes(input).GetBytes());
            hashResults.Add(_hashSha512.ComputeBytes(input).GetBytes());
            hashResults.Add(_hashSha3.ComputeHash(input));
            hashResults.Add(_hashWhirlpool.ComputeBytes(input).GetBytes());
            hashResults.Add(_hashHavel256.ComputeBytes(input).GetBytes());

            _hashTiger.Reset();
            _hashTiger.BlockUpdate(input, 0, input.Length);
            var tigerHash = new byte[_hashTiger.GetDigestSize()];
            _hashTiger.DoFinal(tigerHash, 0);

            hashResults.Add(tigerHash);
            hashResults.Add(_hashRipeMd160.ComputeBytes(input).GetBytes());
            
            var intResults = hashResults.Select(HashToBigInt);

            BigInteger product=1;

            foreach (var intResult in intResults)
            {
                product = product * (intResult == 0 ? new BigInteger(1) : intResult);
                var bp = 123;
            }

            var productBytes = product.ToByteArray();

            var result=HashFactory.Crypto.CreateSHA256().ComputeBytes(productBytes).GetBytes();

            return result;
        }

        private BigInteger HashToBigInt(byte[] hash)
        {
            return new BigInteger(hash.Append(new byte[]{0}));
        }
    }
}
