using UnityEngine;
using System.Collections;
using System;
using System.IO;
using System.Reflection;

namespace Sabresaurus.Sidekick
{
    [System.Serializable]
    public class VariableMetaData
    {
        // Enum
        string[] enumNames;
        int[] enumValues;

        // Unity Object Reference
        Type localModeType; // Only use in local mode

        string typeFullName;
        string assemblyName;
        string valueDisplayName;

        public string[] EnumNames
        {
            get
            {
                return enumNames;
            }
        }

        public int[] EnumValues
        {
            get
            {
                return enumValues;
            }
        }

        public Type LocalModeType
        {
            get
            {
                return localModeType;
            }
        }

        public string TypeFullName
        {
            get
            {
                return typeFullName;
            }
        }

        public string AssemblyName
        {
            get
            {
                return assemblyName;
            }
        }

        public string ValueDisplayName
        {
            get
            {
                return valueDisplayName;
            }
        }

        public VariableMetaData()
        {

        }

        public VariableMetaData(BinaryReader br, DataType dataType)
        {
            if (dataType == DataType.Enum || dataType == DataType.UnityObjectReference)
            {
                typeFullName = br.ReadString();
                assemblyName = br.ReadString();
            }

            if (dataType == DataType.Enum)
            {
                int enumNameCount = br.ReadInt32();
                enumNames = new string[enumNameCount];
                enumValues = new int[enumNameCount];
                for (int i = 0; i < enumNameCount; i++)
                {
                    enumNames[i] = br.ReadString();
                }
                for (int i = 0; i < enumNameCount; i++)
                {
                    enumValues[i] = br.ReadInt32();
                }
            }
            else if (dataType == DataType.UnityObjectReference)
            {
                valueDisplayName = br.ReadString();
            }
        }

        public void Write(BinaryWriter bw, DataType dataType)
        {
            if(dataType == DataType.Enum || dataType == DataType.UnityObjectReference)
            {
                bw.Write(typeFullName);
                bw.Write(assemblyName);
            }

            if (dataType == DataType.Enum)
            {
                bw.Write(enumNames.Length);
                for (int i = 0; i < enumNames.Length; i++)
                {
                    bw.Write(enumNames[i]);
                }
                for (int i = 0; i < enumNames.Length; i++)
                {
                    bw.Write(enumValues[i]);
                }
            }
            else if (dataType == DataType.UnityObjectReference)
            {
                
                bw.Write(valueDisplayName);
            }
        }

        public static VariableMetaData Create(DataType dataType, Type type, object value, VariableAttributes attributes)
        {
            if (dataType == DataType.Enum)
            {
                return CreateFromEnum(type, value as UnityEngine.Object, attributes);
            }
            else if (dataType == DataType.UnityObjectReference)
            {
                return CreateFromUnityObject(type, value as UnityEngine.Object, attributes);
            }
            else
            {
                return null;
            }
        }

        private void ReadTypeMetaData(Type type)
        {
            typeFullName = type.FullName;
            assemblyName = type.Assembly.FullName;
        }

        public Type GetTypeFromMetaData()
        {
            Type type = Assembly.Load(AssemblyName).GetType(TypeFullName);
            return type;
        }

        private static VariableMetaData CreateFromEnum(Type elementType, UnityEngine.Object value, VariableAttributes attributes)
        {
            VariableMetaData metaData = new VariableMetaData();
            metaData.ReadTypeMetaData(elementType);
            metaData.enumNames = Enum.GetNames(elementType);
            metaData.enumValues = new int[metaData.enumNames.Length];
            Array enumValuesArray = Enum.GetValues(elementType);
            for (int i = 0; i < metaData.enumNames.Length; i++)
            {
                metaData.enumValues[i] = (int)enumValuesArray.GetValue(i);
            }
            return metaData;
        }

        private static VariableMetaData CreateFromUnityObject(Type elementType, UnityEngine.Object value, VariableAttributes attributes)
        {
            VariableMetaData metaData = new VariableMetaData();
            metaData.ReadTypeMetaData(elementType);
            if (value != null)
            {
                if (attributes.HasFlagByte(VariableAttributes.IsArray))
                    metaData.valueDisplayName = "Array";
                else if (attributes.HasFlagByte(VariableAttributes.IsList))
                    metaData.valueDisplayName = "List";
                else
                    metaData.valueDisplayName = (value).name;
            }
            else
            {
                metaData.valueDisplayName = "null";
            }
            metaData.localModeType = elementType;
            return metaData;
        }
    }
}