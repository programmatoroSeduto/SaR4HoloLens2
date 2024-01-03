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


## What is MRTK2?

- MRTK2 : *Mixed Reality ToolKit v2*
  - it contains all the *basic* tools for HoloLens2 app implementation, to speed up just the most boring parts of the development such as to interact with some of the low-level runtime aspects. 
  - *So many things are missing in this framework!* It is just a basic low-level framework: you have to build your app on top of the stack. 

MRTK2 installation:

- This tool is available for Unity and for Unreal Engine. I developed using Unity version (Unity is free under Unige Students licence). 
  - [official documentation with Unity](https://learn.microsoft.com/en-us/windows/mixed-reality/mrtk-unity/mrtk2/?view=mrtkunity-2022-05)
  - [VERY IMPORTANT for using GIT in MRTK2 projects](https://learn.microsoft.com/en-us/windows/mixed-reality/mrtk-unity/mrtk2/performance/large-projects?view=mrtkunity-2022-05)
- MRTK2 is not a standalone app, but something you build against your project, meaning that different project require different MRTK2 installations. To import the package, there's a portable wizard: the **Mixed Reality Feature Tool**
  - [GitHub link](https://github.com/microsoft/MixedRealityToolkit-Unity/releases)
  - [Mixed Reality Feature Tool](https://www.microsoft.com/en-us/download/details.aspx?id=102778)
- Are you in a hurry? Well: 
  - [this repository](https://github.com/programmatoroSeduto/HoloLens2ProjectTemplate) contains a template where to start from, with MRTK2 already installed and ready to use. 

How to use that?

- [Official Tutorial](https://learn.microsoft.com/en-us/training/modules/learn-mrtk-tutorials/1-5-exercise-configure-resources#tabpanel_1_openxr)
- Very important the idea of *profile*: MRTK2 takes track of the settings of the framework as a *profile* that is a file containing all the configuration details. Here is a first explainaion:
  - [MRTK2 Profiles Guide](https://learn.microsoft.com/en-us/windows/mixed-reality/mrtk-unity/mrtk2/configuration/mixed-reality-configuration-guide?view=mrtkunity-2022-05)

To deploy a application to HoloLens2:

- [deployment guide in Mixed Reality documentation](https://learn.microsoft.com/en-us/windows/mixed-reality/mrtk-unity/mrtk2/configuration/mixed-reality-configuration-guide?view=mrtkunity-2022-05)
- [deployment guide in MRTK2 documentation](https://learn.microsoft.com/en-us/windows/mixed-reality/mrtk-unity/mrtk2/supported-devices/wmr-mrtk?view=mrtkunity-2022-05)

