# SarServer -- Database Module

## Access the DB with DBeaver

- [DBeaver Official Link](https://dbeaver.io/)

---

## Force init script

- [StackOverflow](https://stackoverflow.com/questions/48933210/how-to-force-postgres-docker-container-to-start-with-new-db)

- `docker compose stop` hibernates the container; when you do `docker compose up`, init is skipped. 
- `docker compose down` destroys the container; init is executed again at the next run of `docker compose up`

This is not sufficient since Postgres will find the partition containing the data. 

```sh
docker compose down ...
docker volume rm <postgres volume storage>
docker compose up ...
```

samples cannot be loaded if init is not forced: Postgres will skip the initialization when a volume is found. 

---

