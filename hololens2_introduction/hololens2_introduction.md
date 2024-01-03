# Hololens2 Introduction by Links

*HoloLens2 technology is not easy to understand, since there are many resources to take into account in order to have a clear idea of how to do. This document tries to provide a vision below the most useful features and resources for HoloLens2 implementation*.


## Main Resources Summary

- Mixed Reality Documentation (more about HoloLens2 experience in general)
  - [Mixed Reality Documentation](https://learn.microsoft.com/en-us/windows/mixed-reality/)
  - [What is mixed reality?](https://learn.microsoft.com/en-us/windows/mixed-reality/discover/mixed-reality)
- Mixed Reality Toolkit (the main low-level development framework)
  - [MRTK2 for Unity](https://learn.microsoft.com/en-us/windows/mixed-reality/mrtk-unity/mrtk2/?view=mrtkunity-2022-05)
  - [MRTK2 API reference](https://learn.microsoft.com/it-it/dotnet/api/microsoft.mixedreality.toolkit?view=mixed-reality-toolkit-unity-2020-dotnet-2.8.0)
- Unity API reference
  - [Unity API reference](https://docs.unity3d.com/ScriptReference/)
  - [Unity API Classes](https://docs.unity3d.com/ScriptReference/AccelerationEvent.html)
- C Sharp Documentation
  - [C\# Reference](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/)
  - [C\# Programming Guide](https://learn.microsoft.com/en-us/dotnet/csharp/programming-guide/)
- Universal Windows Platforms (UWP)
  - [UWP](https://learn.microsoft.com/en-us/windows/uwp/)
  - [Windows UWP Reference](https://learn.microsoft.com/en-us/uwp/api/)
- HoloLens2 User Manual
  - [MS Docs HoloLens2](https://learn.microsoft.com/en-us/hololens/)
  - [Basic edition](https://learn.microsoft.com/en-us/hololens/hololens2-options-device-only)
  - [Development Edition](https://learn.microsoft.com/en-us/hololens/hololens2-options-dev-edition)
  - [Industrial edition](https://learn.microsoft.com/en-us/hololens/hololens2-options-industrial-edition)
  - [HL2 Hardware](https://learn.microsoft.com/en-us/hololens/hololens2-hardware)


## I never used HoloLens2 before!

- This page is a short user manual of the device
  - [look at this](https://learn.microsoft.com/en-us/hololens/hololens2-basic-usage)
- Other useful guides
  - [Start menu](https://learn.microsoft.com/en-us/hololens/holographic-home)
  - [Manage Apps](https://learn.microsoft.com/en-us/hololens/holographic-store-apps)
  - [Photos, Videos, Screenshots and more](https://learn.microsoft.com/en-us/hololens/holographic-photos-and-videos)

Here are some incredibly useful voice commands:

- "start recording"
- "stop recording"
- "take a picture"/"take a photo"
- "go to start"
- "select" for eye interaction (*see-it-say-it*)

About the main features of HoloLens2:

- [How a HoloLens2 application is made](https://learn.microsoft.com/en-us/windows/mixed-reality/design/app-model)
  - [About coordinates systems in HoloLens2](https://learn.microsoft.com/en-us/windows/mixed-reality/design/coordinate-systems)
- [Spatial Mapping](https://learn.microsoft.com/en-us/windows/mixed-reality/design/spatial-mapping)
  - [Scene Understanding](https://learn.microsoft.com/en-us/windows/mixed-reality/design/scene-understanding) is the next step
  - Along with [spatial anchors](https://learn.microsoft.com/en-us/windows/mixed-reality/design/spatial-anchors)

Importat tool for working with HoloLens2 device: 

- [Windows Device Portal](https://learn.microsoft.com/en-us/windows/mixed-reality/develop/advanced-concepts/using-the-windows-device-portal)

Other interesting posts:

- [About Shared Experience](https://learn.microsoft.com/en-us/windows/mixed-reality/design/shared-experiences-in-mixed-reality)


## What is MRTK2?

- MRTK2 : *Mixed Reality ToolKit v2*
  - [Unity Development for HoloLens2](https://learn.microsoft.com/en-us/windows/mixed-reality/develop/unity/unity-development-overview?tabs=arr%2CD365%2Chl2)
  - it contains all the *basic* tools for HoloLens2 app implementation, to speed up just the most boring parts of the development such as to interact with some of the low-level runtime aspects. 
  - *So many things are missing in this framework!* It is just a basic low-level framework: you have to build your app on top of the stack. 
  - MRTK2 will be out of support soon; here is [MRTK3](https://learn.microsoft.com/en-us/windows/mixed-reality/mrtk-unity/mrtk3-overview/) currently in preview

MRTK2 installation:

- This tool is available for Unity and for Unreal Engine. I developed using Unity version (Unity is free under Unige Students licence). 
  - [official documentation with Unity](https://learn.microsoft.com/en-us/windows/mixed-reality/mrtk-unity/mrtk2/?view=mrtkunity-2022-05)
  - [VERY IMPORTANT for using GIT in MRTK2 projects](https://learn.microsoft.com/en-us/windows/mixed-reality/mrtk-unity/mrtk2/performance/large-projects?view=mrtkunity-2022-05)
- MRTK2 is not a standalone app, but something you build against your project, meaning that different project require different MRTK2 installations. To import the package, there's a portable wizard: the **Mixed Reality Feature Tool**
  - [GitHub link](https://github.com/microsoft/MixedRealityToolkit-Unity/releases)
  - [Mixed Reality Feature Tool](https://www.microsoft.com/en-us/download/details.aspx?id=102778)
- Are you in a hurry? Well: 
  - [this repository](https://github.com/programmatoroSeduto/HoloLens2ProjectTemplate) contains a template where to start from, with MRTK2 already installed and ready to use. 
  - [the branch is this](https://github.com/programmatoroSeduto/HoloLens2ProjectTemplate/tree/template_unity_basic)

How to use that?

- [How to install MR Features inside the project](https://learn.microsoft.com/en-us/training/modules/learn-mrtk-tutorials/1-5-exercise-configure-resources#tabpanel_1_openxr)
- Very important the idea of *profile*: MRTK2 takes track of the settings of the framework as a *profile* that is a file containing all the configuration details. Here is a first explainaion:
  - [MRTK2 Profiles Guide](https://learn.microsoft.com/en-us/windows/mixed-reality/mrtk-unity/mrtk2/configuration/mixed-reality-configuration-guide?view=mrtkunity-2022-05)

To deploy a application to HoloLens2:

- [deployment guide in Mixed Reality documentation](https://learn.microsoft.com/en-us/windows/mixed-reality/mrtk-unity/mrtk2/configuration/mixed-reality-configuration-guide?view=mrtkunity-2022-05)
- [deployment guide in MRTK2 documentation](https://learn.microsoft.com/en-us/windows/mixed-reality/mrtk-unity/mrtk2/supported-devices/wmr-mrtk?view=mrtkunity-2022-05)


Other available tutorials for training:

- [Objects Manipulation Introduction](https://learn.microsoft.com/en-us/training/modules/learn-mrtk-tutorials/1-7-exercise-hand-interaction-with-objectmanipulator)


## Voice Commands

Voice Commands are not so efficient in HoloLens2, but in so many cases they are very useful, especially in simulation since they can be bounded to keyboard events!

- [How to register keywords inside MRTK2](https://learn.microsoft.com/en-us/windows/mixed-reality/mrtk-unity/mrtk2/features/input/speech?view=mrtkunity-2022-05)
- The MRTK2 component [Speech Input Handler](https://learn.microsoft.com/en-us/dotnet/api/microsoft.mixedreality.toolkit.input.speechinputhandler?preserve-view=true&view=mixed-reality-toolkit-unity-2020-dotnet-2.8.0) allows to bind a voice command to a particular Unity event or Cs piece of code. 

Using speech commands is simple in general. Please keep in mind these steps:

1. create a profile for the voice commands if needed
2. declare the keywords inside the MRTK2 menu, Input section
3. add then a GameObject with a component called `SpeechInputHandle` (component already implemented in MRTK2)
4. create one or more Cs classes with public methods implementing the voice commands
5. assign those methods to the `SpeechInputHandle` class


## HoloLens2 inputs and UI items

From official Features documentation:

- [hands tracking](https://learn.microsoft.com/en-us/windows/mixed-reality/mrtk-unity/mrtk2/features/input/hand-tracking?view=mrtkunity-2022-05)
- **Near Interactions** are functionalities added to the GameObjects reated to the possibility of *touching virtual objects*.
  - [Touch and Grab](https://learn.microsoft.com/en-us/windows/mixed-reality/mrtk-unity/mrtk2/features/input/how-to-add-near-interactivity?view=mrtkunity-2022-05)

Talking about Unity Scripting, here are the must-have classes to deal with inputs through MRTK2:

- [Toolkit.Input namespace](https://learn.microsoft.com/it-it/dotnet/api/microsoft.mixedreality.toolkit.input?view=mixed-reality-toolkit-unity-2020-dotnet-2.8.0)

Hands Interactions:

- [Bounds Control](https://learn.microsoft.com/en-us/windows/mixed-reality/mrtk-unity/mrtk2/features/ux-building-blocks/bounds-control?view=mrtkunity-2022-05)
  - **VERY IMPORTANT:** Bounding Box is deprecated
  - [Manipulation Handler](https://learn.microsoft.com/en-us/windows/mixed-reality/mrtk-unity/mrtk2/features/ux-building-blocks/manipulation-handler?view=mrtkunity-2022-05) precisely built for direct hands manipulation of virtual objects.
  - See also [Object Mnaipulator](https://learn.microsoft.com/en-us/windows/mixed-reality/mrtk-unity/mrtk2/features/ux-building-blocks/object-manipulator?view=mrtkunity-2022-05)
  - It can be combined perfectly with [Constrains Manager](https://learn.microsoft.com/en-us/windows/mixed-reality/mrtk-unity/mrtk2/features/ux-building-blocks/constraint-manager?view=mrtkunity-2022-05) ... what is this? Well: I want that the object is able to move along X and Y axes only; *the consraint is to deny the motion along Y*, and this is a constraint. There are also other constraints. 

Classical Interactions:

- [Buttons](https://learn.microsoft.com/en-us/windows/mixed-reality/mrtk-unity/mrtk2/features/ux-building-blocks/button?view=mrtkunity-2022-05), incredibly powerful as well as a little "trivial" in terms of user experience
- [Hands Menu](https://learn.microsoft.com/en-us/windows/mixed-reality/mrtk-unity/mrtk2/features/ux-building-blocks/hand-menu?view=mrtkunity-2022-05) very cozy
  - see also [near menus](https://learn.microsoft.com/en-us/windows/mixed-reality/mrtk-unity/mrtk2/features/ux-building-blocks/near-menu?view=mrtkunity-2022-05)
- [Slates (Windows, in simple terms)](https://learn.microsoft.com/en-us/windows/mixed-reality/mrtk-unity/mrtk2/features/ux-building-blocks/slate?view=mrtkunity-2022-05) are useful for building LOG windows

Other useful tools for handling UI:

- [object Collections](https://learn.microsoft.com/en-us/windows/mixed-reality/mrtk-unity/mrtk2/features/ux-building-blocks/object-collection?view=mrtkunity-2022-05) **VERY USEFUL**
- [sliders](https://learn.microsoft.com/en-us/windows/mixed-reality/mrtk-unity/mrtk2/features/ux-building-blocks/sliders?view=mrtkunity-2022-05)
- *Anoher very important UI element*, [Tooltip](https://learn.microsoft.com/en-us/windows/mixed-reality/mrtk-unity/mrtk2/features/ux-building-blocks/tooltip?view=mrtkunity-2022-05) allows to put a "floating label" on a virtual object

Solvers! **FUNDAMENTAL.**

- A *Solver* is a script able to move one object in the space depending on the user's activity; they are incredibly useful
  - [Solvers in MRTK2](https://learn.microsoft.com/en-us/windows/mixed-reality/mrtk-unity/mrtk2/features/ux-building-blocks/solvers/solver?view=mrtkunity-2022-05)
- [Official Introduction in Mixed Reality](https://learn.microsoft.com/en-us/windows/mixed-reality/design/app-patterns-landingpage)


## Unity Fundamentals

General Purpose:

- [Unity Events](https://docs.unity3d.com/ScriptReference/Events.UnityEvent.html)
  - There are several limitations in Unity Events! They are very good when the method to call is just a signal with no parameters. Please give a look at the thesis report for more details about the limitations of his tool.
- [Unity Scenes Management](https://docs.unity3d.com/ScriptReference/SceneManagement.SceneManager.html)
- [Unity Web Requests](https://docs.unity3d.com/ScriptReference/Networking.UnityWebRequest.html)
-  Great if you need to communicate with a REST API
-  but if you need a real socket, you have to use UWP

Multiprocessing:

- [Unity Coroutines](https://docs.unity3d.com/Manual/Coroutines.html)
  - coroutines are functions executed "frame by frame"
  - they are *not* threads, but "distant relaives of" the `Update()` Unity callback
  - Full compatibility
  - There are several types of wait command, as you can see [here](https://docs.unity3d.com/ScriptReference/WaitForEndOfFrame.html) in the list of classes. 
- [C# Theads](https://learn.microsoft.com/en-us/troubleshoot/developer/visualstudio/csharp/language-compilers/create-thread)
  - Not compaible with HoloLens2
- [Tasks](https://learn.microsoft.com/it-it/dotnet/api/system.threading.tasks.task?view=net-8.0)
  - Tasks are processes tha can be run in parallel
  - Task class does not work in Unity: it works only in HoloLens2


## Survive to UWP

Basics:

- [Create a console app in UWP](https://blog.pieeatingninjas.be/2018/04/05/creating-a-uwp-console-app-in-c/)

Interesting features available only in UWP when the application is run on the device:

- Geolocalisation in UWP
  - WIFI ONLY! HoloLens2 does not support any other type of localisation except of WiFi localisation
  - also Unity offers a geolocation API call, but it works only on smartphones. 
- [Windows Dvices Geolocation namespace](https://learn.microsoft.com/en-us/uwp/api/windows.devices.geolocation?view=winrt-22621)
  - see also [this](https://stackoverflow.com/questions/38704125/unity-isnt-allowed-to-use-location-windows-10)
- Sockets: Unity does not offer such a feature
  - [Official UWP documentation about Sockets](https://learn.microsoft.com/it-it/windows/uwp/networking/sockets)
  - [StreamSocket class](https://learn.microsoft.com/en-us/uwp/api/windows.networking.sockets.streamsocket?view=winrt-22621) is the main class of this part of the API, along with [Stream Socket Listener](https://learn.microsoft.com/en-us/uwp/api/windows.networking.sockets.streamsocketlistener?view=winrt-22621)


## Other Interesting (unexplored) Features in HoloLens2

- It is perfectly possible to build a HoloLens2 applicatio using different scenes
  - [MRTK2 Scene System Getting Started](https://learn.microsoft.com/en-us/windows/mixed-reality/mrtk-unity/mrtk2/features/scene-system/scene-system-getting-started?view=mrtkunity-2022-05)
  - [Unity Scene System](https://docs.unity3d.com/ScriptReference/SceneManagement.Scene.html)
  - Unity also support natively [Additive Scene Loading](https://docs.unity3d.com/ScriptReference/SceneManagement.LoadSceneMode.Additive.html) meaning that two scenes can be loaded together: Unity is able to understand how to merge the two scenes. 

- About Scene Observer:
  - [Spatial Awareness](https://learn.microsoft.com/en-us/windows/mixed-reality/mrtk-unity/mrtk2/features/spatial-awareness/spatial-awareness-getting-started?view=mrtkunity-2022-05) is the low level component for space laser scan
  - [Spatial Awareness Scripting](https://learn.microsoft.com/en-us/windows/mixed-reality/mrtk-unity/mrtk2/features/spatial-awareness/usage-guide?view=mrtkunity-2022-05) for informations about the scripting. **VERY IMPORTANT:** a root object must be defined, otherwise the error will be quite strange
  - [Scene Understanding](https://learn.microsoft.com/en-us/windows/mixed-reality/mrtk-unity/mrtk2/features/spatial-awareness/scene-understanding?view=mrtkunity-2022-05) is the higher level of Spatial Awareness, and allows to distinguish floors, ceilings and surfaces, and more generally to enable the device to "understand" the space from laser scannings.

- More specific about assets management: [if you're interested in creating your assets](https://learn.microsoft.com/en-us/windows/mixed-reality/design/asset-creation-process)

- [figma](https://learn.microsoft.com/en-us/windows/mixed-reality/design/figma-toolkit) is a paid tool enabling to create user interfaces, also in HoloLens2
  - There's a [bridge](https://learn.microsoft.com/en-us/windows/mixed-reality/design/figma-unity-bridge) available fr Unity


## Samples

- [Basic Samples](https://www.notion.so/HoloLens2-appunti-brutti-sui-samples-ufficiali-d95f0fdc217d4019b32ef2b808d1840b)
- The true [scene understanding](https://github.com/microsoft/MixedReality-SceneUnderstanding-Samples) project