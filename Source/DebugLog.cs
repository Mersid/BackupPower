// DebugLog.cs
// Copyright Karel Kroeze, 2020-2020

using System.Diagnostics;
using Verse;

namespace BackupPower;

internal static class DebugLog
{
    [Conditional("DEBUG")]
    public static void Debug(string msg)
    {
        Message(msg);
    }

    public static void Message(string msg)
    {
        Log.Message($"BackupPower :: {msg}");
    }
}