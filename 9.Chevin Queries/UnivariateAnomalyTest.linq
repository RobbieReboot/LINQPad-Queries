<Query Kind="Program">
  <NuGetReference>System.Text.Json</NuGetReference>
  <Namespace>System.Runtime.Serialization.Formatters.Binary</Namespace>
  <Namespace>System.Text.Json</Namespace>
  <Namespace>System.Text.Json.Serialization</Namespace>
  <Namespace>static System.Runtime.InteropServices.JavaScript.JSType</Namespace>
</Query>

void Main(string[] args)
{
	int pointCount = 5000;
	var data = GenerateData(pointCount);

	Console.WriteLine($"Data points : {pointCount}");
	int n = 0;
	//foreach (var point in data)
	//{
	//	Console.WriteLine($"Row: {n++:000}, Time: {point.Item1}, Value: {point.Item2}");
	//}

	var anomalies = DetectAnomalies(data);

	Console.WriteLine($"{anomalies.Count} Anomalies:");
	foreach (var anomaly in anomalies)
	{
		Console.WriteLine($"Row: {anomaly.Item1.row}, Time: {anomaly.Item1.date}, Value: {anomaly.temp:#00.000}");
	}
	
	var normalTemps = data.Except(anomalies);
	Console.WriteLine($"{normalTemps.Count()} Normal temperatures");
	var averageTemp=data.Average(d => d.Item2);
	Console.WriteLine($"Normal Average = {averageTemp}");
}

static List<((int row,DateTime date),double)> GenerateData(int pointCount)
{
	var random = new Random();
	var data = new List<((int row,DateTime date),double)>();
	var value = 80.0;  // starting value

	for (int i = 1; i < pointCount; i++)  // generate 1000 data points
   	{
        // Every 100 points, generate an anomaly
        if (i % 100 == 0)
        {
            value = 80+ ((random.NextDouble() ) * 40);  // generate anomaly base + up to 40
        } else
		{
			value += (random.NextDouble() * 5.0) - 2.5;  // 5 degree random walk
		}
		value = Math.Max(Math.Min(value, 130.0), 30.0);  // clamp to [30.0, 130.0]

		data.Add(((i-1, DateTime.Now.AddSeconds(i)), value));		//faje timestamp
	}

	return data;
}

static List<((int row,DateTime date),double temp)> DetectAnomalies(List<((int row,DateTime date),double)> data)
{
	var anomalies = new List<((int row,DateTime date),double temp)>();
	var movingData = new Queue<double>();
	int windowSize = 5;  // consider 5 points for moving average
	double epsilon = 1.95;
	foreach (var point in data)
	{
		movingData.Enqueue(point.Item2);
		if (movingData.Count > windowSize)
			movingData.Dequeue();  // maintain window size

		var average = movingData.Average();
		var variance = movingData.Select(val => Math.Pow(val - average, 2)).Average();
		var stdDev = Math.Sqrt(variance);

		if (Math.Abs(point.Item2 - average) > stdDev * epsilon)  // if point deviates more than (epsilon) standard deviations
		{
			anomalies.Add(point);  // it's an anomaly
		}
	}

	return anomalies;
}
