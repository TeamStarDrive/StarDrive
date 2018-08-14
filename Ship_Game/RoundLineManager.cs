using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Ship_Game
{
	internal sealed class RoundLineManager : IDisposable
	{
		private GraphicsDevice device;

		private Effect effect;

		private EffectParameter viewProjMatrixParameter;

		private EffectParameter instanceDataParameter;

		private EffectParameter timeParameter;

		private EffectParameter lineRadiusParameter;

		private EffectParameter lineColorParameter;

		private EffectParameter blurThresholdParameter;

		private VertexBuffer vb;

		private IndexBuffer ib;

		private VertexDeclaration vdecl;

		private int numInstances;

		private int numVertices;

		private int numIndices;

		private int numPrimitivesPerInstance;

		private int numPrimitives;

		private int bytesPerVertex;

		private float[] translationData;

		public int NumLinesDrawn;

		public float BlurThreshold = 0.97f;

		public string[] TechniqueNames
		{
			get
			{
				string[] names = new string[effect.Techniques.Count];
				int index = 0;
				foreach (EffectTechnique technique in effect.Techniques)
				{
					int num = index;
					index = num + 1;
					names[num] = technique.Name;
				}
				return names;
			}
		}

	    public float ComputeBlurThreshold(float lineRadius, Matrix viewProjMatrix, float viewportWidth)
		{
			Vector4 lineRadiusTestBase = new Vector4(0f, 0f, 0f, 1f);
			Vector4 lineRadiusTest = new Vector4(lineRadius, 0f, 0f, 1f);
			Vector4 output = Vector4.Transform(lineRadiusTest - lineRadiusTestBase, viewProjMatrix);
			output.X = output.X * viewportWidth;
			double newBlur = 0.125 * Math.Log(output.X) + 0.4;
			return MathHelper.Clamp((float)newBlur, 0.5f, 0.99f);
		}

		private void CreateRoundLineMesh()
		{
			numInstances = 200;
			numVertices = 60 * numInstances;
			numPrimitivesPerInstance = 28;
			numPrimitives = numPrimitivesPerInstance * numInstances;
			numIndices = 3 * numPrimitives;
			short[] indices = new short[numIndices];
			bytesPerVertex = RoundLineVertex.SizeInBytes;
			RoundLineVertex[] tri = new RoundLineVertex[numVertices];
			translationData = new float[numInstances * 4];
			int iv = 0;
			int ii = 0;
			for (int instance = 0; instance < numInstances; instance++)
			{
				int iVertex = iv;
				int num = iv;
				iv = num + 1;
				tri[num] = new RoundLineVertex(new Vector3(0f, -1f, 0f), new Vector2(1f, 4.712389f), new Vector2(0f, 0f), instance);
				int num1 = iv;
				iv = num1 + 1;
				tri[num1] = new RoundLineVertex(new Vector3(0f, -1f, 0f), new Vector2(1f, 4.712389f), new Vector2(0f, 1f), instance);
				int num2 = iv;
				iv = num2 + 1;
				tri[num2] = new RoundLineVertex(new Vector3(0f, 0f, 0f), new Vector2(0f, 4.712389f), new Vector2(0f, 1f), instance);
				int num3 = iv;
				iv = num3 + 1;
				tri[num3] = new RoundLineVertex(new Vector3(0f, 0f, 0f), new Vector2(0f, 4.712389f), new Vector2(0f, 0f), instance);
				int num4 = iv;
				iv = num4 + 1;
				tri[num4] = new RoundLineVertex(new Vector3(0f, 0f, 0f), new Vector2(0f, 1.57079637f), new Vector2(0f, 1f), instance);
				int num5 = iv;
				iv = num5 + 1;
				tri[num5] = new RoundLineVertex(new Vector3(0f, 0f, 0f), new Vector2(0f, 1.57079637f), new Vector2(0f, 0f), instance);
				int num6 = iv;
				iv = num6 + 1;
				tri[num6] = new RoundLineVertex(new Vector3(0f, 1f, 0f), new Vector2(1f, 1.57079637f), new Vector2(0f, 1f), instance);
				int num7 = iv;
				iv = num7 + 1;
				tri[num7] = new RoundLineVertex(new Vector3(0f, 1f, 0f), new Vector2(1f, 1.57079637f), new Vector2(0f, 0f), instance);
				int num8 = ii;
				ii = num8 + 1;
				indices[num8] = (short)iVertex;
				int num9 = ii;
				ii = num9 + 1;
				indices[num9] = (short)(iVertex + 1);
				int num10 = ii;
				ii = num10 + 1;
				indices[num10] = (short)(iVertex + 2);
				int num11 = ii;
				ii = num11 + 1;
				indices[num11] = (short)(iVertex + 2);
				int num12 = ii;
				ii = num12 + 1;
				indices[num12] = (short)(iVertex + 3);
				int num13 = ii;
				ii = num13 + 1;
				indices[num13] = (short)iVertex;
				int num14 = ii;
				ii = num14 + 1;
				indices[num14] = (short)(iVertex + 4);
				int num15 = ii;
				ii = num15 + 1;
				indices[num15] = (short)(iVertex + 6);
				int num16 = ii;
				ii = num16 + 1;
				indices[num16] = (short)(iVertex + 5);
				int num17 = ii;
				ii = num17 + 1;
				indices[num17] = (short)(iVertex + 6);
				int num18 = ii;
				ii = num18 + 1;
				indices[num18] = (short)(iVertex + 7);
				int num19 = ii;
				ii = num19 + 1;
				indices[num19] = (short)(iVertex + 5);
				iVertex = iv;
				int iIndex = ii;
				for (int i = 0; i < 13; i++)
				{
					float deltaTheta = 0.2617994f;
					float theta0 = 1.57079637f + i * deltaTheta;
					float theta1 = theta0 + deltaTheta / 2f;
					tri[iVertex] = new RoundLineVertex(new Vector3(0f, 0f, 0f), new Vector2(0f, theta1), new Vector2(0f, 0f), instance);
					float x = (float)Math.Cos(theta0);
					float y = (float)Math.Sin(theta0);
					tri[iVertex + 1] = new RoundLineVertex(new Vector3(x, y, 0f), new Vector2(1f, theta0), new Vector2(1f, 0f), instance);
					if (i < 12)
					{
						indices[iIndex] = (short)iVertex;
						indices[iIndex + 1] = (short)(iVertex + 1);
						indices[iIndex + 2] = (short)(iVertex + 3);
						iIndex = iIndex + 3;
						ii = ii + 3;
					}
					iVertex = iVertex + 2;
					iv = iv + 2;
				}
				for (int i = 0; i < 13; i++)
				{
					float deltaTheta = 0.2617994f;
					float theta0 = 4.712389f + i * deltaTheta;
					float theta1 = theta0 + deltaTheta / 2f;
					tri[iVertex] = new RoundLineVertex(new Vector3(0f, 0f, 0f), new Vector2(0f, theta1), new Vector2(0f, 1f), instance);
					float x = (float)Math.Cos(theta0);
					float y = (float)Math.Sin(theta0);
					tri[iVertex + 1] = new RoundLineVertex(new Vector3(x, y, 0f), new Vector2(1f, theta0), new Vector2(1f, 1f), instance);
					if (i < 12)
					{
						indices[iIndex] = (short)iVertex;
						indices[iIndex + 1] = (short)(iVertex + 1);
						indices[iIndex + 2] = (short)(iVertex + 3);
						iIndex = iIndex + 3;
						ii = ii + 3;
					}
					iVertex = iVertex + 2;
					iv = iv + 2;
				}
			}
			vb = new VertexBuffer(device, numVertices * bytesPerVertex, BufferUsage.None);
			vb.SetData(tri);
			vdecl = new VertexDeclaration(device, RoundLineVertex.VertexElements);
			ib = new IndexBuffer(device, numIndices * 2, BufferUsage.None, IndexElementSize.SixteenBits);
			ib.SetData(indices);
		}

		public void Draw(RoundLine roundLine, float lineRadius, Color lineColor, Matrix viewProjMatrix, float time, string techniqueName)
		{
			device.VertexDeclaration = vdecl;
			device.Vertices[0].SetSource(vb, 0, bytesPerVertex);
			device.Indices = ib;
			viewProjMatrixParameter.SetValue(viewProjMatrix);
			timeParameter.SetValue(time);
			lineColorParameter.SetValue(lineColor.ToVector4());
			lineRadiusParameter.SetValue(lineRadius);
			blurThresholdParameter.SetValue(BlurThreshold);
			int num = 0;
			int iData = num + 1;
			translationData[num] = roundLine.P0.X;
			int num1 = iData;
			iData = num1 + 1;
			translationData[num1] = roundLine.P0.Y;
			int num2 = iData;
			iData = num2 + 1;
			translationData[num2] = roundLine.Rho;
			int num3 = iData;
			iData = num3 + 1;
			translationData[num3] = roundLine.Theta;
			instanceDataParameter.SetValue(translationData);
			if (techniqueName != null)
			{
				effect.CurrentTechnique = effect.Techniques[techniqueName];
			}
			else
			{
				effect.CurrentTechnique = effect.Techniques[0];
			}
			effect.Begin();
			EffectPass pass = effect.CurrentTechnique.Passes[0];
			pass.Begin();
			int numInstancesThisDraw = 1;
			device.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, numVertices, 0, numPrimitivesPerInstance * numInstancesThisDraw);
			RoundLineManager numLinesDrawn = this;
			numLinesDrawn.NumLinesDrawn = numLinesDrawn.NumLinesDrawn + numInstancesThisDraw;
			pass.End();
			effect.End();
		}

		public void Draw(Array<RoundLine> roundLines, float lineRadius, Color lineColor, Matrix viewProjMatrix, float time, string techniqueName)
		{
			device.VertexDeclaration = vdecl;
			device.Vertices[0].SetSource(vb, 0, bytesPerVertex);
			device.Indices = ib;
			viewProjMatrixParameter.SetValue(viewProjMatrix);
			timeParameter.SetValue(time);
			lineColorParameter.SetValue(lineColor.ToVector4());
			lineRadiusParameter.SetValue(lineRadius);
			blurThresholdParameter.SetValue(BlurThreshold);
			if (techniqueName != null)
			{
				effect.CurrentTechnique = effect.Techniques[techniqueName];
			}
			else
			{
				effect.CurrentTechnique = effect.Techniques[0];
			}
			effect.Begin();
			EffectPass pass = effect.CurrentTechnique.Passes[0];
			pass.Begin();
			int iData = 0;
			int numInstancesThisDraw = 0;
			foreach (RoundLine roundLine in roundLines)
			{
				int num = iData;
				iData = num + 1;
				translationData[num] = roundLine.P0.X;
				int num1 = iData;
				iData = num1 + 1;
				translationData[num1] = roundLine.P0.Y;
				int num2 = iData;
				iData = num2 + 1;
				translationData[num2] = roundLine.Rho;
				int num3 = iData;
				iData = num3 + 1;
				translationData[num3] = roundLine.Theta;
				numInstancesThisDraw++;
				if (numInstancesThisDraw != numInstances)
				{
					continue;
				}
				instanceDataParameter.SetValue(translationData);
				effect.CommitChanges();
				device.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, numVertices, 0, numPrimitivesPerInstance * numInstancesThisDraw);
				RoundLineManager numLinesDrawn = this;
				numLinesDrawn.NumLinesDrawn = numLinesDrawn.NumLinesDrawn + numInstancesThisDraw;
				numInstancesThisDraw = 0;
				iData = 0;
			}
			if (numInstancesThisDraw > 0)
			{
				instanceDataParameter.SetValue(translationData);
				effect.CommitChanges();
				device.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, numVertices, 0, numPrimitivesPerInstance * numInstancesThisDraw);
				RoundLineManager roundLineManager = this;
				roundLineManager.NumLinesDrawn = roundLineManager.NumLinesDrawn + numInstancesThisDraw;
			}
			pass.End();
			effect.End();
		}

		public void Init(GraphicsDevice device, GameContentManager content)
		{
			this.device = device;
			effect = content.Load<Effect>("RoundLine");
			viewProjMatrixParameter = effect.Parameters["viewProj"];
			instanceDataParameter = effect.Parameters["instanceData"];
			timeParameter = effect.Parameters["time"];
			lineRadiusParameter = effect.Parameters["lineRadius"];
			lineColorParameter = effect.Parameters["lineColor"];
			blurThresholdParameter = effect.Parameters["blurThreshold"];
			CreateRoundLineMesh();
		}
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        ~RoundLineManager() { Dispose(false); }

        private void Dispose(bool disposing)
        {
            vb?.Dispose(ref vb);
            vdecl?.Dispose(ref vdecl);
            ib?.Dispose(ref ib);
        }
	}
}