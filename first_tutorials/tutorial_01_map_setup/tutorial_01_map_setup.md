# SaR4HL2 Tutorial 1 - Map Setup

*The project starts with a small simulated indoor environment, useful for testing the functionalities during the PC development phase.*

## Setup the default map

The scene template already contains a example of "maze". 

![01 maze perspective](image.png)

In the explorer on the left side of the editor, you can check its structure:

![02 maze explorer structure](image-1.png)

It is very important to point out that each comonent is places in (0, 0, 0), which is the view height for the device. If you start the application without adjusting the height, the user's view will be on the same level of the floor!

![03 position origin](image-2.png)

this is what it would happen in this scenario:

![04 wrong user height](image-3.png)

To adjust the user's height there's a script that moves the map downwards. 

![05 MapPositionSettings script](image-4.png)

To add the script to the scene, you have two choices:

1. select the *ExperimentalMap* GameObject inside the explorer, then drag and drop the script on the GUI in the left pannel

![06 drag&drop](image-5.png)

2. otherwise, you can simply use the *Add Component* button, to search the component with the search textbox by name, and finally to select it. 

![07 component selection from add component](image-6.png)

To adjust the height, you can use a manual methods. There's also a method based on user settings, explained later on.

- Map Root : the `SARHL2DebugMap` GameObject
- Force Active : is True
- Check Debug Mode : is False
- User Height from params : is False
- User Height Gui : your height (in my case, the default)

Here's a screen of the final configuratio:

<!-- ![08 final config script](image-7.png) -->
![08 final config script](image-14.png)

And here's how the map appears in play mode:

![09 play mode](image-8.png)

You can switch in the Scene tab during the play mode:

![10 tabs in play mode](image-9.png)

In the following screen, you can see where the entry point is placed:

![11 maze entry point](image-10.png)

## Build your own Maze

This map is a default. If you want to build your won, there's a template inside the project:

![12 maze module template](image-11.png)

The template is modular: it includes one floor and four walls. 

![13 module structure](image-12.png)

Please take into account the scale of the objects, in particular the one of the floor, which is a square plane:

![14 module scale](image-13.png)

Just remember to don't modify the template! 

## Manage AutoHide

AutoHide is a featur eof the script `MapPositionSettings` useful to manage teh switch between debug mode and testing/production mode, especially working with the device. 

Let's try this combination of settings:

![15 autohide settings](image-15.png)

And let's start the play mode: the map start hidden!

![16 hidden map](image-16.png)

In the screen above, please notice the console tab, at the bottom of the screen: 

![17 log hidden map](image-17.png)

... meaning that the map has been hidden by that component. 