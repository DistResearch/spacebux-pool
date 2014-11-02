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
using System.Globalization;
using System.Reflection;
using System.Threading;
using SpacebuxServ.Factories;
using SpacebuxServ.Repository;
using SpacebuxServ.Utils;
using SpacebuxServ.Utils.Commands;
using SpacebuxServ.Utils.Platform;
using SpacebuxServ.Utils.Versions;
using Nancy.TinyIoc;
using Serilog;

namespace SpacebuxServ
{
    class Program
    {
        /// <summary>
        /// Used for uptime calculations.
        /// </summary>
        public static readonly DateTime StartupTime = DateTime.Now; // used for uptime calculations.

        private static ILogger _logger;

        static void Main(string[] args)
        {
            AppDomain.CurrentDomain.UnhandledException += UnhandledExceptionHandler; // Catch any unhandled exceptions if we are in release mode.

            // use invariant culture - we have to set it explicitly for every thread we create to 
            // prevent any file-reading problems (mostly because of number formats).
            Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;

            // start the ioc kernel.
            var kernel = TinyIoCContainer.Current;
            new Bootstrapper(kernel);
            var objectFactory = kernel.Resolve<IObjectFactory>();
            var configFactory = kernel.Resolve<IConfigFactory>();

            // print intro texts.
            ConsoleWindow.PrintBanner();
            ConsoleWindow.PrintLicense();

            // load the config-manager.
            var configManager = configFactory.GetConfigManager();

            // initialize log-manager as we'll need it below.
            objectFactory.GetLogManager();

            // print a version banner.
            _logger = Log.ForContext<Program>();
            _logger.Information("SpacebuxServ {0:l} {1:l} warming-up..", VersionInfo.CodeName, Assembly.GetAssembly(typeof(Program)).GetName().Version);
            PlatformManager.PrintPlatformBanner();

            // initialize config manager.
            configManager.Initialize();

            // initialize metrics support    
            objectFactory.GetMetricsManager();

            // start pool manager.
            var poolManager = objectFactory.GetPoolManager();
            poolManager.Run();

            // start web server.
            objectFactory.GetWebServer();

            while (true) // idle loop & command parser
            {
                var line = Console.ReadLine();
                CommandManager.Parse(line);
            }
        }

        #region unhandled exception emitter

        /// <summary>
        /// Unhandled exception emitter.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private static void UnhandledExceptionHandler(object sender, UnhandledExceptionEventArgs e)
        {
            var exception = e.ExceptionObject as Exception;

            if (exception == null) // if we can't get the exception, whine about it.
                throw new ArgumentNullException("e");

            if (e.IsTerminating)
            {
                _logger.Fatal(exception, "Terminating because of unhandled exception!");
                #if !DEBUG // prevent console window from being closed when we are in development mode.
                    Environment.Exit(-1);
                #endif
            }
            else
                _logger.Error(exception, "Caught unhandled exception");
        }

        #endregion
    }
}
