﻿using System;
using VVVV.PluginInterfaces.V2;
using VVVV.Utils.Streams;

namespace VVVV.Hosting.IO.Streams
{
	class MultiDimOutStream<T> : BufferedIOStream<IInStream<T>>
	{
		private readonly IOutStream<T> FDataStream;
		private readonly IOutStream<int> FBinSizeStream;
		
		public MultiDimOutStream(IIOFactory ioFactory, OutputAttribute attribute)
		{
			FDataStream = ioFactory.CreateIO<IOutStream<T>>(attribute);
			FBinSizeStream = ioFactory.CreateIO<IOutStream<int>>(attribute.GetBinSizeOutputAttribute());
			Length = 1;
		}
		
		public override void Flush()
		{
			FBinSizeStream.Length = Length;
			
			int binSizeSum = 0;
			using (var binSizeWriter = FBinSizeStream.GetWriter())
			{
				foreach (var outputStream in this)
				{
					binSizeWriter.Write(outputStream.Length);
					binSizeSum += outputStream.Length;
				}
			}
			
			var buffer = MemoryPool<T>.GetArray();
			try 
			{
			    FDataStream.Length = binSizeSum;
    			using (var dataWriter = FDataStream.GetWriter())
    			{
    				foreach (var outputStream in this)
    				{
    				    if (outputStream.IsChanged)
    				        dataWriter.Write(outputStream, buffer);
    				    else
    				        dataWriter.Position += outputStream.Length;
    				}
    			}
			} 
			finally 
			{
			    MemoryPool<T>.PutArray(buffer);
			}
			
			base.Flush();
		}
	}
}
