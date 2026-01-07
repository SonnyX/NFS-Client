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

using System.Net;

namespace NFSLibrary.Rpc.Server
{
    /// <summary>
    /// Objects of class <code>OncRpcCallInformation</code> contain information
    /// about individual ONC/RPC calls.
    /// </summary>
    /// <remarks>
    /// Objects of class <code>OncRpcCallInformation</code> contain information
    /// about individual ONC/RPC calls. They are given to ONC/RPC
    /// <see cref="OncRpcDispatchable">call dispatchers</see>
    /// ,
    /// so they can send back the reply to the appropriate caller, etc. Use only
    /// this call info objects to retrieve call parameters and send back replies
    /// as in the future UDP/IP-based transports may become multi-threaded handling.
    /// The call info object is responsible to control access to the underlaying
    /// transport, so never mess with the transport directly.
    /// <para>Note that this class provides two different patterns for accessing
    /// parameters sent by clients within the ONC/RPC call and sending back replies.</para>
    /// <list type="number">
    /// <item>The convenient high-level access:
    /// <list type="bullet">
    /// <item>Use
    /// <see cref="retrieveCall(NFSLibrary.Rpc.XdrAble)">retrieveCall(NFSLibrary.Rpc.XdrAble)
    /// 	</see>
    /// to retrieve the parameters of
    /// the call and deserialize it into a paramter object.</item>
    /// <item>Use
    /// <see cref="reply(NFSLibrary.Rpc.XdrAble)">reply(NFSLibrary.Rpc.XdrAble)</see>
    /// to send back the reply by serializing
    /// a reply/result object. Or use the <code>failXXX</code> methods to send back
    /// an error indication instead.</item>
    /// </list>
    /// </item>
    /// <item>The lower-level access, giving more control over how and when data
    /// is deserialized and serialized:
    /// <list type="bullet">
    /// <item>Use
    /// <see cref="getXdrDecodingStream()">getXdrDecodingStream()</see>
    /// to get a reference to the XDR
    /// stream from which you can deserialize the call's parameter.</item>
    /// <item>When you are finished deserializing, call
    /// <see cref="EndDecoding()">EndDecoding()</see>
    /// .</item>
    /// <item>To send back the reply/result, call
    /// <see cref="BeginEncoding(OncRpcServerReplyMessage)">BeginEncoding(OncRpcServerReplyMessage)
    /// 	</see>
    /// . Using the XDR stream returned
    /// by
    /// <see cref="getXdrEncodingStream()">getXdrEncodingStream()</see>
    /// serialize the reply/result. Finally finish
    /// the serializing step by calling
    /// <see cref="EndEncoding()">EndEncoding()</see>
    /// .</item>
    /// </list>
    /// </item>
    /// </list>
    /// <para>Converted to C# using the db4o Sharpen tool.</para>
    /// </remarks>
    /// <seealso cref="OncRpcDispatchable">OncRpcDispatchable</seealso>
    /// <version>$Revision: 1.3 $ $Date: 2003/08/14 11:26:50 $ $State: Exp $ $Locker:  $</version>
    /// <author>Harald Albrecht</author>
    /// <author>Jay Walters</author>
    public class OncRpcCallInformation
    {
        /// <summary>
        /// Create an <code>OncRpcCallInformation</code> object and associate it
        /// with a ONC/RPC server transport.
        /// </summary>
        /// <remarks>
        /// Create an <code>OncRpcCallInformation</code> object and associate it
        /// with a ONC/RPC server transport. Typically,
        /// <code>OncRpcCallInformation</code> objects are created by transports
        /// once before handling incoming calls using the same call info object.
        /// To support multithreaded handling of calls in the future (for UDP/IP),
        /// the transport is already divided from the call info.
        /// </remarks>
        /// <param name="transport">ONC/RPC server transport.</param>
        internal OncRpcCallInformation(NFSLibrary.Rpc.Server.OncRpcServerTransport transport
            )
        {
            this.transport = transport;
        }

        /// <summary>
        /// Contains the call message header from ONC/RPC identifying this
        /// particular call.
        /// </summary>
        /// <remarks>
        /// Contains the call message header from ONC/RPC identifying this
        /// particular call.
        /// </remarks>
        public NFSLibrary.Rpc.Server.OncRpcServerCallMessage callMessage = new NFSLibrary.Rpc.Server.OncRpcServerCallMessage
            ();

        /// <summary>
        /// Internet address of the peer from which we received an ONC/RPC call
        /// or whom we intend to call.
        /// </summary>
        /// <remarks>
        /// Internet address of the peer from which we received an ONC/RPC call
        /// or whom we intend to call.
        /// </remarks>
        public IPAddress peerAddress = null;

        /// <summary>
        /// Port number of the peer from which we received an ONC/RPC call or
        /// whom we intend to call.
        /// </summary>
        /// <remarks>
        /// Port number of the peer from which we received an ONC/RPC call or
        /// whom we intend to call.
        /// </remarks>
        public int peerPort = 0;

        /// <summary>
        /// Associated transport from which we receive the ONC/RPC call parameters
        /// and to which we serialize the ONC/RPC reply.
        /// </summary>
        /// <remarks>
        /// Associated transport from which we receive the ONC/RPC call parameters
        /// and to which we serialize the ONC/RPC reply. Never mess with this
        /// member or you might break all future extensions horribly -- but this
        /// warning probably only stimulates you...
        /// </remarks>
        internal NFSLibrary.Rpc.Server.OncRpcServerTransport transport;

        /// <summary>Retrieves the parameters sent within an ONC/RPC call message.</summary>
        /// <remarks>
        /// Retrieves the parameters sent within an ONC/RPC call message. It also
        /// makes sure that the deserialization process is properly finished after
        /// the call parameters have been retrieved.
        /// </remarks>
        /// <exception cref="NFSLibrary.Rpc.OncRpcException">
        /// if an ONC/RPC exception occurs, like the data
        /// could not be successfully deserialized.
        /// </exception>
        /// <exception cref="System.IO.IOException">
        /// if an I/O exception occurs, like transmission
        /// failures over the network, etc.
        /// </exception>
        public virtual void retrieveCall(NFSLibrary.Rpc.XdrAble call)
        {
            transport.retrieveCall(call);
        }

        /// <summary>
        /// Returns XDR stream which can be used for deserializing the parameters
        /// of this ONC/RPC call.
        /// </summary>
        /// <remarks>
        /// Returns XDR stream which can be used for deserializing the parameters
        /// of this ONC/RPC call. This method belongs to the lower-level access
        /// pattern when handling ONC/RPC calls.
        /// </remarks>
        /// <returns>Reference to decoding XDR stream.</returns>
        public virtual NFSLibrary.Rpc.XdrDecodingStream getXdrDecodingStream()
        {
            return transport.getXdrDecodingStream();
        }

        /// <summary>Finishes call parameter deserialization.</summary>
        /// <remarks>
        /// Finishes call parameter deserialization. Afterwards the XDR stream
        /// returned by
        /// <see cref="getXdrDecodingStream()">getXdrDecodingStream()</see>
        /// must not be used any more.
        /// This method belongs to the lower-level access pattern when handling
        /// ONC/RPC calls.
        /// </remarks>
        /// <exception cref="NFSLibrary.Rpc.OncRpcException">
        /// if an ONC/RPC exception occurs, like the data
        /// could not be successfully deserialized.
        /// </exception>
        /// <exception cref="System.IO.IOException">
        /// if an I/O exception occurs, like transmission
        /// failures over the network, etc.
        /// </exception>
        public virtual void EndDecoding()
        {
            transport.EndDecoding();
        }

        /// <summary>Begins the sending phase for ONC/RPC replies.</summary>
        /// <remarks>
        /// Begins the sending phase for ONC/RPC replies. After beginning sending
        /// you can serialize the reply/result (but only if the call was accepted, see
        /// <see cref="NFSLibrary.Rpc.OncRpcReplyMessage">NFSLibrary.Rpc.OncRpcReplyMessage
        /// 	</see>
        /// for details). The stream
        /// to use for serialization can be obtained using
        /// <see cref="getXdrEncodingStream()">getXdrEncodingStream()</see>
        /// .
        /// This method belongs to the lower-level access pattern when handling
        /// ONC/RPC calls.
        /// </remarks>
        /// <param name="state">ONC/RPC reply header indicating success or failure.</param>
        /// <exception cref="NFSLibrary.Rpc.OncRpcException">
        /// if an ONC/RPC exception occurs, like the data
        /// could not be successfully serialized.
        /// </exception>
        /// <exception cref="System.IO.IOException">
        /// if an I/O exception occurs, like transmission
        /// failures over the network, etc.
        /// </exception>
        public virtual void BeginEncoding(NFSLibrary.Rpc.Server.OncRpcServerReplyMessage
             state)
        {
            transport.BeginEncoding(this, state);
        }

        /// <summary>Begins the sending phase for accepted ONC/RPC replies.</summary>
        /// <remarks>
        /// Begins the sending phase for accepted ONC/RPC replies. After beginning
        /// sending you can serialize the result/reply. The stream
        /// to use for serialization can be obtained using
        /// <see cref="getXdrEncodingStream()">getXdrEncodingStream()</see>
        /// .
        /// This method belongs to the lower-level access pattern when handling
        /// ONC/RPC calls.
        /// </remarks>
        /// <exception cref="NFSLibrary.Rpc.OncRpcException">
        /// if an ONC/RPC exception occurs, like the data
        /// could not be successfully serialized.
        /// </exception>
        /// <exception cref="System.IO.IOException">
        /// if an I/O exception occurs, like transmission
        /// failures over the network, etc.
        /// </exception>
        public virtual void BeginEncoding()
        {
            transport.BeginEncoding(this, new NFSLibrary.Rpc.Server.OncRpcServerReplyMessage
                (callMessage, NFSLibrary.Rpc.OncRpcReplyStatus.ONCRPC_MSG_ACCEPTED, NFSLibrary.Rpc.OncRpcAcceptStatus
                .ONCRPC_SUCCESS, NFSLibrary.Rpc.OncRpcReplyMessage.UNUSED_PARAMETER, NFSLibrary.Rpc.OncRpcReplyMessage
                .UNUSED_PARAMETER, NFSLibrary.Rpc.OncRpcReplyMessage.UNUSED_PARAMETER, NFSLibrary.Rpc.OncRpcReplyMessage
                .UNUSED_PARAMETER));
        }

        /// <summary>
        /// Returns XDR stream which can be used for eserializing the reply
        /// to this ONC/RPC call.
        /// </summary>
        /// <remarks>
        /// Returns XDR stream which can be used for eserializing the reply
        /// to this ONC/RPC call. This method belongs to the lower-level access
        /// pattern when handling ONC/RPC calls.
        /// </remarks>
        /// <returns>Reference to enecoding XDR stream.</returns>
        public virtual NFSLibrary.Rpc.XdrEncodingStream getXdrEncodingStream()
        {
            return transport.getXdrEncodingStream();
        }

        /// <summary>Finishes encoding the reply to this ONC/RPC call.</summary>
        /// <remarks>
        /// Finishes encoding the reply to this ONC/RPC call. Afterwards you must
        /// not use the XDR stream returned by
        /// <see cref="getXdrEncodingStream()">getXdrEncodingStream()</see>
        /// any
        /// longer.
        /// </remarks>
        /// <exception cref="NFSLibrary.Rpc.OncRpcException">
        /// if an ONC/RPC exception occurs, like the data
        /// could not be successfully serialized.
        /// </exception>
        /// <exception cref="System.IO.IOException">
        /// if an I/O exception occurs, like transmission
        /// failures over the network, etc.
        /// </exception>
        public virtual void EndEncoding()
        {
            transport.EndEncoding();
        }

        /// <summary>Send back an ONC/RPC reply to the caller who sent in this call.</summary>
        /// <remarks>
        /// Send back an ONC/RPC reply to the caller who sent in this call. This is
        /// a low-level function and typically should not be used by call
        /// dispatchers. Instead use the other
        /// <see cref="reply(NFSLibrary.Rpc.XdrAble)">reply method</see>
        /// which just expects a serializable object to send back to the caller.
        /// </remarks>
        /// <param name="state">
        /// ONC/RPC reply message header indicating success or failure
        /// and containing associated state information.
        /// </param>
        /// <param name="reply">
        /// If not <code>null</code>, then this parameter references
        /// the reply to be serialized after the reply message header.
        /// </param>
        /// <exception cref="NFSLibrary.Rpc.OncRpcException">
        /// if an ONC/RPC exception occurs, like the data
        /// could not be successfully serialized.
        /// </exception>
        /// <exception cref="System.IO.IOException">
        /// if an I/O exception occurs, like transmission
        /// failures over the network, etc.
        /// </exception>
        /// <seealso cref="NFSLibrary.Rpc.OncRpcReplyMessage">NFSLibrary.Rpc.OncRpcReplyMessage
        /// 	</seealso>
        /// <seealso cref="OncRpcDispatchable">OncRpcDispatchable</seealso>
        public virtual void reply(NFSLibrary.Rpc.Server.OncRpcServerReplyMessage state,
            NFSLibrary.Rpc.XdrAble reply)
        {
            transport.reply(this, state, reply);
        }

        /// <summary>Send back an ONC/RPC reply to the caller who sent in this call.</summary>
        /// <remarks>
        /// Send back an ONC/RPC reply to the caller who sent in this call. This
        /// automatically sends an ONC/RPC reply header before the reply part,
        /// indicating success within the header.
        /// </remarks>
        /// <param name="rply">Reply body the ONC/RPC reply message.</param>
        /// <exception cref="NFSLibrary.Rpc.OncRpcException">
        /// if an ONC/RPC exception occurs, like the data
        /// could not be successfully serialized.
        /// </exception>
        /// <exception cref="System.IO.IOException">
        /// if an I/O exception occurs, like transmission
        /// failures over the network, etc.
        /// </exception>
        public virtual void reply(NFSLibrary.Rpc.XdrAble rply)
        {
            reply(new NFSLibrary.Rpc.Server.OncRpcServerReplyMessage(callMessage, NFSLibrary.Rpc.OncRpcReplyStatus
                .ONCRPC_MSG_ACCEPTED, NFSLibrary.Rpc.OncRpcAcceptStatus.ONCRPC_SUCCESS, NFSLibrary.Rpc.OncRpcReplyMessage
                .UNUSED_PARAMETER, NFSLibrary.Rpc.OncRpcReplyMessage.UNUSED_PARAMETER, NFSLibrary.Rpc.OncRpcReplyMessage
                .UNUSED_PARAMETER, NFSLibrary.Rpc.OncRpcReplyMessage.UNUSED_PARAMETER), rply);
        }

        /// <summary>
        /// Send back an ONC/RPC failure indication about invalid arguments to the
        /// caller who sent in this call.
        /// </summary>
        /// <remarks>
        /// Send back an ONC/RPC failure indication about invalid arguments to the
        /// caller who sent in this call.
        /// </remarks>
        /// <exception cref="NFSLibrary.Rpc.OncRpcException">
        /// if an ONC/RPC exception occurs, like the data
        /// could not be successfully serialized.
        /// </exception>
        /// <exception cref="System.IO.IOException">
        /// if an I/O exception occurs, like transmission
        /// failures over the network, etc.
        /// </exception>
        public virtual void failArgumentGarbage()
        {
            reply(new NFSLibrary.Rpc.Server.OncRpcServerReplyMessage(callMessage, NFSLibrary.Rpc.OncRpcReplyStatus
                .ONCRPC_MSG_ACCEPTED, NFSLibrary.Rpc.OncRpcAcceptStatus.ONCRPC_GARBAGE_ARGS, NFSLibrary.Rpc.OncRpcReplyMessage
                .UNUSED_PARAMETER, NFSLibrary.Rpc.OncRpcReplyMessage.UNUSED_PARAMETER, NFSLibrary.Rpc.OncRpcReplyMessage
                .UNUSED_PARAMETER, NFSLibrary.Rpc.OncRpcReplyMessage.UNUSED_PARAMETER), null);
        }

        /// <summary>
        /// Send back an ONC/RPC failure indication about an unavailable procedure
        /// call to the caller who sent in this call.
        /// </summary>
        /// <remarks>
        /// Send back an ONC/RPC failure indication about an unavailable procedure
        /// call to the caller who sent in this call.
        /// </remarks>
        /// <exception cref="NFSLibrary.Rpc.OncRpcException">
        /// if an ONC/RPC exception occurs, like the data
        /// could not be successfully serialized.
        /// </exception>
        /// <exception cref="System.IO.IOException">
        /// if an I/O exception occurs, like transmission
        /// failures over the network, etc.
        /// </exception>
        public virtual void failProcedureUnavailable()
        {
            reply(new NFSLibrary.Rpc.Server.OncRpcServerReplyMessage(callMessage, NFSLibrary.Rpc.OncRpcReplyStatus
                .ONCRPC_MSG_ACCEPTED, NFSLibrary.Rpc.OncRpcAcceptStatus.ONCRPC_PROC_UNAVAIL, NFSLibrary.Rpc.OncRpcReplyMessage
                .UNUSED_PARAMETER, NFSLibrary.Rpc.OncRpcReplyMessage.UNUSED_PARAMETER, NFSLibrary.Rpc.OncRpcReplyMessage
                .UNUSED_PARAMETER, NFSLibrary.Rpc.OncRpcReplyMessage.UNUSED_PARAMETER), null);
        }

        /// <summary>
        /// Send back an ONC/RPC failure indication about an unavailable program
        /// to the caller who sent in this call.
        /// </summary>
        /// <remarks>
        /// Send back an ONC/RPC failure indication about an unavailable program
        /// to the caller who sent in this call.
        /// </remarks>
        /// <exception cref="NFSLibrary.Rpc.OncRpcException">
        /// if an ONC/RPC exception occurs, like the data
        /// could not be successfully serialized.
        /// </exception>
        /// <exception cref="System.IO.IOException">
        /// if an I/O exception occurs, like transmission
        /// failures over the network, etc.
        /// </exception>
        public virtual void failProgramUnavailable()
        {
            reply(new NFSLibrary.Rpc.Server.OncRpcServerReplyMessage(callMessage, NFSLibrary.Rpc.OncRpcReplyStatus
                .ONCRPC_MSG_ACCEPTED, NFSLibrary.Rpc.OncRpcAcceptStatus.ONCRPC_PROG_UNAVAIL, NFSLibrary.Rpc.OncRpcReplyMessage
                .UNUSED_PARAMETER, NFSLibrary.Rpc.OncRpcReplyMessage.UNUSED_PARAMETER, NFSLibrary.Rpc.OncRpcReplyMessage
                .UNUSED_PARAMETER, NFSLibrary.Rpc.OncRpcReplyMessage.UNUSED_PARAMETER), null);
        }

        /// <summary>
        /// Send back an ONC/RPC failure indication about a program version mismatch
        /// to the caller who sent in this call.
        /// </summary>
        /// <remarks>
        /// Send back an ONC/RPC failure indication about a program version mismatch
        /// to the caller who sent in this call.
        /// </remarks>
        /// <param name="lowVersion">lowest supported program version.</param>
        /// <param name="highVersion">highest supported program version.</param>
        /// <exception cref="NFSLibrary.Rpc.OncRpcException">
        /// if an ONC/RPC exception occurs, like the data
        /// could not be successfully serialized.
        /// </exception>
        /// <exception cref="System.IO.IOException">
        /// if an I/O exception occurs, like transmission
        /// failures over the network, etc.
        /// </exception>
        public virtual void failProgramMismatch(int lowVersion, int highVersion)
        {
            reply(new NFSLibrary.Rpc.Server.OncRpcServerReplyMessage(callMessage, NFSLibrary.Rpc.OncRpcReplyStatus
                .ONCRPC_MSG_ACCEPTED, NFSLibrary.Rpc.OncRpcAcceptStatus.ONCRPC_PROG_MISMATCH,
                NFSLibrary.Rpc.OncRpcReplyMessage.UNUSED_PARAMETER, lowVersion, highVersion, NFSLibrary.Rpc.OncRpcReplyMessage
                .UNUSED_PARAMETER), null);
        }

        /// <summary>
        /// Send back an ONC/RPC failure indication about a system error
        /// to the caller who sent in this call.
        /// </summary>
        /// <remarks>
        /// Send back an ONC/RPC failure indication about a system error
        /// to the caller who sent in this call.
        /// </remarks>
        /// <exception cref="NFSLibrary.Rpc.OncRpcException">
        /// if an ONC/RPC exception occurs, like the data
        /// could not be successfully serialized.
        /// </exception>
        /// <exception cref="System.IO.IOException">
        /// if an I/O exception occurs, like transmission
        /// failures over the network, etc.
        /// </exception>
        public virtual void failSystemError()
        {
            reply(new NFSLibrary.Rpc.Server.OncRpcServerReplyMessage(callMessage, NFSLibrary.Rpc.OncRpcReplyStatus
                .ONCRPC_MSG_ACCEPTED, NFSLibrary.Rpc.OncRpcAcceptStatus.ONCRPC_SYSTEM_ERR, NFSLibrary.Rpc.OncRpcReplyMessage
                .UNUSED_PARAMETER, NFSLibrary.Rpc.OncRpcReplyMessage.UNUSED_PARAMETER, NFSLibrary.Rpc.OncRpcReplyMessage
                .UNUSED_PARAMETER, NFSLibrary.Rpc.OncRpcReplyMessage.UNUSED_PARAMETER), null);
        }

        /// <summary>
        /// Send back an ONC/RPC failure indication about a ONC/RPC version mismatch
        /// call to the caller who sent in this call.
        /// </summary>
        /// <remarks>
        /// Send back an ONC/RPC failure indication about a ONC/RPC version mismatch
        /// call to the caller who sent in this call.
        /// </remarks>
        /// <exception cref="NFSLibrary.Rpc.OncRpcException">
        /// if an ONC/RPC exception occurs, like the data
        /// could not be successfully serialized.
        /// </exception>
        /// <exception cref="System.IO.IOException">
        /// if an I/O exception occurs, like transmission
        /// failures over the network, etc.
        /// </exception>
        public virtual void failOncRpcVersionMismatch()
        {
            reply(new NFSLibrary.Rpc.Server.OncRpcServerReplyMessage(callMessage, NFSLibrary.Rpc.OncRpcReplyStatus
                .ONCRPC_MSG_DENIED, NFSLibrary.Rpc.OncRpcReplyMessage.UNUSED_PARAMETER, NFSLibrary.Rpc.OncRpcRejectStatus
                .ONCRPC_RPC_MISMATCH, NFSLibrary.Rpc.OncRpcCallMessage.ONCRPC_VERSION, NFSLibrary.Rpc.OncRpcCallMessage
                .ONCRPC_VERSION, NFSLibrary.Rpc.OncRpcReplyMessage.UNUSED_PARAMETER), null);
        }

        /// <summary>
        /// Send back an ONC/RPC failure indication about a failed authentication
        /// to the caller who sent in this call.
        /// </summary>
        /// <remarks>
        /// Send back an ONC/RPC failure indication about a failed authentication
        /// to the caller who sent in this call.
        /// </remarks>
        /// <param name="authStatus">
        ///
        /// <see cref="NFSLibrary.Rpc.OncRpcAuthStatus">Reason</see>
        /// why authentication
        /// failed.
        /// </param>
        /// <exception cref="NFSLibrary.Rpc.OncRpcException">
        /// if an ONC/RPC exception occurs, like the data
        /// could not be successfully serialized.
        /// </exception>
        /// <exception cref="System.IO.IOException">
        /// if an I/O exception occurs, like transmission
        /// failures over the network, etc.
        /// </exception>
        public virtual void failAuthenticationFailed(int authStatus)
        {
            reply(new NFSLibrary.Rpc.Server.OncRpcServerReplyMessage(callMessage, NFSLibrary.Rpc.OncRpcReplyStatus
                .ONCRPC_MSG_DENIED, NFSLibrary.Rpc.OncRpcReplyMessage.UNUSED_PARAMETER, NFSLibrary.Rpc.OncRpcRejectStatus
                .ONCRPC_AUTH_ERROR, NFSLibrary.Rpc.OncRpcReplyMessage.UNUSED_PARAMETER, NFSLibrary.Rpc.OncRpcReplyMessage
                .UNUSED_PARAMETER, authStatus), null);
        }
    }
}