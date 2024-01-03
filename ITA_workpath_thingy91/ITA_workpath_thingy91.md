# @July 10, 2023 — Thingy:91

- ********created July 10, 2023 7:34 PM*
- getting started
- references
- altre guide
- troubleshooting
- folklore

---

---

## Obiettivo

1. step iniziale — imparare a capire come estrarre le informazion dalla API nordic, una qualunque info
2. intermedio — esplorare la API e trovare dei pattern comodi per leggere le info
3. quello che mi serve — listare tutti i messaggi in un CSV, tutti i dati possibili

Per ora mi accontento di questo. Il secondo step sarà integrare questo meccanismo con un database, calare il tutto in un’architettura basata su Docker, rendere accessibile l’informazione con FastAPI, e automatizzare la raccolta di informazioni. 

—

## Diario di lavoro

Purtroppo il lavoro che ho già fatto con Thinghy fa veramente pena. Vorrei perciò condurre questo studio esplorando step dopo step le opportunità offerte dall’app Nordic, e iniziare a prendere confidenza con questo sistema. 

1. ( *******added July 10, 2023 8:22 PM* ) — il primissimo passo era quello di studiarsi un po’ lo strumento fondamentale su cui baserò tutto, che è `requests` , e direi che ora dovrei sapere tutto ciò che serve a riguardo. 
    
    
2. ( *******added July 10, 2023 8:23 PM* ) — ebbene, come si accede a Thingy? Come fare la prima richiesta?
    
    Anzitutto, serve poter accedere. 
    
    - la guida ufficiale — [nRF Cloud REST API Documentation](https://api.nrfcloud.com/v1) — [nRF Cloud | nRF Cloud Docs](https://docs.nrfcloud.com/) — [Introduction to the REST API | nRF Cloud Docs](https://docs.nrfcloud.com/APIs/REST/RESTIntro/) — [Overview of the nRF Cloud APIs | nRF Cloud Docs](https://docs.nrfcloud.com/APIs/APIOverview/#nrf-cloud-rest-api)
    
    direi di prenderci una richiesta, dopo il gran cappello introduttivo, e chiamarla. 
    
    Dove avevo le mie credenziali di accesso?
    
    - qui — [Credenziali Nordic Cloud](https://www.notion.so/Credenziali-Nordic-Cloud-a0d0cdb21fdf4e52a73c641ed94c0f45?pvs=21)
    
    La pagina (ora) descrive anche come reperire il toen. *****************************E ora che questo bellissimo token lo abbiamo, che si fa?*****************************
    
3. ( *******added July 10, 2023 10:35 PM* ) — mi servono le info per potermi autenticare. Come ci si autentica in questo pianeta? 
    - per autenticarsi con la API vedi — [Introduction to the REST API | nRF Cloud Docs](https://docs.nrfcloud.com/APIs/REST/RESTIntro/)
    - nRF Cloud è un servizio costruito secondo le specifiche OpenAPI 3.x — [OpenAPI Specification v3.1.0 | Introduction, Definitions, & More](https://spec.openapis.org/oas/v3.1.0)
    
    l’header per autenticarsi deve contenere questa riga:
    
    ```
    Authorization: Bearer <API Key>
    ```
    
    forti di questo sapere enciclopedico, ti direi subito di provare la più semplice funzione della API con questa tecnica. una GET, giusto per iniziare a leggere un po’ nel giusto modo le pagine della documentazione della API. 
    
4. ( *******added July 10, 2023 10:50 PM* ) — la prima richiesta!
    
    tipo questa: la
    
    [nRF Cloud REST API Documentation](https://api.nrfcloud.com/v1#tag/Account/operation/FetchAccountInfo)
    
    API entry point:
    
    ```
    https://api.nrfcloud.com/v1/
    ```
    
    Per esercizio me la traduco un attimo in HTTP: (Bearer è l’auth schema)
    
    ```
    GET https://api.nrfcloud.com/v1/account
    
    Authorization: Bearer <API Key>
    ```
    
    Per queste cose, sempre meglio andare sul semplice, per poi raffinare passo dopo passo. E il primo codice che presento è il seguente: una semplice richiesta Account:
    
    ```
    import requests
    import json
    
    api_entry_point = "https://api.nrfcloud.com/v1"
    api_key = "..."
    
    account_url = f"{api_entry_point}/account"
    account_header = {
        "Authorization" : f"Bearer {api_key}"
    }
    
    res = requests.get(
        url = account_url,
        headers = account_header
    )
    
    if res.status_code == requests.codes.ok:
        print(f"{json.dumps(json.loads(res.text), indent=4)}")
    else:
        print(f"ERROR - {res.status_code} - {res.text}")
    ```
    
    la cui risposta è la seguente:
    
    ```
    {
        "mqttEndpoint": "mqtt.nrfcloud.com",
        "mqttTopicPrefix": "prod/0b3486e...f4/",
        "team": {
            "tenantId": "0b3...",
            "name": "..."
        },
        "role": "owner",
        "tags": [],
        "plan": {
            "name": "**DEVELOPER**",
            "limits": {
                "monthlyLocationServiceRequests": 500,
                "monthlyFOTAJobExecutions": 50,
                "devices": 10,
                "monthlyStoredDeviceMessages": 3000
            }
        }
    }
    ```
    
5. ( *******added July 10, 2023 11:04 PM* ) — le cose tutto sommato sono abbastanza semplici: prendi la funzione che ti interessa dal cloud, la imlementi i nHTTP e la trasformi in request. 
    
    Facciamo dunque una rassegna dei metodi più interessanti, giusto per iniziare a pensare un minimo di più a qualcosa che valga la pena fare. 
    
    - per tracciare l’utilizzo della API, anche se però avendo un DWH questa cosa non serve a molto — [nRF Cloud REST API Documentation](https://api.nrfcloud.com/v1#tag/Account/operation/ListApiUsage)
    - ma vuoi vedere che questa mi risolve il problema del dover reinserire a mano tutte le volte quel dannato token??? — [nRF Cloud REST API Documentation](https://api.nrfcloud.com/v1#tag/Account/operation/GetServiceToken)
    - list devices, utile per capire quale device è attivo o no in quel momento forse? — [nRF Cloud REST API Documentation](https://api.nrfcloud.com/v1#tag/All-Devices/operation/ListDevices)
    - forse utile per avere lo stato del device? Quanto meno, qualche info — [nRF Cloud REST API Documentation](https://api.nrfcloud.com/v1#tag/All-Devices/operation/FetchDevice) — [Transforming JSON responses | nRF Cloud Docs](https://docs.nrfcloud.com/APIs/REST/Tutorials/Transforms/)
    - importantissima — [nRF Cloud REST API Documentation](https://api.nrfcloud.com/v1#tag/Messages/operation/ListMessages)
    - per la geolocalizzazione del dispositivo esiste la richiesta esplicita di geolocation assistita — [nRF Cloud REST API Documentation](https://api.nrfcloud.com/v1#tag/Assisted-GPS/operation/GetAssistanceData)
    - per trovare la posizione della cella più vicina — [nRF Cloud REST API Documentation](https://api.nrfcloud.com/v1#tag/Cell-Location)
    - altro tipo di geolocalizzazione, la GNSS — [nRF Cloud REST API Documentation](https://api.nrfcloud.com/v1#tag/GNSS)
    
6. ( *******added July 10, 2023 11:27 PM* ) — iniziamo a lavorare sulla richiesta dei messaggi, che era la prima che volevo vedere. Ne ho trovate anche altre interessanti, ma la più utile per il momento è quella per me ora. 
    - [Device messages | nRF Cloud Docs](https://docs.nrfcloud.com/Devices/MessagesAndAlerts/DeviceMessages/)
    - [nRF Cloud REST API Documentation](https://api.nrfcloud.com/v1#tag/Messages/operation/ListMessages) — ListMessages
    
    versione pulita HTTP:
    
    ```
    GET https://api.nrfcloud.com/v1/messages
    ?deviceId = <vedi API FetchDevice>
    ?pageLinit = <un numero da 1 a 100, default 10>
    ?pageNextToken = <vedi eventuale chiamata precedente>
    start=2020-06-25T21:05:12.830Z
    ?end=2020-06-25T21:05:12.830Z
    ?deviceId=nrf-1234567890123456789000
    ?pageSort=desc
    
    Authorization: Bearer <API Key>
    ```
    
    Nella risposta ci sono anche 
    
    - total — è il numero elementi ritornati nell’attuale richiesta, *******************************quindi non so quanti ne mancano*******************************
    - pageNextToken — da dare alla chiamata successiva per farsi ritornare le informazioni non inviate in quel pacchetto
    
7. ( *******added July 10, 2023 11:35 PM* ) — giusto a scopo didattico,
    
    vado a creare un semplice programma che crea una cartella in cui scrivere un solo file per messaggio. 
    
    … hmmm non è semplicissimo capire come iterare sui messaggi… il seguente codice dovrebbe funzionare, ma come risultato ha solo quello di iterare sempre sullo stesso messaggio:
    
    ```
    import requests
    import json
    import os, shutil
    
    api_entry_point = "https://api.nrfcloud.com/v1"
    api_key = "..."
    
    msg_url = f"{api_entry_point}/messages" + "?pageLimit=1"
    msg_header = {
        "Authorization" : f"Bearer {api_key}"
    }
    res = requests.get(
        url = msg_url,
        headers = msg_header
    )
    
    try:
        shutil.rmtree("./results")
    except:
        pass
    os.mkdir("./results")
    
    fileno = 1
    while res.status_code == requests.codes.ok and res.json()['total'] > 0:
        
        # save the file
        print( "RECEIVED MESSAGE NO.", fileno )
        print( json.dumps(json.loads(res.text), indent=4) )
        '''
        with open( f"./results/MSG_{str(fileno).zfill(4)}.json", 'w' ) as fil:
            fil.write( json.dumps(json.loads(res.text), indent=4) )
            fileno = fileno + 1
        '''
    
        # get next 
        msg_url = f"{api_entry_point}/messages" + "?pageNextToken=" + res.json()['pageNextToken']
        res = requests.get(
            url = msg_url,
            headers = msg_header
        )
    
    print("Done.")
    ```
    
    in teoria dovrebbe essere corretto l’uso del next token. Però per qualche ragione mi ritora un solo messaggio ogni volta, e non un’iterazione. 
    
    un esempio di messaggio:
    
    ```
    {
                "topic": "prod/0b34...f4/m/d/nrf-352656101104563/cell_pos/r",
                "deviceId": "N/A",
                "receivedAt": "2023-06-18T06:23:42.081Z",
                "message": {
                    "appId": "CELL_POS",
                    "messageType": "DATA",
                    "data": {
                        "lat": 44.4434427,
                        "lon": 8.9020756,
                        "uncertainty": 562,
                        "fulfilledWith": "MCELL"
                    }
                },
                "tenantId": "0b...f4"
            },
    ```
    
    forse per iterare posso usare start e end… ma nemmeno così va bene. Quel next token mi ritorna la stessa pagina, dannazione!
    
8. ( *******added July 17, 2023 7:19 PM* ) — io *******voglio******* iterare su tutti i messaggi, ho bisogno di recuperare lo storico. 
    
    Andiamo per gradi. Innanzitutto, voglio il messaggio più recente che hai. In particolare voglio la data più recente che hai. 
    
    ripropongo:
    
    ```
    GET https://api.nrfcloud.com/v1/messages
    ?deviceId = <vedi API FetchDevice>
    ?pageLinit = <un numero da 1 a 100, default 10>
    ?pageNextToken = <vedi eventuale chiamata precedente>
    start=2020-06-25T21:05:12.830Z
    ?end=2020-06-25T21:05:12.830Z
    ?deviceId=nrf-1234567890123456789000
    ?pageSort=desc
    
    Authorization: Bearer <API Key>
    ```
    
    - un esempio di risposta per i messages
        
        ```
        {
            "items": [
                {
                    "topic": "prod/0b3486e5-a1e9-4054-9d3f-c5e623bff7f4/m/d/nrf-352656101104563/c2d",
                    "deviceId": "N/A",
                    "receivedAt": "2023-06-18T06:23:42.083Z",
                    "message": {
                        "appId": "CELL_POS",
                        "messageType": "DATA",
                        "data": {
                            "lat": 44.4434427,
                            "lon": 8.9020756,
                            "uncertainty": 562,
                            "fulfilledWith": "MCELL"
                        }
                    },
                    "tenantId": "0b3486e5-a1e9-4054-9d3f-c5e623bff7f4"
                },
                {
                    "topic": "prod/0b3486e5-a1e9-4054-9d3f-c5e623bff7f4/m/d/nrf-352656101104563/cell_pos/r",
                    "deviceId": "N/A",
                    "receivedAt": "2023-06-18T06:23:42.081Z",
                    "message": {
                        "appId": "CELL_POS",
                        "messageType": "DATA",
                        "data": {
                            "lat": 44.4434427,
                            "lon": 8.9020756,
                            "uncertainty": 562,
                            "fulfilledWith": "MCELL"
                        }
                    },
                    "tenantId": "0b3486e5-a1e9-4054-9d3f-c5e623bff7f4"
                },
                {
                    "topic": "prod/0b3486e5-a1e9-4054-9d3f-c5e623bff7f4/m/d/nrf-352656101104563/d2c",
                    "deviceId": "nrf-352656101104563",
                    "receivedAt": "2023-06-18T06:23:41.821Z",
                    "message": {
                        "appId": "CELL_POS",
                        "messageType": "DATA",
                        "data": {
                            "lte": [
                                {
                                    "eci": 4956959,
                                    "mcc": 222,
                                    "mnc": 10,
                                    "tac": 12050,
                                    "earfcn": 1850,
                                    "rsrp": -97,
                                    "rsrq": -9,
                                    "nmr": [
                                        {
                                            "earfcn": 1850,
                                            "pci": 340,
                                            "rsrp": -98,
                                            "rsrq": -10
                                        },
                                        {
                                            "earfcn": 1850,
                                            "pci": 464,
                                            "rsrp": -105,
                                            "rsrq": -17.5
                                        }
                                    ]
                                }
                            ]
                        }
                    },
                    "tenantId": "0b3486e5-a1e9-4054-9d3f-c5e623bff7f4"
                },
                {
                    "topic": "prod/0b3486e5-a1e9-4054-9d3f-c5e623bff7f4/m/d/nrf-352656101104563/d2c",
                    "deviceId": "nrf-352656101104563",
                    "receivedAt": "2023-06-18T06:22:39.618Z",
                    "message": {
                        "appId": "VOLTAGE",
                        "messageType": "DATA",
                        "ts": 1687069359618,
                        "data": "4015"
                    },
                    "tenantId": "0b3486e5-a1e9-4054-9d3f-c5e623bff7f4"
                },
                {
                    "topic": "prod/0b3486e5-a1e9-4054-9d3f-c5e623bff7f4/m/d/nrf-352656101104563/d2c",
                    "deviceId": "nrf-352656101104563",
                    "receivedAt": "2023-06-18T06:22:39.585Z",
                    "message": {
                        "appId": "RSRP",
                        "messageType": "DATA",
                        "ts": 1687069359585,
                        "data": "-96"
                    },
                    "tenantId": "0b3486e5-a1e9-4054-9d3f-c5e623bff7f4"
                },
                {
                    "topic": "prod/0b3486e5-a1e9-4054-9d3f-c5e623bff7f4/m/d/nrf-352656101104563/d2c",
                    "deviceId": "nrf-352656101104563",
                    "receivedAt": "2023-06-18T06:22:39.585Z",
                    "message": {
                        "appId": "DEVICE",
                        "messageType": "DATA",
                        "ts": 1687069359585,
                        "data": {
                            "networkInfo": {
                                "rsrp": -96,
                                "cellID": 4956959
                            }
                        }
                    },
                    "tenantId": "0b3486e5-a1e9-4054-9d3f-c5e623bff7f4"
                },
                {
                    "topic": "prod/0b3486e5-a1e9-4054-9d3f-c5e623bff7f4/m/d/nrf-352656101104563/d2c",
                    "deviceId": "nrf-352656101104563",
                    "receivedAt": "2023-06-18T06:22:39.524Z",
                    "message": {
                        "appId": "TEMP",
                        "messageType": "DATA",
                        "ts": 1687069359524,
                        "data": "27.56"
                    },
                    "tenantId": "0b3486e5-a1e9-4054-9d3f-c5e623bff7f4"
                },
                {
                    "topic": "prod/0b3486e5-a1e9-4054-9d3f-c5e623bff7f4/m/d/nrf-352656101104563/d2c",
                    "deviceId": "nrf-352656101104563",
                    "receivedAt": "2023-06-18T06:22:39.524Z",
                    "message": {
                        "appId": "AIR_PRESS",
                        "messageType": "DATA",
                        "ts": 1687069359524,
                        "data": "101.33"
                    },
                    "tenantId": "0b3486e5-a1e9-4054-9d3f-c5e623bff7f4"
                },
                {
                    "topic": "prod/0b3486e5-a1e9-4054-9d3f-c5e623bff7f4/m/d/nrf-352656101104563/d2c",
                    "deviceId": "nrf-352656101104563",
                    "receivedAt": "2023-06-18T06:22:39.524Z",
                    "message": {
                        "appId": "HUMID",
                        "messageType": "DATA",
                        "ts": 1687069359524,
                        "data": "58.19"
                    },
                    "tenantId": "0b3486e5-a1e9-4054-9d3f-c5e623bff7f4"
                },
                {
                    "topic": "prod/0b3486e5-a1e9-4054-9d3f-c5e623bff7f4/m/d/nrf-352656101104563/d2c",
                    "deviceId": "nrf-352656101104563",
                    "receivedAt": "2023-06-18T06:22:39.524Z",
                    "message": {
                        "appId": "AIR_QUAL",
                        "messageType": "DATA",
                        "ts": 1687069359524,
                        "data": "14"
                    },
                    "tenantId": "0b3486e5-a1e9-4054-9d3f-c5e623bff7f4"
                }
            ],
            "total": 10,
            "pageNextToken": "G3oA+I3THfVjCJpLl9Mp/3onW8h60J1DgeIoEZ28RCGYyoaH/GCud/v+7hNkqWRN1ccaXWi1td58P7ieah8rhRKVirOBVKeSXZ3GwTKg1/z0z6eZIB18KGmO1NtcKWgMpP1CY5yT88Oy5CXAHw=="
        }
        ```
        
    - un esempio di programma per trovare i 10 messaggi più recenti
        
        ```
        import requests
        import json
        import os, sys, shutil
        
        api_entry_point = "https://api.nrfcloud.com/v1"
        api_key = "e14c3da5855436e8a450cdd8464f7dfd9b120aee"
        
        msg_url = f"{api_entry_point}/messages" + "?pageLimit=10" + "&pageSort=desc"
        msg_header = {
            "Authorization" : f"Bearer {api_key}"
        }
        res = requests.get(
            url = msg_url,
            headers = msg_header
        )
        
        if res.status_code != requests.codes.ok:
            print(f"ERROR: request returned code {res.status_code} with description: {res.content}")
            sys.exit(1)
        else:
            print("SUCCESS")
        
        # print( json.dumps(res.json(), indent=4) )
        # print( json.dumps(res.json()['items'], indent=4) )
        
        content = res.json()
        for msg in content['items']:
            print(msg['receivedAt'] + " -- " + msg['message']['appId'] + " -- " + msg['message']['messageType'])
        print( "pageNextToken:", content['pageNextToken'] )
        
        sys.exit(0)
        ```
        
    
    esempio di output dal programma di esempio:
    
    ```
    SUCCESS
    2023-06-18T06:23:42.083Z -- CELL_POS -- DATA
    2023-06-18T06:23:42.081Z -- CELL_POS -- DATA
    2023-06-18T06:23:41.821Z -- CELL_POS -- DATA
    2023-06-18T06:22:39.618Z -- VOLTAGE -- DATA
    2023-06-18T06:22:39.585Z -- RSRP -- DATA
    2023-06-18T06:22:39.585Z -- DEVICE -- DATA
    2023-06-18T06:22:39.524Z -- TEMP -- DATA
    2023-06-18T06:22:39.524Z -- AIR_PRESS -- DATA
    2023-06-18T06:22:39.524Z -- HUMID -- DATA
    2023-06-18T06:22:39.524Z -- AIR_QUAL -- DATA
    pageNextToken: G3oA+I3T...Oy5CXAHw==
    ```
    
9. ( *******added July 17, 2023 7:37 PM* ) — prima di rinunciare totalmente, vorrei almeno provare qualche alternativa su quell’URL
    - passando il parametro nell’URL come l’ultima volta ottengo i messaggi della richiesta precedente
    - e se provassi a metterli nell’header? nemmeno così pare
    
    però aspetta un attimo:
    
    ![Untitled](./Untitled.png)
    
    sapevo che mi stavo perdendo qualcosa! **********use URL Encoding.********** 
    
    e qui il post che dovrebbe risolvere il problema — [How to encode URLs in Python | URLEncoder](https://www.urlencoder.io/python/)
    
    ******************************questa è la via******************************
    
    ```
    >>> import urllib.parse
    >>> params = {'q': 'Python URL encoding', 'as_sitesearch': 'www.urlencoder.io'}
    >>> urllib.parse.urlencode(params)
    'q=Python+URL+encoding&as_sitesearch=www.urlencoder.io'
    ```
    
    ora finalmente riesco ad ottenere due pagine distinte. Ce l’abbiamo fatta, pare. 
    
    - una copia del programma di esempio per il punto 9
        
        ```
        import requests
        import json
        import os, sys, shutil
        from urllib.parse import urlencode
        
        api_entry_point = "https://api.nrfcloud.com/v1"
        api_key = "e14c3da5855436e8a450cdd8464f7dfd9b120aee"
        
        ## ====== REQUEST NO.1 ====== ## 
        print("first request")
        
        msg_url = f"{api_entry_point}/messages" + "?pageLimit=10" + "&pageSort=desc"
        msg_header = {
            "Authorization" : f"Bearer {api_key}"
        }
        res = requests.get(
            url = msg_url,
            headers = msg_header
        )
        
        if res.status_code != requests.codes.ok:
            print(f"ERROR: request returned code {res.status_code} with description: {res.content}")
            sys.exit(1)
        else:
            print("SUCCESS")
        
        # print( json.dumps(res.json(), indent=4) )
        # print( json.dumps(res.json()['items'], indent=4) )
        
        content = res.json()
        for msg in content['items']:
            print(msg['receivedAt'] + " -- " + msg['message']['appId'] + " -- " + msg['message']['messageType'])
        print( "pageNextToken:", content['pageNextToken'] )
        nextPage = content['pageNextToken']
        
        ## ====== REQUEST NO.2 subpage ====== ## 
        print("second request")
        params = {
            'pageNextToken' : nextPage
        }
        
        msg_url = f"{api_entry_point}/messages" + "?" + urlencode(params)
        msg_header = {
            "Authorization" : f"Bearer {api_key}",
        }
        res = requests.get(
            url = msg_url,
            headers = msg_header
        )
        
        if res.status_code != requests.codes.ok:
            print(f"ERROR: request returned code {res.status_code} with description: {res.content}")
            sys.exit(1)
        else:
            print("SUCCESS")
        
        content = res.json()
        for msg in content['items']:
            print(msg['receivedAt'] + " -- " + msg['message']['appId'] + " -- " + msg['message']['messageType'])
        print( "pageNextToken:", content['pageNextToken'] )
        nextPage = content['pageNextToken']
        
        sys.exit(0)
        ```
        
    
    esempio di output dal programma d test:
    
    ```
    first request
    SUCCESS
    2023-06-18T06:23:42.083Z -- CELL_POS -- DATA
    2023-06-18T06:23:42.081Z -- CELL_POS -- DATA
    2023-06-18T06:23:41.821Z -- CELL_POS -- DATA
    2023-06-18T06:22:39.618Z -- VOLTAGE -- DATA
    2023-06-18T06:22:39.585Z -- RSRP -- DATA
    2023-06-18T06:22:39.585Z -- DEVICE -- DATA
    2023-06-18T06:22:39.524Z -- TEMP -- DATA
    2023-06-18T06:22:39.524Z -- AIR_PRESS -- DATA
    2023-06-18T06:22:39.524Z -- HUMID -- DATA
    2023-06-18T06:22:39.524Z -- AIR_QUAL -- DATA
    pageNextToken: G3oA+I3THfVjCJpLl9Mp/3onW8h60J1DgeIoEZ28RCGYyoaH/GCud/v+7hNkqWRN1ccaXWi1td58P7ieah8rhRKVirOBVKeSXZ3GwTKg1/z0z6eZIB18KGmO1NtcKWgMpP1CY5yT88Oy5CXAHw==
    second request
    SUCCESS
    2023-06-18T06:21:42.113Z -- CELL_POS -- DATA
    2023-06-18T06:21:42.093Z -- CELL_POS -- DATA
    2023-06-18T06:21:41.888Z -- CELL_POS -- DATA
    2023-06-18T06:20:39.621Z -- VOLTAGE -- DATA
    2023-06-18T06:20:39.590Z -- DEVICE -- DATA
    2023-06-18T06:20:39.590Z -- RSRP -- DATA
    2023-06-18T06:20:39.524Z -- TEMP -- DATA
    2023-06-18T06:20:39.524Z -- AIR_PRESS -- DATA
    2023-06-18T06:20:39.524Z -- AIR_QUAL -- DATA
    2023-06-18T06:20:39.524Z -- HUMID -- DATA
    pageNextToken: G3oA+I3THfVjCJpLV9tKffOJrYVIh6yWIP18iejkJRLBVDw85Adzrdvm2ycoknAgTVkxlTa0aepxiKPYMCjaUhitUECrIxOO2qcJFYx6fvrnrSYoDLkIzWj7OKstAYsw5cWOOFPKw7LwUuAP
    ```
    
10. ( *******added July 17, 2023 7:52 PM* ) — ora, voglio *********tutte********* le date come iterazione. 
    - questo fa anche le iterazioni
        
        ```
        import requests
        import json
        import os, sys, shutil
        from urllib.parse import urlencode
        
        api_entry_point = "https://api.nrfcloud.com/v1"
        api_key = "...."
        
        print("first request")
        
        pressC = False
        reqNo = 0
        nextPage = ""
        while True:
        	pressC = (str(input(f"REQUEST NO.{reqNo} - press C to close\n")).strip().upper() == "C")
        	if pressC:
        		break
        	reqNo = reqNo + 1
        
        	msg_url = ""
        	msg_header = {
        		"Authorization" : f"Bearer {api_key}"
        	}
        	if nextPage == "":
        		msg_url = f"{api_entry_point}/messages" + "?pageLimit=10" + "&pageSort=desc"
        	else:
        		msg_url = f"{api_entry_point}/messages" + "?" + urlencode({ 'pageNextToken' : nextPage })
        	res = requests.get(
        		url = msg_url,
        		headers = msg_header
        	)
        
        	if res.status_code != requests.codes.ok:
        		print(f"REQUEST NO.{reqNo} -> ERROR: request returned code {res.status_code} with description: {res.content}")
        		sys.exit(1)
        	else:
        		print(f"REQUEST NO.{reqNo} -> SUCCESS")
        	
        	content = res.json()
        	print( f"RECEIVED {content['total']}" )
        	for msg in content['items']:
        		print(msg['receivedAt'] + " -- " + msg['message']['appId'] + " -- " + msg['message']['messageType'])
        	try:
        		nextPage = content['pageNextToken']
        	except KeyError:
        		break
        
        print("END: closing")
        sys.exit(0)
        ```
        
    
    ce ne sono un bel po’. Le riporto qui di seguito. Per ognuna di queste sarebbe interessante fare un’estrazione. 
    
    - l’estrazione
        
        ```
        REQUEST NO.1 -> SUCCESS
        RECEIVED 10
        2023-06-18T06:23:42.083Z -- CELL_POS -- DATA
        2023-06-18T06:23:42.081Z -- CELL_POS -- DATA
        2023-06-18T06:23:41.821Z -- CELL_POS -- DATA
        2023-06-18T06:22:39.618Z -- VOLTAGE -- DATA
        2023-06-18T06:22:39.585Z -- RSRP -- DATA
        2023-06-18T06:22:39.585Z -- DEVICE -- DATA
        2023-06-18T06:22:39.524Z -- TEMP -- DATA
        2023-06-18T06:22:39.524Z -- AIR_PRESS -- DATA
        2023-06-18T06:22:39.524Z -- HUMID -- DATA
        2023-06-18T06:22:39.524Z -- AIR_QUAL -- DATA
        REQUEST NO.1 - press C to close
        REQUEST NO.2 -> SUCCESS
        RECEIVED 10
        2023-06-18T06:21:42.113Z -- CELL_POS -- DATA
        2023-06-18T06:21:42.093Z -- CELL_POS -- DATA
        2023-06-18T06:21:41.888Z -- CELL_POS -- DATA
        2023-06-18T06:20:39.621Z -- VOLTAGE -- DATA
        2023-06-18T06:20:39.590Z -- DEVICE -- DATA
        2023-06-18T06:20:39.590Z -- RSRP -- DATA
        2023-06-18T06:20:39.524Z -- TEMP -- DATA
        2023-06-18T06:20:39.524Z -- AIR_PRESS -- DATA
        2023-06-18T06:20:39.524Z -- AIR_QUAL -- DATA
        2023-06-18T06:20:39.524Z -- HUMID -- DATA
        REQUEST NO.2 - press C to close
        REQUEST NO.3 -> SUCCESS
        RECEIVED 10
        2023-06-18T06:19:41.872Z -- CELL_POS -- DATA
        2023-06-18T06:19:41.868Z -- CELL_POS -- DATA
        2023-06-18T06:19:41.599Z -- CELL_POS -- DATA
        2023-06-18T06:18:39.619Z -- VOLTAGE -- DATA
        2023-06-18T06:18:39.585Z -- DEVICE -- DATA
        2023-06-18T06:18:39.585Z -- RSRP -- DATA
        2023-06-18T06:18:39.524Z -- HUMID -- DATA
        2023-06-18T06:18:39.524Z -- AIR_PRESS -- DATA
        2023-06-18T06:18:39.524Z -- AIR_QUAL -- DATA
        2023-06-18T06:18:39.524Z -- TEMP -- DATA
        REQUEST NO.3 - press C to close
        REQUEST NO.4 -> SUCCESS
        RECEIVED 10
        2023-06-18T06:16:44.259Z -- CELL_POS -- DATA
        2023-06-18T06:16:44.259Z -- CELL_POS -- DATA
        2023-06-18T06:16:43.982Z -- CELL_POS -- DATA
        2023-06-18T06:16:39.572Z -- VOLTAGE -- DATA
        2023-06-18T06:16:39.507Z -- TEMP -- DATA
        2023-06-18T06:16:39.507Z -- AIR_PRESS -- DATA
        2023-06-18T06:16:39.507Z -- HUMID -- DATA
        2023-06-18T06:16:39.507Z -- AIR_QUAL -- DATA
        2023-06-18T06:15:57.152Z -- CELL_POS -- DATA
        2023-06-18T06:15:57.148Z -- CELL_POS -- DATA
        REQUEST NO.4 - press C to close
        REQUEST NO.5 -> SUCCESS
        RECEIVED 10
        2023-06-18T06:15:56.917Z -- CELL_POS -- DATA
        2023-06-18T06:14:39.619Z -- VOLTAGE -- DATA
        2023-06-18T06:14:39.586Z -- RSRP -- DATA
        2023-06-18T06:14:39.586Z -- DEVICE -- DATA
        2023-06-18T06:14:39.523Z -- AIR_QUAL -- DATA
        2023-06-18T06:14:39.523Z -- HUMID -- DATA
        2023-06-18T06:14:39.523Z -- AIR_PRESS -- DATA
        2023-06-18T06:14:39.523Z -- TEMP -- DATA
        2023-06-18T06:12:45.870Z -- CELL_POS -- DATA
        2023-06-18T06:12:45.868Z -- CELL_POS -- DATA
        REQUEST NO.5 - press C to close
        REQUEST NO.6 -> SUCCESS
        RECEIVED 10
        2023-06-18T06:12:45.602Z -- CELL_POS -- DATA
        2023-06-18T06:12:39.573Z -- VOLTAGE -- DATA
        2023-06-18T06:12:39.540Z -- DEVICE -- DATA
        2023-06-18T06:12:39.540Z -- RSRP -- DATA
        2023-06-18T06:12:39.506Z -- AIR_QUAL -- DATA
        2023-06-18T06:12:39.506Z -- TEMP -- DATA
        2023-06-18T06:12:39.506Z -- HUMID -- DATA
        2023-06-18T06:12:39.506Z -- AIR_PRESS -- DATA
        2023-06-18T06:11:56.570Z -- CELL_POS -- DATA
        2023-06-18T06:11:56.570Z -- CELL_POS -- DATA
        REQUEST NO.6 - press C to close
        REQUEST NO.7 -> SUCCESS
        RECEIVED 10
        2023-06-18T06:11:56.415Z -- CELL_POS -- DATA
        2023-06-18T06:10:39.635Z -- VOLTAGE -- DATA
        2023-06-18T06:10:39.602Z -- DEVICE -- DATA
        2023-06-18T06:10:39.602Z -- RSRP -- DATA
        2023-06-18T06:10:39.536Z -- AIR_QUAL -- DATA
        2023-06-18T06:10:39.536Z -- TEMP -- DATA
        2023-06-18T06:10:39.536Z -- HUMID -- DATA
        2023-06-18T06:10:39.536Z -- AIR_PRESS -- DATA
        2023-06-18T06:08:50.683Z -- CELL_POS -- DATA
        2023-06-18T06:08:50.681Z -- CELL_POS -- DATA
        REQUEST NO.7 - press C to close
        REQUEST NO.8 -> SUCCESS
        RECEIVED 10
        2023-06-18T06:08:50.420Z -- CELL_POS -- DATA
        2023-06-18T06:08:50.280Z -- AGPS -- DATA
        2023-06-18T06:08:49.312Z -- VOLTAGE -- DATA
        2023-06-18T06:08:49.279Z -- DEVICE -- DATA
        2023-06-18T06:08:49.279Z -- RSRP -- DATA
        2023-06-18T06:08:49.247Z -- DEVICE -- DATA
        2023-06-18T06:08:49.174Z -- AIR_QUAL -- DATA
        2023-06-18T06:08:49.174Z -- HUMID -- DATA
        2023-06-18T06:08:49.174Z -- AIR_PRESS -- DATA
        2023-06-18T06:08:49.174Z -- TEMP -- DATA
        END: closing
        ```
        
    
    Nota che l token next page non viene aggiunto alla risposta nel caso la richiesta contenga tutti i messaggi. 
    
11. ( *******added July 17, 2023 8:12 PM* ) — adesso che abbiamo il nostro script iteratore, siamo felici, ok, però adesso mi si pone il problema di scoprire *******quanti tipi di messaggo ho******* direttamente dai dati. 
    
    Potrei fare qualcosa di più complesso, ma per il momento mi accontento di averli tutti in un stesso file. 
    
    - leggermente modificato per scrivere su un file
        
        ```
        import requests
        import json
        import os, sys, shutil
        from urllib.parse import urlencode
        
        api_entry_point = "https://api.nrfcloud.com/v1"
        api_key = "..."
        
        print("first request")
        
        pressC = False
        reqNo = 0
        nextPage = ""
        file_content = ""
        while True:
        	'''
        	pressC = (str(input(f"REQUEST NO.{reqNo} - press C to close\n")).strip().upper() == "C")
        	if pressC:
        		break
        	'''
        	reqNo = reqNo + 1
        
        	msg_url = ""
        	msg_header = {
        		"Authorization" : f"Bearer {api_key}"
        	}
        	if nextPage == "":
        		msg_url = f"{api_entry_point}/messages" + "?pageLimit=10" + "&pageSort=desc"
        	else:
        		msg_url = f"{api_entry_point}/messages" + "?" + urlencode({ 'pageNextToken' : nextPage })
        	res = requests.get(
        		url = msg_url,
        		headers = msg_header
        	)
        
        	if res.status_code != requests.codes.ok:
        		print(f"REQUEST NO.{reqNo} -> ERROR: request returned code {res.status_code} with description: {res.content}")
        		sys.exit(1)
        	else:
        		print(f"REQUEST NO.{reqNo} -> SUCCESS")
        	
        	content = res.json()
        	print( f"RECEIVED {content['total']}" )
        	for msg in content['items']:
        		print(msg['receivedAt'] + " -- " + msg['message']['appId'] + " -- " + msg['message']['messageType'])
        		file_content = file_content + "\n" + json.dumps( msg, indent=4 )
        	try:
        		nextPage = content['pageNextToken']
        	except KeyError:
        		break
        
        print("saving content on file...")
        with open( "../history.txt", "w" ) as fil:
        	fil.write(file_content)
        
        print("END: closing")
        sys.exit(0)
        ```
        
    - esempio di file ottenuto
        
        ```
        {
            "topic": "prod/0b3486e5-a1e9-4054-9d3f-c5e623bff7f4/m/d/nrf-352656101104563/c2d",
            "deviceId": "N/A",
            "receivedAt": "2023-06-18T06:23:42.083Z",
            "message": {
                "appId": "CELL_POS",
                "messageType": "DATA",
                "data": {
                    "lat": 44.4434427,
                    "lon": 8.9020756,
                    "uncertainty": 562,
                    "fulfilledWith": "MCELL"
                }
            },
            "tenantId": "0b3486e5-a1e9-4054-9d3f-c5e623bff7f4"
        }
        {
            "topic": "prod/0b3486e5-a1e9-4054-9d3f-c5e623bff7f4/m/d/nrf-352656101104563/cell_pos/r",
            "deviceId": "N/A",
            "receivedAt": "2023-06-18T06:23:42.081Z",
            "message": {
                "appId": "CELL_POS",
                "messageType": "DATA",
                "data": {
                    "lat": 44.4434427,
                    "lon": 8.9020756,
                    "uncertainty": 562,
                    "fulfilledWith": "MCELL"
                }
            },
            "tenantId": "0b3486e5-a1e9-4054-9d3f-c5e623bff7f4"
        }
        {
            "topic": "prod/0b3486e5-a1e9-4054-9d3f-c5e623bff7f4/m/d/nrf-352656101104563/d2c",
            "deviceId": "nrf-352656101104563",
            "receivedAt": "2023-06-18T06:23:41.821Z",
            "message": {
                "appId": "CELL_POS",
                "messageType": "DATA",
                "data": {
                    "lte": [
                        {
                            "eci": 4956959,
                            "mcc": 222,
                            "mnc": 10,
                            "tac": 12050,
                            "earfcn": 1850,
                            "rsrp": -97,
                            "rsrq": -9,
                            "nmr": [
                                {
                                    "earfcn": 1850,
                                    "pci": 340,
                                    "rsrp": -98,
                                    "rsrq": -10
                                },
                                {
                                    "earfcn": 1850,
                                    "pci": 464,
                                    "rsrp": -105,
                                    "rsrq": -17.5
                                }
                            ]
                        }
                    ]
                }
            },
            "tenantId": "0b3486e5-a1e9-4054-9d3f-c5e623bff7f4"
        }
        {
            "topic": "prod/0b3486e5-a1e9-4054-9d3f-c5e623bff7f4/m/d/nrf-352656101104563/d2c",
            "deviceId": "nrf-352656101104563",
            "receivedAt": "2023-06-18T06:22:39.618Z",
            "message": {
                "appId": "VOLTAGE",
                "messageType": "DATA",
                "ts": 1687069359618,
                "data": "4015"
            },
            "tenantId": "0b3486e5-a1e9-4054-9d3f-c5e623bff7f4"
        }
        {
            "topic": "prod/0b3486e5-a1e9-4054-9d3f-c5e623bff7f4/m/d/nrf-352656101104563/d2c",
            "deviceId": "nrf-352656101104563",
            "receivedAt": "2023-06-18T06:22:39.585Z",
            "message": {
                "appId": "RSRP",
                "messageType": "DATA",
                "ts": 1687069359585,
                "data": "-96"
            },
            "tenantId": "0b3486e5-a1e9-4054-9d3f-c5e623bff7f4"
        }
        {
            "topic": "prod/0b3486e5-a1e9-4054-9d3f-c5e623bff7f4/m/d/nrf-352656101104563/d2c",
            "deviceId": "nrf-352656101104563",
            "receivedAt": "2023-06-18T06:22:39.585Z",
            "message": {
                "appId": "DEVICE",
                "messageType": "DATA",
                "ts": 1687069359585,
                "data": {
                    "networkInfo": {
                        "rsrp": -96,
                        "cellID": 4956959
                    }
                }
            },
            "tenantId": "0b3486e5-a1e9-4054-9d3f-c5e623bff7f4"
        }
        {
            "topic": "prod/0b3486e5-a1e9-4054-9d3f-c5e623bff7f4/m/d/nrf-352656101104563/d2c",
            "deviceId": "nrf-352656101104563",
            "receivedAt": "2023-06-18T06:22:39.524Z",
            "message": {
                "appId": "TEMP",
                "messageType": "DATA",
                "ts": 1687069359524,
                "data": "27.56"
            },
            "tenantId": "0b3486e5-a1e9-4054-9d3f-c5e623bff7f4"
        }
        {
            "topic": "prod/0b3486e5-a1e9-4054-9d3f-c5e623bff7f4/m/d/nrf-352656101104563/d2c",
            "deviceId": "nrf-352656101104563",
            "receivedAt": "2023-06-18T06:22:39.524Z",
            "message": {
                "appId": "AIR_PRESS",
                "messageType": "DATA",
                "ts": 1687069359524,
                "data": "101.33"
            },
            "tenantId": "0b3486e5-a1e9-4054-9d3f-c5e623bff7f4"
        }
        {
            "topic": "prod/0b3486e5-a1e9-4054-9d3f-c5e623bff7f4/m/d/nrf-352656101104563/d2c",
            "deviceId": "nrf-352656101104563",
            "receivedAt": "2023-06-18T06:22:39.524Z",
            "message": {
                "appId": "HUMID",
                "messageType": "DATA",
                "ts": 1687069359524,
                "data": "58.19"
            },
            "tenantId": "0b3486e5-a1e9-4054-9d3f-c5e623bff7f4"
        }
        {
            "topic": "prod/0b3486e5-a1e9-4054-9d3f-c5e623bff7f4/m/d/nrf-352656101104563/d2c",
            "deviceId": "nrf-352656101104563",
            "receivedAt": "2023-06-18T06:22:39.524Z",
            "message": {
                "appId": "AIR_QUAL",
                "messageType": "DATA",
                "ts": 1687069359524,
                "data": "14"
            },
            "tenantId": "0b3486e5-a1e9-4054-9d3f-c5e623bff7f4"
        }
        {
            "topic": "prod/0b3486e5-a1e9-4054-9d3f-c5e623bff7f4/m/d/nrf-352656101104563/cell_pos/r",
            "deviceId": "N/A",
            "receivedAt": "2023-06-18T06:21:42.113Z",
            "message": {
                "appId": "CELL_POS",
                "messageType": "DATA",
                "data": {
                    "lat": 44.43812013,
                    "lon": 8.89336824,
                    "uncertainty": 414,
                    "fulfilledWith": "MCELL"
                }
            },
            "tenantId": "0b3486e5-a1e9-4054-9d3f-c5e623bff7f4"
        }
        {
            "topic": "prod/0b3486e5-a1e9-4054-9d3f-c5e623bff7f4/m/d/nrf-352656101104563/c2d",
            "deviceId": "N/A",
            "receivedAt": "2023-06-18T06:21:42.093Z",
            "message": {
                "appId": "CELL_POS",
                "messageType": "DATA",
                "data": {
                    "lat": 44.43812013,
                    "lon": 8.89336824,
                    "uncertainty": 414,
                    "fulfilledWith": "MCELL"
                }
            },
            "tenantId": "0b3486e5-a1e9-4054-9d3f-c5e623bff7f4"
        }
        {
            "topic": "prod/0b3486e5-a1e9-4054-9d3f-c5e623bff7f4/m/d/nrf-352656101104563/d2c",
            "deviceId": "nrf-352656101104563",
            "receivedAt": "2023-06-18T06:21:41.888Z",
            "message": {
                "appId": "CELL_POS",
                "messageType": "DATA",
                "data": {
                    "lte": [
                        {
                            "eci": 4956960,
                            "mcc": 222,
                            "mnc": 10,
                            "tac": 12050,
                            "earfcn": 1850,
                            "rsrp": -98,
                            "rsrq": -11,
                            "adv": 80,
                            "nmr": [
                                {
                                    "earfcn": 1850,
                                    "pci": 96,
                                    "rsrp": -98,
                                    "rsrq": -10.5
                                },
                                {
                                    "earfcn": 1850,
                                    "pci": 464,
                                    "rsrp": -105,
                                    "rsrq": -17.5
                                }
                            ]
                        }
                    ]
                }
            },
            "tenantId": "0b3486e5-a1e9-4054-9d3f-c5e623bff7f4"
        }
        {
            "topic": "prod/0b3486e5-a1e9-4054-9d3f-c5e623bff7f4/m/d/nrf-352656101104563/d2c",
            "deviceId": "nrf-352656101104563",
            "receivedAt": "2023-06-18T06:20:39.621Z",
            "message": {
                "appId": "VOLTAGE",
                "messageType": "DATA",
                "ts": 1687069239621,
                "data": "4015"
            },
            "tenantId": "0b3486e5-a1e9-4054-9d3f-c5e623bff7f4"
        }
        {
            "topic": "prod/0b3486e5-a1e9-4054-9d3f-c5e623bff7f4/m/d/nrf-352656101104563/d2c",
            "deviceId": "nrf-352656101104563",
            "receivedAt": "2023-06-18T06:20:39.590Z",
            "message": {
                "appId": "DEVICE",
                "messageType": "DATA",
                "ts": 1687069239590,
                "data": {
                    "networkInfo": {
                        "rsrp": -102,
                        "cellID": 4956960
                    }
                }
            },
            "tenantId": "0b3486e5-a1e9-4054-9d3f-c5e623bff7f4"
        }
        {
            "topic": "prod/0b3486e5-a1e9-4054-9d3f-c5e623bff7f4/m/d/nrf-352656101104563/d2c",
            "deviceId": "nrf-352656101104563",
            "receivedAt": "2023-06-18T06:20:39.590Z",
            "message": {
                "appId": "RSRP",
                "messageType": "DATA",
                "ts": 1687069239590,
                "data": "-102"
            },
            "tenantId": "0b3486e5-a1e9-4054-9d3f-c5e623bff7f4"
        }
        {
            "topic": "prod/0b3486e5-a1e9-4054-9d3f-c5e623bff7f4/m/d/nrf-352656101104563/d2c",
            "deviceId": "nrf-352656101104563",
            "receivedAt": "2023-06-18T06:20:39.524Z",
            "message": {
                "appId": "TEMP",
                "messageType": "DATA",
                "ts": 1687069239524,
                "data": "27.30"
            },
            "tenantId": "0b3486e5-a1e9-4054-9d3f-c5e623bff7f4"
        }
        {
            "topic": "prod/0b3486e5-a1e9-4054-9d3f-c5e623bff7f4/m/d/nrf-352656101104563/d2c",
            "deviceId": "nrf-352656101104563",
            "receivedAt": "2023-06-18T06:20:39.524Z",
            "message": {
                "appId": "AIR_PRESS",
                "messageType": "DATA",
                "ts": 1687069239524,
                "data": "101.33"
            },
            "tenantId": "0b3486e5-a1e9-4054-9d3f-c5e623bff7f4"
        }
        {
            "topic": "prod/0b3486e5-a1e9-4054-9d3f-c5e623bff7f4/m/d/nrf-352656101104563/d2c",
            "deviceId": "nrf-352656101104563",
            "receivedAt": "2023-06-18T06:20:39.524Z",
            "message": {
                "appId": "AIR_QUAL",
                "messageType": "DATA",
                "ts": 1687069239524,
                "data": "4"
            },
            "tenantId": "0b3486e5-a1e9-4054-9d3f-c5e623bff7f4"
        }
        {
            "topic": "prod/0b3486e5-a1e9-4054-9d3f-c5e623bff7f4/m/d/nrf-352656101104563/d2c",
            "deviceId": "nrf-352656101104563",
            "receivedAt": "2023-06-18T06:20:39.524Z",
            "message": {
                "appId": "HUMID",
                "messageType": "DATA",
                "ts": 1687069239524,
                "data": "58.85"
            },
            "tenantId": "0b3486e5-a1e9-4054-9d3f-c5e623bff7f4"
        }
        {
            "topic": "prod/0b3486e5-a1e9-4054-9d3f-c5e623bff7f4/m/d/nrf-352656101104563/cell_pos/r",
            "deviceId": "N/A",
            "receivedAt": "2023-06-18T06:19:41.872Z",
            "message": {
                "appId": "CELL_POS",
                "messageType": "DATA",
                "data": {
                    "lat": 44.4434427,
                    "lon": 8.9020756,
                    "uncertainty": 562,
                    "fulfilledWith": "MCELL"
                }
            },
            "tenantId": "0b3486e5-a1e9-4054-9d3f-c5e623bff7f4"
        }
        {
            "topic": "prod/0b3486e5-a1e9-4054-9d3f-c5e623bff7f4/m/d/nrf-352656101104563/c2d",
            "deviceId": "N/A",
            "receivedAt": "2023-06-18T06:19:41.868Z",
            "message": {
                "appId": "CELL_POS",
                "messageType": "DATA",
                "data": {
                    "lat": 44.4434427,
                    "lon": 8.9020756,
                    "uncertainty": 562,
                    "fulfilledWith": "MCELL"
                }
            },
            "tenantId": "0b3486e5-a1e9-4054-9d3f-c5e623bff7f4"
        }
        {
            "topic": "prod/0b3486e5-a1e9-4054-9d3f-c5e623bff7f4/m/d/nrf-352656101104563/d2c",
            "deviceId": "nrf-352656101104563",
            "receivedAt": "2023-06-18T06:19:41.599Z",
            "message": {
                "appId": "CELL_POS",
                "messageType": "DATA",
                "data": {
                    "lte": [
                        {
                            "eci": 4956959,
                            "mcc": 222,
                            "mnc": 10,
                            "tac": 12050,
                            "earfcn": 1850,
                            "rsrp": -99,
                            "rsrq": -11.5,
                            "adv": 80,
                            "nmr": [
                                {
                                    "earfcn": 1850,
                                    "pci": 340,
                                    "rsrp": -98,
                                    "rsrq": -11
                                },
                                {
                                    "earfcn": 1850,
                                    "pci": 464,
                                    "rsrp": -103,
                                    "rsrq": -15.5
                                }
                            ]
                        }
                    ]
                }
            },
            "tenantId": "0b3486e5-a1e9-4054-9d3f-c5e623bff7f4"
        }
        {
            "topic": "prod/0b3486e5-a1e9-4054-9d3f-c5e623bff7f4/m/d/nrf-352656101104563/d2c",
            "deviceId": "nrf-352656101104563",
            "receivedAt": "2023-06-18T06:18:39.619Z",
            "message": {
                "appId": "VOLTAGE",
                "messageType": "DATA",
                "ts": 1687069119619,
                "data": "4019"
            },
            "tenantId": "0b3486e5-a1e9-4054-9d3f-c5e623bff7f4"
        }
        {
            "topic": "prod/0b3486e5-a1e9-4054-9d3f-c5e623bff7f4/m/d/nrf-352656101104563/d2c",
            "deviceId": "nrf-352656101104563",
            "receivedAt": "2023-06-18T06:18:39.585Z",
            "message": {
                "appId": "DEVICE",
                "messageType": "DATA",
                "ts": 1687069119585,
                "data": {
                    "networkInfo": {
                        "rsrp": -98
                    }
                }
            },
            "tenantId": "0b3486e5-a1e9-4054-9d3f-c5e623bff7f4"
        }
        {
            "topic": "prod/0b3486e5-a1e9-4054-9d3f-c5e623bff7f4/m/d/nrf-352656101104563/d2c",
            "deviceId": "nrf-352656101104563",
            "receivedAt": "2023-06-18T06:18:39.585Z",
            "message": {
                "appId": "RSRP",
                "messageType": "DATA",
                "ts": 1687069119585,
                "data": "-98"
            },
            "tenantId": "0b3486e5-a1e9-4054-9d3f-c5e623bff7f4"
        }
        {
            "topic": "prod/0b3486e5-a1e9-4054-9d3f-c5e623bff7f4/m/d/nrf-352656101104563/d2c",
            "deviceId": "nrf-352656101104563",
            "receivedAt": "2023-06-18T06:18:39.524Z",
            "message": {
                "appId": "HUMID",
                "messageType": "DATA",
                "ts": 1687069119524,
                "data": "59.73"
            },
            "tenantId": "0b3486e5-a1e9-4054-9d3f-c5e623bff7f4"
        }
        {
            "topic": "prod/0b3486e5-a1e9-4054-9d3f-c5e623bff7f4/m/d/nrf-352656101104563/d2c",
            "deviceId": "nrf-352656101104563",
            "receivedAt": "2023-06-18T06:18:39.524Z",
            "message": {
                "appId": "AIR_PRESS",
                "messageType": "DATA",
                "ts": 1687069119524,
                "data": "101.33"
            },
            "tenantId": "0b3486e5-a1e9-4054-9d3f-c5e623bff7f4"
        }
        {
            "topic": "prod/0b3486e5-a1e9-4054-9d3f-c5e623bff7f4/m/d/nrf-352656101104563/d2c",
            "deviceId": "nrf-352656101104563",
            "receivedAt": "2023-06-18T06:18:39.524Z",
            "message": {
                "appId": "AIR_QUAL",
                "messageType": "DATA",
                "ts": 1687069119524,
                "data": "0"
            },
            "tenantId": "0b3486e5-a1e9-4054-9d3f-c5e623bff7f4"
        }
        {
            "topic": "prod/0b3486e5-a1e9-4054-9d3f-c5e623bff7f4/m/d/nrf-352656101104563/d2c",
            "deviceId": "nrf-352656101104563",
            "receivedAt": "2023-06-18T06:18:39.524Z",
            "message": {
                "appId": "TEMP",
                "messageType": "DATA",
                "ts": 1687069119524,
                "data": "26.83"
            },
            "tenantId": "0b3486e5-a1e9-4054-9d3f-c5e623bff7f4"
        }
        {
            "topic": "prod/0b3486e5-a1e9-4054-9d3f-c5e623bff7f4/m/d/nrf-352656101104563/c2d",
            "deviceId": "N/A",
            "receivedAt": "2023-06-18T06:16:44.259Z",
            "message": {
                "appId": "CELL_POS",
                "messageType": "DATA",
                "data": {
                    "lat": 44.4434427,
                    "lon": 8.9020756,
                    "uncertainty": 562,
                    "fulfilledWith": "MCELL"
                }
            },
            "tenantId": "0b3486e5-a1e9-4054-9d3f-c5e623bff7f4"
        }
        {
            "topic": "prod/0b3486e5-a1e9-4054-9d3f-c5e623bff7f4/m/d/nrf-352656101104563/cell_pos/r",
            "deviceId": "N/A",
            "receivedAt": "2023-06-18T06:16:44.259Z",
            "message": {
                "appId": "CELL_POS",
                "messageType": "DATA",
                "data": {
                    "lat": 44.4434427,
                    "lon": 8.9020756,
                    "uncertainty": 562,
                    "fulfilledWith": "MCELL"
                }
            },
            "tenantId": "0b3486e5-a1e9-4054-9d3f-c5e623bff7f4"
        }
        {
            "topic": "prod/0b3486e5-a1e9-4054-9d3f-c5e623bff7f4/m/d/nrf-352656101104563/d2c",
            "deviceId": "nrf-352656101104563",
            "receivedAt": "2023-06-18T06:16:43.982Z",
            "message": {
                "appId": "CELL_POS",
                "messageType": "DATA",
                "data": {
                    "lte": [
                        {
                            "eci": 4956959,
                            "mcc": 222,
                            "mnc": 10,
                            "tac": 12050,
                            "earfcn": 1850,
                            "rsrp": -90,
                            "rsrq": -9.5,
                            "nmr": [
                                {
                                    "earfcn": 1850,
                                    "pci": 340,
                                    "rsrp": -91,
                                    "rsrq": -10
                                }
                            ]
                        }
                    ]
                }
            },
            "tenantId": "0b3486e5-a1e9-4054-9d3f-c5e623bff7f4"
        }
        {
            "topic": "prod/0b3486e5-a1e9-4054-9d3f-c5e623bff7f4/m/d/nrf-352656101104563/d2c",
            "deviceId": "nrf-352656101104563",
            "receivedAt": "2023-06-18T06:16:39.572Z",
            "message": {
                "appId": "VOLTAGE",
                "messageType": "DATA",
                "ts": 1687068999572,
                "data": "4003"
            },
            "tenantId": "0b3486e5-a1e9-4054-9d3f-c5e623bff7f4"
        }
        {
            "topic": "prod/0b3486e5-a1e9-4054-9d3f-c5e623bff7f4/m/d/nrf-352656101104563/d2c",
            "deviceId": "nrf-352656101104563",
            "receivedAt": "2023-06-18T06:16:39.507Z",
            "message": {
                "appId": "TEMP",
                "messageType": "DATA",
                "ts": 1687068999507,
                "data": "27.63"
            },
            "tenantId": "0b3486e5-a1e9-4054-9d3f-c5e623bff7f4"
        }
        {
            "topic": "prod/0b3486e5-a1e9-4054-9d3f-c5e623bff7f4/m/d/nrf-352656101104563/d2c",
            "deviceId": "nrf-352656101104563",
            "receivedAt": "2023-06-18T06:16:39.507Z",
            "message": {
                "appId": "AIR_PRESS",
                "messageType": "DATA",
                "ts": 1687068999507,
                "data": "101.32"
            },
            "tenantId": "0b3486e5-a1e9-4054-9d3f-c5e623bff7f4"
        }
        {
            "topic": "prod/0b3486e5-a1e9-4054-9d3f-c5e623bff7f4/m/d/nrf-352656101104563/d2c",
            "deviceId": "nrf-352656101104563",
            "receivedAt": "2023-06-18T06:16:39.507Z",
            "message": {
                "appId": "HUMID",
                "messageType": "DATA",
                "ts": 1687068999507,
                "data": "58.29"
            },
            "tenantId": "0b3486e5-a1e9-4054-9d3f-c5e623bff7f4"
        }
        {
            "topic": "prod/0b3486e5-a1e9-4054-9d3f-c5e623bff7f4/m/d/nrf-352656101104563/d2c",
            "deviceId": "nrf-352656101104563",
            "receivedAt": "2023-06-18T06:16:39.507Z",
            "message": {
                "appId": "AIR_QUAL",
                "messageType": "DATA",
                "ts": 1687068999507,
                "data": "0"
            },
            "tenantId": "0b3486e5-a1e9-4054-9d3f-c5e623bff7f4"
        }
        {
            "topic": "prod/0b3486e5-a1e9-4054-9d3f-c5e623bff7f4/m/d/nrf-352656101104563/cell_pos/r",
            "deviceId": "N/A",
            "receivedAt": "2023-06-18T06:15:57.152Z",
            "message": {
                "appId": "CELL_POS",
                "messageType": "DATA",
                "data": {
                    "lat": 44.4434427,
                    "lon": 8.9020756,
                    "uncertainty": 562,
                    "fulfilledWith": "MCELL"
                }
            },
            "tenantId": "0b3486e5-a1e9-4054-9d3f-c5e623bff7f4"
        }
        {
            "topic": "prod/0b3486e5-a1e9-4054-9d3f-c5e623bff7f4/m/d/nrf-352656101104563/c2d",
            "deviceId": "N/A",
            "receivedAt": "2023-06-18T06:15:57.148Z",
            "message": {
                "appId": "CELL_POS",
                "messageType": "DATA",
                "data": {
                    "lat": 44.4434427,
                    "lon": 8.9020756,
                    "uncertainty": 562,
                    "fulfilledWith": "MCELL"
                }
            },
            "tenantId": "0b3486e5-a1e9-4054-9d3f-c5e623bff7f4"
        }
        {
            "topic": "prod/0b3486e5-a1e9-4054-9d3f-c5e623bff7f4/m/d/nrf-352656101104563/d2c",
            "deviceId": "nrf-352656101104563",
            "receivedAt": "2023-06-18T06:15:56.917Z",
            "message": {
                "appId": "CELL_POS",
                "messageType": "DATA",
                "data": {
                    "lte": [
                        {
                            "eci": 4956959,
                            "mcc": 222,
                            "mnc": 10,
                            "tac": 12050,
                            "earfcn": 1850,
                            "rsrp": -89,
                            "rsrq": -11.5,
                            "adv": 80,
                            "nmr": [
                                {
                                    "earfcn": 1850,
                                    "pci": 340,
                                    "rsrp": -90,
                                    "rsrq": -12.5
                                }
                            ]
                        }
                    ]
                }
            },
            "tenantId": "0b3486e5-a1e9-4054-9d3f-c5e623bff7f4"
        }
        {
            "topic": "prod/0b3486e5-a1e9-4054-9d3f-c5e623bff7f4/m/d/nrf-352656101104563/d2c",
            "deviceId": "nrf-352656101104563",
            "receivedAt": "2023-06-18T06:14:39.619Z",
            "message": {
                "appId": "VOLTAGE",
                "messageType": "DATA",
                "ts": 1687068879619,
                "data": "4015"
            },
            "tenantId": "0b3486e5-a1e9-4054-9d3f-c5e623bff7f4"
        }
        {
            "topic": "prod/0b3486e5-a1e9-4054-9d3f-c5e623bff7f4/m/d/nrf-352656101104563/d2c",
            "deviceId": "nrf-352656101104563",
            "receivedAt": "2023-06-18T06:14:39.586Z",
            "message": {
                "appId": "RSRP",
                "messageType": "DATA",
                "ts": 1687068879586,
                "data": "-92"
            },
            "tenantId": "0b3486e5-a1e9-4054-9d3f-c5e623bff7f4"
        }
        {
            "topic": "prod/0b3486e5-a1e9-4054-9d3f-c5e623bff7f4/m/d/nrf-352656101104563/d2c",
            "deviceId": "nrf-352656101104563",
            "receivedAt": "2023-06-18T06:14:39.586Z",
            "message": {
                "appId": "DEVICE",
                "messageType": "DATA",
                "ts": 1687068879586,
                "data": {
                    "networkInfo": {
                        "rsrp": -92
                    }
                }
            },
            "tenantId": "0b3486e5-a1e9-4054-9d3f-c5e623bff7f4"
        }
        {
            "topic": "prod/0b3486e5-a1e9-4054-9d3f-c5e623bff7f4/m/d/nrf-352656101104563/d2c",
            "deviceId": "nrf-352656101104563",
            "receivedAt": "2023-06-18T06:14:39.523Z",
            "message": {
                "appId": "AIR_QUAL",
                "messageType": "DATA",
                "ts": 1687068879523,
                "data": "0"
            },
            "tenantId": "0b3486e5-a1e9-4054-9d3f-c5e623bff7f4"
        }
        {
            "topic": "prod/0b3486e5-a1e9-4054-9d3f-c5e623bff7f4/m/d/nrf-352656101104563/d2c",
            "deviceId": "nrf-352656101104563",
            "receivedAt": "2023-06-18T06:14:39.523Z",
            "message": {
                "appId": "HUMID",
                "messageType": "DATA",
                "ts": 1687068879523,
                "data": "60.44"
            },
            "tenantId": "0b3486e5-a1e9-4054-9d3f-c5e623bff7f4"
        }
        {
            "topic": "prod/0b3486e5-a1e9-4054-9d3f-c5e623bff7f4/m/d/nrf-352656101104563/d2c",
            "deviceId": "nrf-352656101104563",
            "receivedAt": "2023-06-18T06:14:39.523Z",
            "message": {
                "appId": "AIR_PRESS",
                "messageType": "DATA",
                "ts": 1687068879523,
                "data": "101.33"
            },
            "tenantId": "0b3486e5-a1e9-4054-9d3f-c5e623bff7f4"
        }
        {
            "topic": "prod/0b3486e5-a1e9-4054-9d3f-c5e623bff7f4/m/d/nrf-352656101104563/d2c",
            "deviceId": "nrf-352656101104563",
            "receivedAt": "2023-06-18T06:14:39.523Z",
            "message": {
                "appId": "TEMP",
                "messageType": "DATA",
                "ts": 1687068879523,
                "data": "26.73"
            },
            "tenantId": "0b3486e5-a1e9-4054-9d3f-c5e623bff7f4"
        }
        {
            "topic": "prod/0b3486e5-a1e9-4054-9d3f-c5e623bff7f4/m/d/nrf-352656101104563/c2d",
            "deviceId": "N/A",
            "receivedAt": "2023-06-18T06:12:45.870Z",
            "message": {
                "appId": "CELL_POS",
                "messageType": "DATA",
                "data": {
                    "lat": 44.4434427,
                    "lon": 8.9020756,
                    "uncertainty": 562,
                    "fulfilledWith": "MCELL"
                }
            },
            "tenantId": "0b3486e5-a1e9-4054-9d3f-c5e623bff7f4"
        }
        {
            "topic": "prod/0b3486e5-a1e9-4054-9d3f-c5e623bff7f4/m/d/nrf-352656101104563/cell_pos/r",
            "deviceId": "N/A",
            "receivedAt": "2023-06-18T06:12:45.868Z",
            "message": {
                "appId": "CELL_POS",
                "messageType": "DATA",
                "data": {
                    "lat": 44.4434427,
                    "lon": 8.9020756,
                    "uncertainty": 562,
                    "fulfilledWith": "MCELL"
                }
            },
            "tenantId": "0b3486e5-a1e9-4054-9d3f-c5e623bff7f4"
        }
        {
            "topic": "prod/0b3486e5-a1e9-4054-9d3f-c5e623bff7f4/m/d/nrf-352656101104563/d2c",
            "deviceId": "nrf-352656101104563",
            "receivedAt": "2023-06-18T06:12:45.602Z",
            "message": {
                "appId": "CELL_POS",
                "messageType": "DATA",
                "data": {
                    "lte": [
                        {
                            "eci": 4956959,
                            "mcc": 222,
                            "mnc": 10,
                            "tac": 12050,
                            "earfcn": 1850,
                            "rsrp": -91,
                            "rsrq": -9.5,
                            "adv": 80,
                            "nmr": [
                                {
                                    "earfcn": 1850,
                                    "pci": 340,
                                    "rsrp": -95,
                                    "rsrq": -13.5
                                },
                                {
                                    "earfcn": 1850,
                                    "pci": 464,
                                    "rsrp": -97,
                                    "rsrq": -15.5
                                }
                            ]
                        }
                    ]
                }
            },
            "tenantId": "0b3486e5-a1e9-4054-9d3f-c5e623bff7f4"
        }
        {
            "topic": "prod/0b3486e5-a1e9-4054-9d3f-c5e623bff7f4/m/d/nrf-352656101104563/d2c",
            "deviceId": "nrf-352656101104563",
            "receivedAt": "2023-06-18T06:12:39.573Z",
            "message": {
                "appId": "VOLTAGE",
                "messageType": "DATA",
                "ts": 1687068759573,
                "data": "3981"
            },
            "tenantId": "0b3486e5-a1e9-4054-9d3f-c5e623bff7f4"
        }
        {
            "topic": "prod/0b3486e5-a1e9-4054-9d3f-c5e623bff7f4/m/d/nrf-352656101104563/d2c",
            "deviceId": "nrf-352656101104563",
            "receivedAt": "2023-06-18T06:12:39.540Z",
            "message": {
                "appId": "DEVICE",
                "messageType": "DATA",
                "ts": 1687068759540,
                "data": {
                    "networkInfo": {
                        "rsrp": -96
                    }
                }
            },
            "tenantId": "0b3486e5-a1e9-4054-9d3f-c5e623bff7f4"
        }
        {
            "topic": "prod/0b3486e5-a1e9-4054-9d3f-c5e623bff7f4/m/d/nrf-352656101104563/d2c",
            "deviceId": "nrf-352656101104563",
            "receivedAt": "2023-06-18T06:12:39.540Z",
            "message": {
                "appId": "RSRP",
                "messageType": "DATA",
                "ts": 1687068759540,
                "data": "-96"
            },
            "tenantId": "0b3486e5-a1e9-4054-9d3f-c5e623bff7f4"
        }
        {
            "topic": "prod/0b3486e5-a1e9-4054-9d3f-c5e623bff7f4/m/d/nrf-352656101104563/d2c",
            "deviceId": "nrf-352656101104563",
            "receivedAt": "2023-06-18T06:12:39.506Z",
            "message": {
                "appId": "AIR_QUAL",
                "messageType": "DATA",
                "ts": 1687068759506,
                "data": "7"
            },
            "tenantId": "0b3486e5-a1e9-4054-9d3f-c5e623bff7f4"
        }
        {
            "topic": "prod/0b3486e5-a1e9-4054-9d3f-c5e623bff7f4/m/d/nrf-352656101104563/d2c",
            "deviceId": "nrf-352656101104563",
            "receivedAt": "2023-06-18T06:12:39.506Z",
            "message": {
                "appId": "TEMP",
                "messageType": "DATA",
                "ts": 1687068759506,
                "data": "27.52"
            },
            "tenantId": "0b3486e5-a1e9-4054-9d3f-c5e623bff7f4"
        }
        {
            "topic": "prod/0b3486e5-a1e9-4054-9d3f-c5e623bff7f4/m/d/nrf-352656101104563/d2c",
            "deviceId": "nrf-352656101104563",
            "receivedAt": "2023-06-18T06:12:39.506Z",
            "message": {
                "appId": "HUMID",
                "messageType": "DATA",
                "ts": 1687068759506,
                "data": "58.75"
            },
            "tenantId": "0b3486e5-a1e9-4054-9d3f-c5e623bff7f4"
        }
        {
            "topic": "prod/0b3486e5-a1e9-4054-9d3f-c5e623bff7f4/m/d/nrf-352656101104563/d2c",
            "deviceId": "nrf-352656101104563",
            "receivedAt": "2023-06-18T06:12:39.506Z",
            "message": {
                "appId": "AIR_PRESS",
                "messageType": "DATA",
                "ts": 1687068759506,
                "data": "101.32"
            },
            "tenantId": "0b3486e5-a1e9-4054-9d3f-c5e623bff7f4"
        }
        {
            "topic": "prod/0b3486e5-a1e9-4054-9d3f-c5e623bff7f4/m/d/nrf-352656101104563/cell_pos/r",
            "deviceId": "N/A",
            "receivedAt": "2023-06-18T06:11:56.570Z",
            "message": {
                "appId": "CELL_POS",
                "messageType": "DATA",
                "data": {
                    "lat": 44.4434427,
                    "lon": 8.9020756,
                    "uncertainty": 562,
                    "fulfilledWith": "MCELL"
                }
            },
            "tenantId": "0b3486e5-a1e9-4054-9d3f-c5e623bff7f4"
        }
        {
            "topic": "prod/0b3486e5-a1e9-4054-9d3f-c5e623bff7f4/m/d/nrf-352656101104563/c2d",
            "deviceId": "N/A",
            "receivedAt": "2023-06-18T06:11:56.570Z",
            "message": {
                "appId": "CELL_POS",
                "messageType": "DATA",
                "data": {
                    "lat": 44.4434427,
                    "lon": 8.9020756,
                    "uncertainty": 562,
                    "fulfilledWith": "MCELL"
                }
            },
            "tenantId": "0b3486e5-a1e9-4054-9d3f-c5e623bff7f4"
        }
        {
            "topic": "prod/0b3486e5-a1e9-4054-9d3f-c5e623bff7f4/m/d/nrf-352656101104563/d2c",
            "deviceId": "nrf-352656101104563",
            "receivedAt": "2023-06-18T06:11:56.415Z",
            "message": {
                "appId": "CELL_POS",
                "messageType": "DATA",
                "data": {
                    "lte": [
                        {
                            "eci": 4956959,
                            "mcc": 222,
                            "mnc": 10,
                            "tac": 12050,
                            "earfcn": 1850,
                            "rsrp": -91,
                            "rsrq": -9,
                            "adv": 80,
                            "nmr": [
                                {
                                    "earfcn": 1850,
                                    "pci": 340,
                                    "rsrp": -92,
                                    "rsrq": -10.5
                                }
                            ]
                        }
                    ]
                }
            },
            "tenantId": "0b3486e5-a1e9-4054-9d3f-c5e623bff7f4"
        }
        {
            "topic": "prod/0b3486e5-a1e9-4054-9d3f-c5e623bff7f4/m/d/nrf-352656101104563/d2c",
            "deviceId": "nrf-352656101104563",
            "receivedAt": "2023-06-18T06:10:39.635Z",
            "message": {
                "appId": "VOLTAGE",
                "messageType": "DATA",
                "ts": 1687068639635,
                "data": "4019"
            },
            "tenantId": "0b3486e5-a1e9-4054-9d3f-c5e623bff7f4"
        }
        {
            "topic": "prod/0b3486e5-a1e9-4054-9d3f-c5e623bff7f4/m/d/nrf-352656101104563/d2c",
            "deviceId": "nrf-352656101104563",
            "receivedAt": "2023-06-18T06:10:39.602Z",
            "message": {
                "appId": "DEVICE",
                "messageType": "DATA",
                "ts": 1687068639602,
                "data": {
                    "networkInfo": {
                        "rsrp": -92
                    }
                }
            },
            "tenantId": "0b3486e5-a1e9-4054-9d3f-c5e623bff7f4"
        }
        {
            "topic": "prod/0b3486e5-a1e9-4054-9d3f-c5e623bff7f4/m/d/nrf-352656101104563/d2c",
            "deviceId": "nrf-352656101104563",
            "receivedAt": "2023-06-18T06:10:39.602Z",
            "message": {
                "appId": "RSRP",
                "messageType": "DATA",
                "ts": 1687068639602,
                "data": "-92"
            },
            "tenantId": "0b3486e5-a1e9-4054-9d3f-c5e623bff7f4"
        }
        {
            "topic": "prod/0b3486e5-a1e9-4054-9d3f-c5e623bff7f4/m/d/nrf-352656101104563/d2c",
            "deviceId": "nrf-352656101104563",
            "receivedAt": "2023-06-18T06:10:39.536Z",
            "message": {
                "appId": "AIR_QUAL",
                "messageType": "DATA",
                "ts": 1687068639536,
                "data": "42"
            },
            "tenantId": "0b3486e5-a1e9-4054-9d3f-c5e623bff7f4"
        }
        {
            "topic": "prod/0b3486e5-a1e9-4054-9d3f-c5e623bff7f4/m/d/nrf-352656101104563/d2c",
            "deviceId": "nrf-352656101104563",
            "receivedAt": "2023-06-18T06:10:39.536Z",
            "message": {
                "appId": "TEMP",
                "messageType": "DATA",
                "ts": 1687068639536,
                "data": "25.71"
            },
            "tenantId": "0b3486e5-a1e9-4054-9d3f-c5e623bff7f4"
        }
        {
            "topic": "prod/0b3486e5-a1e9-4054-9d3f-c5e623bff7f4/m/d/nrf-352656101104563/d2c",
            "deviceId": "nrf-352656101104563",
            "receivedAt": "2023-06-18T06:10:39.536Z",
            "message": {
                "appId": "HUMID",
                "messageType": "DATA",
                "ts": 1687068639536,
                "data": "63.26"
            },
            "tenantId": "0b3486e5-a1e9-4054-9d3f-c5e623bff7f4"
        }
        {
            "topic": "prod/0b3486e5-a1e9-4054-9d3f-c5e623bff7f4/m/d/nrf-352656101104563/d2c",
            "deviceId": "nrf-352656101104563",
            "receivedAt": "2023-06-18T06:10:39.536Z",
            "message": {
                "appId": "AIR_PRESS",
                "messageType": "DATA",
                "ts": 1687068639536,
                "data": "101.32"
            },
            "tenantId": "0b3486e5-a1e9-4054-9d3f-c5e623bff7f4"
        }
        {
            "topic": "prod/0b3486e5-a1e9-4054-9d3f-c5e623bff7f4/m/d/nrf-352656101104563/cell_pos/r",
            "deviceId": "N/A",
            "receivedAt": "2023-06-18T06:08:50.683Z",
            "message": {
                "appId": "CELL_POS",
                "messageType": "DATA",
                "data": {
                    "lat": 44.44374204,
                    "lon": 8.90225172,
                    "uncertainty": 488,
                    "fulfilledWith": "SCELL"
                }
            },
            "tenantId": "0b3486e5-a1e9-4054-9d3f-c5e623bff7f4"
        }
        {
            "topic": "prod/0b3486e5-a1e9-4054-9d3f-c5e623bff7f4/m/d/nrf-352656101104563/c2d",
            "deviceId": "N/A",
            "receivedAt": "2023-06-18T06:08:50.681Z",
            "message": {
                "appId": "CELL_POS",
                "messageType": "DATA",
                "data": {
                    "lat": 44.44374204,
                    "lon": 8.90225172,
                    "uncertainty": 488,
                    "fulfilledWith": "SCELL"
                }
            },
            "tenantId": "0b3486e5-a1e9-4054-9d3f-c5e623bff7f4"
        }
        {
            "topic": "prod/0b3486e5-a1e9-4054-9d3f-c5e623bff7f4/m/d/nrf-352656101104563/d2c",
            "deviceId": "nrf-352656101104563",
            "receivedAt": "2023-06-18T06:08:50.420Z",
            "message": {
                "appId": "CELL_POS",
                "messageType": "DATA",
                "data": {
                    "lte": [
                        {
                            "eci": 4956959,
                            "mcc": 222,
                            "mnc": 10,
                            "tac": 12050,
                            "earfcn": 1850,
                            "rsrp": -93,
                            "rsrq": -12,
                            "adv": 80
                        }
                    ]
                }
            },
            "tenantId": "0b3486e5-a1e9-4054-9d3f-c5e623bff7f4"
        }
        {
            "topic": "prod/0b3486e5-a1e9-4054-9d3f-c5e623bff7f4/m/d/nrf-352656101104563/d2c",
            "deviceId": "nrf-352656101104563",
            "receivedAt": "2023-06-18T06:08:50.280Z",
            "message": {
                "appId": "AGPS",
                "messageType": "DATA",
                "data": {
                    "mcc": 222,
                    "mnc": 10,
                    "tac": 12050,
                    "eci": 4956959,
                    "rsrp": -140,
                    "types": [
                        1,
                        2,
                        3,
                        4,
                        6,
                        7,
                        8,
                        9
                    ]
                }
            },
            "tenantId": "0b3486e5-a1e9-4054-9d3f-c5e623bff7f4"
        }
        {
            "topic": "prod/0b3486e5-a1e9-4054-9d3f-c5e623bff7f4/m/d/nrf-352656101104563/d2c",
            "deviceId": "nrf-352656101104563",
            "receivedAt": "2023-06-18T06:08:49.312Z",
            "message": {
                "appId": "VOLTAGE",
                "messageType": "DATA",
                "ts": 1687068529312,
                "data": "4003"
            },
            "tenantId": "0b3486e5-a1e9-4054-9d3f-c5e623bff7f4"
        }
        {
            "topic": "prod/0b3486e5-a1e9-4054-9d3f-c5e623bff7f4/m/d/nrf-352656101104563/d2c",
            "deviceId": "nrf-352656101104563",
            "receivedAt": "2023-06-18T06:08:49.279Z",
            "message": {
                "appId": "DEVICE",
                "messageType": "DATA",
                "ts": 1687068529279,
                "data": {
                    "networkInfo": {
                        "currentBand": 3,
                        "networkMode": "LTE-M",
                        "rsrp": -91,
                        "areaCode": 12050,
                        "mccmnc": 22210,
                        "cellID": 4956959,
                        "ipAddress": "10.40.174.1"
                    }
                }
            },
            "tenantId": "0b3486e5-a1e9-4054-9d3f-c5e623bff7f4"
        }
        {
            "topic": "prod/0b3486e5-a1e9-4054-9d3f-c5e623bff7f4/m/d/nrf-352656101104563/d2c",
            "deviceId": "nrf-352656101104563",
            "receivedAt": "2023-06-18T06:08:49.279Z",
            "message": {
                "appId": "RSRP",
                "messageType": "DATA",
                "ts": 1687068529279,
                "data": "-91"
            },
            "tenantId": "0b3486e5-a1e9-4054-9d3f-c5e623bff7f4"
        }
        {
            "topic": "prod/0b3486e5-a1e9-4054-9d3f-c5e623bff7f4/m/d/nrf-352656101104563/d2c",
            "deviceId": "nrf-352656101104563",
            "receivedAt": "2023-06-18T06:08:49.247Z",
            "message": {
                "appId": "DEVICE",
                "messageType": "DATA",
                "ts": 1687068529247,
                "data": {
                    "deviceInfo": {
                        "imei": "352656101104563",
                        "iccid": "89882280666018841738",
                        "modemFirmware": "mfw_nrf9160_1.3.2",
                        "board": "thingy91_nrf9160",
                        "appVersion": "0.0.0-development"
                    }
                }
            },
            "tenantId": "0b3486e5-a1e9-4054-9d3f-c5e623bff7f4"
        }
        {
            "topic": "prod/0b3486e5-a1e9-4054-9d3f-c5e623bff7f4/m/d/nrf-352656101104563/d2c",
            "deviceId": "nrf-352656101104563",
            "receivedAt": "2023-06-18T06:08:49.174Z",
            "message": {
                "appId": "AIR_QUAL",
                "messageType": "DATA",
                "ts": 1687068529174,
                "data": "35"
            },
            "tenantId": "0b3486e5-a1e9-4054-9d3f-c5e623bff7f4"
        }
        {
            "topic": "prod/0b3486e5-a1e9-4054-9d3f-c5e623bff7f4/m/d/nrf-352656101104563/d2c",
            "deviceId": "nrf-352656101104563",
            "receivedAt": "2023-06-18T06:08:49.174Z",
            "message": {
                "appId": "HUMID",
                "messageType": "DATA",
                "ts": 1687068529174,
                "data": "63.34"
            },
            "tenantId": "0b3486e5-a1e9-4054-9d3f-c5e623bff7f4"
        }
        {
            "topic": "prod/0b3486e5-a1e9-4054-9d3f-c5e623bff7f4/m/d/nrf-352656101104563/d2c",
            "deviceId": "nrf-352656101104563",
            "receivedAt": "2023-06-18T06:08:49.174Z",
            "message": {
                "appId": "AIR_PRESS",
                "messageType": "DATA",
                "ts": 1687068529174,
                "data": "101.33"
            },
            "tenantId": "0b3486e5-a1e9-4054-9d3f-c5e623bff7f4"
        }
        {
            "topic": "prod/0b3486e5-a1e9-4054-9d3f-c5e623bff7f4/m/d/nrf-352656101104563/d2c",
            "deviceId": "nrf-352656101104563",
            "receivedAt": "2023-06-18T06:08:49.174Z",
            "message": {
                "appId": "TEMP",
                "messageType": "DATA",
                "ts": 1687068529174,
                "data": "25.47"
            },
            "tenantId": "0b3486e5-a1e9-4054-9d3f-c5e623bff7f4"
        }
        ```
        
    
12. ( *******added July 17, 2023 8:18 PM* ) — mi tocca classificarli
    - CELL_POS — lat lon uncertainty
        
        ```
        {
            "topic": "prod/0b3486e5-a1e9-4054-9d3f-c5e623bff7f4/m/d/nrf-352656101104563/c2d",
            "deviceId": "N/A",
            "receivedAt": "2023-06-18T06:23:42.083Z",
            "message": {
                "appId": "CELL_POS",
                "messageType": "DATA",
                "data": {
                    "lat": 44.4434427,
                    "lon": 8.9020756,
                    "uncertainty": 562,
                    "fulfilledWith": "MCELL"
                }
            },
            "tenantId": "0b3486e5-a1e9-4054-9d3f-c5e623bff7f4"
        }
        ```
        
    - CELL_POS — lte
        
        ```
        {
            "topic": "prod/0b3486e5-a1e9-4054-9d3f-c5e623bff7f4/m/d/nrf-352656101104563/d2c",
            "deviceId": "nrf-352656101104563",
            "receivedAt": "2023-06-18T06:23:41.821Z",
            "message": {
                "appId": "CELL_POS",
                "messageType": "DATA",
                "data": {
                    "lte": [
                        {
                            "eci": 4956959,
                            "mcc": 222,
                            "mnc": 10,
                            "tac": 12050,
                            "earfcn": 1850,
                            "rsrp": -97,
                            "rsrq": -9,
                            "nmr": [
                                {
                                    "earfcn": 1850,
                                    "pci": 340,
                                    "rsrp": -98,
                                    "rsrq": -10
                                },
                                {
                                    "earfcn": 1850,
                                    "pci": 464,
                                    "rsrp": -105,
                                    "rsrq": -17.5
                                }
                            ]
                        }
                    ]
                }
            },
            "tenantId": "0b3486e5-a1e9-4054-9d3f-c5e623bff7f4"
        }
        ```
        
    - VOLTAGE
        
        ```
        {
            "topic": "prod/0b3486e5-a1e9-4054-9d3f-c5e623bff7f4/m/d/nrf-352656101104563/d2c",
            "deviceId": "nrf-352656101104563",
            "receivedAt": "2023-06-18T06:22:39.618Z",
            "message": {
                "appId": "VOLTAGE",
                "messageType": "DATA",
                "ts": 1687069359618,
                "data": "4015"
            },
            "tenantId": "0b3486e5-a1e9-4054-9d3f-c5e623bff7f4"
        }
        ```
        
    - RSRP
        
        ```
        {
            "topic": "prod/0b3486e5-a1e9-4054-9d3f-c5e623bff7f4/m/d/nrf-352656101104563/d2c",
            "deviceId": "nrf-352656101104563",
            "receivedAt": "2023-06-18T06:22:39.585Z",
            "message": {
                "appId": "RSRP",
                "messageType": "DATA",
                "ts": 1687069359585,
                "data": "-96"
            },
            "tenantId": "0b3486e5-a1e9-4054-9d3f-c5e623bff7f4"
        }
        ```
        
    - === DEVICE networkInfo
        
        ```
        {
            "topic": "prod/0b3486e5-a1e9-4054-9d3f-c5e623bff7f4/m/d/nrf-352656101104563/d2c",
            "deviceId": "nrf-352656101104563",
            "receivedAt": "2023-06-18T06:22:39.585Z",
            "message": {
                "appId": "DEVICE",
                "messageType": "DATA",
                "ts": 1687069359585,
                "data": {
                    "networkInfo": {
                        "rsrp": -96,
                        "cellID": 4956959
                    }
                }
            },
            "tenantId": "0b3486e5-a1e9-4054-9d3f-c5e623bff7f4"
        }
        ```
        
    - === TEMP (TEMPERATURE)
        
        ```
        {
            "topic": "prod/0b3486e5-a1e9-4054-9d3f-c5e623bff7f4/m/d/nrf-352656101104563/d2c",
            "deviceId": "nrf-352656101104563",
            "receivedAt": "2023-06-18T06:22:39.524Z",
            "message": {
                "appId": "TEMP",
                "messageType": "DATA",
                "ts": 1687069359524,
                "data": "27.56"
            },
            "tenantId": "0b3486e5-a1e9-4054-9d3f-c5e623bff7f4"
        }
        ```
        
    - === AIR_PRESS
        
        ```
        {
            "topic": "prod/0b3486e5-a1e9-4054-9d3f-c5e623bff7f4/m/d/nrf-352656101104563/d2c",
            "deviceId": "nrf-352656101104563",
            "receivedAt": "2023-06-18T06:22:39.524Z",
            "message": {
                "appId": "AIR_PRESS",
                "messageType": "DATA",
                "ts": 1687069359524,
                "data": "101.33"
            },
            "tenantId": "0b3486e5-a1e9-4054-9d3f-c5e623bff7f4"
        }
        ```
        
    - === HUMID
        
        ```
        {
            "topic": "prod/0b3486e5-a1e9-4054-9d3f-c5e623bff7f4/m/d/nrf-352656101104563/d2c",
            "deviceId": "nrf-352656101104563",
            "receivedAt": "2023-06-18T06:22:39.524Z",
            "message": {
                "appId": "HUMID",
                "messageType": "DATA",
                "ts": 1687069359524,
                "data": "58.19"
            },
            "tenantId": "0b3486e5-a1e9-4054-9d3f-c5e623bff7f4"
        }
        ```
        
    - === AIR_QUAL
        
        ```
        {
            "topic": "prod/0b3486e5-a1e9-4054-9d3f-c5e623bff7f4/m/d/nrf-352656101104563/d2c",
            "deviceId": "nrf-352656101104563",
            "receivedAt": "2023-06-18T06:22:39.524Z",
            "message": {
                "appId": "AIR_QUAL",
                "messageType": "DATA",
                "ts": 1687069359524,
                "data": "14"
            },
            "tenantId": "0b3486e5-a1e9-4054-9d3f-c5e623bff7f4"
        }
        ```
        
    - giusto per — il file completo
        
        ```
        === CELL POS : lat, lon
        
        {
            "topic": "prod/0b3486e5-a1e9-4054-9d3f-c5e623bff7f4/m/d/nrf-352656101104563/c2d",
            "deviceId": "N/A",
            "receivedAt": "2023-06-18T06:23:42.083Z",
            "message": {
                "appId": "CELL_POS",
                "messageType": "DATA",
                "data": {
                    "lat": 44.4434427,
                    "lon": 8.9020756,
                    "uncertainty": 562,
                    "fulfilledWith": "MCELL"
                }
            },
            "tenantId": "0b3486e5-a1e9-4054-9d3f-c5e623bff7f4"
        }
        
        === CELL POS : lte
        
        {
            "topic": "prod/0b3486e5-a1e9-4054-9d3f-c5e623bff7f4/m/d/nrf-352656101104563/d2c",
            "deviceId": "nrf-352656101104563",
            "receivedAt": "2023-06-18T06:23:41.821Z",
            "message": {
                "appId": "CELL_POS",
                "messageType": "DATA",
                "data": {
                    "lte": [
                        {
                            "eci": 4956959,
                            "mcc": 222,
                            "mnc": 10,
                            "tac": 12050,
                            "earfcn": 1850,
                            "rsrp": -97,
                            "rsrq": -9,
                            "nmr": [
                                {
                                    "earfcn": 1850,
                                    "pci": 340,
                                    "rsrp": -98,
                                    "rsrq": -10
                                },
                                {
                                    "earfcn": 1850,
                                    "pci": 464,
                                    "rsrp": -105,
                                    "rsrq": -17.5
                                }
                            ]
                        }
                    ]
                }
            },
            "tenantId": "0b3486e5-a1e9-4054-9d3f-c5e623bff7f4"
        }
        
        === VOLTAGE
        
        {
            "topic": "prod/0b3486e5-a1e9-4054-9d3f-c5e623bff7f4/m/d/nrf-352656101104563/d2c",
            "deviceId": "nrf-352656101104563",
            "receivedAt": "2023-06-18T06:22:39.618Z",
            "message": {
                "appId": "VOLTAGE",
                "messageType": "DATA",
                "ts": 1687069359618,
                "data": "4015"
            },
            "tenantId": "0b3486e5-a1e9-4054-9d3f-c5e623bff7f4"
        }
        
        === RSRP
        
        {
            "topic": "prod/0b3486e5-a1e9-4054-9d3f-c5e623bff7f4/m/d/nrf-352656101104563/d2c",
            "deviceId": "nrf-352656101104563",
            "receivedAt": "2023-06-18T06:22:39.585Z",
            "message": {
                "appId": "RSRP",
                "messageType": "DATA",
                "ts": 1687069359585,
                "data": "-96"
            },
            "tenantId": "0b3486e5-a1e9-4054-9d3f-c5e623bff7f4"
        }
        
        === DEVICE networkInfo
        
        {
            "topic": "prod/0b3486e5-a1e9-4054-9d3f-c5e623bff7f4/m/d/nrf-352656101104563/d2c",
            "deviceId": "nrf-352656101104563",
            "receivedAt": "2023-06-18T06:22:39.585Z",
            "message": {
                "appId": "DEVICE",
                "messageType": "DATA",
                "ts": 1687069359585,
                "data": {
                    "networkInfo": {
                        "rsrp": -96,
                        "cellID": 4956959
                    }
                }
            },
            "tenantId": "0b3486e5-a1e9-4054-9d3f-c5e623bff7f4"
        }
        
        === TEMP (TEMPERATURE)
        
        {
            "topic": "prod/0b3486e5-a1e9-4054-9d3f-c5e623bff7f4/m/d/nrf-352656101104563/d2c",
            "deviceId": "nrf-352656101104563",
            "receivedAt": "2023-06-18T06:22:39.524Z",
            "message": {
                "appId": "TEMP",
                "messageType": "DATA",
                "ts": 1687069359524,
                "data": "27.56"
            },
            "tenantId": "0b3486e5-a1e9-4054-9d3f-c5e623bff7f4"
        }
        
        === AIR_PRESS 
        
        {
            "topic": "prod/0b3486e5-a1e9-4054-9d3f-c5e623bff7f4/m/d/nrf-352656101104563/d2c",
            "deviceId": "nrf-352656101104563",
            "receivedAt": "2023-06-18T06:22:39.524Z",
            "message": {
                "appId": "AIR_PRESS",
                "messageType": "DATA",
                "ts": 1687069359524,
                "data": "101.33"
            },
            "tenantId": "0b3486e5-a1e9-4054-9d3f-c5e623bff7f4"
        }
        
        === HUMID
        
        {
            "topic": "prod/0b3486e5-a1e9-4054-9d3f-c5e623bff7f4/m/d/nrf-352656101104563/d2c",
            "deviceId": "nrf-352656101104563",
            "receivedAt": "2023-06-18T06:22:39.524Z",
            "message": {
                "appId": "HUMID",
                "messageType": "DATA",
                "ts": 1687069359524,
                "data": "58.19"
            },
            "tenantId": "0b3486e5-a1e9-4054-9d3f-c5e623bff7f4"
        }
        
        === AIR_QUAL
        
        {
            "topic": "prod/0b3486e5-a1e9-4054-9d3f-c5e623bff7f4/m/d/nrf-352656101104563/d2c",
            "deviceId": "nrf-352656101104563",
            "receivedAt": "2023-06-18T06:22:39.524Z",
            "message": {
                "appId": "AIR_QUAL",
                "messageType": "DATA",
                "ts": 1687069359524,
                "data": "14"
            },
            "tenantId": "0b3486e5-a1e9-4054-9d3f-c5e623bff7f4"
        }
        ```
        
    
    è un grosso risultato questo. Per come la vedo io, adesso abbiamo due strade:
    
    1. trovare un modo per catalogare i messaggi in forma tabellare
    2. provare le altre funzioni della API
    
    e tra l’altro noto con piacere che *****************************************************il token che avevo all’inizio non è ancora scaduto.***************************************************** Non è del genere che scade in un’ora (forse mi sono confuso con Spotify)
    

—

## Annotazioni di lavoro

### July 10, 2023 7:40 PM — py requests — perchè un ripassino non fa male a nessuno

- documentazione ufficiale — [Requests: HTTP for Humans™ — Requests 2.31.0 documentation](https://requests.readthedocs.io/en/latest/)
- guida avanzata — [Advanced Usage — Requests 2.31.0 documentation](https://requests.readthedocs.io/en/latest/user/advanced/#advanced)
- il progetto open source ufficiale — https://github.com/psf/requests

esistono siti a cui fare richieste HTTP  a vuoto:

- [httpbin.org](https://httpbin.org/)

```
-- GET
https://httpbin.org/get

-- POST
https://httpbin.org/post

-- EXAMPLE DOMAINS
https://www.example.com/
```

librerie utili:

```
import requests
```

i tipi principali di richiesta HTTP:

**********— GET**********

```
r = requests.get('https://api.github.com/events')

print(type(r)) # : <class 'requests.models.Response'>
```

**********— POST**********

```
req_url = 'https://httpbin.org/post'
req_payload = {
	'key' : value
}
r = requests.post(
	url = req_url,
	data = req_payload
)

print(type(r)) # : <class 'requests.models.Response'>
```

l’oggetto ritornato dal metodo HTTP che vuoi utilizzare contiene informazioni molto utili riguardo la richiesta che vuoi fare:

********************— STATO DI RITORNO DELLA RICHIESTA********************

- questo è magia, ma funziona — [requests/requests/status_codes.py at main · psf/requests · GitHub](https://github.com/psf/requests/blob/main/requests/status_codes.py) — tutti i nomi che vedi qui sono i nomi utilizzabili ***************in dot notation*************** per i codici di riferimento. vedi la `setattr()`

```
-- STATO RITORNATO DAL SERVER
r.status_code # <class 'int'>

# per interpretare il codice di ritorno c'è
requests.codes.ok 
```

- tutti i codici disponibili in Requests
    
    ```
    # Informational.
        100: ("continue",),
        101: ("switching_protocols",),
        102: ("processing",),
        103: ("checkpoint",),
        122: ("uri_too_long", "request_uri_too_long"),
        200: ("ok", "okay", "all_ok", "all_okay", "all_good", "\\o/", "✓"),
        201: ("created",),
        202: ("accepted",),
        203: ("non_authoritative_info", "non_authoritative_information"),
        204: ("no_content",),
        205: ("reset_content", "reset"),
        206: ("partial_content", "partial"),
        207: ("multi_status", "multiple_status", "multi_stati", "multiple_stati"),
        208: ("already_reported",),
        226: ("im_used",),
    
    # Redirection.
        300: ("multiple_choices",),
        301: ("moved_permanently", "moved", "\\o-"),
        302: ("found",),
        303: ("see_other", "other"),
        304: ("not_modified",),
        305: ("use_proxy",),
        306: ("switch_proxy",),
        307: ("temporary_redirect", "temporary_moved", "temporary"),
        308: (
            "permanent_redirect",
            "resume_incomplete",
            "resume",
        ),  # "resume" and "resume_incomplete" to be removed in 3.0
    
    # Client Error.
        400: ("bad_request", "bad"),
        401: ("unauthorized",),
        402: ("payment_required", "payment"),
        403: ("forbidden",),
        404: ("not_found", "-o-"),
        405: ("method_not_allowed", "not_allowed"),
        406: ("not_acceptable",),
        407: ("proxy_authentication_required", "proxy_auth", "proxy_authentication"),
        408: ("request_timeout", "timeout"),
        409: ("conflict",),
        410: ("gone",),
        411: ("length_required",),
        412: ("precondition_failed", "precondition"),
        413: ("request_entity_too_large",),
        414: ("request_uri_too_large",),
        415: ("unsupported_media_type", "unsupported_media", "media_type"),
        416: (
            "requested_range_not_satisfiable",
            "requested_range",
            "range_not_satisfiable",
        ),
        417: ("expectation_failed",),
        418: ("im_a_teapot", "teapot", "i_am_a_teapot"),
        421: ("misdirected_request",),
        422: ("unprocessable_entity", "unprocessable"),
        423: ("locked",),
        424: ("failed_dependency", "dependency"),
        425: ("unordered_collection", "unordered"),
        426: ("upgrade_required", "upgrade"),
        428: ("precondition_required", "precondition"),
        429: ("too_many_requests", "too_many"),
        431: ("header_fields_too_large", "fields_too_large"),
        444: ("no_response", "none"),
        449: ("retry_with", "retry"),
        450: ("blocked_by_windows_parental_controls", "parental_controls"),
        451: ("unavailable_for_legal_reasons", "legal_reasons"),
        499: ("client_closed_request",),
    
    # Server Error.
        500: ("internal_server_error", "server_error", "/o\\", "✗"),
        501: ("not_implemented",),
        502: ("bad_gateway",),
        503: ("service_unavailable", "unavailable"),
        504: ("gateway_timeout",),
        505: ("http_version_not_supported", "http_version"),
        506: ("variant_also_negotiates",),
        507: ("insufficient_storage",),
        509: ("bandwidth_limit_exceeded", "bandwidth"),
        510: ("not_extended",),
        511: ("network_authentication_required", "network_auth", "network_authentication"),
    ```
    

********************— URL USATO PER LA REQUEST********************

```
-- URL UTILIZZATO PER FARE LA RICHIESTA AL SERVER
r.url
```

********************— ACQUSIRE LA RISPOSTA DAL SERVER********************

```
-- RISPOSTA GREZZA DAL SERVER
r.text # : <class 'str'>

-- RISPOSTA BINARIA DAL SERVER
r.content # : <class 'bytes'>

-- RISPOSTA IN JSON (non sempre disponibile)
r.json() # requests.exceptions.JSONDecodeError

-- RAW SOCKET RESPONSE (*********streaming*********)
r.raw
# per leggere un certo tot di bytes
r.raw.read(10)
# o in generale la raw potresti aprirla e scaricarla in un file
with open(filename, 'wb') as fd:
    **for chunk in r.iter_content(chunk_size=128):**
        fd.write(chunk)
```

********************— ENCODING********************

```
-- CODIFICA CON CUI STA RISPONDENDO IL SERVER
r.encoding

-- puoi anche settarla tu, e la classe cambierà codifica
```

********************— HEADER********************

```
r.headers # dict

{
    'content-encoding': 'gzip',
    'transfer-encoding': 'chunked',
    'connection': 'close',
    'server': 'nginx/1.0.4',
    'x-runtime': '148ms',
    'etag': '"e1ca502697e5c9317743dc078f67693f"',
    'content-type': 'application/json'
}
```

********************— COOKIES********************

```
r.cookies
r.cookies['example_cookie_name']
```

tutti gli argomenti utili delle funzioni per fare richieste HTTP:

- headers — dictionary
- url — string
- data — un qualcosa convertibile in bytes in generale — spesso un oggetto json serializzabile, e anzi: qualcosa di già trasformato in JSON con dumps (vedi lib JSON)
- json — come data, ma senza il bisogno di ,castare esplicitament eil JSON  a stringa
- fles — per inviare file al server (o meglio, ad un server che non tratta la cosa in maniera strana, perchè poi ci sono le policies, e quelle cambiano, quindi sei costretto a passare il file n un modo piuttosto che in quello tipico) passi qui un dict del tipo
    
    ```
    {
    	'filename' : ( 'filepath', open( filepath, 'rb' ) )
    }
    ```
    
    per maggiori info a riguardo vedi — [Quickstart — Requests 2.31.0 documentation](https://requests.readthedocs.io/en/latest/user/quickstart/#post-a-multipart-encoded-file)
    
    il metodo funziona anche per inviare semplici stringhe che dall’altra poarte verranno recepite come veri e propri file. 
    
    per approfondimento, vedi HTTP multipart protocol — [Introduction to HTTP Multipart (adamchalmers.com)](https://blog.adamchalmers.com/multipart/) — [MIME types (IANA media types) - HTTP | MDN (mozilla.org)](https://developer.mozilla.org/en-US/docs/Web/HTTP/Basics_of_HTTP/MIME_types)
    
- cookies — per portarsi diietro i cookies della richiesta
    
    usando il cookie jar:
    
    ```
    jar = requests.cookies.RequestsCookieJar()
    jar.set('tasty_cookie', 'yum', domain='httpbin.org', path='/cookies')
    jar.set('gross_cookie', 'blech', domain='httpbin.org', path='/elsewhere')
    
    url = 'https://httpbin.org/cookies'
    r = requests.get(url, cookies=jar)
    
    r.text
    ```
    
    metodo normale:
    
    ```
    url = 'https://httpbin.org/cookies'
    cookies = dict(cookies_are='working')
    
    r = requests.get(url, cookies=cookies)
    ```
    
- timeout — un float che rappresneta il numero di secondi entro cui esaurire la richiiesta

Altri approfondimenti sparsi da guardare magari in seguito:

- SESSIONI CON REQUESTS — [Advanced Usage — Requests 2.31.0 documentation](https://requests.readthedocs.io/en/latest/user/advanced/#session-objects)
- SSL CON CERTIFICATO IN REQUESTS — [Advanced Usage — Requests 2.31.0 documentation](https://requests.readthedocs.io/en/latest/user/advanced/#ssl-cert-verification)
    
    ```
    requests.get('https://github.com', verify='/path/to/certfile')
    ```
    
- INVIARE PIU’ FILE IN UNA SOLA REQUEST — [Advanced Usage — Requests 2.31.0 documentation](https://requests.readthedocs.io/en/latest/user/advanced/#post-multiple-multipart-encoded-files)
- EVENT HOOKS — [Advanced Usage — Requests 2.31.0 documentation](https://requests.readthedocs.io/en/latest/user/advanced/#event-hooks)
- STREAMING CON REQUESTS — [Advanced Usage — Requests 2.31.0 documentation](https://requests.readthedocs.io/en/latest/user/advanced/#streaming-requests)
- PROXIES — [Advanced Usage — Requests 2.31.0 documentation](https://requests.readthedocs.io/en/latest/user/advanced/#proxies)

—

### July 10, 2023 10:39 PM — protocollo MQTT

<aside>
💡 Message Queuing Telemetry Transport

</aside>

è un proocolllo di tipo publish subscribe molto utilizzato in ambito IoT per via dei suoi molti vantaggi per dispositivi particolarmente semplici, come ad esempio semplicità di implementazione e qantità di esergia usata minima.

- una buona introduzione — [What is MQTT? - MQTT Protocol Explained - AWS (amazon.com)](https://aws.amazon.com/what-is/mqtt/?nc1=h_ls)
- una guida per usare MQTT in py — [A Beginner's Guide on Using MQTT in Python | Python in Plain English](https://python.plainenglish.io/mqtt-beginners-guide-in-python-38590c8328ae)
- una buona guida su come usare MQTT in Py in italiano — [Tutorial - Utilizzare MQTT con Python (Parte 1): La classe Client - Antima](https://antima.it/tutorial-utilizzare-mqtt-con-python-la-classe-client-parte-1/)

—

### July 10, 2023 11:01 PM — Orientarsi nella API di NRFCloud

- entry point
    
    `https://api.nrfcloud.com/v1/account`
    

E’ relativamente semplice da leggere. Appena entri, vedi subito qual’è l’entry point della call:

![Untitled](./Untitled%201.png)

vedi anche di che tipo è

![Untitled](./Untitled%202.png)

clickando sul drop down vedi persino il link completo da chiamare:

![Untitled](./Untitled%203.png)

appena sotto l’URL la descrizione della funzione, e persino una chiamata CURL già customizzabile!

![Untitled](./Untitled%204.png)

—

### July 17, 2023 7:22 PM — Date ben formattate nella API di thinghy:91

```
end=2020-06-25T21:05:12.830Z
```

—

### July 17, 2023 7:44 PM — URL encoding

- il post magiico — [How to encode URLs in Python | URLEncoder](https://www.urlencoder.io/python/)

```
import urllib.parse
```

passaggio necessario per far sì che i query parameters possano assumere qualunque carattere sia necessario.

La libreria semplicemente converte i caratteri speciali in caratteri compatibili con la trasmissione come URL. 

```
query = 'Hellö Wörld@Python'
urllib.parse.quote(query) # UTF-8 -> 'Hell%C3%B6%20W%C3%B6rld%40Python'
```

esistono tanti metodi, vedi il post. Quello migliore è questo:

```
params = {'q': 'Python URL encoding', 'as_sitesearch': 'www.urlencoder.io'}
urllib.parse.urlencode(params)
```

—

### July 17, 2023 8:11 PM — iterare su un JSON senza conoscerne la struttura?

- [python - loop through nested json object without knowing the object names - Stack Overflow](https://stackoverflow.com/questions/73930938/loop-through-nested-json-object-without-knowing-the-object-names)

—

—

---

---

## Codice release

---

---