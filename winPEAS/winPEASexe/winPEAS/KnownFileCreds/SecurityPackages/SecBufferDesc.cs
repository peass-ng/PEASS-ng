using System;
using System.IO;
using System.Runtime.InteropServices;

namespace winPEAS.KnownFileCreds.SecurityPackages
{
    // SecBufferDesc structure - https://docs.microsoft.com/en-us/windows/win32/api/sspi/ns-sspi-secbufferdesc    
    [StructLayout(LayoutKind.Sequential)]
    public struct SecBufferDesc : IDisposable
    {
        public int Version;
        public int BufferCount;
        public IntPtr BuffersPtr;

        /// <summary>
        /// Initialization constructor
        /// </summary>
        /// <param name="size">Size of the buffer to allocate</param>
        internal SecBufferDesc(int size)
        {
            // Set version to SECBUFFER_VERSION
            Version = 0;

            // Set the number of buffers
            BufferCount = 1;

            // Allocate a security buffer of the requested size
            var secBuffer = new SecBuffer(size);

            // Allocate a native chunk of memory for security buffer
            BuffersPtr = Marshal.AllocHGlobal(Marshal.SizeOf(secBuffer));

            try
            {
                // Copy managed data into the native memory
                Marshal.StructureToPtr(secBuffer, BuffersPtr, false);
            }
            catch (Exception)
            {
                // Delete native memory
                Marshal.FreeHGlobal(BuffersPtr);

                // Reset native buffer pointer
                BuffersPtr = IntPtr.Zero;

                // Re-throw exception
                throw;
            }
        }

        /// <summary>
        /// Initialization constructor for byte array
        /// </summary>
        /// <param name="buffer">Data</param>
        internal SecBufferDesc(byte[] buffer)
        {
            // Set version to SECBUFFER_VERSION
            Version = 0;

            // We have only one buffer
            BufferCount = 1;

            // Allocate security buffer
            var secBuffer = new SecBuffer(buffer);

            // Allocate native memory for managed block
            BuffersPtr = Marshal.AllocHGlobal(Marshal.SizeOf(secBuffer));

            try
            {
                // Copy managed data into the native memory
                Marshal.StructureToPtr(secBuffer, BuffersPtr, false);
            }
            catch (Exception)
            {
                // Delete native memory
                Marshal.FreeHGlobal(BuffersPtr);

                // Reset native buffer pointer
                BuffersPtr = IntPtr.Zero;

                // Re-throw exception
                throw;
            }
        }

        /// <summary>
        /// Dispose security buffer descriptor
        /// </summary>
        public void Dispose()
        {
            // Check if we have a buffer
            if (BuffersPtr != IntPtr.Zero)
            {
                // Iterate through each buffer than we manage
                for (var index = 0; index < BufferCount; index++)
                {
                    // Calculate pointer to the buffer
                    var currentBufferPtr = new IntPtr(BuffersPtr.ToInt64() + (index * Marshal.SizeOf(typeof(SecBuffer))));

                    // Project the buffer into the managed world
                    var secBuffer = (SecBuffer)Marshal.PtrToStructure(currentBufferPtr, typeof(SecBuffer));

                    // Dispose it
                    secBuffer.Dispose();
                }

                // Release native memory block
                Marshal.FreeHGlobal(BuffersPtr);

                // Reset buffer pointer
                BuffersPtr = IntPtr.Zero;
            }
        }

        /// <summary>
        /// Convert to byte array
        /// </summary>
        internal byte[] ToArray()
        {
            // Check if we have a buffer
            if (BuffersPtr == IntPtr.Zero)
            {
                // We don't have a buffer
                return new byte[] { };
            }

            // Prepare a memory stream to contain all the buffers
            var outputStream = new MemoryStream();

            // Iterate through each buffer and write the data into the stream
            for (var index = 0; index < BufferCount; index++)
            {
                // Calculate pointer to the buffer
                var currentBufferPtr = new IntPtr(BuffersPtr.ToInt64() + (index * Marshal.SizeOf(typeof(SecBuffer))));

                // Project the buffer into the managed world
                var secBuffer = (SecBuffer)Marshal.PtrToStructure(currentBufferPtr, typeof(SecBuffer));

                // Get the byte buffer
                var secBufferBytes = secBuffer.ToArray();

                // Write buffer to the stream
                outputStream.Write(secBufferBytes, 0, secBufferBytes.Length);
            }

            // Convert to byte array
            return outputStream.ToArray();
        }
    }
}
