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

namespace NFSLibrary.Rpc
{
    /// <summary>
    /// The <code>OncRpcReplyMessage</code> class represents an ONC/RPC reply
    /// message as defined by ONC/RPC in RFC 1831.
    /// </summary>
    /// <remarks>
    /// The <code>OncRpcReplyMessage</code> class represents an ONC/RPC reply
    /// message as defined by ONC/RPC in RFC 1831. Such messages are sent back by
    /// ONC/RPC to servers to clients and contain (in case of real success) the
    /// result of a remote procedure call.
    /// <para>The decision to define only one single class for the accepted and
    /// rejected replies was driven by the motivation not to use polymorphism
    /// and thus have to upcast and downcast references all the time.</para>
    /// <para>The derived classes are only provided for convinience on the server
    /// side.</para>
    /// <para>Converted to C# using the db4o Sharpen tool.</para>
    /// </remarks>
    /// <version>$Revision: 1.1.1.1 $ $Date: 2003/08/13 12:03:40 $ $State: Exp $ $Locker:  $
    /// 	</version>
    /// <author>Harald Albrecht</author>
    /// <author>Jay Walters</author>
    public class OncRpcClientReplyMessage : NFSLibrary.Rpc.OncRpcReplyMessage
    {
        /// <summary>
        /// Initializes a new <code>OncRpcReplyMessage</code> object to represent
        /// an invalid state.
        /// </summary>
        /// <remarks>
        /// Initializes a new <code>OncRpcReplyMessage</code> object to represent
        /// an invalid state. This default constructor should only be used if in the
        /// next step the real state of the reply message is immediately decoded
        /// from a XDR stream.
        /// </remarks>
        /// <param name="auth">
        /// Client-side authentication protocol handling object which
        /// is to be used when decoding the verifier data contained in the reply.
        /// </param>
        public OncRpcClientReplyMessage(NFSLibrary.Rpc.OncRpcClientAuth auth) : base()
        {
            this.auth = auth;
        }

        /// <summary>
        /// Check whether this <code>OncRpcReplyMessage</code> represents an
        /// accepted and successfully executed remote procedure call.
        /// </summary>
        /// <remarks>
        /// Check whether this <code>OncRpcReplyMessage</code> represents an
        /// accepted and successfully executed remote procedure call.
        /// </remarks>
        /// <returns>
        /// <code>true</code> if remote procedure call was accepted and
        /// successfully executed.
        /// </returns>
        public virtual bool successfullyAccepted()
        {
            return (replyStatus == NFSLibrary.Rpc.OncRpcReplyStatus.ONCRPC_MSG_ACCEPTED) &&
                 (acceptStatus == NFSLibrary.Rpc.OncRpcAcceptStatus.ONCRPC_SUCCESS);
        }

        /// <summary>
        /// Return an appropriate exception object according to the state this
        /// reply message header object is in.
        /// </summary>
        /// <remarks>
        /// Return an appropriate exception object according to the state this
        /// reply message header object is in. The exception object then can be
        /// thrown.
        /// </remarks>
        /// <returns>
        /// Exception object of class
        /// <see cref="OncRpcException">OncRpcException</see>
        /// or a subclass
        /// thereof.
        /// </returns>
        public virtual NFSLibrary.Rpc.OncRpcException newException()
        {
            switch (replyStatus)
            {
                case NFSLibrary.Rpc.OncRpcReplyStatus.ONCRPC_MSG_ACCEPTED:
                    {
                        switch (acceptStatus)
                        {
                            case NFSLibrary.Rpc.OncRpcAcceptStatus.ONCRPC_SUCCESS:
                                {
                                    return new NFSLibrary.Rpc.OncRpcException(NFSLibrary.Rpc.OncRpcException.RPC_SUCCESS
                                        );
                                }

                            case NFSLibrary.Rpc.OncRpcAcceptStatus.ONCRPC_PROC_UNAVAIL:
                                {
                                    return new NFSLibrary.Rpc.OncRpcException(NFSLibrary.Rpc.OncRpcException.RPC_PROCUNAVAIL
                                        );
                                }

                            case NFSLibrary.Rpc.OncRpcAcceptStatus.ONCRPC_PROG_MISMATCH:
                                {
                                    return new NFSLibrary.Rpc.OncRpcException(NFSLibrary.Rpc.OncRpcException.RPC_PROGVERSMISMATCH
                                        );
                                }

                            case NFSLibrary.Rpc.OncRpcAcceptStatus.ONCRPC_PROG_UNAVAIL:
                                {
                                    return new NFSLibrary.Rpc.OncRpcException(NFSLibrary.Rpc.OncRpcException.RPC_PROGUNAVAIL
                                        );
                                }

                            case NFSLibrary.Rpc.OncRpcAcceptStatus.ONCRPC_GARBAGE_ARGS:
                                {
                                    return new NFSLibrary.Rpc.OncRpcException(NFSLibrary.Rpc.OncRpcException.RPC_CANTDECODEARGS
                                        );
                                }

                            case NFSLibrary.Rpc.OncRpcAcceptStatus.ONCRPC_SYSTEM_ERR:
                                {
                                    return new NFSLibrary.Rpc.OncRpcException(NFSLibrary.Rpc.OncRpcException.RPC_SYSTEMERROR
                                        );
                                }
                        }
                        break;
                    }

                case NFSLibrary.Rpc.OncRpcReplyStatus.ONCRPC_MSG_DENIED:
                    {
                        switch (rejectStatus)
                        {
                            case NFSLibrary.Rpc.OncRpcRejectStatus.ONCRPC_AUTH_ERROR:
                                {
                                    return new NFSLibrary.Rpc.OncRpcAuthenticationException(authStatus);
                                }

                            case NFSLibrary.Rpc.OncRpcRejectStatus.ONCRPC_RPC_MISMATCH:
                                {
                                    return new NFSLibrary.Rpc.OncRpcException(NFSLibrary.Rpc.OncRpcException.RPC_FAILED
                                        );
                                }
                        }
                        break;
                    }
            }
            return new NFSLibrary.Rpc.OncRpcException();
        }

        /// <summary>
        /// Decodes -- that is: deserializes -- a ONC/RPC message header object
        /// from a XDR stream.
        /// </summary>
        /// <remarks>
        /// Decodes -- that is: deserializes -- a ONC/RPC message header object
        /// from a XDR stream.
        /// </remarks>
        /// <exception cref="OncRpcException">if an ONC/RPC error occurs.</exception>
        /// <exception cref="System.IO.IOException">if an I/O error occurs.</exception>
        /// <exception cref="NFSLibrary.Rpc.OncRpcException"></exception>
        public virtual void XdrDecode(NFSLibrary.Rpc.XdrDecodingStream xdr)
        {
            messageId = xdr.XdrDecodeInt();
            //
            // Make sure that we are really decoding an ONC/RPC message call
            // header. Otherwise, throw the appropriate OncRpcException exception.
            //
            messageType = xdr.XdrDecodeInt();
            if (messageType != NFSLibrary.Rpc.OncRpcMessageType.ONCRPC_REPLY)
            {
                throw (new NFSLibrary.Rpc.OncRpcException(NFSLibrary.Rpc.OncRpcException.RPC_WRONGMESSAGE
                    ));
            }
            replyStatus = xdr.XdrDecodeInt();
            switch (replyStatus)
            {
                case NFSLibrary.Rpc.OncRpcReplyStatus.ONCRPC_MSG_ACCEPTED:
                    {
                        //
                        // Decode the information returned for accepted message calls.
                        // If we have an associated client-side authentication protocol
                        // object, we use that. Otherwise we fall back to the default
                        // handling of only the AUTH_NONE authentication.
                        //
                        if (auth != null)
                        {
                            auth.XdrDecodeVerf(xdr);
                        }
                        else
                        {
                            //
                            // If we don't have a protocol handler and the server sent its
                            // reply using another authentication scheme than AUTH_NONE, we
                            // will throw an exception. Also we check that no-one is
                            // actually sending opaque information within AUTH_NONE.
                            //
                            if (xdr.XdrDecodeInt() != NFSLibrary.Rpc.OncRpcAuthType.ONCRPC_AUTH_NONE)
                            {
                                throw (new NFSLibrary.Rpc.OncRpcAuthenticationException(NFSLibrary.Rpc.OncRpcAuthStatus
                                    .ONCRPC_AUTH_FAILED));
                            }
                            if (xdr.XdrDecodeInt() != 0)
                            {
                                throw (new NFSLibrary.Rpc.OncRpcAuthenticationException(NFSLibrary.Rpc.OncRpcAuthStatus
                                    .ONCRPC_AUTH_FAILED));
                            }
                        }
                        //
                        // Even if the call was accepted by the server, it can still
                        // indicate an error. Depending on the status of the accepted
                        // call we will receive an indication about the range of
                        // versions a particular program (server) supports.
                        //
                        acceptStatus = xdr.XdrDecodeInt();
                        switch (acceptStatus)
                        {
                            case NFSLibrary.Rpc.OncRpcAcceptStatus.ONCRPC_PROG_MISMATCH:
                                {
                                    lowVersion = xdr.XdrDecodeInt();
                                    highVersion = xdr.XdrDecodeInt();
                                    break;
                                }

                            default:
                                {
                                    //
                                    // Otherwise "open ended set of problem", like the author
                                    // of Sun's ONC/RPC source once wrote...
                                    //
                                    break;
                                }
                        }
                        break;
                    }

                case NFSLibrary.Rpc.OncRpcReplyStatus.ONCRPC_MSG_DENIED:
                    {
                        //
                        // Encode the information returned for denied message calls.
                        //
                        rejectStatus = xdr.XdrDecodeInt();
                        switch (rejectStatus)
                        {
                            case NFSLibrary.Rpc.OncRpcRejectStatus.ONCRPC_RPC_MISMATCH:
                                {
                                    lowVersion = xdr.XdrDecodeInt();
                                    highVersion = xdr.XdrDecodeInt();
                                    break;
                                }

                            case NFSLibrary.Rpc.OncRpcRejectStatus.ONCRPC_AUTH_ERROR:
                                {
                                    authStatus = xdr.XdrDecodeInt();
                                    break;
                                }

                            default:
                                {
                                    break;
                                }
                        }
                        break;
                    }
            }
        }

        /// <summary>
        /// Client-side authentication protocol handling object to use when
        /// decoding the reply message.
        /// </summary>
        /// <remarks>
        /// Client-side authentication protocol handling object to use when
        /// decoding the reply message.
        /// </remarks>
        internal NFSLibrary.Rpc.OncRpcClientAuth auth;
    }
}