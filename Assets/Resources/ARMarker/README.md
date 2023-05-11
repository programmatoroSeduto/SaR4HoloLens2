# AR Markers

*A little spatial marker mainly used for testing purposes.*

---

## AR Marker Base

- **Asset Name** : *ARMarkerBase* (prefab)

### Scrpting -- controlling ARMarkerBase

The object **ARMarkerBase** is controlled by a script called *ARMakerBaseHandle*, which is not a standard handle: it has only one *EVENT_SET_TextContent(string message)* used to set the text inside the big panel above the little white capsule. 
- **COMPONENT** : `ARMarkerBase` is the "root" controller of the component
- **EVENT** : `EVENT_SET_TextContent(string message)` sets a message on the marker

### Scripting -- spawning ARMarkerBase

To buil the object, a **ARMarkerBaseBuilder** script is available. Even this object is written with a not-so-recent standard. 

- **COMPONENT** : `ARMarkerBaseBuilder` to build the marker. Please notice that the object is made to be instanced as *component*. 
- **EVENT** : `void EVENT_Build()` to be used only with `Invoke()` from Unity GUI .
- **METHOD** : `ARMarkerBase Build()` is the main function used to build the object, after set the fields of the class

Here's a example of procedure for spawning the object:

```cs

// somewhere in the code
private ARMarkerBaseBuilder armb = gameObject.AddComponent<ARMarkerBaseBuilder>();

// STEP 1 : set parameters
InitMarkerName = ... "string: name of the marker" ... ;
InitText = ... "a init text if you want" ... ;
MarkerPosition = ... a Vector3 position ... ;
YawOrientation = ... a angle in degrees... ;
SpawnUnderObject = ... another gameObject reference ... ;

// STEP 2 : build the object
private ARMarkerBaseHandle armh = armb.Build();

```

You can spawn (replicate) the object *every time you want* with the same model: this is the main principle behind this spawning procedure. 

---