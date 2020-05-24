# DiscoSun
A simple, well-explained example and tutorial on using Mono.Cecil to patch an assembly, using Terraria as an example.

# Building
To build this on your machine, you'll need a Microsoft.Xna.Framework.dll and an unmodified Terraria.exe placed into bin/Debug/netcoreapp3.1.
Once both are there, you can edit code to your heart's content - the patched Terraria ends up in bin/Debug/netcoreapp3.1/TerrariaPatched.exe

(Of course these are all arbitrary limitations because I was too lazy to add Microsoft.Xna.Framework and Terraria as dependencies. Feel free to modify the code however you wish and learn)

To use TerrariaPatched.exe just drop it into your Terraria Steam/GOG folder and run it.