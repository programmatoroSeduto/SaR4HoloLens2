
# UNITY PACKAGE -- S&R Exploration Features

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

Direct:

- **StorageManager**
- - **PositionDatabase**

Indirect:

- **ARMarker** : ...
- **CustomRenderers** : ...

---

## Namespaces and Classes

```
Packages.SarExplorationFeatures.Components
```

- SarExplorationControlUnit

    Options:

    <image src="./_docs/images/SarExplorationControlUnitComponent.png" alt="SarExplorationControlUnitComponent"/>

    It provides a unified interface for all the exploration features implemented for this project. 

    - The class can set up the entire stack of components at the Start(), except for the `PositionDatabase` which shall be passed by reference to the class. You can initialize the class when you want with the method `SetupClass()`.

        The structure created by the script is similar to the following one:

        <image src="./_docs/images/SARComponentsStack.png" alt="SARComponentsStack"/>

- FeatureSpatialOnUpdate

    *This component is not usable in the Editor Window*. It implements a feature drawing point as the user walks around. It enables the user to see their steps. 

    It is directly handled by the `SarExplorationControlUnit` component. 

```
Packages.SarExplorationFeatures.Utils
```

- ...

```
Packages.SarExplorationFeatures.ModuleTesting
```

- ...

---

```{toctree}
---
cation: contents:
maxdepth: 2
---
./README.md
```

---