
# UNITY PACKAGE -- Disk Storage Services

---

```{toctree}
---
cation: contents:
maxdepth: 2
---
./README.md
```

---

## Dependencies

*No dependencies are needed for this package.*

--

## Namespaces Classes and Resources

```
Packages.DiskStorageServices.Components
```

- StorageWirterBase
	
	this class implements a base writer for log files to the disk. It works both on PC (flder C:/shared) and on HoloLens2 (temporany storage). 
	
	It is designed to be extended to create different types of logs and behaviours. Suitable for streams of data, not so well for one-shot writings. 

- TxtWriter
	
	direct inheritance from DistStorageBase: it writes a TXT file into the storage. Useful for simple text in streaming. 
	
- CsvWriter
	
	It writes a stream of records into a CSV file. 

- LogStreamToStorage
	
	This component allows to create a very simple output stream on file for the Unity log. 

```
Packages.DiskStorageServices.Utils
```

- ...

```
Packages.DiskStorageServices.ModuleTesting
```

- TestingPCWriter

--