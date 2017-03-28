# Tenable Nessus Client

The C# wrapper for [Tenable Nessus 6.x](http://www.tenable.com/products/nessus-vulnerability-scanner) REST API.
In this initial release it covers:
* Getting list of scans
* Getting history of scan
* Getting vulnerabilities(as DTO) 
* Export



### Installing


Using [Nuget](https://www.nuget.org/packages/NessusClient/1.0.1.4042)
```
Install-Package NessusClient
```

### Usage

Core interface of the client is ```INessusConnection```.

There is default implementation of this interface  - the ```NessusConnection``` class.

Typical usage:
```
async Task SomeMethodAsync(CancellationToken cancellationToken)
{
	using(var conn = new NessusConnection(server, port, userName, password))
	{
		await conn.OpenAsync(cancellationToken);
		//do work...
	}
}
```
 The ```INessusConnection``` provides 3 methods.
```
public interface INessusConnection: IDisposable
{
    Task OpenAsync(CancellationToken cancellationToken);
    Task CloseAsync(CancellationToken cancellationToken);
    WebRequest CreateRequest(string relativeEndpointUrl, string httpMethod, CancellationToken cancellationToken);
}
```


Rest of functionality is implemented as an extention methods in ```Scans\NessusConnectionExtentions.cs```. 


## Examples



### Getting list of scans



```
async Task<IEnumerable<Scan>> GetScansAsync(CancellationToken cancellationToken)
{
	using(var conn = new NessusConnection(server, port, userName, password))
	{
		await conn.OpenAsync(cancellationToken);
		return conn.GetScansAsync(cancellationToken)
	}
}
```

The Scan class is defined as 
```
public class Scan
{

        public int Id { get; }        
        public string Name { get; }
        public DateTimeOffset LastUpdateDate { get; }
}
```

### Getting scan history

```
async Task LoadAllScansHistoryAsync(CancellationToken cancellationToken)
{
	using(var conn = new NessusConnection(server, port, userName, password))
	{
		await conn.OpenAsync(cancellationToken);

		//first, getting list of scans...
		var scans =  await conn.GetScansAsync(cancellationToken);

		foreach(var scan in scans)
		{
			var historyRecords = await conn.GetScanHistoryAsync(scan.Id, cancellationToken);

			foreach(var historyItem in  historyRecords )
			{
				Console.WriteLine($"Id={historyItem.Id}, HistoryId={historyItem.HistoryId}, Name={historyItem.Name}");
			}
		}
	}
}
```

### Load Vulnerabilities

```
//Caution: getting all vulnerabilties may take a long time 
async Task<IEnumerable<ScanResult>> GetAllVulnerabilitiesAsync(CancellationToken cancellationToken)
{
	using(var conn = new NessusConnection(server, port, userName, password))
	{
		await conn.OpenAsync(cancellationToken);

		//first, getting list of scans...
		var historyRecords =  await conn.GetAllScanHistoriesAsync(cancellationToken);

		var result = new List<ScanResult>(historyRecords.Count())

		foreach(var historyItem in  historyRecords )
		{
			var scanResult = await conn.GetScanResultAsync(historyItem.Id, 
							historyItem.HistoryId, 
							cancellationToken);

			result.Add(scanResult);
		}
		return result;
	}
}
```

The ```ScanResult``` class contains list of ```Host```

```
public class ScanResult
{
    public string Name { get; set; }
    public IEnumerable<Host> Hosts { get; set; }                            

}
```
The [```Host```](NessusClient/Scans/Host.cs) class contains list of [```Vulnerability```](NessusClient/Scans/Vulnerability.cs):
```
 public class Host
 {
//other properties omited
        public IEnumerable<Vulnerability> Vulnerabilities { get; set; }
 }
```

Flattening the ```IEnumerable<ScanResult>``` gives plain list of vulneraiblities.


### Export Scan

```

using(var conn = new NessusConnection(server, port, userName, password))
{
	await conn.OpenAsync(cancellationToken);

	using(var stream = File.OpenWrite(filePath))
	{
		await conn.ExportAsync(scanId, 
					historyId, 					 
					ExportFormat.Html, 
					stream, 
					cancellationToken);
	}
	var result = new List<ScanResult>(historyRecords.Count())

	foreach(var historyItem in  historyRecords )
	{
		var scanResult = await conn.GetScanResultAsync(historyItem.Id, 
						historyItem.HistoryId, 
						cancellationToken);

		result.Add(scanResult);
	}
	return result;
}

```
See also [```ExportFormat```](NessusClient/Scans/ExportFormat.cs).


## Authors

* Vlad Ignatenko - *Initial work*

See also the list of [contributors](https://github.com/vignatenko/NessusClient/graphs/contributors) who participated in this project.

## License

This project is licensed under the MIT License - see the [LICENSE.md](LICENSE) file for details


