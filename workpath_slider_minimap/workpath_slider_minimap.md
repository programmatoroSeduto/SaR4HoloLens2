# @June 25, 2023 — PERCORSO — Primi test per la feature minimappa

- ********created June 25, 2023 8:56 AM*
- getting started
- references
- altre guide
- troubleshooting
- folklore

---

---

## Impostazione del lavoro

innanzitutto, una minimappa è una semplice serie di oggetti disposti sotto una certa radice. 

1. creo un empy game object
2. creo anche uno script per popolare random la minimappa entro certe coordinate
3. (vedi script 001)
    
    il risultato è
    
    ![Untitled](./Untitled.png)
    
4. ora che abbiamo la nostra (strana) minimappa, la voglio rendere scalabile con un componente hololens2
    
    il componente che consente di fare questo lavoro è il `BoundsControl`applicato direttamente al root object. 
    
    il component ha bisogno praticamente di zero configurazioni. Unica particolarità richiesta è il box collider, ce ho configurato così in questo esempio:
    
    ![Untitled](./Untitled%201.png)
    
5. tutto molto bello, ma vorrei automatizzare la creazione della minimappa. Perciò, vado a modificare lo script di prima aggiungendo la funzionalità
    
    basta aggiungere il component, previa aggiunta e regolazione di un bounding box. 
    
6. e se volessi portarmela in giro questa minimappa? Un modo è sicuramente usare un solver e fare in modo che mi segua, ma voglio anche spostarla con una sorta di maniglia. 
    
    il bounds control è adatto per fare scaling di oggetti che mi aspetto rimangano immobili. Ma per muovere e manipolare direttamente gli oggetti, sarebbe meglio in realtà usare un `ObjectManipulator` (il manipulation handle è deprecato, occhio). 
    
    Il component richiede l’uso di un `NearInteractionGrabbable`. E richiede anche un BoxCollider, altrimenti va in errore. 
    
    Il component però consente solo di spostare l’oggetto con la mano. Per cui, la combinazione migliore è usare sia un `ObjectManipulator` che un `BoundsControl` con gli opportuni constraints. 
    
    ```
    using Microsoft.MixedReality.Toolkit.Input;
    using Microsoft.MixedReality.Toolkit.UI;
    using Microsoft.MixedReality.Toolkit.UI.BoundsControl;
    
    private void SetupMinimap()
    {
        // box collider
        BoxCollider bc = GoRoot.AddComponent<BoxCollider>();
        bc.center = BoxCenter;
    
        // frame
        NearInteractionGrabbable nig = GoRoot.AddComponent<NearInteractionGrabbable>();
        BoundsControl frame = GoRoot.AddComponent<BoundsControl>();
    		frame.BoundsOverride = bc;
        ObjectManipulator manip = GoRoot.AddComponent<ObjectManipulator>();
    }
    ```
    
7. ora ho la mia fantastica minimappa, ottimo. L’ultimo passo è vedere questa cosa del **************piano passante************** che mette in trasparenza qualunque cosa non faccia parte dell’insieme dei punti “eletti”. 
    
    Anzutto, un “piano” in Unity è sempre un cubo sotto mentite spoglie. **************************E lo voglio in trasparenza**************************, quindi dovrei fare un material apposito per questo. 
    
    ********************************************************************************************************************************************per vincolare il tool all’oggetto, almeno in questo caso, c’è un problema.******************************************************************************************************************************************** Perchè il bounds control purtroppo si adatta ********************************al più grande oggetto sotto il gambe object********************************, il che ha senso, ma mi piacerebbe rivedere questo comportamento almeno in questo caso particolare. (in altre parole, non posso mettere sotto la stessa root i tools…)
    
    ************************************************************************Ma guarda un po’ uno cosa si deve inventare per risolvere il problema.************************************************************************ Se l’origine della minimappa coincide sempre con l’origine del frame dei tools, allora è possibile creare un banalissimo script che si occupa di **********eguagliare********** le transform dei due oggetti. 
    
    (VEDI SCRIPT 003)
    
    Sul tool ho fatto diverse configurazioni particolari, specie sul movimento. Non mi piace tantissimo che non ci sia un constraint sulla portata del movimento, ma per il momento direi che possiamo farcene una ragione. 
    
    ![Untitled](./Untitled%202.png)
    
    ![Untitled](./Untitled%203.png)
    
    Nota che i constraint in questo caso ***************vietano il moto***************, non lo vincolano. Quindi per muoversi solo lungo y bisogna indicare due constraint, *******locali.******* Uno per X e uno per Z. 
    
    ![Untitled](./Untitled%204.png)
    
    ![Untitled](./Untitled%205.png)
    
    sugli altri componenti non è stata fatta alcuna configurazione particolare.
    
8. E infine, la parte più difficile: fare questo meccanismo che assegna la trasparenza a tutti i GO che non si trovano all’interno del’area del cubo. Sono abbastanza vicino, ma prima devo capire come rendere il tutto efficiente…
    
    Supponiamo che sotto all’oggetto Minimap ci siano solo ed esclusivamente gli elementi della minimappa, com’è giusto che sia. Gli oggetti all’interno possono essere anche complessi al loro interno, ma le root sono solo ed esclusivamente quelle delle visualizzazioni.
    
    ![Untitled](./Untitled%206.png)
    
    la cosa ha senso; l’unico problema avrebbe potuto essere legato agli oggetti aggiunti dal component bounds control, ma alla fin fine quelli sono solo oggetti in renderng, quindi nella gerarchia non rientrano. 
    
    L’oggetto si dovrebbe comportae così:
    
    1. ho un comando per farlo apparire e sparire anzitutto (tipo un comando vocale, per ora non mi interessa)
    2. quando faccio apparire il tool, lo faccio apparire e rendo trasparenti o nascosti tutti gli oggetti all’interno del cubo
        
        ******facile******: un grosso ciclo for che si smazza tutto dentro al cubo e dsattiva qualunque cosa al suo interno (calma: avevo detto ***********trasparenza***********, non ammazzali tutti)
        
        per fare questo *****conviene usare un qualche script che tenga traccia degli oggetti all’interno del cubo*****, che può essere anche la minimappa stessa. 
        
        ogni volta che devo inserire un nuovo oggetto nella minimappa, per l’inserimento faccio fare tutto allo script. E (per ragioni di semplicità su passaggi successivi che ho in mente) **************************************************mi serve che lo script raccolta le posizioni ordinate per altitudine.************************************************** Il che si può fare, ****anzi****: alla fine il criterio di ordinamento l’ho messo pure dinamico. 
        
        - prima versione un po’ sbagliata dell’algoritmo di visualizzazione nell’area dello slider
            
            ```
            public void ShowItemsInRange(bool opt = true, float MinHG = float.NaN, float MaxHG = float.NaN, bool hideAllBeforeStarting = false )
            {
            	if (MinHG > MaxHG)
            	{
            		ShowItemsInRange(opt, MaxHG, MinHG, hideAllBeforeStarting);
            		return;
            	}
            	else if ((MinHG < MinOrderCriterion || MaxHG < MinOrderCriterion) || (MinHG > MaxOrderCriterion || MaxHG > MaxOrderCriterion))
            		return;
            
            	MinHG = (float.IsNaN(MinHG) ? MinOrderCriterion : MinHG);
            	MaxHG = (float.IsNaN(MaxHG) ? MinOrderCriterion : MaxHG);
            	float delta = MaxOrderCriterion - MinOrderCriterion;
            	float reqDelta = MaxHG - MinHG;
            
            	int startIdx = Mathf.FloorToInt(((MinHG - MinOrderCriterion)/ delta) *TrackingList.Count);
            	int endIdx = Mathf.Max(new int[] { 
            		Mathf.CeilToInt((1 - (MaxHG - MinOrderCriterion) / delta) * TrackingList.Count), 
            		TrackingList.Count - 1 
            	});
            
            	float startHG = ((MinimapStructureEntry)TrackingList[startIdx]).OrderCriterion;
            	float endHG = ((MinimapStructureEntry)TrackingList[endIdx]).OrderCriterion;
            	float tempDelta = endHG - startHG;
            
            	int startDir = (startHG > MinHG ? -1 : +1);
            	while(startHG != MinHG) // beginning index improvement
            	{
            		int newStartIdx = startIdx + startDir;
            		if (newStartIdx < 0 || newStartIdx >= endIdx) break; // can't improve further
            
            		float newStartHG = ((MinimapStructureEntry)TrackingList[newStartIdx]).OrderCriterion;
            		float newStartError = Mathf.Abs(reqDelta - (endHG - newStartHG));
            
            		if (newStartError < Mathf.Abs(reqDelta - tempDelta))
            		{
            			if (((newStartHG < MinHG) && (startDir > 0)) || ((newStartHG > MinHG) && (startDir < 0)))
            			{
            				startIdx = newStartIdx;
            				startHG = ((MinimapStructureEntry)TrackingList[startIdx]).OrderCriterion;
            				tempDelta = endHG - startHG;
            			}
            			else
            				break;
            		}
            		else break;
            	}
            
            	int endDir = (endHG > MaxHG ? -1 : +1);
            	while (endHG != MaxHG) // final index improvement
            	{
            		int newEndIdx = endIdx + endDir;
            		if (newEndIdx == TrackingList.Count || newEndIdx <= startIdx) break; // can't improve further
            
            		float newEndtHG = ((MinimapStructureEntry)TrackingList[newEndIdx]).OrderCriterion;
            		float newEndError = Mathf.Abs(reqDelta - (newEndtHG - startHG));
            
            		if (newEndError < Mathf.Abs(reqDelta - tempDelta))
            		{
            			if (((newEndtHG > MaxHG) && (startDir < 0)) || ((newEndtHG < MaxHG) && (startDir > 0)))
            			{
            				endIdx = newEndIdx;
            				endHG = ((MinimapStructureEntry)TrackingList[endIdx]).OrderCriterion;
            				tempDelta = endHG - startHG;
            			}
            			else
            				break;
            		}
            		else break;
            	}
            
            	bool quick = hideAllBeforeStarting;
            	if (hideAllBeforeStarting) HideItemsInVisualizationList();
            
            	for (int i = startIdx; i <= endIdx; ++i)
            		ShowItem(((MinimapStructureEntry)TrackingList[endIdx]).Object, quick);
            }
            ```
            
    3. faccio apparire solo gli elementi all’interno della zona
        
        uno script che faccia da DB sufficientemente ottimizzato per tenere traccia di chi è visibile e chi no ce l’ho. Adesso serve uno script che comando il DB in base alla posizione dello slider. 
        
        se l’ordinamento è dato dalla quota relativa, io devo nascondere tutti gli elementi che stanno ad una certa quota iniziale più l’altezza del tool …
        
    
    **********Et voillà!********** Il risultato è abbastanza carino. Stiloso, ed efficiente. 
    
    VEDI SCRIPT 005
    
    VEDI SCRIPT 004
    
    ![Untitled](./Untitled%207.png)
    
    Nota: se il criterio cambiasse, l’ordinamento andrebbe rivisto. In particolare, essendo l’inserimento del criterio dato ad un componente esterno, è bene non mescolare pere con mele. Quando si va a tracciare un nuovo oggetto su una scale che è diversa da quella iniziare, ****************************occorre refreshare l’intero database.**************************** è un difetto che in questo percorso di tesi non penso andrà affrontato, ma che va comunque segnalato. 
    
9. creazione dinamica del tool, alra impresa non esattamente easy perchè lavorando con un dev script le cose sono sono mai proprio realistiche al 100%…
    
    dovedo mantenere una struttura fissa del game object tipo questa …
    
    ![Untitled](./Untitled%208.png)
    
    … la prima cosa  acui mi verrebbe da pensare è creare un handle sistemato che mostri/nasconda la minimappa, e che mostri/nasconda il tool per la visualizzazione lungo un piano. 
    
    però vista la natura del mio codice attuale, direi di non essere ancora pronto per “andare in produzione” con un vero e proprio handle, tanto più che mi manca una vera sorgente dati a cui fare riferimento per le posizioni. 
    
    ragioniamo:
    
    - nello script di test creo una nuova funzione che si occupa di creare il tool
    
    la struttura è quella di prima, quindi per trovare gli oggetti che mi interssano devo fare questi controlli:
    
    verificare che esista `_tools` al di sotto della root (meno grave se manca, posso ricrearlo)
    
    verificare che esista un game object `Minimap`
    
    e soprattutto **********************verificare che `Minimap` sia una minimappa*. In che modo lo verifico? Tentando di reperirne il componente MinimapStructure, e se non esiste significa che quella roba non è una minimappa.
    
    piccolo dramma coi component: **************************************************************avevo disattivato i warning, iniziavo ad essere molto confuso.**************************************************************
    
    metodo 1 per verificare se un qualcosa ha un certo component:
    
    ```
    AudioSource[] co = GoAssetRoot.GetComponents<AudioSource>();
    if (co.Length > 0)
    	Debug.LogWarning("Found a component");
    else
    	Debug.LogWarning("No component");
    Debug.Log($"count: {co.Length}");
    ```
    
    è un metodo buono perchè non fa ritorni nulli
    
    Altrimenti, metodo 2:
    
    ```
    AudioSource co = GoAssetRoot.GetComponent<AudioSource>();
    if (co != null)
    	Debug.LogWarning("inside IF");
    ```
    
    un po’ più grezzo, e ha un tipo di ritorno null.
    
    Nel mio caso, dovrei verificare se un GameObject ha un component chiamato `MinimapStructure`. 
    
    il check si può implementare i nquesta maniera, avendo cura di allocare anche gli elemtni come variabilli private:
    
    ```
    // minimap gameobject
    private GameObject goMinimap = null;
    // minimap structure component in the minimap
    private MinimapStructure coMinimapStruct = null;
    // tools root for the minimap
    private GameObject goTools = null;
    
    private bool CheckMinimapAsset()
    {
        // find minimap
        Transform trMinimap = GoAssetRoot.transform.Find("Minimap");
        if (trMinimap = null)
            return false;
        goMinimap = trMinimap.gameObject;
    
        // check minimap type
        coMinimapStruct = goMinimap.GetComponent<MinimapStructure>();
        if (coMinimapStruct == null)
            return false;
    
        // check or create _tools gambe object
        Transform trTools = GoAssetRoot.transform.Find("_tools");
        if (trTools == null)
            return false;
        goTools = trTools.gameObject;
    
        return true;
    }
    ```
    
    - Il map structure va impostato attivabile e disattivabile gestendo le callbacks
    
    in Unity è molto semplice, perchè l’interfaccia è già implementata:
    
    ```
    void OnDisable()
    {
        foreach (MinimapStructureEntry item in TrackingList)
            item.Object.SetActive(true);
    }
    
    void OnEnable()
    {
    	foreach (MinimapStructureEntry item in TrackingList)
    	  if(!VisualizationList.Contains(item.Object))
    	      item.Object.SetActive(false);
    	  else
    	      item.Object.SetActive(true);
    }
    ```
    
    basta definire queste due callbacks nell’oggetto `MinimapStructure` .
    
    La cosa migliore i nrealtà sarebbe assegnare a queste opzioni alcuni behaviours particolari per gestire cosa fare al momento della riattivazione, cosa fare al momento della disattivazione, e tutto il resto. In effetti ci sono diverse alternative per l’implementaaizone di questi metodi, e nella pratica uno non si può certo limitare ad uno solo. *******************************************************************************Ache solo per una questione di efficienza, questo metodo mi piace molto poco.******************************************************************************* Perchè significa smazzarsi una lista ad ogni attivazione disattivazione. 
    
    nota inoltre che i metodi di Unity si possono chiamare sia quando i lcomponent è aattivo che quando non lo è. Perciò bisogna porre su tutti i metodi pubblici
    
    ```
    if (!this.isActiveAndEnabled) return;
    ```
    
    e questo non basta ancora, perchè la cache può creare un effetto strano di questo tipo. 
    
    PRIMO — avvio tutto
    
    ![Untitled](./Untitled%209.png)
    
    SECONDO — disattivo la structure, e viene visualizzao tutto
    
    ![Untitled](./Untitled%2010.png)
    
    TERZO — muovo il tool, che giustamente non ha effetto
    
    ![Untitled](./Untitled%2011.png)
    
    QUARTO — riattivo il component, ************************************************ma succede che il piano sta da tutt’altra parte************************************************
    
    ![Untitled](./Untitled%2012.png)
    
    se torno a muovere il piano, funziona tutto di nuovo. Questo problema in realtà si può risolvere creando un handle che gestisca queste situazioni coordinando i vari tools. In questo modo si riesce ad evitare, per quanto possibile, situazioni di questo tipo. 
    
    Aggiornamento: *******************questo accade perchè il component `SliderTool` continua a tracciare la posizione del pezzo.* Lo script in particolare deve controllare se il componente è attivo, oltre a controllare se la posizione cambia. Modificando la UPDATE la cosa dovrebbe funzionare. ********************E infatti *****funge.******* 
    
    - quando creo il tool, devo poter attivare la selezione di range. Quando disattivo il tool devo poter vedere tutto
    
    Più che ad un oggetto attivabile o disattivabile, in questa situazione mi verrebbe in mente una chiamata su visualizza tutto o visualizza un range. 
    
    Partiamo dal fatto che all’inizio è tutto visualizzato.
    
    faccio selezione di range, e viene nascosto tutto
    
    ```
    public void ShowItemsInRange(float MinHG, float deltaHG, bool hideAllBeforeStarting = false)
    ```
    
    faccio visualizza tutti, e vengono visualizzati tutti
    
    ```
    public void ToggleVisualizationAll(bool opt = true)
    ```
    
    Pesando di dover implementare un handle, l’handle dovrebbe
    
    attivare o disattivare il tool
    
    l’attivazione autorizza il tool a dare comandi al component
    
    quando si va a disattivare il tool da handle, l’handle si occupa di fare in modo che il component non possa più chiamare il minimap structure
    
    Anche qui, essendo uno script di test le cose non sono proprio facilissime. Ci vorrà un po’ di rework alla fine. 
    
    - sotto al `_tools` va creato un game object che implementa il tool
    
    la mancanza di uno script mi ha indotto a fare una segnalazione ahah
    
    [https://github.com/MicrosoftDocs/mixed-reality/issues/737](https://github.com/MicrosoftDocs/mixed-reality/issues/737)
    
    codice: (solo parte grafica)
    
    <aside>
    📢 Lo script è incompleto perchè manca la parte di controllo della funzionalità.
    
    </aside>
    
    ```
    // === FEATURE PLANE TOOL ===
    
        private void WrapperFunctionCreatePlaneTool()
        {
            if (CheckMinimapAsset())
                CreatePlaneTool();
            done = true;
        }
    
        private bool CheckMinimapAsset()
        {
            // find minimap
            Transform trMinimap = GoAssetRoot.transform.Find("Minimap");
            if (trMinimap == null)
            {
                Debug.LogWarning("ERROR: cannot create tool (Minimap not found)");
                return false;
            }
            goMinimap = trMinimap.gameObject;
    
            // check minimap type
            coMinimapStruct = goMinimap.GetComponent<MinimapStructure>();
            if (coMinimapStruct == null)
            {
                Debug.LogWarning("ERROR: cannot create tool (Minimap is not a minimap)");
                return false;
            }
    
            // check or create _tools gambe object
            Transform trTools = GoAssetRoot.transform.Find("_tools");
            if (trTools == null)
            {
                Debug.LogWarning("ERROR: cannot create tool (object _tools not found)");
                return false;
            }
            goTools = trTools.gameObject;
    
            // check material reference
            if(ToolMaterial == null)
            {
                Debug.LogWarning("ERROR: tool material for the slider tool has not been set!");
                return false;
            }
    
            return true;
        }
    
        private void CreatePlaneTool()
        {
            GameObject goPlaneTool = GameObject.CreatePrimitive(PrimitiveType.Cube);
            goPlaneTool.name = "MinimapSliderTool";
            goPlaneTool.transform.SetParent(goTools.transform);
            goPlaneTool.transform.localPosition = new Vector3(0.5f, 0.0f, 0.5f);
            goPlaneTool.transform.localScale = new Vector3(1.2f, 0.1f, 1.2f);
            goPlaneTool.GetComponent<Renderer>().material = ToolMaterial;
    
            goPlaneTool.AddComponent<NearInteractionGrabbable>();
            goPlaneTool.AddComponent<ObjectManipulator>();
    
            ConstraintManager coConstraints = goPlaneTool.GetComponent<ConstraintManager>();
    
            MoveAxisConstraint coConstraintX = goPlaneTool.AddComponent<MoveAxisConstraint>();
            coConstraintX.ConstraintOnMovement = AxisFlags.XAxis;
            coConstraintX.UseLocalSpaceForConstraint = true;
    
            MoveAxisConstraint coConstraintZ = goPlaneTool.AddComponent<MoveAxisConstraint>();
            coConstraintZ.ConstraintOnMovement = AxisFlags.ZAxis;
            coConstraintZ.UseLocalSpaceForConstraint = true;
        }
    ```
    
    - e va creato il tool per la visualizzazione con le specifiche individuate l’ultima volta
    
    vedi attuale script dello Slider Tool. 
    

—

### Prossimi steps

- premetto che tutti gli script presentati qui sono dei test, magari molto vicini alla realtà, ma ur sempre degli script di test che devono essere adattati un minimo per poter girare in produzione, e possibilmente anche ripuliti di certi errorini qua e là
- rendere lo slider tool attivabile e disattivabile (in linea di principio con una semplice attivazione disattivazione unity)
- deve esistere anche qui un oggetto che permetta di istanziare dinamicamente uno slider tool delle dimensioni corrette
- in generale il problema del corretto dimensionamento degli oggetti non è stato proprio affrontato, e visto come andrà la situazione mi sembra un bel casino
- in particolare oggi ho lavorato su oggetti istanziati un po’ a caso nell’area di riferimento: erano tutti ordinati, e non andava fatta alcuna
- Nota: se il criterio cambiasse, l’ordinamento andrebbe rivisto. In particolare, essendo l’inserimento del criterio dato ad un componente esterno, è bene non mescolare pere con mele. Quando si va a tracciare un nuovo oggetto su una scale che è diversa da quella iniziare, ****************************occorre refreshare l’intero database.**************************** è un difetto che in questo percorso di tesi non penso andrà affrontato, ma che va comunque segnalato.
- non mi sono occupato dell’omino che si muove nella mappa… mi sembra abbastanza semplice da realizzare: si crea un nuovo component, diverso da quello che traccia gli oggetti, e in qualche modo si mappa la posizione dell’omino sulla mappa con quella reale, o quella di qualche altro soccorritore magari (in un altro futuro, in un’altra tesi, forse, *****************************ma che ccertamente non sarà la mia*****************************)

—

### SCRIPT 001 — Script per popolare random al di sotto di una root

- script 001
    
    Il risulato dello script è quellodi creare un insieme di cubi al di sotto di una certa radice, fino a riempire lo spazio di un cubo uniformemente. 
    
    ![Untitled](./Untitled%2013.png)
    
    ```
    using System.Collections;
    using System.Collections.Generic;
    using UnityEngine;
    
    public class MinimapTestingScript : MonoBehaviour
    {
        // === GUI ===
    
        [Header("Component Functions")]
        [Tooltip("Randomly populate the root object")]
        public bool FunctionInstanciateRandom = false;
    
        [Header("Other settings")]
        [Tooltip("The root of the minimap (by defaul, it is the owner of this component)")]
        public GameObject ComponentRoot = null;
    
        [Header("Function: random objects generation")]
        [Tooltip("Number of objects to spawn")]
        public int NummOfObjects = 5;
        [Tooltip("max distance (local) from coordinates")]
        public Vector3 MaxDistVector = Vector3.zero;
        [Tooltip("Local scaling factor for each object inside the map")]
        public float Scale = 0.01f;
        [Tooltip("Object name (the index is added at the ed of this name)")]
        public string SpawnedObjectName = "cube";
    
        // === PRIVATE ===
    
        // for one-shot script
        private bool done = false;
        // the root object to use for generating the minimap (the script beholding this component)
        private GameObject GoRoot = null;
        // default for NumOfObjects
        private int NumOfObjectsDefault = 5;
        // default for Scale
        private float ScaleDefault = 0.05f;
    
        // === UNITY CALLBACKS ===
    
        // Start is called before the first frame update
        void Start()
        {
            GoRoot = (ComponentRoot == null ? gameObject : ComponentRoot);
        }
    
        // Update is called once per frame
        void Update()
        {
            if( !done && FunctionInstanciateRandom )
            {
                if( NummOfObjects < 0 )
                {
                    Debug.LogWarning("Number of Objects unser the minimap cannot be negative; using default");
                    NummOfObjects = NumOfObjectsDefault;
                }
                if( Scale < 0.0f )
                {
                    Debug.LogWarning("Scaling factor cannot be negative; using default");
                    Scale = ScaleDefault;
                }
                GenerateRandomObjectsUnderRoot();
                done = true;
            }
        }
    
        // === FUNCTIONALITIES ===
    
        private void GenerateRandomObjectsUnderRoot()
        {
            for( int i=0; i<NummOfObjects; ++i )
            {
                GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
                go.name = SpawnedObjectName + i.ToString("000");
                go.transform.SetParent(GoRoot.transform);
                go.transform.localPosition += new Vector3(Random.value * MaxDistVector.x, -Random.value * MaxDistVector.y, Random.value * MaxDistVector.z);
                go.transform.localScale = Scale * Vector3.one;
            }
        }
    }
    ```
    

—

### SCRIPT 002 — Setup semplice della minimappa

- script 002
    
    giusto per dare il feeling del risultato:
    
    ![Untitled](./Untitled%2014.png)
    
    ```
    using System.Collections;
    using System.Collections.Generic;
    using UnityEngine;
    using Microsoft.MixedReality.Toolkit.Input;
    using Microsoft.MixedReality.Toolkit.UI;
    using Microsoft.MixedReality.Toolkit.UI.BoundsControl;
    
    public class MinimapTestingScript : MonoBehaviour
    {
        // === GUI ===
    
        [Header("Component Functions")]
        [Tooltip("Randomly populate the root object")]
        public bool FunctionInstanciateRandom = false;
        [Tooltip("Create the minimap")]
        public bool FunctionCreateMinimap = false;
    
        [Header("General settings")]
        [Tooltip("The root of the minimap (by defaul, it is the owner of this component)")]
        public GameObject ComponentRoot = null;
    
        [Header("Function: random objects generation")]
        [Tooltip("Number of objects to spawn")]
        public int NummOfObjects = 5;
        [Tooltip("max distance (local) from coordinates")]
        public Vector3 MaxDistVector = Vector3.zero;
        [Tooltip("Local scaling factor for each object inside the map")]
        public float Scale = 0.01f;
        [Tooltip("Object name (the index is added at the ed of this name)")]
        public string SpawnedObjectName = "cube";
    
        [Header("Function: Create Minimap")]
        [Tooltip("Box Collider center")]
        public Vector3 BoxCenter = new Vector3(0.5f, -0.5f, 0.5f);
    
        // === PRIVATE ===
    
        // for one-shot script
        private bool done = false;
        // the root object to use for generating the minimap (the script beholding this component)
        private GameObject GoRoot = null;
        // default for NumOfObjects
        private int NumOfObjectsDefault = 5;
        // default for Scale
        private float ScaleDefault = 0.05f;
    
        // === UNITY CALLBACKS ===
    
        // Start is called before the first frame update
        void Start()
        {
            GoRoot = (ComponentRoot == null ? gameObject : ComponentRoot);
        }
    
        // Update is called once per frame
        void Update()
        {
            if (done) return;
    
            if (FunctionInstanciateRandom)
                WrapperFunctionInstanciateRandom();
            else if (FunctionCreateMinimap)
                WrapperFunctionCreateMinimap();
        }
    
        // === FEATURE INSTANCIATE RANDOM ===
    
        private void WrapperFunctionInstanciateRandom()
        {
            if (NummOfObjects < 0)
            {
                Debug.LogWarning("Number of Objects unser the minimap cannot be negative; using default");
                NummOfObjects = NumOfObjectsDefault;
            }
            if (Scale < 0.0f)
            {
                Debug.LogWarning("Scaling factor cannot be negative; using default");
                Scale = ScaleDefault;
            }
            GenerateRandomObjectsUnderRoot();
            done = true;
        }
    
        private void GenerateRandomObjectsUnderRoot() // better to use a coroutine?
        {
            for( int i=0; i<NummOfObjects; ++i )
            {
                GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
                go.name = SpawnedObjectName + i.ToString("000");
                go.transform.SetParent(GoRoot.transform);
                go.transform.localPosition += new Vector3(Random.value * MaxDistVector.x, -Random.value * MaxDistVector.y, Random.value * MaxDistVector.z);
                go.transform.localScale = Scale * Vector3.one;
            }
        }
    
        // === FEATURE SETUP MINIMAP ===
    
        private void WrapperFunctionCreateMinimap()
        {
            SetupMinimap();
            done = true;
        }
    
        private void SetupMinimap()
        {
            // box collider
            BoxCollider bc = GoRoot.AddComponent<BoxCollider>();
            bc.center = BoxCenter;
    
            // frame
            NearInteractionGrabbable nig = GoRoot.AddComponent<NearInteractionGrabbable>();
            BoundsControl frame = GoRoot.AddComponent<BoundsControl>();
            ObjectManipulator manip = GoRoot.AddComponent<ObjectManipulator>();
        }
    
    }
    ```
    

creazione di oggetti al di sotto della root:

![Untitled](./Untitled%2015.png)

creazione del frame per la minimappa:

![Untitled](./Untitled%2016.png)

—

### SCRIPT 003 — Glue

- script 003
    
    Lo script ha volutamente una struttura molto semplificata. Il reference è pubblico e può cambiare a runtime senza problemi. Se non c’è, non viene eseguito nulla. 
    
    ```
    using System.Collections;
    using System.Collections.Generic;
    using UnityEngine;
    
    public class Glue : MonoBehaviour
    {
        // === GUI ===
    
        [Tooltip("The reference object (it shall have always the same relative position of the other object)")]
        public GameObject Reference = null;
    
        public bool SetPosition = true;
        public bool SetOrientation = false;
        public bool SetScale = false;
    
        // === Unity Callbacks ===
    
        // Start is called before the first frame update
        void Start()
        {
            
        }
    
        // Update is called once per frame
        void Update()
        {
            if (Reference == null) return;
    
            if (SetPosition)
                gameObject.transform.position = Reference.transform.position;
            if (SetOrientation)
                gameObject.transform.rotation = Reference.transform.rotation;
            if (SetScale)
                gameObject.transform.localScale = Reference.transform.localScale;
        }
    }
    ```
    

Un esempio di come dovrebbe essere configurato:

![Untitled](./Untitled%2017.png)

—

### SCRIPT 004 — MinimapStructure

- script 004
    
    ```
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using UnityEngine;
    
    // it shall be applied on the object containing the minimap (just tracking, no geometrical changes are performed by this script)
    public class MinimapStructure : MonoBehaviour
    {
        public bool VisualizeOnInsert = true;
    
        private List<MinimapStructureEntry> TrackingList = new List<MinimapStructureEntry>();      // MinimapStructureEntry
        private List<GameObject> VisualizationList = new List<GameObject>(); // GameObject
    
        private float MinOrderCriterion = float.MaxValue;
        private float MaxOrderCriterion = float.MinValue;
    
        private class MinimapStructureEntry
        {
            public GameObject Object = null;
            public float OrderCriterion = float.NaN;
    
            public MinimapStructureEntry(GameObject go = null, float hg = float.NaN)
            {
                this.Object = go;
                this.OrderCriterion = hg;
            }
        }
    
        // Start is called before the first frame update
        void Start()
        {
            
        }
    
        // Update is called once per frame
        void Update()
        {
            
        }
    
        // ordered insert using a custom criterion
        public int TrackGameObject(GameObject newGo, float orderCriterion = float.NaN, Nullable<bool> visualize = null)
        {
            if (newGo == null) return -1;
    
            float hg = (orderCriterion == float.NaN ? newGo.transform.localPosition.y : orderCriterion);
            MinimapStructureEntry toInsert = new MinimapStructureEntry(newGo, orderCriterion);
    
            int at = -1;
    
            // insert ordered by hg increasing
            if (TrackingList.Count == 0)
            {
                TrackingList.Add( toInsert );
                at = 0;
                MinOrderCriterion = hg;
                MaxOrderCriterion = hg;
            }
            else
            {
                for(int i=0; i<TrackingList.Count; ++i)
                {
                    MinimapStructureEntry item = TrackingList[i] as MinimapStructureEntry;
                    bool found = false;
                    if(orderCriterion <= item.OrderCriterion)
                    {
                        TrackingList.Insert(i, toInsert);
                        found = true;
                        at = i;
                    }
                    if (found) 
                        break;
                    else if (i == TrackingList.Count - 1)
                    {
                        TrackingList.Add(toInsert);
                        at = i+1;
                    }
                    
                }
    
                if (orderCriterion < MinOrderCriterion)
                    MinOrderCriterion = orderCriterion;
                else if (orderCriterion > MaxOrderCriterion)
                    MaxOrderCriterion = orderCriterion;
            }
    
            // visualization
            ToggleVisualizationItem(newGo, opt: (bool)visualize || VisualizeOnInsert);
    
            return at;
        }
    
        public void UntrackGameObject(GameObject go, float orderCriterion = float.NaN)
        {
            if (go == null) return;
            if (!float.IsNaN(orderCriterion) && (orderCriterion < MinOrderCriterion || orderCriterion > MaxOrderCriterion))
                return;
    
            foreach (MinimapStructureEntry it in TrackingList)
                if(it.Object == go)
                {
                    TrackingList.Remove(it);
                    return;
                }
        }
    
        // visualize one particular game object
        public void ShowItem(GameObject go, bool quick = false)
        {
            if (!quick && VisualizationList.Contains(go)) return;
            
            VisualizationList.Add(go);
            go.SetActive(true);
        }
    
        // visualize one particular game object
        public void HideItem(GameObject go)
        {
            VisualizationList.Remove(go);
            go.SetActive(false);
        }
    
        public void ToggleVisualizationItem(GameObject go, bool opt = true)
        {
            if (opt)
                ShowItem(go);
            else
                HideItem(go);
        }
    
        // hide all or show all
        public void ToggleVisualizationAll(bool opt = true)
        {
            if (opt)
                foreach (MinimapStructureEntry it in TrackingList)
                    ShowItem(it.Object);
            else
                HideItemsInVisualizationList();
        }
    
        // hide elements in visualization list
        public void HideItemsInVisualizationList()
        {
            while (VisualizationList.Count > 0) HideItem(VisualizationList[0]);
        }
    
        // hide or show items in a given interval for the order criterion
        public void ShowItemsInRange(float MinHG, float deltaHG, bool hideAllBeforeStarting = false)
        {
            MinHG = Mathf.Max(new float[] { MinHG, MinOrderCriterion });
    
            int startIdx = Mathf.FloorToInt(
                (MinHG - MinOrderCriterion) / (MaxOrderCriterion - MinOrderCriterion) * TrackingList.Count
            );
            float startHG = ((MinimapStructureEntry)TrackingList[startIdx]).OrderCriterion;
    
            int dir = (startHG > MinHG ? -1 : +1);
            while (startHG != MinHG)
            {
                int newStartIdx = startIdx + dir;
                if (newStartIdx < 0) break; // can't improve further
                float newStartHG = ((MinimapStructureEntry)TrackingList[newStartIdx]).OrderCriterion;
    
                if ((dir > 0) && (startHG > MinHG)) break;
                else if ((dir < 0) && (startHG < MinHG)) break;
    
                startIdx = newStartIdx;
                startHG = newStartHG;
            }
    
            bool quick = hideAllBeforeStarting;
            if (hideAllBeforeStarting) HideItemsInVisualizationList();
    
            int i = startIdx;
            while((startHG < MinHG + deltaHG) && (i < TrackingList.Count))
            {
                ShowItem(((MinimapStructureEntry)TrackingList[i]).Object, quick);
                startHG = ((MinimapStructureEntry)TrackingList[i]).OrderCriterion;
                ++i;
            }
        }
    }
    ```
    

Lo script, in composizione con lo slider tool, permette di realizzare una visione della mappa lungo un determinato piano

—

### SCRIPT 005 — SliderTool

- script 005
    
    ```
    using System.Collections;
    
    using System.Collections.Generic;
    using UnityEngine;
    
    public class SliderTool : MonoBehaviour
    {
        // === GUI ===
    
        [Header("General Tool Settings")]
        [Tooltip("The object controlling the minimap")]
        public MinimapStructure MinimapDriver = null;
    
        // === PRIVATE ===
    
        // init done
        private bool init = false;
        // after tracked everything under the root of the minimap
        private bool tracked = false;
        // root of the minimap
        private GameObject minimapRoot = null;
        // reference to the slider
        private GameObject slider;
        // slider data
        float ystart = float.MaxValue;
        float delta = 0.0f;
    
        // === UNITY CALLBACKS ===
    
        // Start is called before the first frame update
        void Start()
        {
            if (MinimapDriver == null)
            {
                Debug.LogError("no Minimap Structure provided!");
                return;
            }
    
            minimapRoot = MinimapDriver.gameObject;
            slider = gameObject;
            init = true;
        }
    
        // Update is called once per frame
        void Update()
        {
            if (!init) return;
    
            if (!tracked)
            {
                foreach (Transform child in minimapRoot.transform) // https://discussions.unity.com/t/get-all-children-gameobjects/89443/3
                {
                    MinimapDriver.TrackGameObject(child.gameObject, child.gameObject.transform.localPosition.y, visualize: false);
                }
                    
                tracked = true;
            }
            
            float newystart = slider.transform.localPosition.y - slider.transform.localScale.y / 2.0f;
    
            if(newystart != ystart)
            {
                ystart = newystart;
                delta = slider.transform.localScale.y;
    
                MinimapDriver.ShowItemsInRange(ystart, delta, true);
            }
        }
    }
    ```
    

L’effetto è un po’ questo:

![Untitled](./Untitled%2018.png)

—

---

---

## Il codice finale completo

- ******release July 1, 2023 1:14 PM*

### Glue v1

- codice
    
    ```
    using System.Collections;
    using System.Collections.Generic;
    using UnityEngine;
    
    namespace SaR4Hololens2.Scenes.TestingFeatureMinimap.Scripts
    {
        public class Glue : MonoBehaviour
        {
            // === GUI ===
    
            [Tooltip("The reference object (it shall have always the same relative position of the other object)")]
            public GameObject Reference = null;
    
            public bool SetPosition = true;
            public bool SetOrientation = false;
            public bool SetScale = false;
    
            // === Unity Callbacks ===
    
            // Start is called before the first frame update
            void Start()
            {
    
            }
    
            // Update is called once per frame
            void Update()
            {
                if (Reference == null) return;
    
                if (SetPosition)
                    gameObject.transform.position = Reference.transform.position;
                if (SetOrientation)
                    gameObject.transform.rotation = Reference.transform.rotation;
                if (SetScale)
                    gameObject.transform.localScale = Reference.transform.localScale;
            }
        }
    
    }
    ```
    

Consente di aggiornare posizione, orientazione e scala di un oggetto rispetto ad un altro oggetto con una UPDATE a ciclo continuo. In particolare, lo script è stato pensato per legare due frame ****************la cui origine rimane sempre nello stesso punto indipendentemente dal frame****************, il che semplifica molto le cose. 

Opzioni:

![Untitled](./Untitled%2019.png)

—

### MinimapStructure (scritp di test)

<aside>
💡 Aggiornato July 8, 2023 in modo da avere un manager di risorse completo.

</aside>

- codice
    
    ```
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using UnityEngine;
    
    namespace SaR4Hololens2.Scenes.TestingFeatureMinimap.Scripts
    {
        // it shall be applied on the object containing the minimap (just tracking, no geometrical changes are performed by this script)
        public class MinimapStructure : MonoBehaviour
        {
            public bool VisualizeOnInsert = true;
    
            private List<MinimapStructureEntry> TrackingList = new List<MinimapStructureEntry>();      // MinimapStructureEntry
            private List<GameObject> VisualizationList = new List<GameObject>(); // GameObject
    
            private float MinOrderCriterion = float.MaxValue;
            private float MaxOrderCriterion = float.MinValue;
    
            private class MinimapStructureEntry
            {
                public GameObject Object = null;
                public float OrderCriterion = float.NaN;
    
                public MinimapStructureEntry(GameObject go = null, float hg = float.NaN)
                {
                    this.Object = go;
                    this.OrderCriterion = hg;
                }
            }
    
            void OnDisable()
            {
                foreach (MinimapStructureEntry item in TrackingList)
                    item.Object.SetActive(true);
            }
    
            void OnEnable()
            {
                foreach (MinimapStructureEntry item in TrackingList)
                    if (!VisualizationList.Contains(item.Object))
                        item.Object.SetActive(false);
                    else
                        item.Object.SetActive(true);
            }
    
            // ordered insert using a custom criterion
            public int TrackGameObject(GameObject newGo, float orderCriterion = float.NaN, Nullable<bool> visualize = null)
            {
                if (newGo == null) return -1;
                if (!this.isActiveAndEnabled) return -1;
    
                float hg = (orderCriterion == float.NaN ? newGo.transform.localPosition.y : orderCriterion);
                MinimapStructureEntry toInsert = new MinimapStructureEntry(newGo, orderCriterion);
    
                int at = -1;
    
                // insert ordered by hg increasing
                if (TrackingList.Count == 0)
                {
                    TrackingList.Add(toInsert);
                    at = 0;
                    MinOrderCriterion = hg;
                    MaxOrderCriterion = hg;
                }
                else
                {
                    for (int i = 0; i < TrackingList.Count; ++i)
                    {
                        MinimapStructureEntry item = TrackingList[i] as MinimapStructureEntry;
                        bool found = false;
                        if (orderCriterion <= item.OrderCriterion)
                        {
                            TrackingList.Insert(i, toInsert);
                            found = true;
                            at = i;
                        }
                        if (found)
                            break;
                        else if (i == TrackingList.Count - 1)
                        {
                            TrackingList.Add(toInsert);
                            at = i + 1;
                        }
    
                    }
    
                    if (orderCriterion < MinOrderCriterion)
                        MinOrderCriterion = orderCriterion;
                    else if (orderCriterion > MaxOrderCriterion)
                        MaxOrderCriterion = orderCriterion;
                }
    
                // visualization
                ToggleVisualizationItem(newGo, opt: (bool)visualize || VisualizeOnInsert);
    
                return at;
            }
    
            public void UntrackGameObject(GameObject go, float orderCriterion = float.NaN)
            {
                if (go == null) return;
                if (!this.isActiveAndEnabled) return;
    
                if (!float.IsNaN(orderCriterion) && (orderCriterion < MinOrderCriterion || orderCriterion > MaxOrderCriterion))
                    return;
    
                foreach (MinimapStructureEntry it in TrackingList)
                    if (it.Object == go)
                    {
                        TrackingList.Remove(it);
                        return;
                    }
            }
    
            // visualize one particular game object
            public void ShowItem(GameObject go, bool quick = false)
            {
                if (!this.isActiveAndEnabled) return;
                if (!quick && VisualizationList.Contains(go)) return;
    
                VisualizationList.Add(go);
                go.SetActive(true);
            }
    
            // visualize one particular game object
            public void HideItem(GameObject go)
            {
                if (!this.isActiveAndEnabled) return;
                VisualizationList.Remove(go);
                go.SetActive(false);
            }
    
            public void ToggleVisualizationItem(GameObject go, bool opt = true)
            {
                if (!this.isActiveAndEnabled) return;
                if (opt)
                    ShowItem(go);
                else
                    HideItem(go);
            }
    
            // hide all or show all
            public void ToggleVisualizationAll(bool opt = true)
            {
                if (!this.isActiveAndEnabled) return;
                if (opt)
                    foreach (MinimapStructureEntry it in TrackingList)
                        ShowItem(it.Object);
                else
                    HideItemsInVisualizationList();
            }
    
            // hide elements in visualization list
            public void HideItemsInVisualizationList()
            {
                if (!this.isActiveAndEnabled) return;
                while (VisualizationList.Count > 0) HideItem(VisualizationList[0]);
            }
    
            // hide or show items in a given interval for the order criterion
            public void ShowItemsInRange(float MinHG, float deltaHG, bool hideAllBeforeStarting = false)
            {
                if (!this.isActiveAndEnabled) return;
                MinHG = Mathf.Max(new float[] { MinHG, MinOrderCriterion });
    
                int startIdx = Mathf.FloorToInt(
                    (MinHG - MinOrderCriterion) / (MaxOrderCriterion - MinOrderCriterion) * TrackingList.Count
                );
                float startHG = ((MinimapStructureEntry)TrackingList[startIdx]).OrderCriterion;
    
                int dir = (startHG > MinHG ? -1 : +1);
                while (startHG != MinHG)
                {
                    int newStartIdx = startIdx + dir;
                    if (newStartIdx < 0) break; // can't improve further
                    float newStartHG = ((MinimapStructureEntry)TrackingList[newStartIdx]).OrderCriterion;
    
                    if ((dir > 0) && (startHG > MinHG)) break;
                    else if ((dir < 0) && (startHG < MinHG)) break;
    
                    startIdx = newStartIdx;
                    startHG = newStartHG;
                }
    
                bool quick = hideAllBeforeStarting;
                if (hideAllBeforeStarting) HideItemsInVisualizationList();
    
                int i = startIdx;
                while ((startHG < MinHG + deltaHG) && (i < TrackingList.Count))
                {
                    ShowItem(((MinimapStructureEntry)TrackingList[i]).Object, quick);
                    startHG = ((MinimapStructureEntry)TrackingList[i]).OrderCriterion;
                    ++i;
                }
            }
        }
    
    }
    ```
    

Il component viene utilizzato per gestire la visualizzazione di un insieme di GameObjects al di sotto di una Root comune. Gli oggetti possono essere strutturati internamente come si preferisce, tanto l’attivaizone e disattivazione riguarda solo il livello più alto per ognuno di questi oggetti. 

Il component viene anche utilizzato come Handle per contraddistinguere un gameObject che implementa una minimappa, ed è pensato per essere o piazzato sulla root del component principale della minimappa (in questo caso l’oggetto viene usato per contraddistinguere il tipo del gameObject) oppure da altra parte (lo script prende un riferimento esterno)

Opzioni disponibili:

![Untitled](./Untitled%2020.png)

—

### SliderTool (versione di test)

- codice
    
    ```
    using System.Collections;
    
    using System.Collections.Generic;
    using UnityEngine;
    
    namespace SaR4Hololens2.Scenes.TestingFeatureMinimap.Scripts
    {
        public class SliderTool : MonoBehaviour
        {
            // === GUI ===
    
            [Header("General Tool Settings")]
            [Tooltip("The object controlling the minimap")]
            public MinimapStructure MinimapDriver = null;
            [Tooltip("Prefix of the name of the element under the minimap root to track (empty if not userd)")]
            public string ItemsPrefix = "";
    
            // === PRIVATE ===
    
            // init done
            private bool init = false;
            // after tracked everything under the root of the minimap
            private bool tracked = false;
            // root of the minimap
            private GameObject minimapRoot = null;
            // reference to the slider
            private GameObject slider;
            // slider data
            float ystart = float.MaxValue;
            float delta = 0.0f;
    
            // === UNITY CALLBACKS ===
    
            // Start is called before the first frame update
            void Start()
            {
                if (MinimapDriver == null)
                {
                    Debug.LogError("(Slider) ERROR: no Minimap Structure provided!");
                    return;
                }
                ItemsPrefix = ItemsPrefix.Trim();
    
                minimapRoot = MinimapDriver.gameObject;
                slider = gameObject;
                init = true;
            }
    
            // Update is called once per frame
            void Update()
            {
                if (!this.isActiveAndEnabled) return;
                if (!init) return;
    
                if (!tracked)
                {
                    foreach (Transform child in minimapRoot.transform) // https://discussions.unity.com/t/get-all-children-gameobjects/89443/3
                    {
                        if (ItemsPrefix == "" || child.gameObject.name.StartsWith(ItemsPrefix))
                            MinimapDriver.TrackGameObject(child.gameObject, child.gameObject.transform.localPosition.y, visualize: false);
                    }
    
                    tracked = true;
                }
    
                delta = slider.transform.localScale.y;
                float newystart = slider.transform.localPosition.y - delta / 2.0f;
    
                if (newystart != ystart && MinimapDriver.isActiveAndEnabled)
                {
                    ystart = newystart;
                    MinimapDriver.ShowItemsInRange(ystart, delta, true);
                }
            }
    
            private void OnEnable()
            {
    
            }
    
            private void OnDisable()
            {
    
            }
        }
    
    }
    ```
    

Handle del tool per la proiezione sul piano di una minimappa. Lo script ********non crea******** il tool, ma bensì si limita a gestirlo e a settare il driver per poterlo utilizzare. Script attivabile e disattivabile. 

—

### MinimapTestingScript (script di test)

- codice
    
    ```
    using System.Collections;
    using System.Collections.Generic;
    using UnityEngine;
    using Microsoft.MixedReality.Toolkit.Input;
    using Microsoft.MixedReality.Toolkit.UI;
    using Microsoft.MixedReality.Toolkit.Utilities;
    using Microsoft.MixedReality.Toolkit.UI.BoundsControl;
    
    namespace SaR4Hololens2.Scenes.TestingFeatureMinimap.Scripts
    {
        public class MinimapTestingScript : MonoBehaviour
        {
            // === GUI ===
    
            [Header("Component Functions")]
            [Tooltip("Randomly populate the root object")]
            public bool FunctionInstanciateRandom = false;
            [Tooltip("Create the minimap")]
            public bool FunctionCreateFrameMinimap = false;
            [Tooltip("Instanciate the plane visual tool for the minimap")]
            public bool FunctionCreatePlaneTool = false;
    
            [Header("General settings")]
            [Tooltip("The root of the minimap (by defaul, it is the owner of this component)")]
            public GameObject ComponentRoot = null;
    
            [Header("Function: random objects generation")]
            [Tooltip("Minimap Structure Object Reference (optional)")]
            public MinimapStructure MapStructure = null;
            [Tooltip("Number of objects to spawn")]
            public int NummOfObjects = 5;
            [Tooltip("max distance (local) from coordinates")]
            public Vector3 MaxDistVector = Vector3.zero;
            [Tooltip("Local scaling factor for each object inside the map")]
            public float Scale = 0.01f;
            [Tooltip("Object name (the index is added at the ed of this name)")]
            public string SpawnedObjectName = "cube";
    
            [Header("Function: Create Minimap")]
            [Tooltip("Box Collider center")]
            public Vector3 BoxCenter = new Vector3(0.5f, -0.5f, 0.5f);
    
            [Header("Function: create plane tool")]
            [Tooltip("Root G.O. for the minimap asset")]
            public GameObject AssetRoot = null;
            [Tooltip("Material for the slider tool")]
            public Material ToolMaterial = null;
            [Tooltip("Prefix of the items to track under the minimap (empty if unused)")]
            public string ItemPrefix = "";
    
            // === PRIVATE ===
    
            // for one-shot script
            private bool done = false;
            // the root object to use for generating the minimap (the script beholding this component)
            private GameObject GoRoot = null;
            // default for NumOfObjects
            private int NumOfObjectsDefault = 5;
            // default for Scale
            private float ScaleDefault = 0.05f;
            // root of the minimap asset
            private GameObject GoAssetRoot = null;
            // minimap gameobject
            private GameObject goMinimap = null;
            // minimap structure component in the minimap
            private MinimapStructure coMinimapStruct = null;
            // tools root for the minimap
            private GameObject goTools = null;
    
            // === UNITY CALLBACKS ===
    
            // Start is called before the first frame update
            void Start()
            {
                GoRoot = (ComponentRoot == null ? gameObject : ComponentRoot);
                GoAssetRoot = (FunctionCreatePlaneTool ? (AssetRoot == null ? GoRoot.transform.parent.gameObject : AssetRoot) : null);
    
                if (FunctionInstanciateRandom)
                    WrapperFunctionInstanciateRandom();
            }
    
            // Update is called once per frame
            void Update()
            {
                if (done) return;
    
                if (FunctionCreateFrameMinimap)
                    WrapperFunctionCreateFrameMinimap();
                else if (FunctionCreatePlaneTool)
                    WrapperFunctionCreatePlaneTool();
            }
    
            // === FEATURE INSTANCIATE RANDOM ===
    
            private void WrapperFunctionInstanciateRandom()
            {
                if (NummOfObjects < 0)
                {
                    Debug.LogWarning("Number of Objects unser the minimap cannot be negative; using default");
                    NummOfObjects = NumOfObjectsDefault;
                }
                if (Scale < 0.0f)
                {
                    Debug.LogWarning("Scaling factor cannot be negative; using default");
                    Scale = ScaleDefault;
                }
                GenerateRandomObjectsUnderRoot();
                done = true;
            }
    
            private void GenerateRandomObjectsUnderRoot() // better to use a coroutine?
            {
                for (int i = 0; i < NummOfObjects; ++i)
                {
                    GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    go.name = SpawnedObjectName + i.ToString("000");
    
                    // just for testing minimap selection feature
                    if (Random.value > 0.5f)
                    {
                        GameObject go2 = new GameObject();
                        go2.transform.SetParent(go.transform);
                        go2.transform.position = go.transform.position;
                        go2.transform.rotation = go.transform.rotation;
                        go2.transform.localScale = go.transform.localScale;
                    }
    
                    go.transform.SetParent(GoRoot.transform);
                    go.transform.localPosition += new Vector3(Random.value * MaxDistVector.x, -Random.value * MaxDistVector.y, Random.value * MaxDistVector.z);
                    go.transform.localScale = Scale * Vector3.one;
    
                    if (MapStructure != null)
                        MapStructure.TrackGameObject(go, orderCriterion: go.transform.localPosition.y);
                }
            }
    
            // === FEATURE SETUP MINIMAP ===
    
            private void WrapperFunctionCreateFrameMinimap()
            {
                SetupMinimap();
                done = true;
            }
    
            private void SetupMinimap()
            {
                // box collider
                BoxCollider bc = GoRoot.AddComponent<BoxCollider>();
                bc.center = BoxCenter;
    
                // frame
                NearInteractionGrabbable nig = GoRoot.AddComponent<NearInteractionGrabbable>();
                BoundsControl frame = GoRoot.AddComponent<BoundsControl>();
                frame.BoundsOverride = bc;
                ObjectManipulator manip = GoRoot.AddComponent<ObjectManipulator>();
            }
    
            // === FEATURE PLANE TOOL ===
    
            private void WrapperFunctionCreatePlaneTool()
            {
                if (CheckMinimapAsset())
                    CreatePlaneTool();
                done = true;
            }
    
            private bool CheckMinimapAsset()
            {
                // find minimap
                Transform trMinimap = GoAssetRoot.transform.Find("Minimap");
                if (trMinimap == null)
                {
                    Debug.LogWarning("ERROR: cannot create tool (Minimap not found)");
                    return false;
                }
                goMinimap = trMinimap.gameObject;
    
                // check minimap type
                coMinimapStruct = goMinimap.GetComponent<MinimapStructure>();
                if (coMinimapStruct == null)
                {
                    Debug.LogWarning("ERROR: cannot create tool (Minimap is not a minimap)");
                    return false;
                }
    
                // check or create _tools gambe object
                Transform trTools = GoAssetRoot.transform.Find("_tools");
                if (trTools == null)
                {
                    Debug.LogWarning("ERROR: cannot create tool (object _tools not found)");
                    return false;
                }
                goTools = trTools.gameObject;
    
                // check material reference
                if (ToolMaterial == null)
                {
                    Debug.LogWarning("ERROR: tool material for the slider tool has not been set!");
                    return false;
                }
    
                return true;
            }
    
            private void CreatePlaneTool()
            {
                GameObject goPlaneTool = GameObject.CreatePrimitive(PrimitiveType.Cube);
                goPlaneTool.name = "MinimapSliderTool";
                goPlaneTool.transform.SetParent(goTools.transform);
                goPlaneTool.transform.localPosition = new Vector3(0.5f, 0.0f, 0.5f);
                goPlaneTool.transform.localScale = new Vector3(1.2f, 0.1f, 1.2f);
                goPlaneTool.GetComponent<Renderer>().material = ToolMaterial;
    
                goPlaneTool.AddComponent<NearInteractionGrabbable>();
                goPlaneTool.AddComponent<ObjectManipulator>();
    
                ConstraintManager coConstraints = goPlaneTool.GetComponent<ConstraintManager>();
    
                MoveAxisConstraint coConstraintX = goPlaneTool.AddComponent<MoveAxisConstraint>();
                coConstraintX.ConstraintOnMovement = AxisFlags.XAxis;
                coConstraintX.UseLocalSpaceForConstraint = true;
    
                MoveAxisConstraint coConstraintZ = goPlaneTool.AddComponent<MoveAxisConstraint>();
                coConstraintZ.ConstraintOnMovement = AxisFlags.ZAxis;
                coConstraintZ.UseLocalSpaceForConstraint = true;
    
                SliderTool coSider = goPlaneTool.AddComponent<SliderTool>();
                coSider.MinimapDriver = goMinimap.GetComponent<MinimapStructure>();
                coSider.ItemsPrefix = this.ItemPrefix;
            }
    
        }
    
    }
    ```
    

Creazione di una minimappa stub (una nuvola di cubi), creazione dinamica dello slider tool, e tutta una serie di altri strumenti molto importanti da integrare in futuro in un handle. Nota bene che il codice lavora su una struttura di questo tipo:

![Untitled](./Untitled%2021.png)

—

---

---