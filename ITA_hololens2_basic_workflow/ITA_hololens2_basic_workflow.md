# HoloLens2 — workflow — primi passi e aspetti di base

---

---

Questo video costituisce un buon punto di partenza → [https://www.youtube.com/watch?v=P8og3nC5FaQ](https://www.youtube.com/watch?v=P8og3nC5FaQ)

una issue che capita spesso → [https://stackoverflow.com/questions/68459428/reference-to-type-matrix4x4-claims-it-is-defined-in-system-numerics-but-it](https://stackoverflow.com/questions/68459428/reference-to-type-matrix4x4-claims-it-is-defined-in-system-numerics-but-it)

---

# Progetto vuoto — primi settaggi utili

Partiamo da una scena completamente vuota. 

## LA PRIMISSIMA COSA DA FARE — aggiungi MRTK

Prima di fare qualunque cosa, ricorda di applicare le impostazioni consigliate per HoloLens2. In caso qualcosa non ti piacesse, puoi sempre cambiarla più avanti. Dal menu “Mixed Reality > Project” chiama entrambe le opzioni, prima quella sul progetto e poi quella sulla scene. Chiama quella sulla scene ogni volta che crei una nuova scena. 

![vecchia versione](./Untitled.png)

vecchia versione

![nuova versione](./Untitled%201.png)

nuova versione

Alla creazione della nuova scene, l’oggetto `MixedRealityToolkit` non è incluso nella hierarchy: va aggiunto selezionando l’opzione “Mixed Reality > Toolkit > Add to Scene and Configure…”

![Untitled](./Untitled%202.png)

La nuova Hierarchy dovrebbe apparire così:

![Untitled](./Untitled%203.png)

## Selezione di un profilo per MRTK

Un profilo per MRTK è un insieme di impostazioni per il funzionamento della scene. 

### Creazione iniziale nuovo profilo

Seleziona nella hierarchy l’oggetto `MixedRealityToolkit` e seleziona l’opzione “Copy & Customize” per attivare le opzioni di configurazione manuali. 

![Untitled](./Untitled%204.png)

Questo farà apparire una finestra per la creazione di un nuovo profilo per MRTK. Inserisci il nome del nuovo profilo, e seleziona Clone. 

![Untitled](./Untitled%205.png)

In caso servisse, selezionando Advanced Options puoi decidere quali parti del profilo copiare e quali ripristinare a default. 

![Untitled](./Untitled%206.png)

### Cambio di profili

Appena dopo il nome dello script di MixedRealityToolkit c’è un dropdown che permette la selezione di tutti i profili:

![Untitled](./Untitled%207.png)

Ogni profilo è formalmente un asset. MRTK salva i profili custom in `Assets/MixedRealityToolkit.Generated/CustomProfiles`

![Untitled](./Untitled%208.png)

I profili ufficiali invece vengono salvati sempre come assets sotto la cartella

![Untitled](./Untitled%209.png)

# Visualizzazioni e simulazione

## Impostazioni della Camera in-Editor

Vedi la barra sopra alla visualizzazione della scene:

![Untitled](./Untitled%2010.png)

## Play su progetto vuoto

Premendo *Play* in una scene che non contiene l’oggetto `MixedRealityToolkit` non si avrà controllo sulla scena, e qualunque altra feature in generale per simulare il comportamento della scene in realtà aumentata. 

Dopo l’aggiunta di `MixedRealityToolkit` se premi su *Play* avrai tutte le funzionalità necessarie più un pannello che mostra le prestazioni della scene in tempo reale. 

## Pannello di diagnostica

Il pannello di diagnostica è quello che appare sotto la mano nella simulazione e che indica le risorse occupate dall’applicazione. 

![Untitled](./Untitled%2011.png)

Il pannello viene attivato o disattivato a seconda delle impostazioni di profilo, vedi immagine. 

![Untitled](./Untitled%2012.png)

![ecco come appare la finestra con l’opzione non spuntata. ](./Untitled%2013.png)

ecco come appare la finestra con l’opzione non spuntata. 

## Comandi della simulazione in Unity

Movimento

- Tasto Dx mouse temuto premuto + WASD → movimento, camminare in giro
    
    (questo funziona anche con la camera in-editor)
    

Sguardo:

- tieni premuto tasto Dx mouse → focus
- click tasto Sx mouse → click sul focus

Mano:

- tieni premuta spacebar → visualizza input a distanza con la mano
    
    ![Untitled](./Untitled%2014.png)
    
- Per usare la mano sinistra tieni premuto Shift
    
    ![Untitled](./Untitled%2015.png)
    
- Il sistema non supporta (ovviamente) Sx e Dx insieme. Ecco cosa accade:
    
    ![Untitled](./Untitled%2016.png)
    
- click mouse Sx → “pinch” della mano a distanza
    
    ![Untitled](./Untitled%2017.png)
    

## Scene Background — Black BG

Quella specie di cielo che mostra l’orizzonte… Si può attivare e disattivare, “Windows > Rendering > Lighting”

![Untitled](./Untitled%2018.png)

Nella finestra che appare seleziona “Environment”. L’opzione che devi modificare è Skybox, che prende un Material. Seleziona None per settare il BG a black, cosa che può essere utile quando si vuole aumentare la visibilità degli elementi. 

![Untitled](./Untitled%2019.png)

# Primi passi coi Prefabs

I prefabs di MRTK mettono a disposizione tantissimi prefabs per iniziare a sperimentare con le interfacce per realtà aumentata e per prototipare in fretta. 

## MRTK Toolbox — dove trovarlo e cos’è

<aside>
⚠️ Calma: *non è così facile da usare come sembra. Per ogni widget, fai sempre riferimento alla documentazione allegata.*

</aside>

Seleziona “Mixed Reality > Toolkit > Toolbox”. Apparirà una nuova tab in Unity. 

![Untitled](./Untitled%2020.png)

La tab contiene un sacco di elementi per costruire subito una primissima interfaccia per realtà aumentata con i più comuni elementi. Click sull’icona dell’elemento, e questi verrà aggiunto alla scena. Click su “Documentation” sotto l’icona per aprire la documentazione su Internet relativa a quel determinato oggetto. 

![Untitled](./Untitled%2021.png)

---