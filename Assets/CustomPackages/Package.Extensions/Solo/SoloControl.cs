using System;
using CustomPackages.Package.Extensions.Solo.Services;

namespace CustomPackages.Package.Extensions.Solo
{
    public struct SoloControl : IDisposable
    {
        public SoloControl(SoloThread mSoloThread)
        {
            m_soloThread = mSoloThread;
            m_soloThread.SetBusy();
        }

        private SoloThread m_soloThread;

        public void Dispose()
        {
            m_soloThread.SetPending();
        }
    }
}