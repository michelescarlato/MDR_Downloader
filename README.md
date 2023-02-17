# MDR_Downloader
Downloads or otherwise obtains data from mdr data sources and transfers that data to local files, stored as JSON.

The data extraction within the MDR begins with the creation of a local copy of the source data. A folder is created for each source, and the data download process adds files to that folder. The download events are self contained and can take place independently of any further processing. The local copy of the source data simply grows with successive download events. At any point in time the folder therefore holds *all* the data relevant to the NDR from its source.<br/>
The basic details of each file, including the date and time of its download, are recorded in the monitoring database so that later processing stages can select subsets of files from that data store. Sources are trial registries and data repositories, and the download mechanisms used include
* Downloading JSON files directly from a source's API, (e.g. for ClicalTrials.gov, PubMed) or downloading XML files directly from an API (ISRCTN)
* Scraping web pages and generating the XML files from the data obtained (e.g. for EUCTR, Yoda, BioLincc)
* Downloading CSV files and converting the data into JSON files (e.g. for WHO ICTRP data).
The format of the JSON files created vary from source to source but represent the initial stage in the process of converting the source data into a consistent schema.<br/><br/>
The program represents the first stage in the 5 stage MDR extraction process:<br/>
&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;**Download** => Harvest => Import => Coding => Aggregation<br/><br/>
For a much more detailed explanation of the extraction process,and the MDR system as a whole, please see the project wiki (landing page at https://ecrin-mdr.online/index.php/Project_Overview).<br/>
In particular, for the download prosesses, see<br/>
https://ecrin-mdr.online/index.php/Downloading_Data<br/>

## Parameters and Usage
The system can take the following parameters:<br/>
**-s:** expects to be followed by a single integer source ids, representing a data source within the system. The data is obtained and added to the store of JSON source files for that source.<br/>
**-t:** followed by an integer. Indicates the type of download to be carried out. Types available vary for different source systems, and each type is linked with possible pre-requisites, e.g. cut-off date, start and end pages, filter to be applied, etc. - see below for specific details. The system checks for the presence of appropriate suitable pre-requisites, for the specified type, before proceeding.<br/>
<br/>
<br/>
Routine usage, as in the scheduled extraction process, will vary with different sources, but in general looks for new and revised records in the source data and obtains only that. It ispossible to over-ride this, however, and (re)obtain all source records. <br/>

## Dependencies
The program used the Nuget packages:
* CommandLineParser - to carry out initial processing of the CLI arguments
* Npgsql, Dapper and Dapper.contrib to handle database connectivity
* Microsoft.Extensions.Configuration, .Configuration.Json, and .Hosting to read the json settings file and support the initial host setup.

## Provenance
* Author: Steve Canham
* Organisation: ECRIN (https://ecrin.org)
* System: Clinical Research Metadata Repository (MDR)
* Project: EOSC Life
* Funding: EU H2020 programme, grant 824087
