Gradient Brushes Craft
======================

**A tiny .NET developer gui tool** &emsp;
![dotnet](https://img.shields.io/badge/-.NET%20Framework-purple?logo=.net)

![maintenance](https://img.shields.io/badge/maintenance-moderately_developed-blue)

About
-----

:art: **Gradient Brushes Craft** :paintbrush: is an unpretentious tiny application that manages and displays a gradient brush object while enabling the user to interactively play with some of its parameters.

The brush object is an instance of the `PathGradientBrush` or `LinearGradientBrush` classes. As of now, the gradient is a simple 2-colors (using the `PathGradientBrush.SurroundColors` or `LinearGradientBrush.LinearColors` properties), from which a `Blend` object may be attached to and modified (though limited to 3 variable positions/factors, which are hard-coded in the GUI).  
If you are like me:
 - think that the .Net documention isn't too verbose about the gradient parameters
 - whereas the expected visual gradient result of the `PathGradientBrush`/`LinearGradientBrush`/`Blend` classes properties changes is not always obvious
 - maybe this tool could allow you to save time while testing rendering effects.

:infinity: **Scope and limitations:**  
As of now, this tool targets the *GDI+ Graphics* (and is limited to), thus it may be qualified as quite old and deprecated. Indeed, the *GDI* is heavily used especially with the .Net Framework and/or the Windows Forms. In other words, we target the `System.Drawing` namespace.&emsp;![platform](https://img.shields.io/badge/platform-windows-green)

Features
--------

- interactive edition of the main parameters (colors, gradient distribution, scale and factors related to)
- basic shapes selection
- source code excerpts according to the actual parameters and values

<p align="middle">
<a href="./doc/assets/text-gradient.png">
<img src="./doc/assets/text-gradient.png" alt="screenshot1" style="width:30%; height:auto;">
</a>
&emsp; 
<a href="./doc/assets/ellipse-gradient.png">
<img src="./doc/assets/ellipse-gradient.png" alt="screenshot2" style="width:30%; height:auto;">
</a>
</p>


Usage
-----

No installation is required, as long as *.NET Framework* is available. The target version is **4.8**.

**4.7.1** version: despite notice, the application should run seamlessly \
**4.5[.x] - 4.7** versions: the application may be used (some limitations) \
**4.0 and earlier** versions: unsupported

That means for most of system versions from Win10, nothing should be needed.

> Copy and Run the executable `GradientBrushCraft.exe` :shield:

:shield: The *Windows SmartScreen* undoubtedly identifies the binary as *an unrecognised application from an unknown publisher*. The executable isn't signed and SmartScreen infrastructure isn't really smart with small applications w/o any reputation or broadcast.   
:nerd_face: *Auto-signing* the executable would be quite useless. The source code is freely available, so feel free to build it or to take a look on it.

> Or compile the source code and start the (sub)project `GradientBrushCraft`.

The C# source code is provided as a *Visual Studio* solution

Development notes
-----------------

### GUI

`Windows Forms` are used, which is more consistent with the *GDI primitives* usage goal (although it might be considered as legacy).  
A second tool (or maybe an update) is foreseen to target the Presentation Framework (`WPF`).  
However, some properties and principles between `System.Drawing.Drawing2D.xxxxGradientBrush` and `System.Windows.Media.xxxxxGradientBrush` are related, thus the tool could remain of interest.

### Deployment topics

Having dependencies or installation requirements of any kind would have been boring for such a tiny tool. Besides, keeping source code in organised units is still key rule. For this reason, I chose to embed as resources and dynamically load at runtime the(my) class library(ies)

### Sub-projects

The solution also hosts a few other small projects
- :construction: `DotNetInventory`: is a command line that copes with .NetFX detection (a bit out of scope).\
Nothing fancy, but shall be broadened to support more features
- `libBCL`: is a minimalistic general purpose library dedicated to the other projects
- `UnitTests`: *NUnit* testing related to the *libBCL*

History
-------
| Version | Date | Notes |
| ------- | ---- | ----- |
| 1.0.0-beta.1 | 2025-04-03 | filever(1.0.11.6) |
| 1.0.0-beta.2 | 2025-04-21 | filever(1.0.14.26) |
| <td colspan="3">(add) shape panel background color <br> (fixed) textures list behavior </td> |

 
---

![warning](https://img.shields.io/badge/Contains-resentment-orange) <span style="vertical-align:top;">&nbsp;while &nbsp;</span>![madewith](https://img.shields.io/badge/made_with-care-green)
