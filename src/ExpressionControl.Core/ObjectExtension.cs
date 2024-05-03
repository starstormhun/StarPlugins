using System;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
namespace ExpressionControl
{
	public static class ObjectExtension
	{
		public static T DeepClone<T>(this T src)
		{
			T result;
			using (MemoryStream memoryStream = new MemoryStream())
			{
				BinaryFormatter binaryFormatter = new BinaryFormatter();
				binaryFormatter.Serialize(memoryStream, src);
				memoryStream.Seek(0L, SeekOrigin.Begin);
				result = (T)((object)binaryFormatter.Deserialize(memoryStream));
			}
			return result;
		}
	}
}