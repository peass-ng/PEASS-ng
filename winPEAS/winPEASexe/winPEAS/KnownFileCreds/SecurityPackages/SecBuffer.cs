using System;
using System.Runtime.InteropServices;

namespace winPEAS.KnownFileCreds.SecurityPackages
{
    [StructLayout(LayoutKind.Sequential)]
    public struct SecBuffer : IDisposable
    {
        public int BufferSize;
        public int BufferType;
        public IntPtr BufferPtr;

        /// <summary>
        /// Initialization constructor that allocates a new security buffer
        /// </summary>
        /// <param name="bufferSize">Size of the buffer to allocate</param>
        internal SecBuffer(int bufferSize)
        {
            // Save buffer size
            BufferSize = bufferSize;

            // Set buffer type (2 = Token)
            BufferType = 2;

            // Allocate buffer
            BufferPtr = Marshal.AllocHGlobal(bufferSize);
        }

        /// <summary>
        /// Initialization constructor for existing buffer
        /// </summary>
        /// <param name="buffer">Data</param>
        internal SecBuffer(byte[] buffer)
        {
            // Save buffer size
            BufferSize = buffer.Length;

            // Set buffer type (2 = Token)
            BufferType = 2;

            // Allocate unmanaged memory for the buffer
            BufferPtr = Marshal.AllocHGlobal(BufferSize);


            try
            {
                // Copy data into the unmanaged memory
                Marshal.Copy(buffer, 0, BufferPtr, BufferSize);
            }
            catch (Exception)
            {
                // Dispose object
                Dispose();

                // Re-throw exception
                throw;
            }
        }

        /// <summary>
        /// Extract raw byte data from the security buffer
        /// </summary>
        internal byte[] ToArray()
        {
            // Check if we have a security buffer
            if (BufferPtr == IntPtr.Zero)
            {
                return new byte[] { };
            }

            // Allocate byte buffer
            var buffer = new byte[BufferSize];

            // Copy data from the native space
            Marshal.Copy(BufferPtr, buffer, 0, BufferSize);

            return buffer;
        }

        /// <summary>
        /// Dispose security buffer
        /// </summary>
        public void Dispose()
        {
            // Check buffer pointer validity
            if (BufferPtr != IntPtr.Zero)
            {
                // Release memory associated with it
                Marshal.FreeHGlobal(BufferPtr);

                // Reset buffer pointer
                BufferPtr = IntPtr.Zero;
            }
        }
    }
}
