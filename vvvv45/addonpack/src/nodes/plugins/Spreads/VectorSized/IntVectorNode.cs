#region usings
using System;
using System.ComponentModel.Composition;

using VVVV.PluginInterfaces.V2;
using VVVV.Utils.Streams;
#endregion usings

namespace VVVV.Nodes
{
	#region PluginInfo
	[PluginInfo(Name = "Integral", Category = "Spreads", Version = "Vector", Help = "Integral (Spreads) with vector size", Author = "woei")]
	#endregion PluginInfo
	public class IntegralVectorNode : IPluginEvaluate
	{
		#region fields & pins
		#pragma warning disable 649
		[Input("Input")]
		IInStream<double> FInput;

		[Input("Vector Size", MinValue = 1, DefaultValue = 1, IsSingle = true)]
		IInStream<int> FVec;
		
		[Input("Bin Size", DefaultValue = -1)]
		IInStream<int> FBin;
		
		[Input("Offset")]
		ISpread<double> FOffset;
				
		[Input("Append Offset", DefaultBoolean = true, Visibility = PinVisibility.OnlyInspector)]
		ISpread<bool> FInclOffset;
		
		[Output("Output")]
		IOutStream<double> FOutput;
		
		[Output("Output Bin Size")]
		IOutStream<int> FOutBin;
		#pragma warning restore
		#endregion fields & pins
		
		//called when data for any output pin is requested
		public void Evaluate(int SpreadMax)
		{
			if (FVec.Length>0)
			{						 
				int vecSize = Math.Max(1,FVec.GetReader().Read());
				int offsetCount = (int)Math.Ceiling(FOffset.SliceCount/(double)vecSize);
				offsetCount = offsetCount.CombineWith(FInclOffset);
				VecBinSpread<double> spread = new VecBinSpread<double>(FInput,vecSize,FBin,offsetCount);				
				
				FOutBin.Length = spread.Count;
				FOutput.Length = spread.ItemCount+(spread.Count*spread.VectorSize);
				using (var binWriter = FOutBin.GetWriter())
				using (var dataWriter = FOutput.GetWriter())
				{
					int incr = 0;
					for (int b = 0; b < spread.Count; b++)
					{
						for (int v = 0; v < vecSize; v++)
						{
							dataWriter.Position = incr+v;
							double sum = FOffset[b*vecSize+v];
							if (FInclOffset[b])
								dataWriter.Write(sum, vecSize);
							double[] column = spread.GetBinColumn(b,v).ToArray();
							for (int s=0; s<column.Length;s++)
							{
								sum += column[s];
								dataWriter.Write(sum,vecSize);
							}
						}
						incr+=spread[b].Length+(FInclOffset[b] ? vecSize:0);
						binWriter.Write((spread[b].Length/vecSize)+1,1);
					}
					FOutput.Length=incr;
				}
			}
			else
				FOutput.Length = FOutBin.Length = 0;
		}
	}
}
