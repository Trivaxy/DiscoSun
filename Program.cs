using System;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;

namespace DiscoSun
{
	public class Program
	{
		static void Main(string[] args)
		{
			Console.WriteLine("Patching Terraria...");

			ModuleDefinition module = ModuleDefinition.ReadModule("Terraria.exe"); // get the Terraria module
			TypeDefinition main = module.GetType("Terraria.Main"); // get the type of Terraria.Main

			// Now that we have the type we're going to patch (the Main class) it's up to us to do whatever we want
			// First, let's make the main menu indicate that Terraria was patched by changing the version string,
			// which happens to be Main.versionNumber.
			// Let's make a reference to this field. We'll be using it later.
			FieldDefinition versionNumber = main.Fields.First(f => f.Name == "versionNumber");

			// Before we continue, I need to explain something important in Mono.Cecil -
			// there is a key destinction between references and definitions.
			// For example, a TypeReference is not the same as a TypeDefinition.
			// Definitions are members that exist within a module - they are defined there, hence the name "Definition"
			// References refer to members that have been defined outside the module
			// However, from a reference, you can get a definition. Converting a reference to a definition is called resolving.
			// We'll use this concept later on.
			// For example, we can easily have a TypeDefinition to Terraria.Main since it's within our module
			// but to refer to something like Microsoft.Xna.Framework.Color we need a TypeReference.
			// You will see this distinction made later on in the code

			// Back to business:

			// In IL, static fields are initialized in a special constructor, named the static constructor
			// You can typically find static constructors as methods named .cctor in IL
			// Each class/struct has only one static constructor
			// Reminder: Constructors are just methods
			MethodDefinition method = main.GetStaticConstructor(); // get the static constructor of Main

			// Now we get an ILProcessor so we can easily modify the constructor
			ILProcessor il = method.Body.GetILProcessor();

			// We also create a collection of its instructions
			var instructions = method.Body.Instructions;

			// We're looking for this particular instruction, which sets Main.versionNumber:
			// stsfld string Terraria.Main::versionNumber
			// The opcode stsfld simply stores the value on the stack to the specified static field

			// note: we can use instructions.First(i => ...) from here on out, but i opted to use a for loop
			// so i can explain things more easily
			for (int i = 0; i < instructions.Count; i++) // let's loop through all the instructions
			{
				Instruction instruction = instructions[i]; // the intruction we're looking at

				// is the opcode a Stsfld, and is its operand a FieldDefinition referring to Main.versionNumber?
				if (instruction.OpCode == OpCodes.Stsfld && (FieldDefinition)instruction.Operand == versionNumber)
				{
					// if yes, then we most likely found the instruction we're looking for.
					// the instruction before this is:
					// ldstr "v1.4.0.4"
					// which simply pushes the string "v1.4.0.4" onto the stack
					// so we'll remove that.
					il.Remove(instruction.Previous); // remove ldstr "v1.4.0.4"

					// now we'll replace it with the same opcode, but with a different operand:
					// ldstr "v1.4.0.4 PATCHED"
					Instruction newInstruction = il.Create(OpCodes.Ldstr, "v1.4.0.4 PATCHED");
					il.InsertBefore(instruction, newInstruction); // insert it before the stsfld instruction

					// and done. we patched Main.versionString. now it'll say 1.4.0.4 PATCHED in the main menu
					break; // let's get outta the loop
				}
			}

			// okay, now let's make the sun draw with a disco color as another example.
			// let's reuse our method reference that we created earlier and point it at Main.DrawSunAndMoon
			method = main.Methods.First(m => m.Name == "DrawSunAndMoon");

			// now let's reuse our ILProcessor and use it for DrawSunAndMoon
			il = method.Body.GetILProcessor();

			// get a collection of DrawSunAndMoon's instructions
			instructions = method.Body.Instructions;

			// we need to target the 17th and 18th local variables - the ones depicting the color of the sun.
			// Local variables in IL do not have any names - instead, each method has an array of local variables
			// and this array can only be indexed by numbers. Think of it as an array of object[], and its length
			// is the maximum number of local variables used in the method.
			// when removing or adding local variables, make sure you always update this array to include/exclude your local variable
			
			// Back to business:

			// instead of removing all the instructions used in calculating the colors, we simply set them to whatever we
			// want just before the spritebatch.Draw call - so we'll search for that.
			// the opcode we're looking for is:
			// ldsfld class [Microsoft.Xna.Framework.Graphics]Microsoft.Xna.Framework.Graphics.SpriteBatch Terraria.Main::spriteBatch
			// 
			// don't let the long name intimidate you - we're just loading Main.spriteBatch onto the stack.
			// (ldsfld loads the value of a static field onto the stack)

			// let's grab the definition of Main.spriteBatch
			FieldDefinition mainSpriteBatch = main.Fields.First(f => f.Name == "spriteBatch");

			// we'll need Color's constructor for later - let's create it now.
			// but wait - Color is not defined within the Terraria assembly. how can we get its definition at all?
			// the answer lies in TypeReferences - we can have a reference to the color type
			// let's ask the Terraria module to get a reference to Color for us
			module.TryGetTypeReference("Microsoft.Xna.Framework.Color", out TypeReference colorReference);

			// let's *resolve* the definition of the Color constructor which takes 4 ints as parameters
			MethodDefinition colorCtor = module.ImportReference(colorReference).Resolve()
				.GetConstructors().First(c => c.Parameters.All(p => p.ParameterType.Name == "Int32"));

			// the very first two spritebatch calls in the method are related to drawing the sun 
			// so we only need the first occurrence. We'll look for the opcode i mentioned earlier
			// again, using an optional for loop for readability:
			for (int i = 0; i < instructions.Count; i++)
			{
				Instruction instruction = instructions[i];

				if (instruction.OpCode == OpCodes.Ldsfld && (FieldDefinition)instruction.Operand == mainSpriteBatch)
				{
					// now we need to set the 17th and 18th local variables to a disco color
					// to save us time, we'll just set the 17th one then set the 18th one equal to it

					// the opcodes we're about to write will load Main.DiscoR, Main.DiscoG, Main.DiscoB onto the stack
					il.InsertBefore(instruction, il.Create(OpCodes.Ldsfld, main.Fields.First(f => f.Name == "DiscoR")));
					il.InsertBefore(instruction, il.Create(OpCodes.Ldsfld, main.Fields.First(f => f.Name == "DiscoG")));
					il.InsertBefore(instruction, il.Create(OpCodes.Ldsfld, main.Fields.First(f => f.Name == "DiscoB")));

					// all right, we got all the values we want onto the stack
					// we have the color constructor definition. let's create a reference to it and call it
					// this'll store the resulting value of the Color onto the stack
					// (we don't pass a MethodDefinition to newobj - only a MethodReference)
					il.InsertBefore(instruction, il.Create(OpCodes.Newobj, module.ImportReference(colorCtor)));

					// now let's save it to the 17th local variable
					il.InsertBefore(instruction, il.Create(OpCodes.Stloc_S, (byte)17));

					// perfect, now we've set the 17th local to
					// new Color(Main.DiscoR, Main.DiscoG, Main.DiscoB);

					// now all we have to do is set the 18th local to the 17th local
					// let's push the value of the 17th local onto the stack (not its address!)
					il.InsertBefore(instruction, il.Create(OpCodes.Ldloc_S, (byte)17));

					// now let's store it into the 18th local
					il.InsertBefore(instruction, il.Create(OpCodes.Stloc_S, (byte)18));
					break; // get out of the for loop.
				}
			}
			module.Write("TerrariaPatched.exe");
			Console.WriteLine("Successfully patched Terraria!");
		}
	}
}