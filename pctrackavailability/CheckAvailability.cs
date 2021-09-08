using System;
using System.Net.Http; 

using System.Diagnostics; 
using Microsoft.ApplicationInsights; 
using Microsoft.ApplicationInsights.Channel; 
using Microsoft.ApplicationInsights.DataContracts; 
using Microsoft.ApplicationInsights.Extensibility; 
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;

using Microsoft.Extensions.Logging;
using System.Threading.Tasks;



namespace pctrackavailability
{
    public static class CheckAvailability
    {
        private static TelemetryClient telemetryClient; 
        [FunctionName("CheckAvailability")]
        public static async Task Run([TimerTrigger("0 */5 * * * *")]TimerInfo myTimer, ILogger log,  ExecutionContext executionContext)
        {
            log.LogInformation($"C# Timer trigger function executed at: {DateTime.Now}");
            if (telemetryClient == null) 
            { 
                // Initializing a telemetry configuration for Application Insights based on connection string 

                var telemetryConfiguration = new TelemetryConfiguration(); 
                telemetryConfiguration.ConnectionString = Environment.GetEnvironmentVariable("APPLICATIONINSIGHTS_CONNECTION_STRING"); 
                telemetryConfiguration.TelemetryChannel = new InMemoryChannel(); 
                telemetryClient = new TelemetryClient(telemetryConfiguration); 
            } 
            log.LogInformation($"Telemetry Client has been initialized");
            string testName = executionContext.FunctionName; 
            log.LogInformation($"testName={testName}");
            string location = Environment.GetEnvironmentVariable("REGION_NAME");
            log.LogInformation($"location={location}");
            var availability = new AvailabilityTelemetry 
            { 
                Name = testName, 

                RunLocation = location, 

                Success = false, 
            }; 
            log.LogInformation($"AvailabilityTelemetry has been set");
            
            availability.Context.Operation.ParentId = Activity.Current.SpanId.ToString();
            log.LogInformation($"ParentId={availability.Context.Operation.ParentId}");
            availability.Context.Operation.Id = Activity.Current.RootId; 
            log.LogInformation($"OperationId={availability.Context.Operation.Id}");
            var stopwatch = new Stopwatch(); 
            stopwatch.Start(); 
            log.LogInformation($"StopWatch has started");
            try 
            { 
                var activity = new Activity("AvailabilityContext");
                if (activity != null)
                { 
                    activity.Start(); 
                    availability.Id = Activity.Current.SpanId.ToString(); 
                    // Run business logic 
                    await RunAvailabilityTestAsync(log);
                    activity.Stop();
                } 
                availability.Success = true; 
            } 

            catch (Exception ex) 
            { 
                availability.Message = ex.Message; 
                throw; 
            } 

            finally 
            { 
                stopwatch.Stop(); 
                log.LogInformation($"StopWatch has stopped");
                availability.Duration = stopwatch.Elapsed; 
                availability.Timestamp = DateTimeOffset.UtcNow; 
                telemetryClient.TrackAvailability(availability); 
                telemetryClient.Flush(); 
            } 
        }

        public async static Task RunAvailabilityTestAsync(ILogger log) 
        { 
            log.LogInformation($"Attempting to run Availability Test");
            using (var httpClient = new HttpClient()) 
            { 
                try{
                    string BASEURL = Environment.GetEnvironmentVariable("PETCLINIC_BASEURL"); 
                    HttpResponseMessage response = await httpClient.GetAsync($"{BASEURL}"); 
                    response.EnsureSuccessStatusCode();
                    response = await httpClient.GetAsync($"{BASEURL}/owners/find"); 
                    response.EnsureSuccessStatusCode();
                    response = await httpClient.GetAsync($"{BASEURL}/owners?lastName=Selenium"); 
                    response.EnsureSuccessStatusCode();
                }
                catch(HttpRequestException e)
                {
                     log.LogError("Message: {0}", e.Message)
                }
            } 
        } 
    }
}
