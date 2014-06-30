#if DEBUG

using System;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;

namespace Ship_Game
{

    /// <summary>
    /// reflector easinator
    /// </summary>
    public static class MethodUtil
    {
        /// <summary>
        /// Replaces the method.
        /// </summary>
        /// <param name="source">The source.</param>
        /// <param name="dest">The dest.</param>
        public static void ReplaceMethod(MethodBase source, MethodBase dest)
        {
            if (!MethodSignaturesEqual(source, dest))
            {
                throw new ArgumentException("The method signatures are not the same.", "source");
            }
            ReplaceMethod(GetMethodAddress(source), dest);
        
        }
        
        /// <summary>
        /// Replaces the method.
        /// </summary>
        /// <param name="srcAdr">The SRC adr.</param>
        /// <param name="dest">The dest.</param>
        public static void ReplaceMethod(IntPtr srcAdr, MethodBase dest)
        {
            var destAdr = GetMethodAddress(dest);
            unsafe
            {
                if (IntPtr.Size == 8)
                {
                    var d = (ulong*)destAdr.ToPointer();
                    *d = *((ulong*)srcAdr.ToPointer());
                }
                else
                {
                    var d = (uint*)destAdr.ToPointer();
                    *d = *((uint*)srcAdr.ToPointer());
                }
            }
        }

        /// <summary>
        /// Gets the address of the method stub
        /// </summary>
        /// <returns></returns>
        public static IntPtr GetMethodAddress(MethodBase method)
        {
            if ((method is DynamicMethod))
            {
                return GetDynamicMethodAddress(method);
            }

            // Prepare the method so it gets jited
            RuntimeHelpers.PrepareMethod(method.MethodHandle);

            unsafe
            {
                return new IntPtr(((int*)method.MethodHandle.Value.ToPointer() + 2));
            }
        }

        private static IntPtr GetDynamicMethodAddress(MethodBase method)
        {
            unsafe
            {
                var handle = GetDynamicMethodRuntimeHandle(method);
                var ptr = (byte*)handle.Value.ToPointer();
              
                RuntimeHelpers.PrepareMethod(handle);
                if (IntPtr.Size == 8)
                {
                    var address = (ulong*)ptr;
                    address = (ulong*)*(address + 5);
                    return new IntPtr(address + 12);
                }
                else
                {
                    var address = (uint*)ptr;
                    address = (uint*)*(address + 5);
                    return new IntPtr(address + 12);
                }
            }
        }
        private static RuntimeMethodHandle GetDynamicMethodRuntimeHandle(MethodBase method)
        {
            if (!(method is DynamicMethod)) return method.MethodHandle;
            var fieldInfo = typeof(DynamicMethod).GetField("m_method", BindingFlags.NonPublic | BindingFlags.Instance);
            if (fieldInfo == null) throw new Exception("GetDynamicRuntimeHandle failed!");
            var handle = ((RuntimeMethodHandle)fieldInfo.GetValue(method));
            return handle;
        }

        private static bool MethodSignaturesEqual(MethodBase x, MethodBase y)
        {
            if (x.CallingConvention != y.CallingConvention)
            {
                return false;
            }
            Type returnX = GetMethodReturnType(x), returnY = GetMethodReturnType(y);
            if (returnX != returnY)
            {
                return false;
            }
            ParameterInfo[] xParams = x.GetParameters(), yParams = y.GetParameters();
            if (xParams.Length != yParams.Length)
            {
                return false;
            }
            return !xParams.Where((t, i) => t.ParameterType != yParams[i].ParameterType).Any();
        }

        private static Type GetMethodReturnType(MethodBase method)
        {
            var methodInfo = method as MethodInfo;
            if (methodInfo == null)
            {
                // Constructor info.
                throw new ArgumentException("Unsupported MethodBase : " + method.GetType().Name, "method");
            }
            return methodInfo.ReturnType;
        }
    }
}
#endif