using OpenTK.Graphics.OpenGL;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
namespace Microsoft.Xna.Framework.Graphics
{
    public static class EffectPassExtension
    {
        private static ConditionalWeakTable<EffectPass, EffectPassProperties> PropertyTable { get; set; } = new ConditionalWeakTable<EffectPass, EffectPassProperties>();
        internal static void InitGL(this EffectPass pass)
        {
            EffectPassProperties props = PropertyTable.GetValue(pass,x => { return new EffectPassProperties(x); });
            //EffectPassProperties props = new EffectPassProperties(pass);
            //if ()
            //PropertyTable.Add(pass, props);
        }

        private static void SetShaderSamplers(this EffectPass pass, EffectPassProperties props, Shader shader, TextureCollection textures, SamplerStateCollection samplerStates)
        {
            foreach (var sampler in shader.Samplers)
            {
                var param = props.Effect.Parameters[sampler.parameter];
                var texture = param.Data as Texture;

                textures[sampler.textureSlot] = texture;

                // If there is a sampler state set it.
                if (sampler.state != null)
                    samplerStates[sampler.samplerSlot] = sampler.state;
            }
        }
        public static void ApplyGL(this EffectPass pass)
        {
            // Set/get the correct shader handle/cleanups.
            //
            // TODO: This "reapply" if the shader index changes
            // trick is sort of ugly.  We should probably rework
            // this to use some sort of "technique/pass redirect".
            //
            EffectPassProperties props = PropertyTable.GetValue(pass, x => { return new EffectPassProperties(pass); });

            if (props.Effect.OnApply())
            {
                props.Effect.CurrentTechnique.Passes[0].ApplyGL();
                return;
            }

            var device = props.GraphicsDevice;
            ShaderProgram program = props.ShaderCache.GetProgram(props.VertexShader, props.PixelShader);

            GL.UseProgram(program.Program);
            if (props.VertexShader != null)
            {
                device.VertexShader = props.VertexShader;

                // Update the texture parameters.
                pass.SetShaderSamplers(props, props.VertexShader, device.VertexTextures, device.VertexSamplerStates);

                foreach (int paramIndex in props.VertexShaderParams)
                {
                    EffectParameter param = props.Effect.Parameters[paramIndex];
                    SetEffectParameter(program, param);
                }
                // Update the constant buffers.
                foreach(int c in props.VertexShader.CBuffers)
                {
                    device.SetConstantBuffer(ShaderStage.Vertex, c, null);
                }
            }

            if (props.PixelShader != null)
            {
                device.PixelShader = props.PixelShader;

                // Update the texture parameters.
                pass.SetShaderSamplers(props, props.PixelShader, device.Textures, device.SamplerStates);

                foreach (int paramIndex in props.PixelShaderParams)
                {
                    EffectParameter param = props.Effect.Parameters[paramIndex];
                    SetEffectParameter(program, param);
                }
                // Update the constant buffers.
                foreach (int c in props.PixelShader.CBuffers)
                {
                    device.SetConstantBuffer(ShaderStage.Pixel, c, null);
                }
            }
            

            // Set the render states if we have some.
            if (props.RasterizerState != null)
                device.RasterizerState = props.RasterizerState;
            if (props.BlendState != null)
                device.BlendState = props.BlendState;
            if (props.DepthStencilState != null)
                device.DepthStencilState = props.DepthStencilState;

        }
        private static void SetEffectParameter(ShaderProgram program, EffectParameter param)
        {
            if (param.Name == "posFixup")
                return;
            int location = program.GetUniformLocation(param.Name);
            switch (param.ParameterClass)
            {
                case EffectParameterClass.Object:
                    SetEffectParameterObject(program,location, param);
                    break;
                case EffectParameterClass.Vector:
                    SetEffectParameterVector(program, location, param);
                    break;
                case EffectParameterClass.Matrix:
                    SetEffectParameterMatrix(program,location, param);
                    break;
                case EffectParameterClass.Scalar:
                    SetEffectParameterFloat(program, location, param);
                    break;
            }
            GraphicsExtensions.CheckGLError();
        }
        private static void SetEffectParameterObject(ShaderProgram program,int location,EffectParameter param)
        {
            object obj = param.Data;
        }
        private static void SetEffectParameterFloat(ShaderProgram program, int location, EffectParameter param)
        {
            //int elementCount = param.RowCount * param.ColumnCount;
            unsafe
            {
                float[] objArray = (float[])param.Data;
                fixed (float* data = objArray)
                {
                    GL.Uniform1(location, objArray.Length, data);
                }
            }
        }
        private static void SetEffectParameterMatrix(ShaderProgram program, int location, EffectParameter param)
        {
            //int elementCount = param.RowCount * param.ColumnCount;
            unsafe
            {
                float[] objArray = (float[])param.Data;
                fixed (float* data = objArray)
                {
                    GL.UniformMatrix4(location, 1,false, data);
                }
            }
        }
        private static void SetEffectParameterVector(ShaderProgram program,int location, EffectParameter param)
        {
            //int elementCount = param.RowCount * param.ColumnCount;
            unsafe
            {
                float[] objArray = (float[])param.Data;
                fixed (float* data = objArray)
                {
                    if (objArray.Length == 2)
                        GL.Uniform2(location, 1, data);
                    else if(objArray.Length == 3)
                        GL.Uniform3(location, 1, data);
                    else
                        GL.Uniform4(location, 1, data);
                }
            }
        }
        class EffectPassProperties
        {
            public EffectPassProperties(EffectPass pass)
            {
                Pass = pass;
                GetEffect();

            }
            private void GetEffect()
            {
                Effect = (Effect)typeof(EffectPass).GetField("_effect", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(Pass);
                GraphicsDevice = Effect.GraphicsDevice;

                BlendState = (BlendState)typeof(EffectPass).GetField("_blendState", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(Pass);
                DepthStencilState = (DepthStencilState)typeof(EffectPass).GetField("_depthStencilState", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(Pass);
                RasterizerState = (RasterizerState)typeof(EffectPass).GetField("_rasterizerState", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(Pass);

                FieldInfo pixelShaderField = typeof(EffectPass).GetField("_pixelShader", BindingFlags.NonPublic | BindingFlags.Instance);

                object ps = pixelShaderField.GetValue(Pass);
                object vs = typeof(EffectPass).GetField("_vertexShader", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(Pass);
                PixelShader = (Shader)ps;
                VertexShader = (Shader)vs;

                FieldInfo programCacheField = typeof(GraphicsDevice).GetField("_programCache", BindingFlags.NonPublic | BindingFlags.Instance);
                ShaderCache = (ShaderProgramCache)programCacheField.GetValue(GraphicsDevice);


                VertexShaderParams = getParameters(VertexShader);
                PixelShaderParams = getParameters(PixelShader);
            }
            private int[] getParameters(Shader shader)
            {
                
                FieldInfo fieldInfo = typeof(ConstantBuffer).GetField("_parameters", BindingFlags.NonPublic | BindingFlags.Instance);
                List<int> res = new List<int>();

                
                for (var p = 0; p < shader.CBuffers.Length; p++)
                {
                    var c = shader.CBuffers[p];
                    var cb = Effect.ConstantBuffers[c];
                    foreach (int parameter in (int[])fieldInfo.GetValue(cb))
                    {
                        res.Add(parameter);
                    }
                }
                return res.ToArray();
            }
            public int[] VertexShaderParams { get; private set; }
            public int[] PixelShaderParams { get; private set; }
            public EffectPass Pass { get; private set; }
            public GraphicsDevice GraphicsDevice { get; private set; }
            public ShaderProgramCache ShaderCache { get; private set; }
            public Effect Effect { get; private set; }

            public Shader PixelShader { get; private set; }
            public Shader VertexShader { get; private set; }

            public BlendState BlendState { get; private set; }
            public DepthStencilState DepthStencilState { get; private set; }
            public RasterizerState RasterizerState { get; private set; }
        }
    }
}
