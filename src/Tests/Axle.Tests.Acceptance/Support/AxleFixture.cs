﻿namespace Axle.Tests.Acceptance.Support
{
    using System;
    using System.Diagnostics;
    using System.Globalization;
    using System.IO;
    using System.Net;
    using System.Net.Http;
    using System.Threading;
    using Microsoft.Extensions.Configuration;

    // NOTE (Marta): Based on Ironclad. This is used to create a shared
    // context between a number of tests. This shared context will include
    // a running instance of Axle. For more information on shared context in XUnit,
    // go to: https://xunit.github.io/docs/shared-context#collection-fixture
    public sealed class AxleFixture : IDisposable
    {
        private readonly Process axleProcess;

        public AxleFixture()
        {
            var config = new ConfigurationBuilder().AddJsonFile("testsettings.json").Build();
            this.AxleUrl = config.GetValue<Uri>("axleUrl");

            this.axleProcess = this.StartAxle();
        }

        public Uri AxleUrl { get; }

        public void Dispose()
        {
            try
            {
                this.axleProcess.Kill();
            }
            catch (InvalidOperationException)
            {
            }

            this.axleProcess.Dispose();
        }

        [DebuggerStepThrough]
        private Process StartAxle()
        {
            var path = string.Format(
                CultureInfo.InvariantCulture,
                "..{0}..{0}..{0}..{0}..{0}Axle{0}Axle.csproj",
                Path.DirectorySeparatorChar);

            Process.Start(
                new ProcessStartInfo("dotnet", $"run -p {path}")
                {
                    UseShellExecute = true,
                });

            return Process.GetProcessById(this.GetProcessIdFromAxleWebApi());
        }

        private int GetProcessIdFromAxleWebApi()
        {
            using (var client = new HttpClient())
            {
                for (var attempt = 1; attempt <= 20; attempt++)
                {
                    Thread.Sleep(500);
                    try
                    {
                        using (var response = client.GetAsync(new Uri(this.AxleUrl, "api/status")).GetAwaiter().GetResult())
                        {
                            if (response.StatusCode == HttpStatusCode.OK)
                            {
                                return int.Parse(response.Content.ReadAsStringAsync().GetAwaiter().GetResult(), CultureInfo.InvariantCulture);
                            }
                        }

                        break;
                    }
                    catch (HttpRequestException)
                    {
                        if (attempt >= 20)
                        {
                            throw;
                        }
                    }
                }
            }

            throw new InvalidOperationException("Axle is not responding.");
        }
    }
}
