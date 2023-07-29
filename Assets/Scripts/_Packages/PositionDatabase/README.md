
# UNITY PACKAGE -- Position Database

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

- **Disk Storage Services** : StorageWriterOneShot

--

## Namespaces Classes and Resources

```
Packages.PositionDatabase.Components
```

- PositionsDatabase
- PositionDatabaseWaypointHandle

```
Packages.PositionDatabase.Utils
```

- PositionDatabasePath
- PositionDatabaseWaypoint

```
Packages.PositionDatabase.ModuleTesting
```

- TestingPositionDB
	
	<img src="./_docs/images/SETTINGS_TestingPositionDB.png" alt="">

--

## Installation

1. place the script inside the folder `_Packages`

--

## Perform module testing and tuning

*Simple version*:

1. create a empty GameObject
2. attach component : `PositionsDatabase`
3. attach component : `TestingPositionDB`
4. assign the `PositionsDatabase` reference to the `TestingPositionDB`
5. assign on Zone Created to the DB
6. assign on Zone Changed to the DB

then, play. While you move, you should see the values of current zone ID, debug hit and debug miss changing:

<img src="./_docs/images/PLAYMODE_TestingPositionDB.png" alt="">

- **current zone ID** : each zone has a unique number when it is created into the database
- **hit** : how many times the DB recognized a zone previoulsy recorded
- **miss** : how many times he database created a new position, detecting the current zone as new

The script can be used for tuning the Position Database Component:

- passing in a common area, the number of miss should keep the same value during the exploration
- if the number of misses increases even if you're passing on a previously explored zone, the position database is not capable of following the user's movement
- use clusters and max indexes to improve the follow rate

--

## Database Import Export

currently, the database can be exported and imported in JSON format. 

- **Export** : EVENT_ExportJson()

--