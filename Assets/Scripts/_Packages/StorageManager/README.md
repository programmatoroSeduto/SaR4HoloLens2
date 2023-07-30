
# UNITY PACKAGE -- StorageManager

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

- **ARMarker** : ARMarkerBuilder, ARMarkerHandle
- **PositionDatabase** : PositionDatabase, PositionDatabaseWaypointHandle, PositionDatabaseWaypoint, PositionDatabasePath
- **CustomRenderers** : FlexibleLineRenderer

---

## Namespaces and Classes

```
Packages.StorageManager.Components
```

- MinimapStructure

    Options:

    <image src="./_docs/images/MinimapStructureComponent.png" alt="MinimapStructureComponent"/>

    The script can be used to implement a minimap (in fact, in the past it is born in this way) but it can do more. In general, this component keeps track of a set of already instanced GameObjects, and provides some ways to manage the visualization of the object (based on Unity API for enabling the objects). It has been implemented to obtain as much performance as possibile. 

    **NOTE WELL** : it does not instanciate objects. It just keeps tracks of objects inside the scene, but the spawning shall be done by another component. 

    Here's a  brief summary of the features inside MinimapStructure:

    - *ordered/unordered insert* : check the section below for further information
    - *tagging system* : each game object can be tracked under a tag, which is a string known to the script calling the minimap structure. There's no default for tagging: if the tag is missing, the tracking action fails. Some packages such as PositionDatabase offer a standard way to obtain unique identifiers (see the *Key* feature). **NOTE WELL**: after the first phase, you can access the GameObject by its tag; direct access is not useful: in the typical case you want to retrieve the reference without worrying about where it is. The access by tag is always efficient. 
    - *visualization* : the class offers function to enable or disable tracked objects. The class has a visualization cache inside, to provide better performances. See also `ShowItemsInRange()` 

    There are currently some known limitation to take ino account in using the class:

    - be careful to untrack a gameObject before destroying it: if the object is destroyed *but* the reference is still in the Minimap Structure, there's the concrete risk of using a *dangling pointer* which can crash your application. 

- PathDrawer

    Options:

    <image src="./_docs/images/PathDrawerComponent.png" alt="PathDrawerComponent"/>

    Many features need to "draw" something in Augmented Reality space: for instance, a navigation system needs to draw a sequence of linked markers in the space to make a path the user can follow; a feature exploring the neighborhood of one position, showing all the waypoints the user can follow starting from their current position, needs to draw a graph inside the space. These are the kind of functionality the PathDrawer class supports. 

    Here's a brief overview of the features provided by the component:

    - *AR Markers Allocation and link* : the component allocates all the markers and line renderes under une given game object. If not specificed, the GO is the one owning the component. 
    - *Tagging system* : the component, built on top of the stack on MinimapReference, simplifies the management of the tags. It is still possible to assign the tags manually, but the class is also capable to assign tags automatically using keys. 

```
Packages.StorageManager.Utils
```

*The package doesn't include Utils classes.*

```
Packages.StorageManager.ModuleTesting
```

*The package doesn't include ModuleTesting classes.*

---

## MinimapStructure - Ordered/Unordered mode

The component can work in two ways:

- **ORDERED MODE** : the structure keeps track of the objects in a ordered list; inserts are ordered using a order criterion. Many methods are more effcient since the structure can check for instance the existence of a given value depending on the value of the order criterion. Moreover, the function `ShowItemsInRange()` can be used only in this mode.

- **UNORDERED MODE** : no need to use the order criterion. *It is automatcally enabled when a unordered insert is performed upon the component*. Or it can be set manually using `SetOrderedStructure(false)`. It is good when the situation requires a great frequency of inserts, and you just need a storage manager with no need to keep ordered the structure. 

Here are some limitations you should take into account to understand how the component works:

- the order criterion is not updated dynamically: when the order criterion changes, the application have to rewrite each order criterion, and then re-sort the list. This is fundamental when you want to use the method `SetOrderedStructure(true)` to switch between the modalities
- currently the class implements a security system which requires that *there must be no tracked object by the structure* while the structure is going to be ordered. To enable ordering, the structure must be empty. 
- to have more order criterion at the same time without reloading the entire structure, the best thing at the moment is to have *two MinimapStructure components*; another component is needed to manage the conflicts that could happen when using two independent structures.
- for historical reasons, the default order criterion is the local Y axis when no order criterion is provided. 
- **Please don't mix manual and automatic order criterion**! Be sure that the order criterion either is always provided by a external class, or it is generated automatically.  

---

## How to setup the package

This setup provides a subsystem to stack with some visualization logic. 

1. Create a GameObject for general services
2. Create a GameObject collecting markers from the PathDrawer

    <image src="./_docs/images/SETUP_step2.png" alt="SETUP_step2"/>

3. COMPONENT : PositionDatabase, as general service

    <image src="./_docs/images/SETUP_step3.png" alt="SETUP_step3"/>

4. COMPONENT : MinimapStructure, as general service
5. COMPONENT : PathDrawer, on the root of the GameObject for collecting the markers

    <image src="./_docs/images/SETUP_step5.png" alt="SETUP_step5"/>

You can now use PathDrawer to do everything you want from your visualization logic on top of the stack. 

---

```{toctree}
---
cation: contents:
maxdepth: 2
---
./README.md
```

---