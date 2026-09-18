using System;
using System.Collections.Generic;

namespace PLAYERTWO.ARPGProject
{
    [Serializable]
    public class SalvageReceiptRecord
    {
        public string operationId;
        public string requestHash;
        public string summary;
    }

    /// <summary>Character-scoped salvage idempotency receipts.</summary>
    [Serializable]
    public class CharacterSalvageState
    {
        public List<SalvageReceiptRecord> receipts = new();

        public SalvageReceiptRecord FindReceipt(string operationId) =>
            receipts.Find(value => value.operationId == operationId);
    }
}
