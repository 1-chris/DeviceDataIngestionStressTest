using System.Runtime.InteropServices;
using System.Text.Json;


int numberOfClients = 100;
string httpEndpoint = "https://placeholder/DeviceDataEndpoint";
string authToken = "placeholder";

if (args.Length == 3)
{
    int.TryParse(args[0], out numberOfClients);
    httpEndpoint = args[1];
    authToken = args[2];
}

List<BogusDataClient> bogusDataClients = new();

for (int i = 0; i < numberOfClients; i++)
{
    bogusDataClients.Add(new BogusDataClient(httpEndpoint, authToken, i));
}

foreach (BogusDataClient bogusDataClient in bogusDataClients) 
{
    Console.WriteLine($"Initialising bogus data client: {bogusDataClient.ComputerName}");
    bogusDataClient.Start();
}

for (int i = 0; i < 30; i++)
{
    Console.WriteLine("@@@@@@@@@@@@@@@@@@@@@@@@ Finished initialising clients @@@@@@@@@@@@@@@@@@@@@@@@");
}

while (true) 
{ 
    Task.Delay(1000);
}

public class BogusDataClient
{
    private readonly string _computerNamePrefix = "FakeHost-";
    private readonly int _dataCollectionInterval = 60000;
    public string Endpoint { get; }
    public string DeviceId { get; } = Guid.Empty.ToString();
    public string AzureADDeviceId { get; } = Guid.Empty.ToString();
    public string AuthorizationToken { get; }
    public string ComputerName { get; }
    public DateTime LastBootUpTime { get; set; }
    public int TotalPhysicalMemoryMB { get; set; }


    public BogusDataClient(string endpoint, string authToken, int preSeed)
    {
        Random random = new Random(1337+preSeed);
        Endpoint = endpoint;
        AuthorizationToken = authToken;
        ComputerName = _computerNamePrefix + random.Next(1000,1000000).ToString("D7");

        DateTime currentDate = DateTime.UtcNow;
        int daysAgo = random.Next(2, 100);

        TimeSpan timeAgo = TimeSpan.FromDays(daysAgo);
        long ticksAgo = timeAgo.Ticks;

        long randomTicks = (long)(random.NextDouble() * ticksAgo);
        TimeSpan randomTimeSpan = new TimeSpan(randomTicks);
        LastBootUpTime = currentDate.Subtract(timeAgo).Add(randomTimeSpan);

        TotalPhysicalMemoryMB = (int)Math.Round(random.Next(1000,256000) / 1000d, 0) * 1000;
    }

    public async void Start()
    {
        Random random = new();
        int randomStartDelay = random.Next(1, 120000);
        await Task.Delay(randomStartDelay);

        Console.WriteLine("-------  SENDING FIRST REQUEST -------");
        while (true)
        {
            var data = new 
            {
                deviceId = DeviceId,
                azureAdDeviceId = AzureADDeviceId,
                ComputerName = ComputerName,
                LastBootUpTime = LastBootUpTime,
                UptimeTotalDays = (DateTime.Now - LastBootUpTime).TotalDays,
                FreePhysicalMemoryMB = random.Next((int)Math.Round((long)TotalPhysicalMemoryMB*0.85), (int)Math.Round((long)TotalPhysicalMemoryMB*0.98)),
                TotalPhysicalMemoryMB = TotalPhysicalMemoryMB,
                CpuLoad = random.Next(0,60),
                ProcessCount = random.Next(100,140),
                FreeStorage = random.Next(1000,1200),
                PingMs = random.Next(1,10),
                OSEnvironment = RuntimeInformation.OSDescription,
            };

            string jsonData = JsonSerializer.Serialize(data);
            Console.WriteLine($"Sending data {jsonData}");

            using (HttpClient httpClient = new())
            {
                httpClient.DefaultRequestHeaders.Add("Authorization", AuthorizationToken);
                httpClient.DefaultRequestHeaders.Add("DeviceId", DeviceId);
                httpClient.Timeout = new TimeSpan(0, 5, 0);
                
                _ = await httpClient.PostAsync(Endpoint, new StringContent(jsonData, System.Text.Encoding.UTF8, "application/json"));
            }

            Console.WriteLine("Response received");

            await Task.Delay(_dataCollectionInterval);
        }
    }
}
