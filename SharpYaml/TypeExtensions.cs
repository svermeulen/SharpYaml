﻿// Copyright (c) 2015 SharpYaml - Alexandre Mutel
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
// 
// -------------------------------------------------------------------------------
// SharpYaml is a fork of YamlDotNet https://github.com/aaubry/YamlDotNet
// published with the following license:
// -------------------------------------------------------------------------------
// 
// Copyright (c) 2008, 2009, 2010, 2011, 2012 Antoine Aubry
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy of
// this software and associated documentation files (the "Software"), to deal in
// the Software without restriction, including without limitation the rights to
// use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies
// of the Software, and to permit persons to whom the Software is furnished to do
// so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace SharpYaml
{
	internal static class TypeExtensions
	{
		private static Dictionary<Type, bool> anonymousTypes = new Dictionary<Type, bool>();

		public static bool HasInterface(this Type type, Type lookInterfaceType)
		{
			return type.GetInterface(lookInterfaceType) != null;
		}

	    public static bool ExtendsGeneric(this Type type, Type genericType)
	    {
	        if (genericType == null) throw new ArgumentNullException("genericType");
            if (!genericType.IsGenericTypeDefinition) throw new ArgumentException("Expecting a generic type definition", "genericType");

	        var nextType = type;
	        while (nextType != null)
	        {
                var checkType = nextType.IsGenericType ? nextType.GetGenericTypeDefinition() : nextType;
                if (checkType == genericType)
	            {
	                return true;
	            }
	            nextType = nextType.BaseType;
	        }
	        return false;
	    }

	    public static Type GetInterface(this Type type, Type lookInterfaceType)
		{
			if (type == null)
				throw new ArgumentNullException("type");
			if (lookInterfaceType == null)
				throw new ArgumentNullException("lookInterfaceType");

			if (lookInterfaceType.IsGenericTypeDefinition)
			{
				if (lookInterfaceType.IsInterface)
					foreach (var interfaceType in type.GetInterfaces())
						if (interfaceType.IsGenericType
							&& interfaceType.GetGenericTypeDefinition() == lookInterfaceType)
							return interfaceType;

				for (Type t = type; t != null; t = t.BaseType)
					if (t.IsGenericType && t.GetGenericTypeDefinition() == lookInterfaceType)
						return t;
			}
			else
			{
				if (lookInterfaceType.IsAssignableFrom(type))
					return lookInterfaceType;
			}

			return null;
		}

		/// <summary>
		/// Gets the assembly qualified name of the type, but without the assembly version or public token.
		/// </summary>
		/// <param name="type">The type.</param>
		/// <returns>The assembly qualified name of the type, but without the assembly version or public token.</returns>
		/// <exception cref="System.InvalidOperationException">Unable to get an assembly qualified name for type [{0}].DoFormat(type)</exception>
		public static string GetShortAssemblyQualifiedName(this Type type)
		{
			var typeName = type.AssemblyQualifiedName;
			if (typeName == null)
			{
				throw new InvalidOperationException("Unable to get an assembly qualified name for type [{0}]".DoFormat(type));
			}

			var indexAfterType = typeName.IndexOf(',');
			if (indexAfterType >= 0)
			{
				var indexAfterAssembly = typeName.IndexOf(',', indexAfterType + 1);
				if (indexAfterAssembly >= 0)
				{
					typeName = typeName.Substring(0, indexAfterAssembly).Replace(" ", string.Empty);
				}
			}
			return typeName;
		}

		/// <summary>
		/// Determines whether the specified type is an anonymous type.
		/// </summary>
		/// <param name="type">The type.</param>
		/// <returns><c>true</c> if the specified type is anonymous; otherwise, <c>false</c>.</returns>
		public static bool IsAnonymous(this Type type)
		{
			if (type == null)
				return false;

			lock (anonymousTypes)
			{
				bool isAnonymous;
				if (anonymousTypes.TryGetValue(type, out isAnonymous))
					return isAnonymous;

				isAnonymous = type.GetCustomAttributes(typeof (CompilerGeneratedAttribute), false).Length > 0
							  && type.Namespace == null
							  && type.FullName.Contains("AnonymousType");

				anonymousTypes.Add(type, isAnonymous);
				return isAnonymous;
			}
		}

		/// <summary>
		/// Determines whether the specified type is nullable <see cref="Nullable{T}"/>.
		/// </summary>
		/// <param name="type">The type.</param>
		/// <returns><c>true</c> if the specified type is nullable; otherwise, <c>false</c>.</returns>
		public static bool IsNullable(this Type type)
		{
			return Nullable.GetUnderlyingType(type) != null;			
		}

		/// <summary>
		/// Check if the type is a ValueType and does not contain any non ValueType members.
		/// </summary>
		/// <param name="type"></param>
		/// <returns></returns>
		public static bool IsPureValueType(this Type type)
		{
			if (type == null)
				return false;
			if (type == typeof(IntPtr))
				return false;
			if (type.IsPrimitive)
				return true;
			if (type.IsEnum)
				return true;
			if (!type.IsValueType)
				return false;
			// struct
			foreach (var f in type.GetFields(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance))
				if (!IsPureValueType(f.FieldType))
					return false;
			return true;
		}

		/// <summary>
		/// Returnes true if the specified <paramref name="type"/> is a struct type.
		/// </summary>
		/// <param name="type"><see cref="Type"/> to be analyzed.</param>
		/// <returns>true if the specified <paramref name="type"/> is a struct type; otehrwise false.</returns>
		public static bool IsStruct(this Type type)
		{
			return type != null && type.IsValueType && !type.IsPrimitive;
		}

		/// <summary>
		/// Return if an object is a numeric value.
		/// </summary>
		/// <param name="type">The type.</param>
		/// <returns>True if object is a numeric value.</returns>
		public static bool IsNumeric(this Type type)
		{
			return type != null && (type == typeof(sbyte) || type == typeof(short) || type == typeof(int) || type == typeof(long) ||
				   type == typeof(byte) || type == typeof(ushort) || type == typeof(uint) || type == typeof(ulong) ||
				   type == typeof(float) || type == typeof(double) || type == typeof(decimal));
		}

		/// <summary>
		/// Compare two objects to see if they are equal or not. Null is acceptable.
		/// </summary>
		/// <param name="a"></param>
		/// <param name="b"></param>
		/// <returns></returns>
		public static bool AreEqual(object a, object b)
		{
			if (a == null)
				return b == null;
			if (b == null)
				return false;
			return a.Equals(b) || b.Equals(a);
		}

		/// <summary>
		/// Cast an object to a specified numeric type.
		/// </summary>
		/// <param name="obj">Any object</param>
		/// <param name="type">Numric type</param>
		/// <returns>Numeric value or null if the object is not a numeric value.</returns>
		public static object CastToNumericType(this Type type, object obj)
		{
			var doubleValue = CastToDouble(obj);
			if (double.IsNaN(doubleValue))
				return null;

			if (obj is decimal && type == typeof(decimal))
				return obj; // do not convert into double

			object result = null;
			if (type == typeof(sbyte))
				result = (sbyte)doubleValue;
			if (type == typeof(byte))
				result = (byte)doubleValue;
			if (type == typeof(short))
				result = (short)doubleValue;
			if (type == typeof(ushort))
				result = (ushort)doubleValue;
			if (type == typeof(int))
				result = (int)doubleValue;
			if (type == typeof(uint))
				result = (uint)doubleValue;
			if (type == typeof(long))
				result = (long)doubleValue;
			if (type == typeof(ulong))
				result = (ulong)doubleValue;
			if (type == typeof(float))
				result = (float)doubleValue;
			if (type == typeof(double))
				result = doubleValue;
			if (type == typeof(decimal))
				result = (decimal)doubleValue;
			return result;
		}

		/// <summary>
		/// Cast boxed numeric value to double
		/// </summary>
		/// <param name="obj">boxed numeric value</param>
		/// <returns>Numeric value in double. Double.Nan if obj is not a numeric value.</returns>
		public static double CastToDouble(object obj)
		{
			var result = double.NaN;
			var type = obj != null ? obj.GetType() : null;
			if (type == typeof(sbyte))
				result = (double)(sbyte)obj;
			if (type == typeof(byte))
				result = (double)(byte)obj;
			if (type == typeof(short))
				result = (double)(short)obj;
			if (type == typeof(ushort))
				result = (double)(ushort)obj;
			if (type == typeof(int))
				result = (double)(int)obj;
			if (type == typeof(uint))
				result = (double)(uint)obj;
			if (type == typeof(long))
				result = (double)(long)obj;
			if (type == typeof(ulong))
				result = (double)(ulong)obj;
			if (type == typeof(float))
				result = (double)(float)obj;
			if (type == typeof(double))
				result = (double)obj;
			if (type == typeof(decimal))
				result = (double)(decimal)obj;
			return result;
		}
	}
}