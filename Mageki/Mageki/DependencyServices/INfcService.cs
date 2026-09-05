using System;
using System.Collections.Generic;
using System.Text;

namespace Mageki.DependencyServices
{
    public interface INfcService
    {
        bool ReadingAvailable { get; }
        void StartReadAime(
            Action<byte[]> onFelicaScan,
            Action<byte[]> onMifareScan,
            Action onInvalidate
        );
    }
}
