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
using System.IO;
using System.Linq;
using SpacebuxServ.Coin.Coinbase;
using SpacebuxServ.Cryptology;
using SpacebuxServ.Daemon;
using SpacebuxServ.Daemon.Responses;
using SpacebuxServ.Jobs;
using SpacebuxServ.Miners;
using SpacebuxServ.Payments;
using SpacebuxServ.Transactions.Script;
using SpacebuxServ.Utils.Extensions;
using SpacebuxServ.Utils.Helpers.Time;
using SpacebuxServ.Pools.Config;
using Gibbed.IO;

namespace SpacebuxServ.Transactions
{
    // TODO: convert generation transaction to ioc & DI based.

    /// <summary>
    /// A generation transaction.
    /// </summary>
    /// <remarks>
    /// * It has exactly one txin.
    /// * Txin's prevout hash is always 0000000000000000000000000000000000000000000000000000000000000000.
    /// * Txin's prevout index is 0xFFFFFFFF.
    /// More info:  http://bitcoin.stackexchange.com/questions/20721/what-is-the-format-of-coinbase-transaction
    ///             http://bitcoin.stackexchange.com/questions/21557/how-to-fully-decode-a-coinbase-transaction
    ///             http://bitcoin.stackexchange.com/questions/4990/what-is-the-format-of-coinbase-input-scripts
    /// </remarks>
    /// <specification>
    /// https://en.bitcoin.it/wiki/Protocol_specification#tx
    /// https://en.bitcoin.it/wiki/Transactions#Generation
    /// </specification>
    public class GenerationTransaction : IGenerationTransaction
    {
        /// <summary>
        /// Part 1 of the generation transaction.
        /// </summary>
        public byte[] Initial { get; private set; }

        /// <summary>
        /// Part 2 of the generation transaction.
        /// </summary>
        public byte[] Final { get; private set; }

        public IBlockTemplate BlockTemplate { get; private set; }

        public IExtraNonce ExtraNonce { get; private set; }

		/// <summary>
		/// Creates a new instance of generation transaction.
		/// </summary>
		/// <param name="extraNonce">The extra nonce.</param>
		/// <param name="daemonClient">The daemon client.</param>
		/// <param name="blockTemplate">The block template.</param>
		/// <param name="poolConfig">The associated pool's configuration</param>
		/// <remarks>
		/// Reference implementations:
		/// https://github.com/zone117x/node-stratum-pool/blob/b24151729d77e0439e092fe3a1cdbba71ca5d12e/lib/transactions.js
		/// https://github.com/Crypto-Expert/stratum-mining/blob/master/lib/coinbasetx.py
		/// </remarks>
		public GenerationTransaction(IExtraNonce extraNonce, IBlockTemplate blockTemplate)
		{
			BlockTemplate = blockTemplate;
			ExtraNonce = extraNonce;
		}
        
        public void Create()
        {
            //number of coinbase tx bytes upto message
			const int initialTxLength = 0
				+ 4 // version
				+ 1 // txin count - varint(1)
				+ 20 // pubKey - uint160
				+ 8 // amount - uint64
				+ 1 // sigscript len - varint(0)
				+ 1 // txout count - varint(1)
				+ 8 // amount - uint64
				+ 20; // pubKeyHash

            byte[] txBytes = BlockTemplate.CoinbaseTx.HexToByteArray();

            byte[] varintExtraNonceLength = Serializers.VarInt((UInt32)ExtraNonce.ExtraNoncePlaceholder.Length);

            using (var stream = new MemoryStream())
            {
                stream.WriteBytes(txBytes.Take(initialTxLength).ToArray());
                stream.WriteBytes(varintExtraNonceLength);
                Initial = stream.ToArray();
            }

            int finalTxStart = initialTxLength + Serializers.VarInt(16).Length + 16;

            using (var stream = new MemoryStream())
            {
                stream.WriteBytes(txBytes.Slice(finalTxStart,txBytes.Length));
                Final = stream.ToArray();
            }
        }
    }    
}
