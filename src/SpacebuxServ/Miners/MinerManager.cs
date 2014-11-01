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
using System.Collections.Generic;
using System.Linq;
using SpacebuxServ.Daemon;
using SpacebuxServ.Daemon.Exceptions;
using SpacebuxServ.Pools;
using SpacebuxServ.Pools.Config;
using SpacebuxServ.Server.Mining.Stratum;
using SpacebuxServ.Server.Mining.Stratum.Sockets;
using SpacebuxServ.Server.Mining.Vanilla;
using Serilog;

namespace SpacebuxServ.Miners
{
    public class MinerManager : IMinerManager
    {
        public IList<IMiner> Miners { get { return _miners.Values.ToList(); } }

        public event EventHandler MinerAuthenticated;

        private readonly Dictionary<int, IMiner> _miners;        

        private int _counter = 0; // counter for assigining unique id's to miners.

        private readonly IMinerConfig _minerConfig;

        private readonly IMetaConfig _metaConfig;

        private readonly IDaemonClient _daemonClient;

        private readonly IStratumServerConfig _stratumServerConfig;

        private readonly ILogger _logger;        

        public MinerManager(IPoolConfig poolConfig, IDaemonClient daemonClient)
        {
            _minerConfig = poolConfig.Miner;
            _metaConfig = poolConfig.Meta;
            _daemonClient = daemonClient;
            _stratumServerConfig = poolConfig.Stratum;

            _miners = new Dictionary<int, IMiner>();
            _logger = Log.ForContext<MinerManager>().ForContext("Component", poolConfig.Coin.Name);
        }

        public IMiner GetMiner(Int32 id)
        {
            return _miners.ContainsKey(id) ? _miners[id] : null;
        }

        public IMiner GetByConnection(IConnection connection)
        {
            return (from pair in _miners  // returned the miner associated with the given connection.
                let client = (IClient) pair.Value 
                where client.Connection == connection 
                select pair.Value).FirstOrDefault();
        }

        public T Create<T>(IPool pool) where T : IVanillaMiner
        {
            var @params = new object[]
            {
                _counter++,
                pool,
                this
            };

            var instance = Activator.CreateInstance(typeof(T), @params); // create an instance of the miner.
            var miner = (IVanillaMiner)instance;
            _miners.Add(miner.Id, miner); // add it to our collection.

            return (T)miner;
        }

        public T Create<T>(UInt32 extraNonce, IConnection connection, IPool pool) where T : IStratumMiner
        {
            var @params = new object[]
            {
                _counter++, 
                extraNonce, 
                connection, 
                pool, 
                this
            };

            var instance = Activator.CreateInstance(typeof(T), @params);  // create an instance of the miner.
            var miner = (IStratumMiner)instance;
            _miners.Add(miner.Id, miner); // add it to our collection.           

            return (T)miner;
        }

        public void Remove(IConnection connection)
        {
            var miner = (from pair in _miners // find the miner associated with the connection.
                let client = (IClient)pair.Value 
                where client.Connection == connection 
                select pair.Value).FirstOrDefault();

            if (miner != null)
                _miners.Remove(miner.Id);
        }

        public void Authenticate(IMiner miner)
        {
            if (_minerConfig.ValidateUsername) // should we validate miner username?
                try
                {
                    miner.Authenticated = _daemonClient.ValidateAddress(miner.Username).IsValid; // if so validate it against coin daemon as an address.
                }
                catch (RpcException)
                {
                    miner.Authenticated = false;
                }
            else
                miner.Authenticated = true; // else just accept him.

            _logger.Information(
                miner.Authenticated ? "Authenticated miner: {0:l} [{1:l}]" : "Miner authentication failed: {0:l} [{1:l}]",
                miner.Username, ((IClient) miner).Connection.RemoteEndPoint);

            if (!miner.Authenticated) 
                return;

            if (miner is IStratumMiner)
            {
                var stratumMiner = (IStratumMiner) miner;
                stratumMiner.SetDifficulty(_stratumServerConfig.Diff); // set the initial difficulty for the miner and send it.
                stratumMiner.SendMessage(_metaConfig.MOTD); // send the motd.
            }
           
            OnMinerAuthenticated(new MinerEventArgs(miner)); // notify listeners about the new authenticated miner.            
        }

        // todo: consider exposing this event by miner object itself.
        protected virtual void OnMinerAuthenticated(MinerEventArgs e)
        {
            var handler = MinerAuthenticated;

            if (handler != null)
                handler(this, e);
        }
    }
}
