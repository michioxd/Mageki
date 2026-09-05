using System;
using Mageki.DependencyServices;

namespace Mageki.WPF.DependencyServices
{
    /// <summary>
    /// WPF: NFC is not available on desktop — stub that reports unavailable.
    /// </summary>
    public class NfcService : INfcService
    {
        public bool ReadingAvailable => false;

        public void StartReadAime(
            Action<byte[]> onFelicaScan,
            Action<byte[]> onMifareScan,
            Action onInvalidate
        )
        {
            // NFC not supported on WPF desktop — no-op
        }
    }
}
