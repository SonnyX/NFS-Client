namespace NFSLibrary.Protocols.V4.RPC.Stubs
{
    /// <summary>
    /// Provides stub methods for creating NFSv4.1 SEQUENCE operation requests.
    /// The SEQUENCE operation provides session semantics, exactly-once execution guarantees,
    /// and request ordering in NFSv4.1. It must be the first operation in every compound
    /// request when using sessions. It manages sequence IDs, slot IDs, and caching to prevent
    /// duplicate request execution.
    /// </summary>
    internal class SequenceStub
    {
        /// <summary>
        /// Generates a SEQUENCE operation request for session management.
        /// Note: This method increments the SeqId parameter, so the caller should pass
        /// the current sequence ID, not the next one.
        /// </summary>
        /// <param name="CacheThis">If true, server should cache this request for replay detection.</param>
        /// <param name="SessId">The session ID obtained from CREATE_SESSION.</param>
        /// <param name="SeqId">The current sequence ID (will be incremented by this method).</param>
        /// <param name="HighestSlot">The highest slot ID in use by the client.</param>
        /// <param name="SlotId">The slot ID for this request (usually 0 for single-threaded clients).</param>
        /// <returns>An NfsArgop4 structure containing the SEQUENCE operation request.</returns>
        public static NfsArgop4 GenerateRequest(bool CacheThis, byte[] SessId,
        int SeqId, int HighestSlot, int SlotId)
        {
            NfsArgop4 op = new NfsArgop4();
            op.Argop = NfsOpnum4.OP_SEQUENCE;
            op.Opsequence = new Sequence4Args();
            op.Opsequence.SaCachethis = CacheThis;

            Slotid4 sId = new Slotid4();
            sId.Value = new Uint32T(SlotId);
            op.Opsequence.SaSlotid = sId;

            Slotid4 HsId = new Slotid4();
            HsId.Value = new Uint32T(HighestSlot);
            op.Opsequence.SaHighestSlotid = HsId;

            Sequenceid4 seq = new Sequenceid4();
            seq.Value = new Uint32T(++SeqId);
            op.Opsequence.SaSequenceid = seq;

            Sessionid4 sess = new Sessionid4();
            sess.Value = SessId;
            op.Opsequence.SaSessionid = sess;

            return op;
        }
    }
}
