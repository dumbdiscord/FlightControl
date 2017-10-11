using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using SpaceEngineers.Game.ModAPI.Ingame;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text;
using System;
using VRage.Collections;
using VRage.Game.Components;
using VRage.Game.ModAPI.Ingame;
using VRage.Game.ObjectBuilders.Definitions;
using VRage.Game;
using VRageMath;

namespace IngameScript
{
    partial class Program
    {
        public class BinarySerializer : IDisposable
        {
            public byte[] buffer;
            public bool shouldAutoResize = true;
            public BinarySerializer(int bufferlength)
            {
                buffer = new byte[bufferlength];
            }
            public BinarySerializer(byte[] buffer)
            {
                this.buffer = buffer;
            }

            public long Position = 0;
            public byte[] Read(int length)
            {
                byte[] output = new byte[length];
                Array.Copy(buffer, Position, output, 0, length);
                Position += length;
                return output;
            }
            public byte[] ReadToEnd()
            {

                return Read(buffer.Length - (int)Position);
            }
            public void Write(byte[] input)
            {
                if (shouldAutoResize == true)
                {
                    if (input.Length + Position > buffer.Length - 1)
                    {
                        Array.Resize(ref buffer, buffer.Length + input.Length);
                    }
                }
                Array.Copy(input, 0, buffer, Position, input.Length);
                Position += input.Length;
            }
            public void Write(byte input)
            {
                Write(new byte[input]);
            }
            public Object Read(Type type)
            {
                if (type == typeof(int))
                {
                    return ReadInt();
                }
                else if (type == typeof(long))
                {
                    return ReadLong();
                }
                else if (type == typeof(string))
                {
                    return ReadString();
                }
                else if (type == typeof(Vector3D))
                {
                    return ReadVector3D();
                }
                else if (type == typeof(double))
                {
                    return ReadDouble();
                }
                else if (type == typeof(MatrixD))
                {
                    return ReadMatrixD();
                }
                return null;
            }
            public int ReadInt()
            {

                return BitConverter.ToInt32(Read(4), 0);
            }
            public void WriteInt(int val)
            {
                Write(BitConverter.GetBytes(val));
            }
            public double ReadDouble()
            {
                return BitConverter.ToDouble(Read(8), 0);
            }
            public void WriteDouble(double val)
            {
                Write(BitConverter.GetBytes(val));
            }
            public float ReadFloat()
            {
                return BitConverter.ToSingle(Read(4), 0);
            }
            public void WriteFloat(float val)
            {
                Write(BitConverter.GetBytes(val));
            }
            public Vector3D ReadVector3D()
            {
                return new Vector3D(ReadDouble(), ReadDouble(), ReadDouble());
            }
            public void WriteVector3D(Vector3D val)
            {
                WriteDouble(val.X);
                WriteDouble(val.Y);
                WriteDouble(val.Z);
            }
            public void WriteMatrixD(MatrixD val)
            {
                WriteDouble(val.M11);
                WriteDouble(val.M12);
                WriteDouble(val.M13);
                WriteDouble(val.M14);
                WriteDouble(val.M21);
                WriteDouble(val.M22);
                WriteDouble(val.M23);
                WriteDouble(val.M24);
                WriteDouble(val.M31);
                WriteDouble(val.M32);
                WriteDouble(val.M33);
                WriteDouble(val.M34);
                WriteDouble(val.M41);
                WriteDouble(val.M42);
                WriteDouble(val.M43);
                WriteDouble(val.M44);
            }
            public MatrixD ReadMatrixD()
            {
                return new MatrixD(
                    ReadDouble(),
                    ReadDouble(),
                    ReadDouble(),
                    ReadDouble(),
                    ReadDouble(),
                    ReadDouble(),
                    ReadDouble(),
                    ReadDouble(),
                    ReadDouble(),
                    ReadDouble(),
                    ReadDouble(),
                    ReadDouble(),
                    ReadDouble(),
                    ReadDouble(),
                    ReadDouble(),
                    ReadDouble()
                );
            }
            public void WriteString(string val)
            {
                var bytes = Encoding.UTF8.GetBytes(val);
                WriteInt(bytes.Length);
                Write(bytes);
            }
            public long ReadLong()
            {
                return BitConverter.ToInt64(Read(8), 0);
            }
            public void WriteLong(long val)
            {
                Write(BitConverter.GetBytes(val));
            }
            public string ReadString()
            {
                int length = ReadInt();
                return Encoding.UTF8.GetString(Read(length));

            }
            public void WriteBool(bool val)
            {
                Write((byte)(val ? 1:0));
            }
            public bool ReadBool()
            {
                return Read(1)[0]>0;
            }
            public void Dispose() { }
        }
    }
}
