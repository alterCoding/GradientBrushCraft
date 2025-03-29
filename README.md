# Gradient Brushes Craft

## About
:dart: **A tiny .NET developer tool**

:art: **"Gradient Brushes Craft"** :paintbrush: is an unpretentious tiny application that manages and displays a gradient brush object while enabling the user to interactively play with some of its parameters.

The brush object is an instance of the `PathGradientBrush` or `LinearGradientBrush` classes. As of now, the gradient is a simple 2-colors (using the `PathGradientBrush.SurroundColors` or `LinearGradientBrush.LinearColors` properties), from which a `Blend` object may be attached to and modified (though limited to 3 variable positions/factors).  
If you are like me:
 - think that the .Net documention isn't too verbose about the gradient parameters
 - whereas the expected visual gradient result of the `PathGradientBrush`/`LinearGradientBrush`/`Blend` classes properties changes is not always obvious
 - maybe this tool could allow you to save time.

:infinity: Scope and limitations:  
As of now, this tool targets the *GDI+ Graphics* (and is limited to), thus it may be qualified as quite old and deprecated. Indeed, the *GDI* is heavily used especially with the .Net Framework and/or the Windows Forms. In other words, we target the `System.Drawing` namespace. 

## Features
- interactive edition of the main parameters (colors, gradient distribution, scale and factors related to)
- basic shapes selection
- source code excerpts according to the actual parameters and values

## Usage
No installation is required, as long as *.NET Framework* is available. The target version is 4.8 but 4.5 is enough.  
That means nothing should be needed from Win10 and later.

> Copy and Run the executable `GradientBrushCraft.exe` (:shield:)

:shield: The *Windows SmartScreen* undoubtedly identifies the binary as *an unrecognised application from an unknown publisher*. The executable isn't signed and SmartScreen infrastructure doesn't suit well for small applications w/o reputation.   
:nerd_face: *Auto-signing* the executable would be quite useless. The source code is freely available, so feel free to build it or to take a look on it.

> Or compile the source code and start the (sub)project `GradientBrushCraft`.

The C# source code is provided as a *Visual Studio* solution

## Development notes
### GUI
`Windows Forms` are used, which is more consistent with the *GDI primitives* usage goal (although it might be considered as legacy).  
A second tool (or maybe an update) is foreseen to target the Presentation Framework (`WPF`).  
However, some properties and principles between `System.Drawing.Drawing2D.xxxxGradientBrush` and `System.Windows.Media.xxxxxGradientBrush` are related, thus the tool could remain of interest.

### Deployment topics
Having dependencies or installation requirements of any kind would have been boring for such a tiny tool. Besides, keeping source code in organised units is still key rule. For this reason, I chose to embed as resources and dynamically load at runtime the(my) class library(ies)

### SubProjects
The solution also hosts a few other projects
- `DotNetInventory`: is a small command line that copes with .NetFX detection [:construction:].  
Nothing fancy, but shall be broadened to support more features
- `libBCL`: is a small general purpose library
- `UnitTests`: related to the *libBCL*