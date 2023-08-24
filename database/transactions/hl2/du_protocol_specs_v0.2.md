## Algoritmo D.U.

*************************Upload-Download Procedure*************************

—

### Funzionamento alto livello per HoloLens2

Sulla procedura iniziale siamo tutti d’accordo:

1. CALIBRAZIONE — DOWNLOAD
    
    ********************************E’ la prima richiesta da fare: l’upload fallisce se lo si tenta di chiamare senza aver prima scritto la propria origine nella tabella di staging*
    
    Dalla richiesta ottengo le prime informazioni sulla zona se ce ne sono
    
    La fase di download è anche quella in cui viene generato il fae token
    
2. ESPLORAZIONE
    
    raccolgo un po’ di misurazioni in giro, alcune magari le conoscevo già, altre invece non le conoscevo
    
3. UPLOAD
    
    invio al server solo le posizioni ************************che penso di aver trovato************************ (questo perchè potrebbero esserci anche altre sessioni attive).
    
    dal server ottengo informazioni per allineare il mio DB a quello presente sul server, e vado a marcare tutti i punti che sono riuscito ad esportare con successo
    

Sul seguito un po’ meno. L’uso di DOWNLOAD e UPLOAD 

— DOWNLOAD E POI UPLOAD, O VICEVERSA? 

è un dubbio che mi p venuto in mente mentre pensavo allo use case. La mia risposta: il download dovrebbe venire prima. POtrei anche mettere l’upload prima, il che ha ad esempio il vantaggio di costringere il client a fornire informazioni (fin quando un fake token non esiste, è lecito diffidare: il fake token aggiunge un “livello di credibilità” alla richiesta). Però ha anche lo svantaggio tecnico di richiedere al device due richieste e non una sola, per cui, per accelerare i tempi, e dato il modo in cui HoloLens2 dovrà essere attivato, ho pensato fosse meglio prima il download, in modo da fornire subito una base dati al device da cui partire per l’esplorazione. 

—

### Tabella Session Alias — protocollo fake token

è in generale una pessima idea inviare al client un ID di sessione di un altro utente, che sia attivo o meno. Per questo è stata creata la tabella `F_SESSION_ALIAS` che mappa un session token in un fake session token che può essere trasmesso. Solo il server conosce la regola per il mapping. 

- CREAZIONE FAKE TOKEN — il valore numerico del SALT va generato via Python, per poi chiamare un prepared statement facendo `MD5()` ogni volta che è richiesto.
    
    **************************la generazione di un fake token avviene solo quando davvero necessario.************************** 
    
- LOGGING — la creazione del fake token viene loggata nella tabella `F_ACTIVITY_LOG_SEQUENCE`
- TROVARE UN TOKEN A PARTIRE DA UN FAKE TOKEN — essendo chiave il fake token, vai a cercare il fake token e trova il token associato, che deve corrispondere anche in termini di user_id e device_id
- LOGOUT — vengono eliminati anche tutti i fake token associati alla sessione utente (anche lo user token è una chiave della tabella, perciò è possibile andare semplicemente in JOIN sul session token e contrassegnare come eliminati tutti i fake tokens eventualmente attivi).

Il fake token aggiungeun livello di sicurezza in più al sistema. Ad ogni accesso ad una funzionalità della API è richiesto di usare il fake token, che viene assegnato solo per quella data funzionalità richiesta. Se ad esempio faccio per la prima volta rchiesta ad una API di hololens2, il server mi ritorna all’inizio il fake token, che poi dovrò utilizzare per tute le transazioni successive. Il fake token viene ritornato dal server una sola volta, dopodichè il server si aspetta che il client glielo rimandi ad ogni richiesta. 

Per l’implementazione python è molto conveniente implementare questi checks direttamente come una singola classe. Dato il grado di riutilizzo e di definizione della procedura, questo è uno dei casi in cui frammentare la transazione è una buona idea. 

—

### Staging ID Sharing

Il requisito è duplice, e parzialmente contrastante:

- basarsi sugi ID piuttosto che sulle distanze — per molte cose accelera il lavoro, il vantaggio è chiaro
- tracciare la sessione corrente — in particolare tracciare cosa è stato passato all’utente
- mantenere una coerenza con le sessioni precedenti — se io comparo cosa ho in tabella con cosa ho passato all’utente, noto subito cosa ***************non ho passato*************** all’utente
- ************************************contenere le dimensioni della tabella************************************
    
    uno potrebbe dire: quando vado ad aprire una nuova sessione, vado anche a copiare tutte le posizioni precedenti nella nuova sessione manenendo comunque il riferimento … pessima idea : a parte il fatto che viola la specifica di tracciare l’attività dell’utente, c’è anche il fatto che le dimensioni della tabella decuplicano. Se redito una sessione da 5000 punti, e ne apro un’altra, vato ad ottenere una tabella di 10k punti di cui 5k sono totalmente inutili. 
    

Ecco una delle possibili soluzioni per questo problema:

- la sessione da ereditare *****************************************è quella che ha il session inherited id NULL*****************************************
    
    in questo modo risolvo implicitamente almeno due problemi: la parzialità delle sessioni precedenti (così posso tenere traccia dell’attività dell’utente senza farmi grossi problemi sulle informazioni mancanti) e l’assegnazione di una sessione precedente (che in questo caso diventa molto più efficiente che fare una ricerca su una tabella ordinata).
    
- procedura per estrarre l’intero set delle misure
    
    Selezionando la inherited in questo modo, **********************************************************************************facendo la ricerca delle posizioni sulla sessione ereditata e punto di riferimento********************************************************************************** anzichè usare la sessione corrente e facendo il giro sulla sessione precedente******************************************, trovo effettivamente l’unione di tutte le informazioni su tutte le varie sessioni******************************************. 
    
- procedura per contrapporre le misure attualmente utilizzate con quelle
    
    Dal se di misure ottenuto vado poi ad escludere i doppioni eliminando il riferimento ai punti ridondanti, e ho tutti gli ID dei punti, a cui posso contrapporre qelli correnti della sessione attuale per trovare i punti effettivamente non ancora utilizzati. Più una ricerca sulla distanza, e dovrei essere a posto. 
    

—

### Staging Vs. Quality

- percorsi ottimi
    
    ************************se due punti registrati in due istanti diversi sono particolarmente vicini, c’è il forte sospetto che quelli siano in qualche modo collegati, vale a dire che sia effettivamente possibile transitare da un punto all’altro. La mancanza di questa informazione potrebbe portare all’incapacità di trovare un percorso ottimo, perchè manca proprio un arco.************************ 
    
    ****in QUALITY e’ possibile mettere su un data processor che individui questi casi e che li corregga****
    
    *in STAGING però ll sistema dipende solo dalle informazioni ricevute dai device, per motivi di efficienza. Ci sono policies per diminuire l’entropia e la ridondanza, però nessuno di questi meccanismi va a modirifcare o correggere l’info perchè quello è un processo laborioso e per cui forse non basta un metodo elementare (probabilmente bisogna pure scomodare il machine learning lì)*
    
- 

—

### JSON formato pacchetti — download

request download:

- `based_on` — indica che la sessione è ereditata da un’altra sessione
    
    è una stringa vuota nel caso in cui la sessione non sia nota
    
    nel caso non ci fosse una sessione ereditata, il client deve mettere il suo session token nella richiesta. NOTA BENE: il server si aspetta di ricevere il fake token qui. 
    
- `current_pos` — il punto in cui si trova il device in coordinate relative
    
    *****************************************se il dispositivo si sta calibrando, allora deve per forza trovasrsi nell’origine.***************************************** 
    

```json
{
	"user_id" : "SARHL2_ID2894646521_USER",
	"device_id" : "SARHL2_ID0931557300_DEVC",
	"session_token" : "71fe96d81a11a32a39ba410d812181ad",
	"based_on" : "", 
	"reference_pos" : "SARHL2_ID1234567890_REFP",
	"current_pos" : [0, 0, 0],
	"radius" : 500.0f
}
```

—

response download:

- `waypoints_alignment` — serve per mantenere allineati il client e il server per quanto riguarda gli ID. Con questo campo, il server informa il client che tot nodi devono cambiare ID per poter essere coerenti con le informazioni del server.
    
    per via dello ******************staging ID sharing******************, gli ID dei waypoints vanno fatti corrispondere alle info che il server ha già registrato in precedenza. 
    
    il client cambia gli ID, e qualunque aggiustamento sugli ID ancora non scambiati col server è affare del client che non riguarda il server.
    
    l’ID registrato e ottenuto in caso di allineamento è quello della sessione corrente, no nquello ereditato dalla sessione precedente. Quello è l’ID globale, che è la chiave della tabella staging. 
    
- `based_on` : serve per ritornare eventualmente il fake token generato alla richiesta quando la richiesta è di calibrazione.
    
    → il server va in allarme se si tenta di usare la richiesta per farsi ritornare un fake token che è stato già generato per quello user e quel device
    
    → viene popolato una sola volta a seguito della calibrazione, poi basta
    
    → ad ogni richiesta successiva, il server si occupa di controllare che il fake token sia stato istanziato
    
- `max_id` — il device deve impostare il suo indice interno delle posizioni a questo valore +1.

```json
{
	"status" : 200,
	"status_detail" : "...",
	"based_on" : "",
	"max_id" : 57,
	"waypoints_alignment" : {
		... ??? ...
	}
}
```

—

### Download Procedure

Assunzioni:

- il download è chiamato immediatamente dopo la calibrazione
- gli ID locali della sessione sono allineati
    
    **************************è un concetto che tiene conto dell’ordinamento locale della sessione. Considera un percorso di 3 punti in una certa sequenza locale: magari sono già stati registrati in altre sessioni, ma l’ordine locale degli ID è vincolato anche all’evoluzione temporale dell’esplorazione.************************** 
    
    ***********************************************Per ogni sessione, l’incremento dell’ID è monotono e denso, non lascia buchi.*********************************************** 
    

descrizione della procedura:

1. informazioni deducibili dal JSON
    1. prima calibrazione?
        
        *********************ovvero controlla che `base_on` sia popolato. Se non lo è, il dispositivo sta tentando di calibrarsi rispetto al reference point.* 
        
    2. session token a partire dal `based_on`
        
        *********lascia NULL se non serve*********
        
2. check sul fake token se presente
    
    ******se la `based_on` è una empty string, allora il dispositivo sta tentando di calibrarsi*
    
    1. è vero che il dispositivo non si è ancora calibrato?
        
        ****************************************vai a vedere nella tabella dei fake token e vedi se per quella sessione con quel dvice e quello user è stato staaccato un fake token****************************************
        
        → unsecure request : ***********************************************************************************************è già stato staccat un fake token, quindi è anche già stato trasmesso, allora perchè il client non me lo sta mandando? forse un tentativo malizioso di avere un nuovo fake token? (sempre pensare al peggio)***********************************************************************************************
        
3. informazioni preliminari dalla tabella di staging dal server
    1. GET — tutte le sessioni e sessioni ereditate ordinate a partire dalla più recente
        
        **************cioè controlla in tabella 1) se esiste la mia sessione e 2) se esiste una sessione ereditabile, vale a dire con l’inherited ID a null**************
        
    2. GET — tutti gli ID già passati al device
        
        ****************vedi staging IDs sharing.**************** 
        
4. la sessione è stata già attivata nell’area di staging?
    
    ****************************************per fare il check, usa il token di sessione vero sulla abella di staging (ediqueries precedenti). Occhio che deve corrispondere anche il reference position oltre che il device****************************************
    
    1. NO — 
        
        ****************se la sessione è stata precedentemente attivata, allora la tabella dovrebbe contenere anche il session token passato dall’utente. Se non è così, questo è il comportamento da seguire:****************
        
        1. INSERT — l’origine del device *********************fissa all’origine (0,0,0)*********************
            
            ***********************************corrisponde ad attivare la sessione sulla tabella di staging. Allegare anche la sessione ereditata come real session token***********************************
            
        2. INSERT — fissare area renaming 0
        3. INSERT — genera un nuovo fake token apposta per il servizio
            
            *******************************a paritre dall’inherited session token*******************************
            
    2. YES — 
        1. e sta ereditando da quella sessione che è stata dichiarata?
            1. NO — 
                
                → unsecure request a questo punto viene lecito chiedersi se quel token NON POSSEDUTO DA ME sia già stato staccato per un altro utente. Se è così, come diavolo hai fatto ad ottenere quel token? E’ qualcosa che più si avvicina al security breach. In ogni caso la cosa è sospetta e va segnalata. 
                
5. GET — ricerca dei punti da ritornare
    
    <aside>
    💡 il GET può essere messo su un thread separato
    
    </aside>
    
    ***********************ricerca coi parametri:*********************** 
    
    - DEVICE_ID
    - SESSION_TOKEN_ID <> *****************il session token corrente*****************
        
        ************************usa il real session token, ottenuto all’inizio della transazione. Questo esclude i punti che sono già stati ritornati dal server.************************ 
        
    - SESSION_TOKEN_INHERITED_ID
        
        *********************per farsi ritornare il set completo; vedi staging ID sharing*********************
        
    - U_REFERENCE_POSITION_ID
        
        ****************attualmente la possibilità di mappare sessioni su diversi sistemi di riferimento non è supportata.****************
        
    - diistanza massima dal punto centrale
        
        *************************************************questa info si trova all’interno della richiesta*************************************************
        
6. GET — ricerca degli archi da ritornare
    
    <aside>
    💡 il GET può essere messo su un thread separato
    
    </aside>
    
    ****************************in questa fase il server tenta di fare il massimo per ritornare archi che colleghino solo i punti che io non ho ancora a disposizione. La ricerca usa fondamentalmente gli stessi parameri usati per i WPs per trovare una base comune di informazioni. La ricerca può essere implementata come singola query:****************************
    
    - dati i WPs che sono o saranno noti al device a seguito di questa richiesta
        - in particolare dati i WPs che saranno noti
    - dati i WPs a cui il device non avrà ancora accesso
    - dati gli archi appartenenti alla base comune
        - usando delle JOIN secche, escludi tutti gli archi che contengono punti non ancora noti al device
        - **********************************************la distanza sull’arco te la passa direttamente il device********************************************** (dio grazie)
    - prendi gli archi in cui almeno una delle Posizioni è completamente nuova
7. PYTHON ricostruzione dei cammini
    
    ***********************************************************cioè escludere gli archi checausano la disconnessione del grafo. Il risultato non è un albero di cammini minimi, ma bensì un insieme di archi tale da creare una coverage totale dei WPs selezionati***********************************************************
    
8. INSERT — genera i nuovi archi sulla sessione in staging
    
    ******************************passa direttamente dal JSON e siamo a posto. Lo step viene fatto anche nella DOWNLOAD perchè introduce un’utile semplificazione, che però ha lo svantaggio tecnico di non dare posibilità di aggiungere nuovi archi se non in un’area diversa.****************************** 
    
9. logging
10. invio del contenuto del download

—

### JSON formato pacchetti — upload

request upload:

- `base_on` — sempre valurizzato nella request (errore se non lo è), corrisponde al fake session token.

```json
{
	"user_id" : "SARHL2_ID2894646521_USER",
	"device_id" : "SARHL2_ID0931557300_DEVC",
	"ref_id" : "SARHL2_ID1234567890_REFP",
	"session_token" : "71fe96d81a11a32a39ba410d812181ad",
	"base_on" : "12345...",	
	"radius" : 0.8,
	"waypoints" : [
		{
			"pos_id" : 1,
			"area_id" : 0,
			"v" : [ 0, 0, 1 ],
			"tstmp" : "2023/08/23 00:00:02"
		},
		{
			"pos_id" : 2,
			"area_id" : 0,
			"v" : [ 0, 0, 2 ],
			"tstmp" : "2023/08/23 00:00:03"
		},
		{
			"pos_id" : 3,
			"area_id" : 0,
			"v" : [ 0, 0, 3 ],
			"tstmp" : "2023/08/23 00:00:05"
		},
		{
			"pos_id" : 4,
			"area_id" : 0,
			"v" : [ 1, 0, 0 ],
			"tstmp" : "2023/08/23 00:00:10"
		}
	],
	"paths" : [
		{"wp1" : 0, "wp1" : 1, "dist" : 1.0, "tstmp" : "2023/08/23, 00:00:02"},
		{"wp1" : 1, "wp1" : 2, "dist" : 1.0, "tstmp" : "2023/08/23, 00:00:03"},
		{"wp1" : 2, "wp1" : 3, "dist" : 1.0, "tstmp" : "2023/08/23, 00:00:05"}
	]
}
```

—

response upload:

```json
{
	"status" : 200,
	"status_detail" : "...",
	"added_wps" : 0,
	"discarded_wps" : 0,
	"wp_alignment" : [],  ...????
	"zone_alignment" : []
}
```

—

### Upload Procedure

Assunzioni:

- gl iID sono allineati tra device e hololens2
    
    ****************magari su HL2 ci sono buchi, ma non importa.**************** 
    
- per semplicità, per ora escludiamo gli area index pls

Procedura:

1. `based_on` non può essere non valorizzato
    
    *se è così, ritorna 404 secco*
    
2. GET informazioni ausiliarie pre-elaborazione
    1. la sessione esiste in staging?
        1. YES — 
            1. controlla se c’è una sessione ereditata
                
                **************caso rarissimo: se la sessione in oggetto non eredita da nesusna altra sessione, puoi saltare direttamente all’insert**************
                
                altrimenti controlla che la sessione ereditata coincida con quella dichiarata. Se non è così, unsecure request controlla che quel fake token non venga utilizzato da qualcun altro, e se è così allora abbiamo una situazione molto pericolosa.
                
                se tutto va bene, ottieni il token della sessione ereditata se presente
                
        2. NO —
            
            ************************************************************************allora rifiuta la richiesta con 404, evidentemente il device non ha ancora chiamato la DOWNLOAD prima di procedere a caricare il dato.************************************************************************
            
    2. … concurrency check … 
        
        ********************non implementato qui********************
        
    3. il valore massimo degli indici sulla sessione originale
    4. gli indici trasmessi finora al server o dal server
        
        ***************************************************************************************fai una SELECT sulla sessione attuale in stagging e vedi che cosa è stato ritornato. Con questa sai quali sono le informazioni che erano note al device prima dell’upload, e si riesce anche a determinare quali informazioni sono da escludere dall’elaborazione perchè ridondanti (HL2 non dovrebbe scambiare info già note… ma nel caso, meglio selezionare)*
        
3. ALIGNMENT ALGORITHM
    
    ************vedi query gigante scritta ieri che prende le richieste e individua le ridondanze in base alla semplice distanza tra di loro, parametrico, e tutto il resto.************
    
    l’algoritmo tira fuori tutte le somiglianze con quanto è già stato memorizzato nel database, attribuendo alal scelta anche un valore di euristica che indica la qualità della scelta
    
4. WRITE WAYPOINTS
    1. per i WPs individuati nuovi rispetto alle precedenti misurazioni,
        1. LOCAL_POSITION_ID — rownumber+id massimo (per semplicità puoi assegnare anche direttamente da Python)
        2. U_SOURCE_FROM_SERVER_FL — false
        3. LOCAL_AREA_INDEX_ID — … ??? 
        4. ALIGNMENT_ALIGNED_WITH_WAYPOINT_FK — NULL (default)
        5. ALIGNMENT_TYPE_FL — false, ovvero il punto non è stato allineato, non è ridondante
        6. ALIGNMENT_QUALITY_VL — la qualità tirata fuori dall’algoritmo
    2. per quelli che l’algoritmo è riuscito ad allineare, 
        1. LOCAL_POSITION_ID — l’ID allineato con la precedente misurazione
        2. U_SOURCE_FROM_SERVER_FL — true
        3. ALIGNMENT_ALIGNED_WITH_WAYPOINT_FK — la chiave te la da direttamente l’algoritmo
        4. ALIGNMENT_TYPE_FL — true, ovvero il punto è stato allineato con un altro già presente nella sessione
        5. ALIGNMENT_QUALITY_VL — la qualità tirata fuori dall’algoritmo
5. WRITE PATHS
    - ogni path deve risultare in tabella riportano le chiavi primarie dei waypoints
    - per semplificare l’iter, puoi sfruttare la situazione sulla tabella dei waypoints per aggiungere tutte quante le informazioni
6. logging
7. risposta alla richiesta

—

### Next Steps (To Be Done)

- DOWNLOAD — cammini minimi in uscita dalla query in staging
- tutta l’area di QUALITY compresi eventuali data processors non saranno affar mio
- area renamings — per il momento non implemento questa cosa
    
    *toglierò questa funzionalità anche da HoloLens2, oppure farò in modo di averla sul device ma per il momento non supportata anche dal databae, quindi fi quando non si riesce a trovare una connessione tra zone, il device non invia informazioni (pericoloso eh, però non sarà un problema mio)*
    
    ovviamente la cosa verrà proposta in discussione…
    

