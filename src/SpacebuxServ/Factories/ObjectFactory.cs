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

using SpacebuxServ.Banning;
using SpacebuxServ.Blocks;
using SpacebuxServ.Cryptology.Algorithms;
using SpacebuxServ.Daemon;
using SpacebuxServ.Jobs.Manager;
using SpacebuxServ.Jobs.Tracker;
using SpacebuxServ.Logging;
using SpacebuxServ.Metrics;
using SpacebuxServ.Miners;
using SpacebuxServ.Payments;
using SpacebuxServ.Persistance;
using SpacebuxServ.Pools;
using SpacebuxServ.Pools.Config;
using SpacebuxServ.Repository.Context;
using SpacebuxServ.Server.Mining;
using SpacebuxServ.Server.Mining.Service;
using SpacebuxServ.Server.Web;
using SpacebuxServ.Shares;
using SpacebuxServ.Statistics;
using SpacebuxServ.Vardiff;
using Nancy.Bootstrapper;
using Nancy.TinyIoc;

namespace SpacebuxServ.Factories
{
    /// <summary>
    /// Object factory that creates instances of objects
    /// </summary>
    public class ObjectFactory:IObjectFactory
    {
        #region context

        /// <summary>
        /// The application context for internal use.
        /// </summary>
        private readonly IApplicationContext _applicationContext;

        /// <summary>
        /// Initializes a new instance of the <see cref="ObjectFactory" /> class.
        /// </summary>
        /// <param name="applicationContext">The application context.</param>
        public ObjectFactory(IApplicationContext applicationContext)
        {
            _applicationContext = applicationContext;
        }

        #endregion

        #region hash algorithms

        /// <summary>
        /// Returns instance of the given hash algorithm.
        /// </summary>
        /// <param name="algorithm"></param>
        /// <returns></returns>
        public IHashAlgorithm GetHashAlgorithm(string algorithm)
        {
            return _applicationContext.Container.Resolve<IHashAlgorithm>(algorithm);
        }

        public IPoolManager GetPoolManager()
        {
            return _applicationContext.Container.Resolve<IPoolManager>();
        }

        public IPool GetPool(IPoolConfig poolConfig)
        {
            var @params = new NamedParameterOverloads
            {
                {"poolConfig", poolConfig},                
            };

            return _applicationContext.Container.Resolve<IPool>(@params);
        }

        #endregion

        #region pool objects

        /// <summary>
        /// Returns a new instance of daemon client.
        /// </summary>
        /// <returns></returns>
        public IDaemonClient GetDaemonClient(IPoolConfig poolConfig)
        {
            var @params = new NamedParameterOverloads
            {
                {"poolConfig", poolConfig}              
            };

            return _applicationContext.Container.Resolve<IDaemonClient>(@params);
        }

        public IMinerManager GetMinerManager(IPoolConfig poolConfig, IDaemonClient daemonClient)
        {
            var @params = new NamedParameterOverloads
            {
                {"poolConfig", poolConfig},
                {"daemonClient", daemonClient},
            };

            return _applicationContext.Container.Resolve<IMinerManager>(@params);
        }

        public IJobManager GetJobManager(IPoolConfig poolConfig, IDaemonClient daemonClient, IJobTracker jobTracker, IShareManager shareManager,
            IMinerManager minerManager, IHashAlgorithm hashAlgorithm)
        {
            var @params = new NamedParameterOverloads
            {
                {"poolConfig", poolConfig},
                {"daemonClient", daemonClient},
                {"jobTracker", jobTracker},
                {"shareManager", shareManager},
                {"minerManager", minerManager},
                {"hashAlgorithm", hashAlgorithm},
            };

            return _applicationContext.Container.Resolve<IJobManager>(@params);
        }

        public IJobTracker GetJobTracker()
        {
            return _applicationContext.Container.Resolve<IJobTracker>();
        }

        public IShareManager GetShareManager(IPoolConfig poolConfig, IDaemonClient daemonClient, IJobTracker jobTracker, IStorage storage, IBlockProcessor blockProcessor)
        {
            var @params = new NamedParameterOverloads
            {
                {"poolConfig", poolConfig},
                {"daemonClient", daemonClient},
                {"jobTracker", jobTracker},
                {"storage", storage},
                {"blockProcessor", blockProcessor}
            };

            return _applicationContext.Container.Resolve<IShareManager>(@params);
        }

        public IPaymentProcessor GetPaymentProcessor(IPoolConfig poolConfig, IDaemonClient daemonClient, IStorage storage, IBlockProcessor blockProcessor)
        {
            var @params = new NamedParameterOverloads
            {
                {"poolConfig", poolConfig},
                {"daemonClient", daemonClient},
                {"storage", storage},
                {"blockProcessor", blockProcessor},
            };

            return _applicationContext.Container.Resolve<IPaymentProcessor>(@params);
        }

        public IBlockProcessor GetBlockProcessor(IPoolConfig poolConfig, IDaemonClient daemonClient)
        {
            var @params = new NamedParameterOverloads
            {
                {"poolConfig", poolConfig},
                {"daemonClient", daemonClient},           
            };

            return _applicationContext.Container.Resolve<IBlockProcessor>(@params);
        }

        public IBanManager GetBanManager(IPoolConfig poolConfig, IShareManager shareManager)
        {
            var @params = new NamedParameterOverloads
            {
                {"poolConfig", poolConfig},
                {"shareManager", shareManager},                
            };

            return _applicationContext.Container.Resolve<IBanManager>(@params);
        }

        public IVardiffManager GetVardiffManager(IPoolConfig poolConfig, IShareManager shareManager)
        {
            var @params = new NamedParameterOverloads
            {
                {"poolConfig", poolConfig},
                {"shareManager", shareManager},
            };

            return _applicationContext.Container.Resolve<IVardiffManager>(@params);
        }

        #endregion

        #region pool statistics objects

        public IStatistics GetStatistics()
        {
            return _applicationContext.Container.Resolve<IStatistics>();
        }

        public IGlobal GetGlobalStatistics()
        {
            return _applicationContext.Container.Resolve<IGlobal>();
        }

        public IAlgorithms GetAlgorithmStatistics()
        {
            return _applicationContext.Container.Resolve<IAlgorithms>();
        }

        public IPools GetPoolStats()
        {
            return _applicationContext.Container.Resolve<IPools>();
        }

        public IPerPool GetPerPoolStats(IPoolConfig poolConfig, IDaemonClient daemonClient, IMinerManager minerManager, IHashAlgorithm hashAlgorithm, IBlocksCount blockStatistics, IStorage storage)
        {
            var @params = new NamedParameterOverloads
            {
                {"poolConfig", poolConfig},
                {"daemonClient", daemonClient},
                {"minerManager",minerManager},
                {"hashAlgorithm", hashAlgorithm},
                {"blockStatistics", blockStatistics},
                {"storage", storage},
            };

            return _applicationContext.Container.Resolve<IPerPool>(@params);
        }

        public ILatestBlocks GetLatestBlocks(IStorage storage)
        {
            var @params = new NamedParameterOverloads
            {
                {"storage", storage}
            };

            return _applicationContext.Container.Resolve<ILatestBlocks>(@params);
        }

        public IBlocksCount GetBlockStats(ILatestBlocks latestBlocks, IStorage storage)
        {
            var @params = new NamedParameterOverloads
            {
                {"latestBlocks", latestBlocks},
                {"storage", storage},
            };

            return _applicationContext.Container.Resolve<IBlocksCount>(@params);
        }

        #endregion

        #region server & service objects

        public IMiningServer GetMiningServer(string type, IPoolConfig poolConfig, IPool pool, IMinerManager minerManager, IJobManager jobManager,
            IBanManager banManager)
        {
            var @params = new NamedParameterOverloads
            {
                {"poolConfig", poolConfig},
                {"pool", pool},
                {"minerManager", minerManager},
                {"jobManager", jobManager},
                {"banManager", banManager},
            };

            return _applicationContext.Container.Resolve<IMiningServer>(type, @params);
        }

        public IRpcService GetMiningService(string type, IPoolConfig poolConfig, IShareManager shareManager, IDaemonClient daemonClient)
        {
            var @params = new NamedParameterOverloads
            {
                {"poolConfig", poolConfig},
                {"shareManager", shareManager}, 
                {"daemonClient", daemonClient}
            };

            return _applicationContext.Container.Resolve<IRpcService>(type, @params);
        }

        public IWebServer GetWebServer()
        {
            return _applicationContext.Container.Resolve<IWebServer>();
        }

        public INancyBootstrapper GetWebBootstrapper()
        {
            return _applicationContext.Container.Resolve<INancyBootstrapper>();
        }

        #endregion

        #region other objects

        public IStorage GetStorage(string type, IPoolConfig poolConfig)
        {
            var @params = new NamedParameterOverloads
            {
                {"poolConfig", poolConfig}
            };

            return _applicationContext.Container.Resolve<IStorage>(type, @params);
        }

        public ILogManager GetLogManager()
        {
            return _applicationContext.Container.Resolve<ILogManager>();
        }

        public IMetricsManager GetMetricsManager()
        {
            return _applicationContext.Container.Resolve<IMetricsManager>();
        }

        #endregion
    }
}
