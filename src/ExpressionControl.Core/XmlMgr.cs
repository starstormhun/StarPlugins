using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Xml.Serialization;
namespace ExpressionControl
{
	[XmlRoot("root")]
	public class XmlMgr
	{
		public XmlMgr()
		{
			this.keyConfig = new XmlMgr.KeyConfig();
			this.expressions = new List<ExpressionControl.Expression>();
			bool flag = !File.Exists(XmlMgr.FILENAME);
			if (flag)
			{
				this.Save(false);
			}
		}
		public void Save(bool load)
		{
			XmlSerializer xmlSerializer = new XmlSerializer(typeof(XmlMgr));
			StreamWriter streamWriter = new StreamWriter(XmlMgr.FILENAME, false, new UTF8Encoding(false));
			xmlSerializer.Serialize(streamWriter, this);
			streamWriter.Close();
			if (load)
			{
				this.LoadFinish();
			}
		}
		public void Load()
		{
			XmlSerializer xmlSerializer = new XmlSerializer(typeof(XmlMgr));
			StreamReader streamReader = new StreamReader(XmlMgr.FILENAME, new UTF8Encoding(false));
			XmlMgr xmlMgr = (XmlMgr)xmlSerializer.Deserialize(streamReader);
			streamReader.Close();
			this.keyConfig = xmlMgr.keyConfig;
			this.expressions = xmlMgr.expressions;
			this.programs = xmlMgr.programs;
			this.LoadFinish();
		}
		private void LoadFinish()
		{
			bool flag = this.expressions == null;
			if (!flag)
			{
				this.expressions = this.expressions.Distinct(new XmlMgr.ExpressionComparer()).ToList<ExpressionControl.Expression>();
			}
		}
		private static readonly string FILENAME = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + "\\" + ExpressionControlPlugin.Name + ".xml";
		[XmlElement("KeyConfig")]
		public XmlMgr.KeyConfig keyConfig;
		[XmlArray("Expressions")]
		[XmlArrayItem("item")]
		public List<ExpressionControl.Expression> expressions;
		[XmlArray("Programs")]
		[XmlArrayItem("Program")]
		public List<ExpressionControl.Program> programs = new List<ExpressionControl.Program>();
		public class KeyConfig
		{
			public string showGUI { get; set; }
			public KeyConfig()
			{
				this.showGUI = "L";
			}
		}
		private class ExpressionComparer : IEqualityComparer<ExpressionControl.Expression>
		{
			public bool Equals(ExpressionControl.Expression x, ExpressionControl.Expression y)
			{
				return (x == null && y == null) || (x != null && y != null && x.guid == y.guid);
			}
			public int GetHashCode(ExpressionControl.Expression p)
			{
				return p.guid.GetHashCode();
			}
		}
	}
}