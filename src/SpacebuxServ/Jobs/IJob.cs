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
using System.Collections.Generic;
using SpacebuxServ.Cryptology.Algorithms;
using SpacebuxServ.Cryptology.Merkle;
using SpacebuxServ.Daemon.Responses;
using SpacebuxServ.Shares;
using SpacebuxServ.Transactions;
using SpacebuxServ.Utils.Numerics;

namespace SpacebuxServ.Jobs
{
    public interface IJob : IEnumerable<object>
    {
        UInt64 Id { get; }

        UInt64 Height { get; }

        string PreviousBlockHash { get; }

        string PreviousBlockHashReversed { get;  }

        string CoinbaseInitial { get; }

        string CoinbaseFinal { get; }

        string Version { get; }

        string EncodedDifficulty { get; }

        BigInteger Target { get; }

		string TargetString { get; }

        double Difficulty { get; }

        string nTime { get; }

        bool CleanJobs { get; set; }

        IHashAlgorithm HashAlgorithm { get; }

        IBlockTemplate BlockTemplate { get; }

        IGenerationTransaction GenerationTransaction { get; }

        string AccountRootHash { get; }

        string AccountRootHashReversed { get; }

        IMerkleTree MerkleTree { get; }

        new IEnumerator<object> GetEnumerator();

        bool RegisterShare(IShare share);
    }
}
