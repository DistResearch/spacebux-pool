#region License
// 
//     CoiniumServ - Crypto Currency Mining Pool Server Software
//     Copyright (C) 2013 - 2014, CoiniumServ Project - http://www.coinium.org
//     http://www.coiniumserv.com - https://github.com/CoiniumServ/CoiniumServ
// 
//     This software is dual-licensed: you can redistribute it and/or modify
//     it under the terms of the GNU General Public License as published by
//     the Free Software Foundation, either version 3 of the License, or
//     (at your option) any later version.
// 
//     This program is distributed in the hope that it will be useful,
//     but WITHOUT ANY WARRANTY; without even the implied warranty of
//     MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//     GNU General Public License for more details.
//    
//     For the terms of this license, see licenses/gpl_v3.txt.
// 
//     Alternatively, you can license this software under a commercial
//     license or white-label it as set out in licenses/commercial.txt.
// 
#endregion

using System;
using System.Linq;
using SpacebuxServ.Cryptology.Algorithms;
using SpacebuxServ.Cryptology.Merkle;
using SpacebuxServ.Daemon;
using SpacebuxServ.Daemon.Responses;
using SpacebuxServ.Transactions.Utils;
using SpacebuxServ.Utils.Extensions;
using SpacebuxServ.Utils.Numerics;
using Newtonsoft.Json;
using Should.Fluent;
using Xunit;

namespace SpacebuxServ.Tests.Cryptology.Hash
{
    public class M7Tests
    {
        [Fact]
        public void TestM7Hash()
        {
            var input = StringToByteArray("9BDA1BFBCE2E6098921610547B3C1F97F539904629619474C1D69FE4BA010000D2B89E58AA3689780C1701EC9F1E6ED9AB52FFA9E548C4AA6B7F341412DBEA604F6F59E372E2051C58D1047C04A1203642B69F7017E73D012EA3ECC1BF8017FB9442185400000000060000000000000049400140000000000100");
            //for (byte i = 0; i < 64; i++)
            //    input[i]=i;

            var hashM7=new M7();

            var result = hashM7.Hash(input,null);

            var knownResult = new Byte[] {0x08,0x5f,0x7c,0xea,0xf7,0x01,0x78,0xba,0x0c,0x88,0x01,0xc5,0xdc,0x7e,0xe2,0x52,0x8d,0xec,0xca,0x43,0x05,0x34,0x92,0x4a,0x97,0xc6,0xec,0xab,0xba,0xca,0x29,0xef};

            result.ToHexString().Should().Equal(knownResult.ToHexString());
        }

        public static byte[] StringToByteArray(string hex)
        {
            return Enumerable.Range(0, hex.Length)
                             .Where(x => x % 2 == 0)
                             .Select(x => Convert.ToByte(hex.Substring(x, 2), 16))
                             .ToArray();
        }
    }
}
