﻿#region License
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
using System.Diagnostics;
using System.Linq;
using AustinHarris.JsonRpc;
using SpacebuxServ.Blocks;
using SpacebuxServ.Daemon;
using SpacebuxServ.Jobs.Tracker;
using SpacebuxServ.Persistance;
using SpacebuxServ.Pools.Config;
using SpacebuxServ.Server.Mining.Stratum;
using SpacebuxServ.Server.Mining.Stratum.Errors;
using SpacebuxServ.Server.Mining.Vanilla;
using SpacebuxServ.Utils.Extensions;
using Serilog;

namespace SpacebuxServ.Shares
{
    public class ShareManager : IShareManager
    {
        public event EventHandler BlockFound;

        public event EventHandler ShareSubmitted;

        private readonly IJobTracker _jobTracker;

        private readonly IDaemonClient _daemonClient;

        private readonly IStorage _storage;

        private readonly IBlockProcessor _blockProcessor;

        private readonly IPoolConfig _poolConfig;

        private readonly ILogger _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="ShareManager" /> class.
        /// </summary>
        /// <param name="poolConfig"></param>
        /// <param name="daemonClient"></param>
        /// <param name="jobTracker"></param>
        /// <param name="storage"></param>
        /// <param name="blockProcessor"></param>
        public ShareManager(IPoolConfig poolConfig, IDaemonClient daemonClient, IJobTracker jobTracker, IStorage storage, IBlockProcessor blockProcessor)
        {
            _poolConfig = poolConfig;
            _daemonClient = daemonClient;
            _jobTracker = jobTracker;
            _storage = storage;
            _blockProcessor = blockProcessor;
            _logger = Log.ForContext<ShareManager>().ForContext("Component", poolConfig.Coin.Name);
        }

        /// <summary>
        /// Processes the share.
        /// </summary>
        /// <param name="miner">The miner.</param>
        /// <param name="jobId">The job identifier.</param>
        /// <param name="extraNonce2">The extra nonce2.</param>
        /// <param name="nTimeString">The n time string.</param>
        /// <param name="nonceString">The nonce string.</param>
        /// <returns></returns>
        public IShare ProcessShare(IStratumMiner miner, string jobId, string extraNonce2, string nTimeString, string nonceString)
        {
            // check if the job exists
            var id = Convert.ToUInt64(jobId, 16);
            var job = _jobTracker.Get(id);

            // create the share
            var share = new Share(miner, id, job, extraNonce2, nTimeString, nonceString);

            if (share.IsValid)
                HandleValidShare(share);
            else
                HandleInvalidShare(share);

            OnShareSubmitted(new ShareEventArgs(miner));  // notify the listeners about the share.

            return share;
        }

        public IShare ProcessShare(IVanillaMiner miner, string data)
        {
            throw new NotImplementedException();
        }

        private void HandleValidShare(IShare share)
        {
            var miner = (IStratumMiner) share.Miner;
            miner.ValidShares++;

            _storage.AddShare(share); // commit the share.
            _logger.Debug("Share accepted at {0:0.00}/{1} by miner {2:l}", share.Difficulty, miner.Difficulty, miner.Username);

            // check if share is a block candidate
            if (!share.IsBlockCandidate)
                return;
            
            // submit block candidate to daemon.
            var accepted = SubmitBlock(share);

            // log about the acceptance status of the block.
            _logger.Information(
                accepted
                    ? "Found block [{0}] with hash: {1:l}"
                    : "Submit block [{0}] failed with hash: {1:l}",
                share.Height, share.BlockHash.ToHexString());

            if (!accepted) // if block wasn't accepted
                return; // just return as we don't need to notify about it and store it.

            OnBlockFound(EventArgs.Empty); // notify the listeners about the new block.

            _storage.AddBlock(share); // commit the block details to storage.
        }

        private void HandleInvalidShare(IShare share)
        {
            var miner = (IStratumMiner)share.Miner;
            miner.InvalidShares++;

            JsonRpcException exception = null; // the exception determined by the stratum error code.
            switch (share.Error)
            {
                case ShareError.DuplicateShare:
                    exception = new DuplicateShareError(share.Nonce);                    
                    break;
                case ShareError.IncorrectExtraNonce2Size:
                    exception = new OtherError("Incorrect extranonce2 size");
                    break;
                case ShareError.IncorrectNTimeSize:
                    exception = new OtherError("Incorrect nTime size");
                    break;
                case ShareError.IncorrectNonceSize:
                    exception = new OtherError("Incorrect nonce size");
                    break;
                case ShareError.JobNotFound:
                    exception = new JobNotFoundError(share.JobId);
                    break;
                case ShareError.LowDifficultyShare:
				exception = new LowDifficultyShare(share.Difficulty,((IStratumMiner)share.Miner).Difficulty);
                    break;
                case ShareError.NTimeOutOfRange:
                    exception = new OtherError("nTime out of range");
                    break;
            }
            JsonRpcContext.SetException(exception); // set the stratum exception within the json-rpc reply.

            Debug.Assert(exception != null); // exception should be never null when the share is marked as invalid.
            _logger.Debug("Rejected share by miner {0:l}, reason: {1:l}", miner.Username, exception.message);
        }

        private bool SubmitBlock(IShare share)
        {
            // TODO: we should try different submission techniques and probably more then once: https://github.com/ahmedbodi/stratum-mining/blob/master/lib/bitcoin_rpc.py#L65-123

            try
            {
                _daemonClient.SubmitBlock(share.BlockHex.ToHexString());

                var block = _blockProcessor.GetBlock(share.BlockHash.ToHexString()); // query the block.

                if (block == null) // make sure the block exists
                    return false;

                if (block.Confirmations == -1) // make sure the block is accepted.
                {
                    _logger.Debug("Submitted block {0:l} is orphaned", block.Hash);
                    return false;
                }

                var expectedTxHash = share.CoinbaseTxId.Bytes.ReverseBuffer().ToHexString(); // calculate our expected generation transactions's hash
                var genTxHash = block.Tx.First(); // read the hash of very first (generation transaction) of the block

                if (expectedTxHash != genTxHash) // make sure our calculated generated transaction and one reported by coin daemon matches.
                {
                    _logger.Debug("Submitted block {0:l} doesn't seem to belong us as reported generation transaction hash [{1:l}] doesn't match our expected one [{2:l}]", block.Hash, genTxHash, expectedTxHash);
                    return false;
                }

                var genTx = _blockProcessor.GetGenerationTransaction(block); // get the generation transaction.

                var poolOutput = _blockProcessor.GetPoolOutput(genTx); // get the output that targets pool's central address.

                // make sure the blocks generation transaction contains our central pool wallet address
                if (poolOutput == null)
                {
                    _logger.Debug("Submitted block doesn't seem to belong us as generation transaction doesn't contain an output for pool's central wallet address: {0:}", _poolConfig.Wallet.Adress);
                    return false;
                }

                // if the code flows here, then it means the block was succesfully submitted and belongs to us.
                share.SetFoundBlock(block, genTx); // assign the block to share.

                return true;
            }
            catch (Exception e)
            {
                _logger.Error("Submit block failed - height: {0}, hash: {1:l} - {2:l}", share.Height, share.BlockHash.ToHexString(), e.Message);
                return false;
            }
        }

        private void OnBlockFound(EventArgs e)
        {
            var handler = BlockFound;

            if (handler != null)
                handler(this, e);
        }

        private void OnShareSubmitted(EventArgs e)
        {
            var handler = ShareSubmitted;

            if (handler != null)
                handler(this, e);
        }
    }
}
