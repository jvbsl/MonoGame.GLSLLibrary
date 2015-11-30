
namespace Microsoft.Xna.Framework.Graphics
{
    public static class EffectExtension
    {
        public static void InitGL(this Effect effect)
        {
            foreach (Microsoft.Xna.Framework.Graphics.EffectTechnique technique in effect.Techniques)
            {
                foreach(EffectPass pass in technique.Passes)
                {
                    pass.InitGL();
                }
            }
        }
    }
}
