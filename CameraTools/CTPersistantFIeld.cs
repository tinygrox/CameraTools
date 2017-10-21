using System;

namespace CameraTools
{
	[AttributeUsage(AttributeTargets.Field)]
	public class CtPersistantField : Attribute
	{
		public static string SettingsUrl = "GameData/CameraTools/settings.cfg";

		public CtPersistantField()
		{

		}

		public static void Save()
		{
			ConfigNode fileNode = ConfigNode.Load(SettingsUrl);
			ConfigNode settings = fileNode.GetNode("CToolsSettings");


			foreach(var field in typeof(CamTools).GetFields())
			{
				if(!field.IsDefined(typeof(CtPersistantField), false)) continue;

				settings.SetValue(field.Name, field.GetValue(CamTools.Fetch).ToString(), true);
			}

			fileNode.Save(SettingsUrl);
		}

		public static void Load()
		{
			ConfigNode fileNode = ConfigNode.Load(SettingsUrl);
			ConfigNode settings = fileNode.GetNode("CToolsSettings");

			foreach(var field in typeof(CamTools).GetFields())
			{
				if(!field.IsDefined(typeof(CtPersistantField), false)) continue;

				if(settings.HasValue(field.Name))
				{
					object parsedValue = ParseValue(field.FieldType, settings.GetValue(field.Name));
					if(parsedValue != null)
					{
						field.SetValue(CamTools.Fetch, parsedValue);
					}
				}
			}
		}

		public static object ParseValue(Type type, string value)
		{
			if(type == typeof(string))
			{
				return value;
			}

			if(type == typeof(bool))
			{
				return bool.Parse(value);
			}
			else if(type.IsEnum)
			{
				return Enum.Parse(type, value);
			}
			else if(type == typeof(float))
			{
				return float.Parse(value);
			}
			else if(type == typeof(Single))
			{
				return Single.Parse(value);
			}


			UnityEngine.Debug.LogError("CameraTools failed to parse settings field of type "+type.ToString()+" and value "+value);

			return null;
		}
	}
}

