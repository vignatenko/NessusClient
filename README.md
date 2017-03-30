# .NET Nessus Client

The C# library for asynchronously accessing the [Tenable Nessus 6.x](http://www.tenable.com/products/nessus-vulnerability-scanner) REST API.

This initial release covers:
* [Getting list of scans](#listOfScans)
* [Getting history of scan](#scanHistory)
* [Getting vulnerabilities(as POCOs) ](#loadVulns)
* [Export](#export)

Fill free to post feature requests.


### Installing


Using Nuget
```
Install-Package NessusClient
```

### Usage

Core interface of the client is ```INessusConnection```.


 The ```INessusConnection``` provides 3 methods.
```CSharp
public interface INessusConnection: IDisposable
{
    Task OpenAsync(CancellationToken cancellationToken);

    Task CloseAsync(CancellationToken cancellationToken);

    WebRequest CreateRequest(string relativeEndpointUrl, 
                             string httpMethod, 
                             CancellationToken cancellationToken);
}
```


There is default implementation of this interface  - the [```NessusConnection```](NessusClient/NessusConnection.cs) class.

Rest of functionality is implemented as an extention methods in [```Scans\NessusConnectionExtentions.cs```](NessusClient/Scans/NessusConnectionExtentions.cs). 

Typical usage:
```CSharp
async Task SomeMethodAsync(CancellationToken cancellationToken)
{
	using(var conn = new NessusConnection(server, port, userName, password))
	{
		await conn.OpenAsync(cancellationToken);
		//do work...
	}
}
```

NOTE. To easily convert password stored in ```byte[]``` to ```SecureString``` the [SecureStringExtentions](NessusClient/SecureStringExtentions.cs) can be used:
```CSharp
var secret = new byte[]{...};
var secureStr = secret.ToSecureString();
```

## Examples

<a name="listOfScans"></a>
### Getting list of scans



```CSharp
async Task<IEnumerable<Scan>> GetScansAsync(CancellationToken cancellationToken)
{
	using(var conn = new NessusConnection(server, port, userName, password))
	{
		await conn.OpenAsync(cancellationToken);
		return conn.GetScansAsync(cancellationToken)
	}
}
```

The [Scan](NessusClient/Scans/Scan.cs) class is holding minumum  information about the scan
```CSharp
public class Scan
{
.....
        public int Id { get; }        
        public string Name { get; }
        public DateTimeOffset LastUpdateDate { get; }
}
```

<a name="scanHistory"></a>
### Getting scan's history

```CSharp
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
<a name="loadVulns"></a>
### Load Vulnerabilities

```CSharp
//Caution: getting all vulnerabilties may take a long time 
async Task<IEnumerable<ScanResult>> GetAllVulnerabilitiesAsync(CancellationToken cancellationToken)
{
	using(var conn = new NessusConnection(server, port, userName, password))
	{
		await conn.OpenAsync(cancellationToken);

		//first, get all scan histories...
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

```CSharp
public class ScanResult
{
    public string Name { get; set; }
    public IEnumerable<Host> Hosts { get; set; }                            

}
```
The [```Host```](NessusClient/Scans/Host.cs) class contains list of [```Vulnerability```](NessusClient/Scans/Vulnerability.cs):
```CSharp
 public class Host
 {
//other properties omited
        public IEnumerable<Vulnerability> Vulnerabilities { get; set; }
 }
```

Flattening the ```IEnumerable<ScanResult>``` gives plain list of vulneraiblities.

<a name="export"></a>
### Export Scan

```CSharp
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
}

```
See also [```ExportFormat```](NessusClient/Scans/ExportFormat.cs).


## Authors

* Vlad Ignatenko - *Initial work*

See also the list of [contributors](https://github.com/vignatenko/NessusClient/graphs/contributors) who participated in this project.

## License

This project is licensed under the MIT License - see the [LICENSE.md](LICENSE) file for details


