/*
 * $Header: /cvsroot/remotetea/remotetea/src/org/acplt/oncrpc/XdrVoid.java,v 1.1.1.1 2003/08/13 12:03:41 haraldalbrecht Exp $
 *
 * Copyright (c) 1999, 2000
 * Lehrstuhl fuer Prozessleittechnik (PLT), RWTH Aachen
 * D-52064 Aachen, Germany.
 * All rights reserved.
 *
 * This library is free software; you can redistribute it and/or modify
 * it under the terms of the GNU Library General Public License as
 * published by the Free Software Foundation; either version 2 of the
 * License, or (at your option) any later version.
 *
 * This library is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU Library General Public License for more details.
 *
 * You should have received a copy of the GNU Library General Public
 * License along with this program (see the file COPYING.LIB for more
 * details); if not, write to the Free Software Foundation, Inc.,
 * 675 Mass Ave, Cambridge, MA 02139, USA.
 */

using System;
using System.Net;
using System.Text;

namespace NFSLibrary.Rpc
{
    /// <summary>Defines the abstract base class for all encoding XDR streams.</summary>
    /// <remarks>
    /// Defines the abstract base class for all encoding XDR streams. An encoding
    /// XDR stream receives data in the form of Java data types and writes it to
    /// a data sink (for instance, network or memory buffer) in the
    /// platform-independent XDR format.
    /// <para>Derived classes need to implement the
    /// <see cref="XdrEncodeInt(int)">XdrEncodeInt(int)</see>
    /// ,
    /// <see cref="XdrEncodeOpaque(byte[])">XdrEncodeOpaque(byte[])</see>
    /// and
    /// <see cref="XdrEncodeOpaque(byte[], int, int)">XdrEncodeOpaque(byte[], int, int)</see>
    /// methods to make this complete
    /// mess workable.</para>
    /// Converted to C# using the db4o Sharpen tool.
    /// </remarks>
    /// <version>$Revision: 1.2 $ $Date: 2003/08/14 13:48:33 $ $State: Exp $ $Locker:  $</version>
    /// <author>Harald Albrecht</author>
    /// <author>Jay Walters</author>
    public abstract class XdrEncodingStream
    {
        /// <summary>Begins encoding a new XDR record.</summary>
        /// <remarks>
        /// Begins encoding a new XDR record. This typically involves resetting this
        /// encoding XDR stream back into a known state.
        /// </remarks>
        /// <param name="receiverAddress">
        /// Indicates the receiver of the XDR data. This can
        /// be <code>null</code> for XDR streams connected permanently to a
        /// receiver (like in case of TCP/IP based XDR streams).
        /// </param>
        /// <param name="receiverPort">Port number of the receiver.</param>
        /// <exception cref="OncRpcException">if an ONC/RPC error occurs.</exception>
        /// <exception cref="System.IO.IOException">if an I/O error occurs.</exception>
        /// <exception cref="NFSLibrary.Rpc.OncRpcException"></exception>
        public virtual void BeginEncoding(IPAddress receiverAddress, int receiverPort
            )
        {
        }

        /// <summary>
        /// Flushes this encoding XDR stream and forces any buffered output bytes
        /// to be written out.
        /// </summary>
        /// <remarks>
        /// Flushes this encoding XDR stream and forces any buffered output bytes
        /// to be written out. The general contract of <code>endEncoding</code> is that
        /// calling it is an indication that the current record is finished and any
        /// bytes previously encoded should immediately be written to their intended
        /// destination.
        /// <para>The <code>endEncoding</code> method of <code>XdrEncodingStream</code>
        /// does nothing.</para>
        /// </remarks>
        /// <exception cref="OncRpcException">if an ONC/RPC error occurs.</exception>
        /// <exception cref="System.IO.IOException">if an I/O error occurs.</exception>
        /// <exception cref="NFSLibrary.Rpc.OncRpcException"></exception>
        public virtual void EndEncoding()
        {
        }

        /// <summary>
        /// Closes this encoding XDR stream and releases any system resources
        /// associated with this stream.
        /// </summary>
        /// <remarks>
        /// Closes this encoding XDR stream and releases any system resources
        /// associated with this stream. The general contract of <code>close</code>
        /// is that it closes the encoding XDR stream. A closed XDR stream cannot
        /// perform encoding operations and cannot be reopened.
        /// <para>The <code>close</code> method of <code>XdrEncodingStream</code>
        /// does nothing.</para>
        /// </remarks>
        /// <exception cref="OncRpcException">if an ONC/RPC error occurs.</exception>
        /// <exception cref="System.IO.IOException">if an I/O error occurs.</exception>
        /// <exception cref="NFSLibrary.Rpc.OncRpcException"></exception>
        public virtual void Close()
        {
        }

        /// <summary>
        /// Encodes (aka "serializes") a "XDR int" value and writes it down a
        /// XDR stream.
        /// </summary>
        /// <remarks>
        /// Encodes (aka "serializes") a "XDR int" value and writes it down a
        /// XDR stream. A XDR int is 32 bits wide -- the same width Java's "int"
        /// data type has. This method is one of the basic methods all other
        /// methods can rely on. Because it's so basic, derived classes have to
        /// implement it.
        /// </remarks>
        /// <param name="value">The int value to be encoded.</param>
        /// <exception cref="OncRpcException">if an ONC/RPC error occurs.</exception>
        /// <exception cref="System.IO.IOException">if an I/O error occurs.</exception>
        /// <exception cref="NFSLibrary.Rpc.OncRpcException"></exception>
        public abstract void XdrEncodeInt(int value);

        /// <summary>
        /// Encodes (aka "serializes") a XDR opaque value, which is represented
        /// by a vector of byte values, and starts at <code>offset</code> with a
        /// length of <code>length</code>.
        /// </summary>
        /// <remarks>
        /// Encodes (aka "serializes") a XDR opaque value, which is represented
        /// by a vector of byte values, and starts at <code>offset</code> with a
        /// length of <code>length</code>. Only the opaque value is encoded, but
        /// no length indication is preceeding the opaque value, so the receiver
        /// has to know how long the opaque value will be. The encoded data is
        /// always padded to be a multiple of four. If the given length is not a
        /// multiple of four, zero bytes will be used for padding.
        /// <para>Derived classes must ensure that the proper semantic is maintained.</para>
        /// </remarks>
        /// <param name="value">
        /// The opaque value to be encoded in the form of a series of
        /// bytes.
        /// </param>
        /// <param name="offset">Start offset in the data.</param>
        /// <param name="length">the number of bytes to encode.</param>
        /// <exception cref="OncRpcException">if an ONC/RPC error occurs.</exception>
        /// <exception cref="System.IO.IOException">if an I/O error occurs.</exception>
        /// <exception cref="NFSLibrary.Rpc.OncRpcException"></exception>
        public abstract void XdrEncodeOpaque(byte[] value, int offset, int length);

        /// <summary>
        /// Encodes (aka "serializes") a XDR opaque value, which is represented
        /// by a vector of byte values.
        /// </summary>
        /// <remarks>
        /// Encodes (aka "serializes") a XDR opaque value, which is represented
        /// by a vector of byte values. The length of the opaque value is written
        /// to the XDR stream, so the receiver does not need to know
        /// the exact length in advance. The encoded data is always padded to be
        /// a multiple of four to maintain XDR alignment.
        /// </remarks>
        /// <param name="value">
        /// The opaque value to be encoded in the form of a series of
        /// bytes.
        /// </param>
        /// <exception cref="OncRpcException">if an ONC/RPC error occurs.</exception>
        /// <exception cref="System.IO.IOException">if an I/O error occurs.</exception>
        /// <exception cref="NFSLibrary.Rpc.OncRpcException"></exception>
        public void XdrEncodeDynamicOpaque(byte[] value)
        {
            XdrEncodeInt(value.Length);
            XdrEncodeOpaque(value);
        }

        /// <summary>
        /// Encodes (aka "serializes") a XDR opaque value, which is represented
        /// by a vector of byte values.
        /// </summary>
        /// <remarks>
        /// Encodes (aka "serializes") a XDR opaque value, which is represented
        /// by a vector of byte values. Only the opaque value is encoded, but
        /// no length indication is preceeding the opaque value, so the receiver
        /// has to know how long the opaque value will be. The encoded data is
        /// always padded to be a multiple of four. If the length of the given byte
        /// vector is not a multiple of four, zero bytes will be used for padding.
        /// </remarks>
        /// <param name="value">
        /// The opaque value to be encoded in the form of a series of
        /// bytes.
        /// </param>
        /// <exception cref="OncRpcException">if an ONC/RPC error occurs.</exception>
        /// <exception cref="System.IO.IOException">if an I/O error occurs.</exception>
        /// <exception cref="NFSLibrary.Rpc.OncRpcException"></exception>
        public void XdrEncodeOpaque(byte[] value)
        {
            XdrEncodeOpaque(value, 0, value.Length);
        }

        /// <summary>
        /// Encodes (aka "serializes") a XDR opaque value, which is represented
        /// by a vector of byte values.
        /// </summary>
        /// <remarks>
        /// Encodes (aka "serializes") a XDR opaque value, which is represented
        /// by a vector of byte values. Only the opaque value is encoded, but
        /// no length indication is preceeding the opaque value, so the receiver
        /// has to know how long the opaque value will be. The encoded data is
        /// always padded to be a multiple of four. If the length of the given byte
        /// vector is not a multiple of four, zero bytes will be used for padding.
        /// </remarks>
        /// <param name="value">
        /// The opaque value to be encoded in the form of a series of
        /// bytes.
        /// </param>
        /// <param name="length">
        /// of vector to write. This parameter is used as a sanity
        /// check.
        /// </param>
        /// <exception cref="OncRpcException">if an ONC/RPC error occurs.</exception>
        /// <exception cref="System.IO.IOException">if an I/O error occurs.</exception>
        /// <exception cref="System.ArgumentException">
        /// if the length of the vector does not
        /// match the specified length.
        /// </exception>
        /// <exception cref="NFSLibrary.Rpc.OncRpcException"></exception>
        public void XdrEncodeOpaque(byte[] value, int length)
        {
            if (value.Length != length)
            {
                throw (new System.ArgumentException("array size does not match protocol specification"
                    ));
            }
            XdrEncodeOpaque(value, 0, value.Length);
        }

        /// <summary>
        /// Encodes (aka "serializes") a vector of bytes, which is nothing more
        /// than a series of octets (or 8 bits wide bytes), each packed into its
        /// very own 4 bytes (XDR int).
        /// </summary>
        /// <remarks>
        /// Encodes (aka "serializes") a vector of bytes, which is nothing more
        /// than a series of octets (or 8 bits wide bytes), each packed into its
        /// very own 4 bytes (XDR int). Byte vectors are encoded together with a
        /// preceeding length value. This way the receiver doesn't need to know
        /// the length of the vector in advance.
        /// </remarks>
        /// <param name="value">Byte vector to encode.</param>
        /// <exception cref="OncRpcException">if an ONC/RPC error occurs.</exception>
        /// <exception cref="System.IO.IOException">if an I/O error occurs.</exception>
        /// <exception cref="NFSLibrary.Rpc.OncRpcException"></exception>
        public void XdrEncodeByteVector(byte[] value)
        {
            int length = value.Length;
            // well, silly optimizations appear here...
            XdrEncodeInt(length);
            if (length != 0)
            {
                //
                // For speed reasons, we do sign extension here, but the higher bits
                // will be removed again when deserializing.
                //
                for (int i = 0; i < length; ++i)
                {
                    XdrEncodeInt((int)value[i]);
                }
            }
        }

        /// <summary>
        /// Encodes (aka "serializes") a vector of bytes, which is nothing more
        /// than a series of octets (or 8 bits wide bytes), each packed into its
        /// very own 4 bytes (XDR int).
        /// </summary>
        /// <remarks>
        /// Encodes (aka "serializes") a vector of bytes, which is nothing more
        /// than a series of octets (or 8 bits wide bytes), each packed into its
        /// very own 4 bytes (XDR int).
        /// </remarks>
        /// <param name="value">Byte vector to encode.</param>
        /// <param name="length">
        /// of vector to write. This parameter is used as a sanity
        /// check.
        /// </param>
        /// <exception cref="OncRpcException">if an ONC/RPC error occurs.</exception>
        /// <exception cref="System.IO.IOException">if an I/O error occurs.</exception>
        /// <exception cref="System.ArgumentException">
        /// if the length of the vector does not
        /// match the specified length.
        /// </exception>
        /// <exception cref="NFSLibrary.Rpc.OncRpcException"></exception>
        public void XdrEncodeByteFixedVector(byte[] value, int length)
        {
            if (value.Length != length)
            {
                throw (new System.ArgumentException("array size does not match protocol specification"
                    ));
            }
            if (length != 0)
            {
                //
                // For speed reasons, we do sign extension here, but the higher bits
                // will be removed again when deserializing.
                //
                for (int i = 0; i < length; ++i)
                {
                    XdrEncodeInt((int)value[i]);
                }
            }
        }

        /// <summary>Encodes (aka "serializes") a byte and write it down this XDR stream.</summary>
        /// <remarks>Encodes (aka "serializes") a byte and write it down this XDR stream.</remarks>
        /// <param name="value">Byte value to encode.</param>
        /// <exception cref="OncRpcException">if an ONC/RPC error occurs.</exception>
        /// <exception cref="System.IO.IOException">if an I/O error occurs.</exception>
        /// <exception cref="NFSLibrary.Rpc.OncRpcException"></exception>
        public void XdrEncodeByte(byte value)
        {
            //
            // For speed reasons, we do sign extension here, but the higher bits
            // will be removed again when deserializing.
            //
            XdrEncodeInt((int)value);
        }

        /// <summary>
        /// Encodes (aka "serializes") a short (which is a 16 bits wide quantity)
        /// and write it down this XDR stream.
        /// </summary>
        /// <remarks>
        /// Encodes (aka "serializes") a short (which is a 16 bits wide quantity)
        /// and write it down this XDR stream.
        /// </remarks>
        /// <param name="value">Short value to encode.</param>
        /// <exception cref="OncRpcException">if an ONC/RPC error occurs.</exception>
        /// <exception cref="System.IO.IOException">if an I/O error occurs.</exception>
        /// <exception cref="NFSLibrary.Rpc.OncRpcException"></exception>
        public void XdrEncodeShort(short value)
        {
            XdrEncodeInt((int)value);
        }

        /// <summary>
        /// Encodes (aka "serializes") a long (which is called a "hyper" in XDR
        /// babble and is 64 bits wide) and write it down this XDR stream.
        /// </summary>
        /// <remarks>
        /// Encodes (aka "serializes") a long (which is called a "hyper" in XDR
        /// babble and is 64 bits wide) and write it down this XDR stream.
        /// </remarks>
        /// <param name="value">Long value to encode.</param>
        /// <exception cref="OncRpcException">if an ONC/RPC error occurs.</exception>
        /// <exception cref="System.IO.IOException">if an I/O error occurs.</exception>
        /// <exception cref="NFSLibrary.Rpc.OncRpcException"></exception>
        public void XdrEncodeLong(long value)
        {
            //
            // Just encode the long (which is called a "hyper" in XDR babble) as
            // two ints in network order, that is: big endian with the high int
            // comming first.
            //
            XdrEncodeInt((int)((value) >> 32) & unchecked((int)(0xffffffff)));
            XdrEncodeInt((int)(value & unchecked((int)(0xffffffff))));
        }

        /// <summary>
        /// Encodes (aka "serializes") a float (which is a 32 bits wide floating
        /// point quantity) and write it down this XDR stream.
        /// </summary>
        /// <remarks>
        /// Encodes (aka "serializes") a float (which is a 32 bits wide floating
        /// point quantity) and write it down this XDR stream.
        /// </remarks>
        /// <param name="value">Float value to encode.</param>
        /// <exception cref="OncRpcException">if an ONC/RPC error occurs.</exception>
        /// <exception cref="System.IO.IOException">if an I/O error occurs.</exception>
        /// <exception cref="NFSLibrary.Rpc.OncRpcException"></exception>
        public void XdrEncodeFloat(float value)
        {
            XdrEncodeInt(BitConverter.ToInt32(BitConverter.GetBytes(value), 0));
        }

        /// <summary>
        /// Encodes (aka "serializes") a double (which is a 64 bits wide floating
        /// point quantity) and write it down this XDR stream.
        /// </summary>
        /// <remarks>
        /// Encodes (aka "serializes") a double (which is a 64 bits wide floating
        /// point quantity) and write it down this XDR stream.
        /// </remarks>
        /// <param name="value">Double value to encode.</param>
        /// <exception cref="OncRpcException">if an ONC/RPC error occurs.</exception>
        /// <exception cref="System.IO.IOException">if an I/O error occurs.</exception>
        /// <exception cref="NFSLibrary.Rpc.OncRpcException"></exception>
        public void XdrEncodeDouble(double value)
        {
            XdrEncodeLong(BitConverter.DoubleToInt64Bits(value));
        }

        /// <summary>Encodes (aka "serializes") a boolean and writes it down this XDR stream.
        /// 	</summary>
        /// <remarks>Encodes (aka "serializes") a boolean and writes it down this XDR stream.
        /// 	</remarks>
        /// <param name="value">Boolean value to be encoded.</param>
        /// <exception cref="OncRpcException">if an ONC/RPC error occurs.</exception>
        /// <exception cref="System.IO.IOException">if an I/O error occurs.</exception>
        /// <exception cref="NFSLibrary.Rpc.OncRpcException"></exception>
        public void XdrEncodeBoolean(bool value)
        {
            XdrEncodeInt(value ? 1 : 0);
        }

        internal byte[] StrToByteArray(string str)
        {
            System.Text.ASCIIEncoding encoding = new System.Text.ASCIIEncoding();
            return StrToByteArray(str, encoding);
        }

        internal byte[] StrToByteArray(string str, Encoding encoding)
        {
            return encoding.GetBytes(str);
        }

        /// <summary>Encodes (aka "serializes") a string and writes it down this XDR stream.</summary>
        /// <remarks>Encodes (aka "serializes") a string and writes it down this XDR stream.</remarks>
        /// <param name="value">String value to be encoded.</param>
        /// <exception cref="OncRpcException">if an ONC/RPC error occurs.</exception>
        /// <exception cref="System.IO.IOException">if an I/O error occurs.</exception>
        /// <exception cref="NFSLibrary.Rpc.OncRpcException"></exception>
        public void XdrEncodeString(string value)
        {
            if (characterEncoding != null)
            {
                XdrEncodeDynamicOpaque(StrToByteArray(value, Encoding.GetEncoding(characterEncoding)
                    ));
            }
            else
            {
                XdrEncodeDynamicOpaque(StrToByteArray(value));
            }
        }

        /// <summary>
        /// Encodes (aka "serializes") a vector of short integers and writes it down
        /// this XDR stream.
        /// </summary>
        /// <remarks>
        /// Encodes (aka "serializes") a vector of short integers and writes it down
        /// this XDR stream.
        /// </remarks>
        /// <param name="value">short vector to be encoded.</param>
        /// <exception cref="OncRpcException">if an ONC/RPC error occurs.</exception>
        /// <exception cref="System.IO.IOException">if an I/O error occurs.</exception>
        /// <exception cref="NFSLibrary.Rpc.OncRpcException"></exception>
        public void XdrEncodeShortVector(short[] value)
        {
            int size = value.Length;
            XdrEncodeInt(size);
            for (int i = 0; i < size; i++)
            {
                XdrEncodeShort(value[i]);
            }
        }

        /// <summary>
        /// Encodes (aka "serializes") a vector of short integers and writes it down
        /// this XDR stream.
        /// </summary>
        /// <remarks>
        /// Encodes (aka "serializes") a vector of short integers and writes it down
        /// this XDR stream.
        /// </remarks>
        /// <param name="value">short vector to be encoded.</param>
        /// <param name="length">
        /// of vector to write. This parameter is used as a sanity
        /// check.
        /// </param>
        /// <exception cref="OncRpcException">if an ONC/RPC error occurs.</exception>
        /// <exception cref="System.IO.IOException">if an I/O error occurs.</exception>
        /// <exception cref="System.ArgumentException">
        /// if the length of the vector does not
        /// match the specified length.
        /// </exception>
        /// <exception cref="NFSLibrary.Rpc.OncRpcException"></exception>
        public void XdrEncodeShortFixedVector(short[] value, int length)
        {
            if (value.Length != length)
            {
                throw (new System.ArgumentException("array size does not match protocol specification"
                    ));
            }
            for (int i = 0; i < length; i++)
            {
                XdrEncodeShort(value[i]);
            }
        }

        /// <summary>
        /// Encodes (aka "serializes") a vector of ints and writes it down
        /// this XDR stream.
        /// </summary>
        /// <remarks>
        /// Encodes (aka "serializes") a vector of ints and writes it down
        /// this XDR stream.
        /// </remarks>
        /// <param name="value">int vector to be encoded.</param>
        /// <exception cref="OncRpcException">if an ONC/RPC error occurs.</exception>
        /// <exception cref="System.IO.IOException">if an I/O error occurs.</exception>
        /// <exception cref="NFSLibrary.Rpc.OncRpcException"></exception>
        public void XdrEncodeIntVector(int[] value)
        {
            int size = value.Length;
            XdrEncodeInt(size);
            for (int i = 0; i < size; i++)
            {
                XdrEncodeInt(value[i]);
            }
        }

        /// <summary>
        /// Encodes (aka "serializes") a vector of ints and writes it down
        /// this XDR stream.
        /// </summary>
        /// <remarks>
        /// Encodes (aka "serializes") a vector of ints and writes it down
        /// this XDR stream.
        /// </remarks>
        /// <param name="value">int vector to be encoded.</param>
        /// <param name="length">
        /// of vector to write. This parameter is used as a sanity
        /// check.
        /// </param>
        /// <exception cref="OncRpcException">if an ONC/RPC error occurs.</exception>
        /// <exception cref="System.IO.IOException">if an I/O error occurs.</exception>
        /// <exception cref="System.ArgumentException">
        /// if the length of the vector does not
        /// match the specified length.
        /// </exception>
        /// <exception cref="NFSLibrary.Rpc.OncRpcException"></exception>
        public void XdrEncodeIntFixedVector(int[] value, int length)
        {
            if (value.Length != length)
            {
                throw (new System.ArgumentException("array size does not match protocol specification"
                    ));
            }
            for (int i = 0; i < length; i++)
            {
                XdrEncodeInt(value[i]);
            }
        }

        /// <summary>
        /// Encodes (aka "serializes") a vector of long integers and writes it down
        /// this XDR stream.
        /// </summary>
        /// <remarks>
        /// Encodes (aka "serializes") a vector of long integers and writes it down
        /// this XDR stream.
        /// </remarks>
        /// <param name="value">long vector to be encoded.</param>
        /// <exception cref="OncRpcException">if an ONC/RPC error occurs.</exception>
        /// <exception cref="System.IO.IOException">if an I/O error occurs.</exception>
        /// <exception cref="NFSLibrary.Rpc.OncRpcException"></exception>
        public void XdrEncodeLongVector(long[] value)
        {
            int size = value.Length;
            XdrEncodeInt(size);
            for (int i = 0; i < size; i++)
            {
                XdrEncodeLong(value[i]);
            }
        }

        /// <summary>
        /// Encodes (aka "serializes") a vector of long integers and writes it down
        /// this XDR stream.
        /// </summary>
        /// <remarks>
        /// Encodes (aka "serializes") a vector of long integers and writes it down
        /// this XDR stream.
        /// </remarks>
        /// <param name="value">long vector to be encoded.</param>
        /// <param name="length">
        /// of vector to write. This parameter is used as a sanity
        /// check.
        /// </param>
        /// <exception cref="OncRpcException">if an ONC/RPC error occurs.</exception>
        /// <exception cref="System.IO.IOException">if an I/O error occurs.</exception>
        /// <exception cref="System.ArgumentException">
        /// if the length of the vector does not
        /// match the specified length.
        /// </exception>
        /// <exception cref="NFSLibrary.Rpc.OncRpcException"></exception>
        public void XdrEncodeLongFixedVector(long[] value, int length)
        {
            if (value.Length != length)
            {
                throw (new System.ArgumentException("array size does not match protocol specification"
                    ));
            }
            for (int i = 0; i < length; i++)
            {
                XdrEncodeLong(value[i]);
            }
        }

        /// <summary>
        /// Encodes (aka "serializes") a vector of floats and writes it down
        /// this XDR stream.
        /// </summary>
        /// <remarks>
        /// Encodes (aka "serializes") a vector of floats and writes it down
        /// this XDR stream.
        /// </remarks>
        /// <param name="value">float vector to be encoded.</param>
        /// <exception cref="OncRpcException">if an ONC/RPC error occurs.</exception>
        /// <exception cref="System.IO.IOException">if an I/O error occurs.</exception>
        /// <exception cref="NFSLibrary.Rpc.OncRpcException"></exception>
        public void XdrEncodeFloatVector(float[] value)
        {
            int size = value.Length;
            XdrEncodeInt(size);
            for (int i = 0; i < size; i++)
            {
                XdrEncodeFloat(value[i]);
            }
        }

        /// <summary>
        /// Encodes (aka "serializes") a vector of floats and writes it down
        /// this XDR stream.
        /// </summary>
        /// <remarks>
        /// Encodes (aka "serializes") a vector of floats and writes it down
        /// this XDR stream.
        /// </remarks>
        /// <param name="value">float vector to be encoded.</param>
        /// <param name="length">
        /// of vector to write. This parameter is used as a sanity
        /// check.
        /// </param>
        /// <exception cref="OncRpcException">if an ONC/RPC error occurs.</exception>
        /// <exception cref="System.IO.IOException">if an I/O error occurs.</exception>
        /// <exception cref="System.ArgumentException">
        /// if the length of the vector does not
        /// match the specified length.
        /// </exception>
        /// <exception cref="NFSLibrary.Rpc.OncRpcException"></exception>
        public void XdrEncodeFloatFixedVector(float[] value, int length)
        {
            if (value.Length != length)
            {
                throw (new System.ArgumentException("array size does not match protocol specification"
                    ));
            }
            for (int i = 0; i < length; i++)
            {
                XdrEncodeFloat(value[i]);
            }
        }

        /// <summary>
        /// Encodes (aka "serializes") a vector of doubles and writes it down
        /// this XDR stream.
        /// </summary>
        /// <remarks>
        /// Encodes (aka "serializes") a vector of doubles and writes it down
        /// this XDR stream.
        /// </remarks>
        /// <param name="value">double vector to be encoded.</param>
        /// <exception cref="OncRpcException">if an ONC/RPC error occurs.</exception>
        /// <exception cref="System.IO.IOException">if an I/O error occurs.</exception>
        /// <exception cref="NFSLibrary.Rpc.OncRpcException"></exception>
        public void XdrEncodeDoubleVector(double[] value)
        {
            int size = value.Length;
            XdrEncodeInt(size);
            for (int i = 0; i < size; i++)
            {
                XdrEncodeDouble(value[i]);
            }
        }

        /// <summary>
        /// Encodes (aka "serializes") a vector of doubles and writes it down
        /// this XDR stream.
        /// </summary>
        /// <remarks>
        /// Encodes (aka "serializes") a vector of doubles and writes it down
        /// this XDR stream.
        /// </remarks>
        /// <param name="value">double vector to be encoded.</param>
        /// <param name="length">
        /// of vector to write. This parameter is used as a sanity
        /// check.
        /// </param>
        /// <exception cref="OncRpcException">if an ONC/RPC error occurs.</exception>
        /// <exception cref="System.IO.IOException">if an I/O error occurs.</exception>
        /// <exception cref="System.ArgumentException">
        /// if the length of the vector does not
        /// match the specified length.
        /// </exception>
        /// <exception cref="NFSLibrary.Rpc.OncRpcException"></exception>
        public void XdrEncodeDoubleFixedVector(double[] value, int length)
        {
            if (value.Length != length)
            {
                throw (new System.ArgumentException("array size does not match protocol specification"
                    ));
            }
            for (int i = 0; i < length; i++)
            {
                XdrEncodeDouble(value[i]);
            }
        }

        /// <summary>
        /// Encodes (aka "serializes") a vector of booleans and writes it down
        /// this XDR stream.
        /// </summary>
        /// <remarks>
        /// Encodes (aka "serializes") a vector of booleans and writes it down
        /// this XDR stream.
        /// </remarks>
        /// <param name="value">long vector to be encoded.</param>
        /// <exception cref="OncRpcException">if an ONC/RPC error occurs.</exception>
        /// <exception cref="System.IO.IOException">if an I/O error occurs.</exception>
        /// <exception cref="NFSLibrary.Rpc.OncRpcException"></exception>
        public void XdrEncodeBooleanVector(bool[] value)
        {
            int size = value.Length;
            XdrEncodeInt(size);
            for (int i = 0; i < size; i++)
            {
                XdrEncodeBoolean(value[i]);
            }
        }

        /// <summary>
        /// Encodes (aka "serializes") a vector of booleans and writes it down
        /// this XDR stream.
        /// </summary>
        /// <remarks>
        /// Encodes (aka "serializes") a vector of booleans and writes it down
        /// this XDR stream.
        /// </remarks>
        /// <param name="value">long vector to be encoded.</param>
        /// <param name="length">
        /// of vector to write. This parameter is used as a sanity
        /// check.
        /// </param>
        /// <exception cref="OncRpcException">if an ONC/RPC error occurs.</exception>
        /// <exception cref="System.IO.IOException">if an I/O error occurs.</exception>
        /// <exception cref="System.ArgumentException">
        /// if the length of the vector does not
        /// match the specified length.
        /// </exception>
        /// <exception cref="NFSLibrary.Rpc.OncRpcException"></exception>
        public void XdrEncodeBooleanFixedVector(bool[] value, int length)
        {
            if (value.Length != length)
            {
                throw (new System.ArgumentException("array size does not match protocol specification"
                    ));
            }
            for (int i = 0; i < length; i++)
            {
                XdrEncodeBoolean(value[i]);
            }
        }

        /// <summary>
        /// Encodes (aka "serializes") a vector of strings and writes it down
        /// this XDR stream.
        /// </summary>
        /// <remarks>
        /// Encodes (aka "serializes") a vector of strings and writes it down
        /// this XDR stream.
        /// </remarks>
        /// <param name="value">String vector to be encoded.</param>
        /// <exception cref="OncRpcException">if an ONC/RPC error occurs.</exception>
        /// <exception cref="System.IO.IOException">if an I/O error occurs.</exception>
        /// <exception cref="NFSLibrary.Rpc.OncRpcException"></exception>
        public void XdrEncodeStringVector(string[] value)
        {
            int size = value.Length;
            XdrEncodeInt(size);
            for (int i = 0; i < size; i++)
            {
                XdrEncodeString(value[i]);
            }
        }

        /// <summary>
        /// Encodes (aka "serializes") a vector of strings and writes it down
        /// this XDR stream.
        /// </summary>
        /// <remarks>
        /// Encodes (aka "serializes") a vector of strings and writes it down
        /// this XDR stream.
        /// </remarks>
        /// <param name="value">String vector to be encoded.</param>
        /// <param name="length">
        /// of vector to write. This parameter is used as a sanity
        /// check.
        /// </param>
        /// <exception cref="OncRpcException">if an ONC/RPC error occurs.</exception>
        /// <exception cref="System.IO.IOException">if an I/O error occurs.</exception>
        /// <exception cref="System.ArgumentException">
        /// if the length of the vector does not
        /// match the specified length.
        /// </exception>
        /// <exception cref="NFSLibrary.Rpc.OncRpcException"></exception>
        public void XdrEncodeStringFixedVector(string[] value, int length)
        {
            if (value.Length != length)
            {
                throw (new System.ArgumentException("array size does not match protocol specification"
                    ));
            }
            for (int i = 0; i < length; i++)
            {
                XdrEncodeString(value[i]);
            }
        }

        /// <summary>Set the character encoding for serializing strings.</summary>
        /// <remarks>Set the character encoding for serializing strings.</remarks>
        /// <param name="characterEncoding">
        /// the encoding to use for serializing strings.
        /// If <code>null</code>, the system's default encoding is to be used.
        /// </param>
        public virtual void SetCharacterEncoding(string characterEncoding)
        {
            this.characterEncoding = characterEncoding;
        }

        /// <summary>Get the character encoding for serializing strings.</summary>
        /// <remarks>Get the character encoding for serializing strings.</remarks>
        /// <returns>
        /// the encoding currently used for serializing strings.
        /// If <code>null</code>, then the system's default encoding is used.
        /// </returns>
        public virtual string GetCharacterEncoding()
        {
            return characterEncoding;
        }

        /// <summary>
        /// Encoding to use when serializing strings or <code>null</code> if
        /// the system's default encoding should be used.
        /// </summary>
        /// <remarks>
        /// Encoding to use when serializing strings or <code>null</code> if
        /// the system's default encoding should be used.
        /// </remarks>
        private string characterEncoding = null;
    }
}