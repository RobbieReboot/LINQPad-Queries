<Query Kind="Program">
  <NuGetReference>System.Text.Json</NuGetReference>
  <Namespace>System.Net.Http</Namespace>
  <Namespace>System.Net.Http.Headers</Namespace>
  <Namespace>System.Runtime.Serialization.Formatters.Binary</Namespace>
  <Namespace>System.Text.Json</Namespace>
  <Namespace>System.Text.Json.Serialization</Namespace>
  <Namespace>System.Threading.Tasks</Namespace>
</Query>

static readonly HttpClient client = new HttpClient();
static readonly string organization = "chevin-devops";
static readonly string personalAccessToken = Util.GetPassword("adopat");
/*
Json Commit from the API

{
  "commitId": "fe738c9ba1847b88b54c06faf9baa8ab6182ecdb",
  "author": {
    "name": "Rob",
    "email": "rob.hill@chevinfleet.com",
    "date": "2023-06-28T10:24:54Z"
  },
  "committer": {
    "name": "Rob",
    "email": "rob.hill@chevinfleet.com",
    "date": "2023-06-28T10:24:54Z"
  },
  "comment": "Merge remote-tracking branch 'origin/master'",
  "changeCounts": { "Add": 0, "Edit": 7, "Delete": 0 },
  "url": "https://dev.azure.com/chevin-devops/8c4531dd-7e7b-4dda-800a-5b97e74af65a/_apis/git/repositories/53a02161-3e22-4cbd-a31c-ad38385bb72b/commits/fe738c9ba1847b88b54c06faf9baa8ab6182ecdb",
  "remoteUrl": "https://dev.azure.com/chevin-devops/Receipt.Service/_git/Receipt.Service/commit/fe738c9ba1847b88b54c06faf9baa8ab6182ecdb"
}

*/

static async Task Main()
{
	var byteArray = Encoding.ASCII.GetBytes($":{personalAccessToken}");
	client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(byteArray));

	var userCommitCounts = new Dictionary<string, (int count,int adds,int edits,int deletes)>();

	var projects = await GetProjects();
	Console.WriteLine($"Projects : {projects.Count(),3:###}");
	Console.WriteLine($"==============");
	foreach (var project in projects)
	{
		var repositories = await GetRepositories(project.GetProperty("id").GetString());
		Console.WriteLine($"Project      : {project.GetProperty("name")}");
		Console.WriteLine($"Repositories : {repositories.Count()}");

		foreach (var repo in repositories)
		{
			string continuationToken = null;

			do
			{
				var (commits, token) = await GetCommits(project.GetProperty("id").GetString(), repo.GetProperty("id").GetString(), continuationToken);

				foreach (var commit in commits)
				{
					var authorEmail = commit.GetProperty("author").GetProperty("email").GetString();
					if (!userCommitCounts.ContainsKey(authorEmail))
					{
						userCommitCounts[authorEmail] = (0,0,0,0);
					}
					var t = userCommitCounts[authorEmail];
					var changeCounts = commit.GetProperty("changeCounts");
					t.count++;
					t.adds += changeCounts.GetProperty("Add").GetInt32();
					t.edits += changeCounts.GetProperty("Edit").GetInt32();
					t.deletes += changeCounts.GetProperty("Delete").GetInt32();
					userCommitCounts[authorEmail] = t;
				}

				continuationToken = token;

			} while (continuationToken != null);
		}
	}
	const string emailTitle ="Committer";

	string filePath = @"c:\dumpzone\committers.md";

	// Sample data for the table
	string[,] commitTableData = new string[,]
	{
			{ "Committer", "Count", "Adds", "Edits", "Deletes" }
	};

	// Create the Markdown table
	string markdownTable = "|";
	for (int col = 0; col < commitTableData.GetLength(1); col++)
	{
		markdownTable += $" {commitTableData[0, col]} |";
	}
	markdownTable += Environment.NewLine+ "|";
	for (int col = 0; col < commitTableData.GetLength(1); col++)
	{
		markdownTable += " ---- |";
	}
	markdownTable += Environment.NewLine + "|";

	markdownTable += Environment.NewLine;
	Console.WriteLine($"{"Committer",-40} {"Count",16} {"Adds",16} {"Edits",16} {"Deletes",16}");

	foreach (var kvp in userCommitCounts)
	{
		//only dump realistic committers.
		if (kvp.Value.count > 50)
		{
			Console.WriteLine($"{kvp.Key,-40} {kvp.Value.count,16:#####} {kvp.Value.adds,16:######} {kvp.Value.edits,16:######}, {kvp.Value.deletes,16:######}");
			markdownTable+=$"|{kvp.Key,-40} | {kvp.Value.count,16:#####} | {kvp.Value.adds,16:######} | {kvp.Value.edits,16:######} | {kvp.Value.deletes,16:######} |";
			markdownTable += Environment.NewLine;
		}
	}
	File.WriteAllText(filePath, markdownTable);
}

static async Task<JsonElement.ArrayEnumerator> GetProjects()
{
	var response = await client.GetStringAsync($"https://dev.azure.com/{organization}/_apis/projects?api-version=6.0");
	var jsonDocument = JsonDocument.Parse(response);
	return jsonDocument.RootElement.GetProperty("value").EnumerateArray();
}

static async Task<JsonElement.ArrayEnumerator> GetRepositories(string projectId)
{
	var response = await client.GetStringAsync($"https://dev.azure.com/{organization}/{projectId}/_apis/git/repositories?api-version=6.0");
	var jsonDocument = JsonDocument.Parse(response);
	return jsonDocument.RootElement.GetProperty("value").EnumerateArray();
}

static async Task<(JsonElement.ArrayEnumerator, string)> GetCommits(string projectId, string repoId, string continuationToken)
{
	var request = new HttpRequestMessage(HttpMethod.Get, $"https://dev.azure.com/{organization}/{projectId}/_apis/git/repositories/{repoId}/commits?api-version=6.0&$top=2147483647");
	if (continuationToken != null)
	{
		request.Headers.Add("X-MS-ContinuationToken", continuationToken);
	}

	var response = await client.SendAsync(request);
	if (!response.IsSuccessStatusCode)
		return (new JsonElement.ArrayEnumerator(), null);
		
	var jsonDocument = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
	var commits = new JsonElement.ArrayEnumerator();
	try
	{
		commits = jsonDocument.RootElement.GetProperty("value").EnumerateArray();
	}
	catch (Exception ex) {
		ex.Dump();
		return (new JsonElement.ArrayEnumerator(), null);
	}

	IEnumerable<string> continuationTokens;
	if (response.Headers.TryGetValues("X-MS-ContinuationToken", out continuationTokens))
	{
		continuationToken = continuationTokens.FirstOrDefault();
	}
	else
	{
		continuationToken = null;
	}

	return (commits, continuationToken);
}