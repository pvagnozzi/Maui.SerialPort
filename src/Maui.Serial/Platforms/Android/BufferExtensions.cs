using Android.Runtime;
using Java.Nio;

namespace Maui.Serial.Platforms.Android;

public static class BufferExtensions
{
    private static nint _byteBufferClassRef;

    private static nint _byteBufferGetBii;

    public static ByteBuffer GetBuffer(this ByteBuffer buffer, JavaArray<byte> dst, int dstOffset, int byteCount)
    {
        if (_byteBufferClassRef == 0)
        {
            _byteBufferClassRef = JNIEnv.FindClass("java/nio/ByteBuffer");
        }

        if (_byteBufferGetBii == 0)
        {
            _byteBufferGetBii = JNIEnv.GetMethodID(_byteBufferClassRef, "get", "([BII)Ljava/nio/ByteBuffer;");
        }

        return Java.Lang.Object.GetObject<ByteBuffer>(
            JNIEnv.CallObjectMethod(buffer.Handle, _byteBufferGetBii, new JValue(dst), new JValue(dstOffset),
                new JValue(byteCount)), JniHandleOwnership.TransferLocalRef);
    }

    public static byte[] ToByteArray(this ByteBuffer buffer)
    {
        var classHandle = JNIEnv.FindClass("java/nio/ByteBuffer");
        var methodId = JNIEnv.GetMethodID(classHandle, "array", "()[B");
        var resultHandle = JNIEnv.CallObjectMethod(buffer.Handle, methodId);
        var result = JNIEnv.GetArray<byte>(resultHandle);
        JNIEnv.DeleteLocalRef(resultHandle);
        return result;
    }
}
