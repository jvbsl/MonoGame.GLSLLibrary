# MonoGame.GLSLLibrary

[The MonoGame Project](https://github.com/mono/MonoGame/)

A MonoGame GLSL Library to load GLSL Effect files compiled by (MonoGame.GLSLCompiler)
## Usage

Use the MonoGame.Framework.dll from the lib folder, as it is a patched version of the MonoGame Framework, which allows us to get access to multiple internal types and members.

Load your XNB Content files as usual:

```C#
Effect effect = Content.Load<Effect> ("yourEffectFile");
effect.InitGL(); //Optional initializes at start, otherwise this happens the first time the Shader is used
```
Instead of applying the Shader with Apply use ApplyGL instead
```C#
for (int j = 0; j < effect.CurrentTechnique.Passes.Count; j++) {
  effect.CurrentTechnique.Passes [j].ApplyGL();
  //Your Drawing Code comes here(e.g. GraphicsDevice.DrawUserIndexedPrimitives)
}
```

## TODO
* Drawing of Models needs still to be done manually with the custom Effects
* Drawing in SpriteBatch with custom Effect is not possible(yet)
