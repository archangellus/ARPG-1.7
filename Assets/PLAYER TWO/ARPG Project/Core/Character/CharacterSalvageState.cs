using System;
using System.Collections.Generic;

namespace PLAYERTWO.ARPGProject
{
    [Serializable]
    public class SalvageMaterialBalance
    {
        public string materialId;
        public long quantity;
    }

    [Serializable]
    public class SalvageReceiptRecord
    {
        public string operationId;
        public string requestHash;
        public string summary;
    }

    /// <summary>Character-scoped salvage wallet and idempotency receipts.</summary>
    [Serializable]
    public class CharacterSalvageState
    {
        public List<SalvageMaterialBalance> materials = new();
        public List<SalvageReceiptRecord> receipts = new();
        public int revision;

        public long Get(string materialId)
        {
            var entry = materials.Find(value => value.materialId == materialId);
            return entry != null ? entry.quantity : 0;
        }

        public bool TryCredit(string materialId, long amount, long cap)
        {
            if (string.IsNullOrEmpty(materialId) || amount <= 0) return false;
            var current = Get(materialId);
            if (current > cap - amount) return false;
            var entry = materials.Find(value => value.materialId == materialId);
            if (entry == null)
            {
                entry = new SalvageMaterialBalance { materialId = materialId };
                materials.Add(entry);
            }
            entry.quantity = checked(current + amount);
            revision++;
            return true;
        }

        public SalvageReceiptRecord FindReceipt(string operationId) =>
            receipts.Find(value => value.operationId == operationId);
    }
}
